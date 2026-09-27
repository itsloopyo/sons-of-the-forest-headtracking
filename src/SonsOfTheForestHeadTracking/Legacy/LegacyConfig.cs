// SPDX-License-Identifier: MIT
// Copyright (c) 2026 itsloopyo

using UnityEngine;

namespace SonsOfTheForestHeadTracking.Legacy;

/// <summary>
/// The settings every published build read from
/// BepInEx\config\com.cameraunlock.sonsoftheforest.headtracking.cfg, with their defaults. Frozen:
/// a later change to the runtime settings must never change what an old .cfg, or one missing a
/// key, reads as.
/// </summary>
internal sealed class LegacyConfig
{
    public bool EnabledOnStartup = true;
    public bool WorldSpaceYaw = true;

    public float YawSensitivity = 1.0f;
    public float PitchSensitivity = 1.0f;
    public float RollSensitivity = 1.0f;

    public float LocalSmoothing = 0.0f;
    public float RemoteSmoothing = 0.15f;

    public bool InvertYaw = false;
    public bool InvertPitch = true;
    public bool InvertRoll = false;

    public bool PositionEnabled = true;
    public float PositionSensitivityX = 1.0f;
    public float PositionSensitivityY = 1.0f;
    public float PositionSensitivityZ = 1.0f;
    public float PositionLimitX = 0.30f;
    public float PositionLimitY = 0.20f;
    public float PositionLimitZ = 0.40f;
    public float PositionLimitZBack = 0.10f;
    public bool InvertPositionX = true;
    public bool InvertPositionY = false;
    public bool InvertTrackerZ = false;

    public KeyCode ToggleKey = KeyCode.End;
    public KeyCode YawModeKey = KeyCode.PageDown;
    public KeyCode PositionToggleKey = KeyCode.PageUp;

    public bool DebugFastBoot = false;
}
