using System;
using System.Collections.Generic;
using CameraUnlock.Core.Input;
using CameraUnlock.Core.Tracking;
using CameraUnlock.Core.Unity.Extensions;
using CameraUnlock.Core.Unity.Il2Cpp;
using SonsOfTheForestHeadTracking.Configuration;
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
    private SplitInjectionCameraTracker? _tracker;
    private Action<Action<SonsOfTheForestConfig>>? _saveConfig;

    private KeyBinding[] _toggleKeys = new KeyBinding[0];
    private KeyBinding[] _cycleTrackingModeKeys = new KeyBinding[0];
    private KeyBinding[] _yawModeKeys = new KeyBinding[0];

    private bool _trackingEnabled = true;
    private bool _worldSpaceYaw = true;
    private bool _initialized;
    private bool _hotkeysAvailable = true;

    private bool _wasTracking;
    private int _diagnosticLogsRemaining = DiagnosticLogBudget;
    private string? _lastGateReason;

    public HeadTrackingBehaviour(IntPtr ptr) : base(ptr) { }

    internal void Initialize(HeadTrackingSession session, SonsOfTheForestConfig config,
        Action<Action<SonsOfTheForestConfig>> saveConfig)
    {
        _session = session;
        _saveConfig = saveConfig;
        _tracker = new SplitInjectionCameraTracker { Log = msg => Plugin.Logger.LogInfo(msg) };
        _trackingEnabled = config.EnableOnStartup;
        _worldSpaceYaw = config.WorldSpaceYaw;
        _toggleKeys = ParseKeys("ToggleKey", config.ToggleKeyName);
        _cycleTrackingModeKeys = ParseKeys("CycleTrackingModeKey", config.CycleTrackingModeKeyName);
        _yawModeKeys = ParseKeys("YawModeKey", config.YawModeKeyName);
        _initialized = true;

        Plugin.Logger.LogInfo($"Hotkeys: [{config.ToggleKeyName}] toggle, [{config.CycleTrackingModeKeyName}] cycle tracking mode, " +
                              $"[{config.YawModeKeyName}] yaw mode");

        Plugin.Logger.LogInfo("HeadTrackingBehaviour initialized (split matrix/transform injection in LateUpdate).");
    }

    private void Update()
    {
        if (!_initialized || _session == null || _tracker == null || !_hotkeysAvailable) return;

        try
        {
            // The on/off toggle is never saved: the next start follows EnableOnStartup.
            if (KeyBindingInput.IsTriggered(_toggleKeys))
            {
                _trackingEnabled = !_trackingEnabled;
                Plugin.Logger.LogInfo($"Head tracking {(_trackingEnabled ? "ENABLED" : "DISABLED")}");
                if (!_trackingEnabled) _tracker.ResetAll();
                else _session.Reset();
            }

            if (KeyBindingInput.IsTriggered(_cycleTrackingModeKeys))
            {
                TrackingMode mode = _session.CycleMode();
                if (!_session.RotationActive) _tracker.ResetMatrices();
                Plugin.Logger.LogInfo($"Tracking mode: {mode.Description()}");
                TrackingModeChannels.Encode(mode, out bool rotation, out bool position);
                _saveConfig!(c =>
                {
                    c.RotationEnabled = rotation;
                    c.PositionEnabled = position;
                });
            }

            if (KeyBindingInput.IsTriggered(_yawModeKeys))
            {
                bool worldSpaceYaw = !_worldSpaceYaw;
                _worldSpaceYaw = worldSpaceYaw;
                Plugin.Logger.LogInfo($"Yaw mode: {(_worldSpaceYaw ? "world-space (horizon-locked)" : "camera-local")}");
                _saveConfig!(c => c.WorldSpaceYaw = worldSpaceYaw);
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
            -_session.PositionOffset.X, _session.PositionOffset.Y, _session.PositionOffset.Z);

        // The axis conversion into SotF's view space, applied once here at the boundary: pitch
        // and lateral position are negated, which the published builds did through their shipped
        // InvertPitch=true and InvertPositionX=true (the lateral limit is symmetric, so negating
        // after the clamp gives the same offset). Roll is passed through un-negated: SotF's
        // view-space convention is opposite to OpenTrack's (verified in-game - negated roll tilts
        // the wrong way).
        _tracker.Apply(rotation.Yaw, -rotation.Pitch, rotation.Roll, positionOffset,
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

    // The table's hotkey codec has read every list the file holds, so a list that does not parse
    // reaches here only from a legacy import the owner deferred: a .cfg key code Unity names no key
    // for, which the import writes as the number. The items that parse, the chord among them, are
    // bound and the rest are named in the log.
    private static KeyBinding[] ParseKeys(string key, string text)
    {
        if (KeyBindings.TryParse(text, out KeyBinding[] bindings, out _)) return bindings;

        var kept = new List<KeyBinding>();
        foreach (string item in text.Split(','))
        {
            if (KeyBindings.TryParse(item, out bindings, out string? error)) kept.AddRange(bindings);
            else Plugin.Logger.LogWarning($"[Hotkeys] {key}: {error}, so it is not bound this session");
        }
        return kept.ToArray();
    }

    private void OnDestroy()
    {
        _tracker?.Clear();
    }
}
