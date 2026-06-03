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
    public ConfigEntry<float> Smoothing { get; }
    public ConfigEntry<bool> WorldSpaceYaw { get; }

    public ConfigEntry<bool> PositionEnabled { get; }
    public ConfigEntry<float> PositionSensitivityX { get; }
    public ConfigEntry<float> PositionSensitivityY { get; }
    public ConfigEntry<float> PositionSensitivityZ { get; }
    public ConfigEntry<float> PositionLimitX { get; }
    public ConfigEntry<float> PositionLimitY { get; }
    public ConfigEntry<float> PositionLimitZ { get; }
    public ConfigEntry<float> PositionLimitZBack { get; }
    public ConfigEntry<float> PositionSmoothing { get; }
    public ConfigEntry<bool> InvertPositionX { get; }
    public ConfigEntry<bool> InvertPositionY { get; }
    public ConfigEntry<bool> InvertPositionZ { get; }

    public ConfigEntry<KeyCode> ToggleKey { get; }
    public ConfigEntry<KeyCode> RecenterKey { get; }
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
        Smoothing = cfg.Bind("Sensitivity", "Smoothing", 0.0f,
            new ConfigDescription("Rotation smoothing (0=none, 1=heavy). A 0.15 floor is applied internally.",
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
        PositionLimitX = cfg.Bind("Position", "PositionLimitX", 0.30f,
            new ConfigDescription("Maximum lateral displacement in meters.", new AcceptableValueRange<float>(0.01f, 0.5f)));
        PositionLimitY = cfg.Bind("Position", "PositionLimitY", 0.20f,
            new ConfigDescription("Maximum vertical displacement in meters.", new AcceptableValueRange<float>(0.01f, 0.5f)));
        PositionLimitZ = cfg.Bind("Position", "PositionLimitZ", 0.40f,
            new ConfigDescription("Maximum forward lean in meters.", new AcceptableValueRange<float>(0.01f, 0.5f)));
        PositionLimitZBack = cfg.Bind("Position", "PositionLimitZBack", 0.10f,
            new ConfigDescription("Maximum backward lean in meters (small, prevents clipping into the player).",
                new AcceptableValueRange<float>(0.01f, 0.5f)));
        PositionSmoothing = cfg.Bind("Position", "PositionSmoothing", 0.15f,
            new ConfigDescription("Position smoothing (0=minimum, 1=heavy).", new AcceptableValueRange<float>(0f, 1f)));
        InvertPositionX = cfg.Bind("Position", "InvertPositionX", true,
            "Invert lateral movement (default on: converts OpenTrack X to Unity, verified for Sons of the Forest).");
        InvertPositionY = cfg.Bind("Position", "InvertPositionY", false,
            "Invert vertical movement.");
        InvertPositionZ = cfg.Bind("Position", "InvertPositionZ", true,
            "Invert depth movement (default on: converts OpenTrack Z to Unity so leaning in moves the camera forward).");

        ToggleKey = cfg.Bind("Hotkeys", "ToggleKey", KeyCode.End,
            "Toggle head tracking on/off.");
        RecenterKey = cfg.Bind("Hotkeys", "RecenterKey", KeyCode.Home,
            "Recenter head tracking to the current pose.");
        YawModeKey = cfg.Bind("Hotkeys", "YawModeKey", KeyCode.PageDown,
            "Toggle world-space vs camera-local yaw.");
        PositionToggleKey = cfg.Bind("Hotkeys", "PositionToggleKey", KeyCode.PageUp,
            "Cycle tracking mode: 6DOF (rotation + position) -> rotation only -> position only.");

        DebugFastBoot = cfg.Bind("Debug", "DebugFastBoot", false,
            "Skip intro/splash VideoPlayers on every scene load. Dev-time only - leave off for releases.");
    }
}
