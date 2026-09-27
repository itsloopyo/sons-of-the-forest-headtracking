// SPDX-License-Identifier: MIT
// Copyright (c) 2026 itsloopyo

using CameraUnlock.Core.Config;

namespace SonsOfTheForestHeadTracking.Legacy;

/// <summary>Every section and key <see cref="LegacyConfigReader"/> reads. Frozen with it.</summary>
internal static class LegacyConfigKeys
{
    public static LegacyKey[] All()
    {
        return new[]
        {
            new LegacyKey("General", "EnabledOnStartup"),
            new LegacyKey("General", "WorldSpaceYaw"),
            new LegacyKey("Sensitivity", "YawSensitivity"),
            new LegacyKey("Sensitivity", "PitchSensitivity"),
            new LegacyKey("Sensitivity", "RollSensitivity"),
            new LegacyKey("Smoothing", "LocalSmoothing"),
            new LegacyKey("Smoothing", "RemoteSmoothing"),
            new LegacyKey("CoordinateTransform", "InvertYaw"),
            new LegacyKey("CoordinateTransform", "InvertPitch"),
            new LegacyKey("CoordinateTransform", "InvertRoll"),
            new LegacyKey("Position", "PositionEnabled"),
            new LegacyKey("Position", "PositionSensitivityX"),
            new LegacyKey("Position", "PositionSensitivityY"),
            new LegacyKey("Position", "PositionSensitivityZ"),
            new LegacyKey("Position", "PositionLimitX"),
            new LegacyKey("Position", "PositionLimitY"),
            new LegacyKey("Position", "PositionLimitZ"),
            new LegacyKey("Position", "PositionLimitZBack"),
            new LegacyKey("Position", "InvertPositionX"),
            new LegacyKey("Position", "InvertPositionY"),
            new LegacyKey("Position", "InvertTrackerZ"),
            new LegacyKey("Hotkeys", "ToggleKey"),
            new LegacyKey("Hotkeys", "YawModeKey"),
            new LegacyKey("Hotkeys", "PositionToggleKey"),
            new LegacyKey("Debug", "DebugFastBoot"),
        };
    }
}
