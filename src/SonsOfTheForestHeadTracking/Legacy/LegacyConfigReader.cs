// SPDX-License-Identifier: MIT
// Copyright (c) 2026 itsloopyo

using System.IO;
using BepInEx.Configuration;

namespace SonsOfTheForestHeadTracking.Legacy;

/// <summary>
/// The plugin's BepInEx Bind calls as the published builds ran them, each definition's section,
/// key, type, description and acceptable values unchanged and its default taken from
/// <see cref="LegacyConfig"/>. Frozen for the life of the repo: it is how a player's .cfg is read,
/// whichever earlier build wrote it.
/// <para>
/// It writes nothing. BepInEx's ConfigFile read the .cfg in its constructor, before whoever calls
/// this held the file, so saving on set is turned off first and the file is read again before
/// anything is bound. A missing file reads as the defaults.
/// </para>
/// </summary>
internal static class LegacyConfigReader
{
    /// <summary>Reads <paramref name="cfg"/>'s file into a new <see cref="LegacyConfig"/>.</summary>
    /// <param name="found">Whether the file existed.</param>
    public static LegacyConfig Read(ConfigFile cfg, out bool found)
    {
        cfg.SaveOnConfigSet = false;
        found = File.Exists(cfg.ConfigFilePath);
        if (found)
        {
            cfg.Reload();
        }

        var read = new LegacyConfig();

        read.EnabledOnStartup = cfg.Bind("General", "EnabledOnStartup", read.EnabledOnStartup,
            "Enable head tracking automatically when the game starts.").Value;
        read.WorldSpaceYaw = cfg.Bind("General", "WorldSpaceYaw", read.WorldSpaceYaw,
            "True = horizon-locked yaw (rotates around world up). False = camera-local yaw.").Value;

        read.YawSensitivity = cfg.Bind("Sensitivity", "YawSensitivity", read.YawSensitivity,
            new ConfigDescription("Yaw sensitivity multiplier.", new AcceptableValueRange<float>(-5f, 5f))).Value;
        read.PitchSensitivity = cfg.Bind("Sensitivity", "PitchSensitivity", read.PitchSensitivity,
            new ConfigDescription("Pitch sensitivity multiplier.", new AcceptableValueRange<float>(-5f, 5f))).Value;
        read.RollSensitivity = cfg.Bind("Sensitivity", "RollSensitivity", read.RollSensitivity,
            new ConfigDescription("Roll sensitivity multiplier.", new AcceptableValueRange<float>(-5f, 5f))).Value;
        read.LocalSmoothing = cfg.Bind("Smoothing", "LocalSmoothing", read.LocalSmoothing,
            new ConfigDescription("Smoothing applied when the tracker runs on this machine (loopback). 0 = no smoothing, 1 = heavy. Covers rotation and position.",
                new AcceptableValueRange<float>(0f, 1f))).Value;
        read.RemoteSmoothing = cfg.Bind("Smoothing", "RemoteSmoothing", read.RemoteSmoothing,
            new ConfigDescription("Smoothing applied when the tracker is a remote device on the network. 0 = no smoothing, 1 = heavy. Covers rotation and position.",
                new AcceptableValueRange<float>(0f, 1f))).Value;

        read.InvertYaw = cfg.Bind("CoordinateTransform", "InvertYaw", read.InvertYaw,
            "Invert yaw axis if turning your head right turns the camera left.").Value;
        read.InvertPitch = cfg.Bind("CoordinateTransform", "InvertPitch", read.InvertPitch,
            "Invert pitch axis (default on: converts OpenTrack pitch to Unity).").Value;
        read.InvertRoll = cfg.Bind("CoordinateTransform", "InvertRoll", read.InvertRoll,
            "Invert roll axis if tilting your head right tilts the camera left.").Value;

        read.PositionEnabled = cfg.Bind("Position", "PositionEnabled", read.PositionEnabled,
            "Enable positional (6DOF) tracking - lean and move your head to shift the camera.").Value;
        read.PositionSensitivityX = cfg.Bind("Position", "PositionSensitivityX", read.PositionSensitivityX,
            new ConfigDescription("Lateral (left/right) position sensitivity.", new AcceptableValueRange<float>(0f, 5f))).Value;
        read.PositionSensitivityY = cfg.Bind("Position", "PositionSensitivityY", read.PositionSensitivityY,
            new ConfigDescription("Vertical (up/down) position sensitivity.", new AcceptableValueRange<float>(0f, 5f))).Value;
        read.PositionSensitivityZ = cfg.Bind("Position", "PositionSensitivityZ", read.PositionSensitivityZ,
            new ConfigDescription("Depth (lean in/out) position sensitivity.", new AcceptableValueRange<float>(0f, 5f))).Value;
        read.PositionLimitX = cfg.Bind("Position", "PositionLimitX", read.PositionLimitX,
            new ConfigDescription("Maximum lateral displacement in meters.", new AcceptableValueRange<float>(0.01f, 0.5f))).Value;
        read.PositionLimitY = cfg.Bind("Position", "PositionLimitY", read.PositionLimitY,
            new ConfigDescription("Maximum vertical displacement in meters.", new AcceptableValueRange<float>(0.01f, 0.5f))).Value;
        read.PositionLimitZ = cfg.Bind("Position", "PositionLimitZ", read.PositionLimitZ,
            new ConfigDescription("Maximum forward lean in meters.", new AcceptableValueRange<float>(0.01f, 0.5f))).Value;
        read.PositionLimitZBack = cfg.Bind("Position", "PositionLimitZBack", read.PositionLimitZBack,
            new ConfigDescription("Maximum backward lean in meters (small, prevents clipping into the player).",
                new AcceptableValueRange<float>(0.01f, 0.5f))).Value;
        read.InvertPositionX = cfg.Bind("Position", "InvertPositionX", read.InvertPositionX,
            "Invert lateral movement (default on: converts OpenTrack X to Unity, verified for Sons of the Forest).").Value;
        read.InvertPositionY = cfg.Bind("Position", "InvertPositionY", read.InvertPositionY,
            "Invert vertical movement.").Value;
        read.InvertTrackerZ = cfg.Bind("Position", "InvertTrackerZ", read.InvertTrackerZ,
            "Invert depth movement (only for a tracker whose Z axis runs backwards).").Value;

        read.ToggleKey = cfg.Bind("Hotkeys", "ToggleKey", read.ToggleKey,
            "Toggle head tracking on/off.").Value;
        read.YawModeKey = cfg.Bind("Hotkeys", "YawModeKey", read.YawModeKey,
            "Toggle world-space vs camera-local yaw.").Value;
        read.PositionToggleKey = cfg.Bind("Hotkeys", "PositionToggleKey", read.PositionToggleKey,
            "Cycle tracking mode: 6DOF (rotation + position) -> rotation only -> position only.").Value;

        read.DebugFastBoot = cfg.Bind("Debug", "DebugFastBoot", read.DebugFastBoot,
            "Skip intro/splash VideoPlayers on every scene load. Dev-time only - leave off for releases.").Value;

        return read;
    }
}
