using BepInEx.Configuration;
using UnityEngine;

namespace SonsOfTheForestHeadTracking;

public sealed class PluginConfig
{
    public ConfigEntry<bool> EnabledOnStartup { get; }
    public ConfigEntry<float> YawSensitivity { get; }
    public ConfigEntry<float> PitchSensitivity { get; }
    public ConfigEntry<float> RollSensitivity { get; }
    public ConfigEntry<bool> InvertYaw { get; }
    public ConfigEntry<bool> InvertPitch { get; }
    public ConfigEntry<bool> InvertRoll { get; }
    public ConfigEntry<float> LocalSmoothing { get; }
    public ConfigEntry<float> RemoteSmoothing { get; }
    public ConfigEntry<bool> WorldSpaceYaw { get; }

    public ConfigEntry<bool> PositionEnabled { get; }
    public ConfigEntry<float> PositionSensitivityX { get; }
    public ConfigEntry<float> PositionSensitivityY { get; }
    public ConfigEntry<float> PositionSensitivityZ { get; }
    public ConfigEntry<float> PositionLimitX { get; }
    public ConfigEntry<float> PositionLimitY { get; }
    public ConfigEntry<float> PositionLimitZ { get; }
    public ConfigEntry<float> PositionLimitZBack { get; }
    public ConfigEntry<bool> InvertPositionX { get; }
    public ConfigEntry<bool> InvertPositionY { get; }
    public ConfigEntry<bool> InvertTrackerZ { get; }

    public ConfigEntry<KeyCode> ToggleKey { get; }
    public ConfigEntry<KeyCode> YawModeKey { get; }
    public ConfigEntry<KeyCode> PositionToggleKey { get; }

    public ConfigEntry<bool> DebugFastBoot { get; }

    public PluginConfig(ConfigFile cfg)
    {
        EnabledOnStartup = cfg.Bind("General", "EnabledOnStartup", true,
            "Enable head tracking automatically when the game starts.");
        WorldSpaceYaw = cfg.Bind("General", "WorldSpaceYaw", true,
            "True = horizon-locked yaw (rotates around world up). False = camera-local yaw.");

        YawSensitivity = cfg.Bind("Sensitivity", "YawSensitivity", 1.0f,
            new ConfigDescription("Yaw sensitivity multiplier.", new AcceptableValueRange<float>(-5f, 5f)));
        PitchSensitivity = cfg.Bind("Sensitivity", "PitchSensitivity", 1.0f,
            new ConfigDescription("Pitch sensitivity multiplier.", new AcceptableValueRange<float>(-5f, 5f)));
        RollSensitivity = cfg.Bind("Sensitivity", "RollSensitivity", 1.0f,
            new ConfigDescription("Roll sensitivity multiplier.", new AcceptableValueRange<float>(-5f, 5f)));
        LocalSmoothing = cfg.Bind("Smoothing", "LocalSmoothing", CameraUnlock.Core.Math.SmoothingUtils.DefaultLocalSmoothing,
            new ConfigDescription("Smoothing applied when the tracker runs on this machine (loopback). 0 = no smoothing, 1 = heavy. Covers rotation and position.",
                new AcceptableValueRange<float>(0f, 1f)));
        RemoteSmoothing = cfg.Bind("Smoothing", "RemoteSmoothing", CameraUnlock.Core.Math.SmoothingUtils.DefaultRemoteSmoothing,
            new ConfigDescription("Smoothing applied when the tracker is a remote device on the network. 0 = no smoothing, 1 = heavy. Covers rotation and position.",
                new AcceptableValueRange<float>(0f, 1f)));

        InvertYaw = cfg.Bind("CoordinateTransform", "InvertYaw", false,
            "Invert yaw axis if turning your head right turns the camera left.");
        InvertPitch = cfg.Bind("CoordinateTransform", "InvertPitch", true,
            "Invert pitch axis (default on: converts OpenTrack pitch to Unity).");
        InvertRoll = cfg.Bind("CoordinateTransform", "InvertRoll", false,
            "Invert roll axis if tilting your head right tilts the camera left.");

        PositionEnabled = cfg.Bind("Position", "PositionEnabled", true,
            "Enable positional (6DOF) tracking - lean and move your head to shift the camera.");
        PositionSensitivityX = cfg.Bind("Position", "PositionSensitivityX", 1.0f,
            new ConfigDescription("Lateral (left/right) position sensitivity.", new AcceptableValueRange<float>(0f, 5f)));
        PositionSensitivityY = cfg.Bind("Position", "PositionSensitivityY", 1.0f,
            new ConfigDescription("Vertical (up/down) position sensitivity.", new AcceptableValueRange<float>(0f, 5f)));
        PositionSensitivityZ = cfg.Bind("Position", "PositionSensitivityZ", 1.0f,
            new ConfigDescription("Depth (lean in/out) position sensitivity.", new AcceptableValueRange<float>(0f, 5f)));
        PositionLimitX = cfg.Bind("Position", "PositionLimitX", CameraUnlock.Core.Data.PositionSettings.Default.LimitX,
            new ConfigDescription("Maximum lateral displacement in meters.", new AcceptableValueRange<float>(0.01f, 0.5f)));
        PositionLimitY = cfg.Bind("Position", "PositionLimitY", CameraUnlock.Core.Data.PositionSettings.Default.LimitY,
            new ConfigDescription("Maximum vertical displacement in meters.", new AcceptableValueRange<float>(0.01f, 0.5f)));
        PositionLimitZ = cfg.Bind("Position", "PositionLimitZ", CameraUnlock.Core.Data.PositionSettings.Default.LimitZ,
            new ConfigDescription("Maximum forward lean in meters.", new AcceptableValueRange<float>(0.01f, 0.5f)));
        PositionLimitZBack = cfg.Bind("Position", "PositionLimitZBack", CameraUnlock.Core.Data.PositionSettings.Default.LimitZBack,
            new ConfigDescription("Maximum backward lean in meters (small, prevents clipping into the player).",
                new AcceptableValueRange<float>(0.01f, 0.5f)));
        InvertPositionX = cfg.Bind("Position", "InvertPositionX", true,
            "Invert lateral movement (default on: converts OpenTrack X to Unity, verified for Sons of the Forest).");
        InvertPositionY = cfg.Bind("Position", "InvertPositionY", false,
            "Invert vertical movement.");
        // Renamed from InvertPositionZ, which every existing config file carries as true.
        // It used to double as the conversion into Unity's +z-forward space, a job
        // cameraunlock-core now does at the engine boundary; left in place it would invert
        // the lean. The key has to change so those files re-default.
        InvertTrackerZ = cfg.Bind("Position", "InvertTrackerZ", false,
            "Invert depth movement (only for a tracker whose Z axis runs backwards).");

        ToggleKey = cfg.Bind("Hotkeys", "ToggleKey", KeyCode.End,
            "Toggle head tracking on/off.");
        YawModeKey = cfg.Bind("Hotkeys", "YawModeKey", KeyCode.PageDown,
            "Toggle world-space vs camera-local yaw.");
        PositionToggleKey = cfg.Bind("Hotkeys", "PositionToggleKey", KeyCode.PageUp,
            "Cycle tracking mode: 6DOF (rotation + position) -> rotation only -> position only.");

        DebugFastBoot = cfg.Bind("Debug", "DebugFastBoot", false,
            "Skip intro/splash VideoPlayers on every scene load. Dev-time only - leave off for releases.");
    }
}
