using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Tracking;
using CameraUnlock.Core.Unity.Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace SonsOfTheForestHeadTracking;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BasePlugin
{
    public const string PluginGuid = "com.cameraunlock.sonsoftheforest.headtracking";
    public const string PluginName = "Sons of the Forest Head Tracking";
    public const string PluginVersion = "0.0.0";

    internal static ManualLogSource Logger = null!;

    private static GameObject? _hostObject;
    private static HeadTrackingBehaviour? _host;

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo($"Loading {PluginName} v{PluginVersion}...");

        var config = new PluginConfig(Config);

        var receiver = new OpenTrackReceiver();
        receiver.Log = msg => Logger.LogInfo(msg);
        receiver.Start(OpenTrackReceiver.DefaultPort);

        var processor = new TrackingProcessor
        {
            SmoothingFactor = config.Smoothing.Value,
            Sensitivity = new SensitivitySettings(
                config.YawSensitivity.Value,
                config.PitchSensitivity.Value,
                config.RollSensitivity.Value,
                invertYaw: config.InvertYaw.Value,
                invertPitch: config.InvertPitch.Value,
                invertRoll: config.InvertRoll.Value
            ),
            Deadzone = DeadzoneSettings.None
        };

        var positionProcessor = new PositionProcessor
        {
            Settings = new PositionSettings(
                config.PositionSensitivityX.Value,
                config.PositionSensitivityY.Value,
                config.PositionSensitivityZ.Value,
                config.PositionLimitX.Value,
                config.PositionLimitY.Value,
                config.PositionLimitZ.Value,
                config.PositionLimitZBack.Value,
                config.PositionSmoothing.Value,
                invertX: config.InvertPositionX.Value,
                invertY: config.InvertPositionY.Value,
                invertZ: config.InvertPositionZ.Value)
        };

        var session = new HeadTrackingSession(receiver, processor, positionProcessor)
        {
            Mode = config.PositionEnabled.Value ? TrackingMode.RotationAndPosition : TrackingMode.RotationOnly,
            Log = msg => Logger.LogInfo(msg)
        };

        ClassInjector.RegisterTypeInIl2Cpp<HeadTrackingBehaviour>();
        ClassInjector.RegisterTypeInIl2Cpp<FastBootBehaviour>();

        _hostObject = new GameObject("SotF.HeadTrackingHost");
        _hostObject.hideFlags = HideFlags.DontSave;
        Object.DontDestroyOnLoad(_hostObject);

        _host = _hostObject.AddComponent<HeadTrackingBehaviour>();
        _host.Initialize(session, config);

        if (config.DebugFastBoot.Value)
        {
            FastBootBehaviour.Log = msg => Logger.LogInfo(msg);
            _hostObject.AddComponent<FastBootBehaviour>();
            Logger.LogInfo("DebugFastBoot enabled - splash/intro VideoPlayers will be killed on each scene load.");
        }

        Logger.LogInfo($"{PluginName} loaded. Press End to toggle, Home to recenter.");
    }
}
