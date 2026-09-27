using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CameraUnlock.Core.Config;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Tracking;
using CameraUnlock.Core.Unity.Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using SonsOfTheForestHeadTracking.Configuration;
using UnityEngine;
using Object = UnityEngine.Object;

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

    private ConfigOwner<SonsOfTheForestConfig>? _configOwner;

    public override void Load()
    {
        Logger = Log;
        Logger.LogInfo($"Loading {PluginName} v{PluginVersion}...");

        SonsOfTheForestConfig config = LoadConfig();

        var receiver = new OpenTrackReceiver();
        receiver.Log = msg => Logger.LogInfo(msg);
        receiver.Start(OpenTrackReceiver.DefaultPort);
        Logger.LogInfo($"Listening for tracker data on UDP port {OpenTrackReceiver.DefaultPort}.");

        // The pipeline runs at 1:1: no sensitivity, no deadzone, no inversion. The tracker owns
        // pose shaping, and the axis signs this engine needs are applied once at the camera
        // boundary in HeadTrackingBehaviour.
        var processor = new TrackingProcessor
        {
            // Selected per connection by HeadTrackingSession, which re-reads locality
            // from the receiver every Update: loopback senders get LocalSmoothing,
            // remote network devices get RemoteSmoothing.
            LocalSmoothing = config.LocalSmoothing,
            RemoteSmoothing = config.RemoteSmoothing,
            Sensitivity = SensitivitySettings.Default,
            Deadzone = DeadzoneSettings.None
        };

        var positionProcessor = new PositionProcessor
        {
            Settings = new PositionSettings(
                1f, 1f, 1f,
                config.Position.LimitX,
                config.Position.LimitY,
                config.Position.LimitYDown,
                config.Position.LimitZ,
                config.Position.LimitZBack,
                localSmoothing: config.LocalSmoothing,
                remoteSmoothing: config.RemoteSmoothing,
                invertX: false, invertY: false, invertZ: false)
        };

        var session = new HeadTrackingSession(receiver, processor, positionProcessor)
        {
            Mode = TrackingModeChannels.Decode(config.RotationEnabled, config.PositionEnabled)!.Value,
            Log = msg => Logger.LogInfo(msg)
        };

        ClassInjector.RegisterTypeInIl2Cpp<HeadTrackingBehaviour>();
        ClassInjector.RegisterTypeInIl2Cpp<FastBootBehaviour>();

        _hostObject = new GameObject("SotF.HeadTrackingHost");
        _hostObject.hideFlags = HideFlags.DontSave;
        Object.DontDestroyOnLoad(_hostObject);

        _host = _hostObject.AddComponent<HeadTrackingBehaviour>();
        _host.Initialize(session, config, SaveConfig);

        if (config.DebugFastBoot)
        {
            FastBootBehaviour.Log = msg => Logger.LogInfo(msg);
            _hostObject.AddComponent<FastBootBehaviour>();
            Logger.LogInfo("DebugFastBoot enabled - splash/intro VideoPlayers will be killed on each scene load.");
        }

        Logger.LogInfo($"{PluginName} loaded. Hotkeys: [{config.ToggleKeyName}] toggle.");
    }

    /// <summary>
    /// The settings live in BepInEx\config\CameraUnlock.ini, read and written by core's config
    /// owner, with rows set to default following the player's Defaults.ini. Nothing is bound on
    /// the plugin's Config, so ConfigurationManager does not list them. While CameraUnlock.ini is
    /// absent the owner imports the plugin's .cfg, the file every earlier build read, through the
    /// frozen reader on a ConfigFile of its own, and never writes that file.
    ///
    /// The mod has nothing on screen to show a message with, so the owner's messages for the
    /// player go to the log beside its other lines.
    /// </summary>
    private SonsOfTheForestConfig LoadConfig()
    {
        ConfigOwnerOptions<SonsOfTheForestConfig> options =
            SonsOfTheForestConfig.Options(ConfigPath, Config.ConfigFilePath, DefaultsFile.PerUser());
        options.StatusSink = message => Logger.LogWarning(message);
        _configOwner = new ConfigOwner<SonsOfTheForestConfig>(options);

        ConfigLoadResult<SonsOfTheForestConfig> loaded = _configOwner.Load();

        // The owner writes each diagnostic as "<path>: <description>" among lines that only
        // report what it did, so the complaints are picked out by their text.
        var complaints = new HashSet<string>();
        foreach (CanonicalDiagnostic diagnostic in loaded.Diagnostics)
        {
            complaints.Add(ConfigPath + ": " + diagnostic.Describe());
        }
        bool usable = loaded.Status == ConfigLoadStatus.Canonical
                      || loaded.Status == ConfigLoadStatus.Migrated
                      || loaded.Status == ConfigLoadStatus.Created;
        foreach (string line in loaded.Log)
        {
            if (usable && !complaints.Contains(line)) Logger.LogInfo(line);
            else Logger.LogWarning(line);
        }
        Logger.LogInfo($"Config {ConfigPath}: {loaded.Status}");
        return loaded.Config;
    }

    private static string ConfigPath => Path.Combine(Paths.ConfigPath, "CameraUnlock.ini");

    /// <summary>
    /// Called after the new value is already applied. A save that fails is logged and the
    /// session keeps the new value.
    /// </summary>
    private void SaveConfig(Action<SonsOfTheForestConfig> change)
    {
        ConfigSaveResult saved = _configOwner!.Save(change);
        if (saved.Status == ConfigSaveStatus.Saved)
        {
            // A row that held default and now holds a value, so it stops following
            // Defaults.ini in this game.
            foreach (string line in saved.Log) Logger.LogInfo(line);
            return;
        }
        foreach (string line in saved.Log) Logger.LogWarning(line);
        Logger.LogWarning($"{ConfigPath}: {saved.Status}: {saved.Reason} The change applies to this session only.");
    }
}
