using System;
using CameraUnlock.Core.Tracking;
using CameraUnlock.Core.Unity.Extensions;
using CameraUnlock.Core.Unity.Il2Cpp;
using TheForest.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SonsOfTheForestHeadTracking;

/// <summary>
/// Drives head tracking for Sons of the Forest (Unity 2022.2 IL2CPP + HDRP).
///
/// The tracking pipeline (receiver -> interpolation -> processing,
/// tracking-loss hold, mode cycling) lives in CameraUnlock.Core's
/// <see cref="HeadTrackingSession"/>; the camera injection (multi-camera split
/// matrix/transform writes) lives in <see cref="SplitInjectionCameraTracker"/>.
/// This behaviour owns only the SotF-specific parts: gameplay gating, hotkeys,
/// and diagnostics.
///
/// Decoupling: game aim/raycasts read LocalPlayer look state and the (game-owned)
/// camera rotation; only the rendered image gets the head pose.
/// </summary>
public class HeadTrackingBehaviour : MonoBehaviour
{
    private const int DiagnosticLogInterval = 120;
    // The pose dump answers "is tracker data flowing and does it look sane". Once
    // answered it repeats forever, so it is capped rather than left periodic.
    private const int DiagnosticLogBudget = 10;

    private HeadTrackingSession? _session;
    private PluginConfig? _config;
    private SplitInjectionCameraTracker? _tracker;

    private bool _trackingEnabled = true;
    private bool _worldSpaceYaw = true;
    private bool _initialized;
    private bool _hotkeysAvailable = true;

    private bool _wasTracking;
    private int _diagnosticLogsRemaining = DiagnosticLogBudget;
    private string? _lastGateReason;

    public HeadTrackingBehaviour(IntPtr ptr) : base(ptr) { }

    public void Initialize(HeadTrackingSession session, PluginConfig config)
    {
        _session = session;
        _config = config;
        _tracker = new SplitInjectionCameraTracker { Log = msg => Plugin.Logger.LogInfo(msg) };
        _trackingEnabled = config.EnabledOnStartup.Value;
        _worldSpaceYaw = config.WorldSpaceYaw.Value;
        _initialized = true;

        Plugin.Logger.LogInfo("HeadTrackingBehaviour initialized (split matrix/transform injection in LateUpdate).");
    }

    private void Update()
    {
        if (!_initialized || _config == null || _session == null || _tracker == null || !_hotkeysAvailable) return;

        try
        {
            if (ChordHotkeys.IsActionPressed(_config.ToggleKey.Value, ChordHotkeys.ToggleLetter))
            {
                _trackingEnabled = !_trackingEnabled;
                Plugin.Logger.LogInfo($"Head tracking {(_trackingEnabled ? "ENABLED" : "DISABLED")}");
                if (!_trackingEnabled) _tracker.ResetAll();
                else _session.Reset();
            }

            if (ChordHotkeys.IsActionPressed(_config.PositionToggleKey.Value, ChordHotkeys.PositionLetter))
            {
                TrackingMode mode = _session.CycleMode();
                if (!_session.RotationActive) _tracker.ResetMatrices();
                Plugin.Logger.LogInfo($"Tracking mode: {mode.Description()}");
            }

            if (ChordHotkeys.IsActionPressed(_config.YawModeKey.Value, ChordHotkeys.FourthToggleLetter))
            {
                _worldSpaceYaw = !_worldSpaceYaw;
                Plugin.Logger.LogInfo($"Yaw mode: {(_worldSpaceYaw ? "world-space (horizon-locked)" : "camera-local")}");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Legacy Input manager disabled in this game build - hotkeys unavailable.
            // Boundary with the game's input configuration; tracking itself is unaffected.
            _hotkeysAvailable = false;
            Plugin.Logger.LogWarning($"Hotkeys disabled - legacy Input unavailable: {ex.Message}");
        }
    }

    private void LateUpdate()
    {
        if (!_initialized || _session == null || _tracker == null) return;

        // Undo last frame's position offsets FIRST so the compose below starts from the
        // game's clean camera state and gating leaves the cameras untouched.
        _tracker.RestorePositions();

        // Hard gates (disabled / menu / paused): tracking fully off, view returns to the game's own.
        if (!_trackingEnabled || !IsGameplay())
        {
            if (_wasTracking)
            {
                _tracker.ResetMatrices();
                _wasTracking = false;
            }
            return;
        }

        _tracker.RefreshTargetsIfDue();
        if (_tracker.TargetCount == 0) return;

        // Session runs the whole pipeline: interpolation, processing,
        // and tracking-loss hold. False only when no tracker data has ever arrived.
        if (!_session.Update(Time.deltaTime))
        {
            LogGate("no tracker data yet (is OpenTrack sending to UDP 4242?)");
            return;
        }

        var rotation = _session.Rotation;
        Vector3 positionOffset = new Vector3(
            _session.PositionOffset.X, _session.PositionOffset.Y, _session.PositionOffset.Z);

        // Roll is passed through un-negated: SotF's view-space convention is opposite to
        // OpenTrack's (verified in-game - negated roll tilts the wrong way).
        _tracker.Apply(rotation.Yaw, rotation.Pitch, rotation.Roll, positionOffset,
            _session.RotationActive, _session.PositionActive, _worldSpaceYaw);

        if (!_wasTracking)
        {
            _wasTracking = true;
            _lastGateReason = null;
            Plugin.Logger.LogInfo($"Tracking ACTIVE on {_tracker.TargetCount} camera(s) (scene '{SceneManager.GetActiveScene().name}')");
        }

        if (_diagnosticLogsRemaining > 0 && Time.frameCount % DiagnosticLogInterval == 0)
        {
            _diagnosticLogsRemaining--;
            Plugin.Logger.LogInfo($"HT rot: Y={rotation.Yaw:F1} P={rotation.Pitch:F1} R={rotation.Roll:F1} " +
                $"pos=({positionOffset.x:F3},{positionOffset.y:F3},{positionOffset.z:F3}) " +
                $"mode={_session.Mode.Description()}{(_session.IsHolding ? " [holding]" : "")} ({_tracker.TargetCount} cams)");
        }
    }

    /// <summary>
    /// In-world (the game's own gameplay flag) and not paused.
    /// </summary>
    private bool IsGameplay()
    {
        bool inWorld = LocalPlayer.IsInWorld;
        if (!inWorld)
        {
            LogGate("LocalPlayer.IsInWorld=false (menu/loading)");
            return false;
        }

        if (Time.timeScale <= 0f)
        {
            LogGate("game paused (timeScale=0)");
            return false;
        }

        return true;
    }

    // Logged on transitions only. Gates hold for as long as the player sits in a menu,
    // so a periodic line here is an unbounded write for one unchanging fact.
    private void LogGate(string reason)
    {
        if (reason == _lastGateReason) return;
        _lastGateReason = reason;
        Plugin.Logger.LogInfo($"Tracking gated: {reason} [scene='{SceneManager.GetActiveScene().name}']");
    }

    private void OnDestroy()
    {
        _tracker?.Clear();
    }
}
