// SPDX-License-Identifier: MIT
// Copyright (c) 2026 itsloopyo

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx.Configuration;
using CameraUnlock.Core.Config;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Input;
using SonsOfTheForestHeadTracking.Configuration;
using UnityEngine;

namespace SonsOfTheForestHeadTracking.Legacy;

/// <summary>
/// The import the config owner runs on com.cameraunlock.sonsoftheforest.headtracking.cfg while
/// CameraUnlock.ini is absent: <see cref="LegacyConfigReader"/> on a ConfigFile of its own over
/// that file, then the map into <see cref="SonsOfTheForestConfig"/>.
/// <para>
/// Not the plugin's Config: ConfigurationManager lists every entry bound there, and one bound by
/// the import would sit in its window for the rest of the session doing nothing. A new ConfigFile
/// reads the file as the one the loader builds for the plugin does.
/// </para>
/// </summary>
internal static class LegacyConfigImport
{
    public static LegacyImport<SonsOfTheForestConfig> Create()
    {
        return new LegacyImport<SonsOfTheForestConfig>(
            (input, config) => Run(new ConfigFile(input.Path, false), input, config), LegacyConfigKeys.All());
    }

    /// <param name="legacyFile">A ConfigFile over the legacy file that nothing has bound to.</param>
    public static ImportResult Run(ConfigFile legacyFile, LegacyImportInput input, SonsOfTheForestConfig config)
    {
        if (!string.Equals(Path.GetFullPath(input.Path), Path.GetFullPath(legacyFile.ConfigFilePath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("the owner hands over " + input.Path + ", and the ConfigFile reads "
                                                + legacyFile.ConfigFilePath);
        }

        LegacyConfig legacy = LegacyConfigReader.Read(legacyFile, out bool found);
        var dropped = new List<DroppedValue>();
        var poseShaping = new List<PoseShapingValue>();
        var followsDefaultsIni = new LegacyFollowsDefaultsIni();
        Map(legacy, config, dropped, poseShaping, followsDefaultsIni);
        return found
            ? ImportResult.Imported(dropped, poseShaping, followsDefaultsIni.Concepts)
            : ImportResult.Absent(dropped, poseShaping, followsDefaultsIni.Concepts);
    }

    /// <summary>
    /// Every float the reader returns is inside its AcceptableValueRange, which BepInEx clamps NaN
    /// and infinity into, so no value reaches here that normalisation N2 would change. Every row
    /// is compared with the published build's own default, a fresh <see cref="LegacyConfig"/>, so
    /// a setting the player never changed follows Defaults.ini.
    /// </summary>
    public static void Map(LegacyConfig legacy, SonsOfTheForestConfig config, List<DroppedValue> dropped,
        List<PoseShapingValue> poseShaping, LegacyFollowsDefaultsIni followsDefaultsIni)
    {
        var shipped = new LegacyConfig();

        config.EnableOnStartup = legacy.EnabledOnStartup;
        followsDefaultsIni.Setting(ConfigConcepts.EnableOnStartup, legacy.EnabledOnStartup, shipped.EnabledOnStartup);
        config.WorldSpaceYaw = legacy.WorldSpaceYaw;
        followsDefaultsIni.Setting(ConfigConcepts.WorldSpaceYaw, legacy.WorldSpaceYaw, shipped.WorldSpaceYaw);

        // The tracker owns pose shaping (approved change pose_shaping). The shipped InvertPitch=true
        // and InvertPositionX=true are the axis conversion, which the mod now applies at the camera
        // boundary; a value the player set away from the shipped one is dropped and logged.
        LegacyPoseShaping.Record(legacy.YawSensitivity, shipped.YawSensitivity, "Sensitivity", "YawSensitivity", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.PitchSensitivity, shipped.PitchSensitivity, "Sensitivity", "PitchSensitivity", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.RollSensitivity, shipped.RollSensitivity, "Sensitivity", "RollSensitivity", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.InvertYaw, shipped.InvertYaw, "CoordinateTransform", "InvertYaw", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.InvertPitch, shipped.InvertPitch, "CoordinateTransform", "InvertPitch", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.InvertRoll, shipped.InvertRoll, "CoordinateTransform", "InvertRoll", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.PositionSensitivityX, shipped.PositionSensitivityX, "Position", "PositionSensitivityX", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.PositionSensitivityY, shipped.PositionSensitivityY, "Position", "PositionSensitivityY", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.PositionSensitivityZ, shipped.PositionSensitivityZ, "Position", "PositionSensitivityZ", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.InvertPositionX, shipped.InvertPositionX, "Position", "InvertPositionX", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.InvertPositionY, shipped.InvertPositionY, "Position", "InvertPositionY", poseShaping, dropped);
        LegacyPoseShaping.Record(legacy.InvertTrackerZ, shipped.InvertTrackerZ, "Position", "InvertTrackerZ", poseShaping, dropped);

        // [Position] PositionEnabled set the startup mode and nothing else: the published cycle
        // started from rotation plus that switch, and its next step turned position back on.
        config.RotationEnabled = true;
        config.PositionEnabled = legacy.PositionEnabled;
        followsDefaultsIni.TrackingMode(legacy.PositionEnabled, shipped.PositionEnabled);

        config.LocalSmoothing = legacy.LocalSmoothing;
        followsDefaultsIni.Setting(ConfigConcepts.LocalSmoothing, legacy.LocalSmoothing, shipped.LocalSmoothing);
        config.RemoteSmoothing = legacy.RemoteSmoothing;
        followsDefaultsIni.Setting(ConfigConcepts.RemoteSmoothing, legacy.RemoteSmoothing, shipped.RemoteSmoothing);

        // PositionSettings.Symmetric gave the downward limit the upward one, so both rows carry it.
        PositionSettings p = config.Position;
        config.Position = new PositionSettings(
            p.SensitivityX, p.SensitivityY, p.SensitivityZ,
            legacy.PositionLimitX, legacy.PositionLimitY, legacy.PositionLimitY, legacy.PositionLimitZ,
            legacy.PositionLimitZBack,
            legacy.LocalSmoothing, legacy.RemoteSmoothing,
            p.InvertX, p.InvertY, p.InvertZ);
        followsDefaultsIni.Setting(ConfigConcepts.PositionLimitX, legacy.PositionLimitX, shipped.PositionLimitX);
        followsDefaultsIni.Setting(ConfigConcepts.PositionLimitY, legacy.PositionLimitY, shipped.PositionLimitY);
        followsDefaultsIni.Setting(ConfigConcepts.PositionLimitYDown, legacy.PositionLimitY, shipped.PositionLimitY);
        followsDefaultsIni.Setting(ConfigConcepts.PositionLimitZ, legacy.PositionLimitZ, shipped.PositionLimitZ);
        followsDefaultsIni.Setting(ConfigConcepts.PositionLimitZBack, legacy.PositionLimitZBack, shipped.PositionLimitZBack);

        config.ToggleKeyName = HotkeyList(legacy.ToggleKey, KeyCode.Y, "ToggleKey", dropped);
        followsDefaultsIni.Setting(ConfigConcepts.ToggleKey, legacy.ToggleKey, shipped.ToggleKey);
        config.CycleTrackingModeKeyName = HotkeyList(legacy.PositionToggleKey, KeyCode.G, "PositionToggleKey", dropped);
        followsDefaultsIni.Setting(ConfigConcepts.CycleTrackingModeKey, legacy.PositionToggleKey, shipped.PositionToggleKey);
        config.YawModeKeyName = HotkeyList(legacy.YawModeKey, KeyCode.H, "YawModeKey", dropped);
        followsDefaultsIni.Setting(ConfigConcepts.YawModeKey, legacy.YawModeKey, shipped.YawModeKey);

        config.DebugFastBoot = legacy.DebugFastBoot;
    }

    /// <summary>
    /// The keys the published build fired an action on: the configured key, unless it was None,
    /// and the Ctrl+Shift chord ChordHotkeys.IsActionPressed checked beside it. A Ctrl, Shift or
    /// Alt key on its own is left unbound and recorded under <paramref name="legacyKey"/>
    /// (normalisation N3), and the chord stays. A key code Unity names no key for (a number in the
    /// .cfg, which BepInEx's enum parse accepts) is written as that number, which no hotkey list
    /// reads, so the owner defers the import and says which line.
    /// </summary>
    public static string HotkeyList(KeyCode primary, KeyCode chordLetter, string legacyKey, List<DroppedValue> dropped)
    {
        string chord = KeyBindings.Format(new[] { new KeyBinding(KeyModifiers.Ctrl | KeyModifiers.Shift, (int)chordLetter) });
        string key = KeyText((int)primary, legacyKey, dropped);
        return key.Length == 0 ? chord : key + ", " + chord;
    }

    private static string KeyText(int unityKeyCode, string legacyKey, List<DroppedValue> dropped)
    {
        try
        {
            return LegacyNormalisations.KeyCodeToBindings(unityKeyCode, "Hotkeys", legacyKey, dropped);
        }
        catch (ArgumentException)
        {
            return unityKeyCode.ToString(CultureInfo.InvariantCulture);
        }
    }
}
