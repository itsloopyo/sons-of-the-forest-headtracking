using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Tracking;
using CameraUnlock.Core.Unity.Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using SonsOfTheForestHeadTracking.Configuration;
using SonsOfTheForestHeadTracking.Legacy;
using UnityEngine;

namespace SonsOfTheForestHeadTracking;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BasePlugin
{
    public const string PluginGuid = "com.cameraunlock.sonsoftheforest.headtracking";
    public const string PluginName = "Sons of the Forest Head Tracking";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource Logger = null!;

    private static GameObject? _hostObject;
    private static HeadTrackingBehaviour? _host;

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo($"Loading {PluginName} v{PluginVersion}...");

        var config = LoadConfig();

        var receiver = new OpenTrackReceiver();
        receiver.Log = msg => Logger.LogInfo(msg);
        receiver.Start(OpenTrackReceiver.DefaultPort);
        Logger.LogInfo($"Listening for tracker data on UDP port {OpenTrackReceiver.DefaultPort}.");

        var processor = new TrackingProcessor
        {
            // Selected per connection by HeadTrackingSession, which re-reads locality
            // from the receiver every Update: loopback senders get LocalSmoothing,
            // remote network devices get RemoteSmoothing.
            LocalSmoothing = config.LocalSmoothing,
            RemoteSmoothing = config.RemoteSmoothing,
            Sensitivity = new SensitivitySettings(
                config.YawSensitivity,
                config.PitchSensitivity,
                config.RollSensitivity,
                invertYaw: config.InvertYaw,
                invertPitch: config.InvertPitch,
                invertRoll: config.InvertRoll
            ),
            Deadzone = DeadzoneSettings.None
        };

        var positionProcessor = new PositionProcessor
        {
            Settings = PositionSettings.Symmetric(
                config.PositionSensitivityX,
                config.PositionSensitivityY,
                config.PositionSensitivityZ,
                config.PositionLimitX,
                config.PositionLimitY,
                config.PositionLimitZ,
                config.PositionLimitZBack,
                localSmoothing: config.LocalSmoothing,
                remoteSmoothing: config.RemoteSmoothing,
                invertX: config.InvertPositionX,
                invertY: config.InvertPositionY,
                invertZ: config.InvertTrackerZ)
        };

        var session = new HeadTrackingSession(receiver, processor, positionProcessor)
        {
            Mode = config.PositionEnabled ? TrackingMode.RotationAndPosition : TrackingMode.RotationOnly,
            Log = msg => Logger.LogInfo(msg)
        };

        ClassInjector.RegisterTypeInIl2Cpp<HeadTrackingBehaviour>();
        ClassInjector.RegisterTypeInIl2Cpp<FastBootBehaviour>();

        _hostObject = new GameObject("SotF.HeadTrackingHost");
        _hostObject.hideFlags = HideFlags.DontSave;
        Object.DontDestroyOnLoad(_hostObject);

        _host = _hostObject.AddComponent<HeadTrackingBehaviour>();
        _host.Initialize(session, config);

        if (config.DebugFastBoot)
        {
            FastBootBehaviour.Log = msg => Logger.LogInfo(msg);
            _hostObject.AddComponent<FastBootBehaviour>();
            Logger.LogInfo("DebugFastBoot enabled - splash/intro VideoPlayers will be killed on each scene load.");
        }

        Logger.LogInfo($"{PluginName} loaded. Press End to toggle tracking.");
    }

    /// <summary>
    /// Reads the plugin's .cfg through the frozen reader, then saves it once, the write every
    /// published build's Bind calls made at each start, so the file on disk stays what it was.
    /// </summary>
    private ModConfig LoadConfig()
    {
        LegacyConfig legacy = LegacyConfigReader.Read(Config, out _);
        Config.Save();
        return new ModConfig
        {
            EnabledOnStartup = legacy.EnabledOnStartup,
            WorldSpaceYaw = legacy.WorldSpaceYaw,
            YawSensitivity = legacy.YawSensitivity,
            PitchSensitivity = legacy.PitchSensitivity,
            RollSensitivity = legacy.RollSensitivity,
            LocalSmoothing = legacy.LocalSmoothing,
            RemoteSmoothing = legacy.RemoteSmoothing,
            InvertYaw = legacy.InvertYaw,
            InvertPitch = legacy.InvertPitch,
            InvertRoll = legacy.InvertRoll,
            PositionEnabled = legacy.PositionEnabled,
            PositionSensitivityX = legacy.PositionSensitivityX,
            PositionSensitivityY = legacy.PositionSensitivityY,
            PositionSensitivityZ = legacy.PositionSensitivityZ,
            PositionLimitX = legacy.PositionLimitX,
            PositionLimitY = legacy.PositionLimitY,
            PositionLimitZ = legacy.PositionLimitZ,
            PositionLimitZBack = legacy.PositionLimitZBack,
            InvertPositionX = legacy.InvertPositionX,
            InvertPositionY = legacy.InvertPositionY,
            InvertTrackerZ = legacy.InvertTrackerZ,
            ToggleKey = legacy.ToggleKey,
            YawModeKey = legacy.YawModeKey,
            PositionToggleKey = legacy.PositionToggleKey,
            DebugFastBoot = legacy.DebugFastBoot,
        };
    }
}
