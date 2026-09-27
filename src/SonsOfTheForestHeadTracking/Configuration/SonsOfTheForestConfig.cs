// SPDX-License-Identifier: MIT
// Copyright (c) 2026 itsloopyo

using CameraUnlock.Core.Config;
using SonsOfTheForestHeadTracking.Legacy;

namespace SonsOfTheForestHeadTracking.Configuration;

/// <summary>
/// Everything the mod reads from BepInEx\config\CameraUnlock.ini. Unity-free, so the test
/// project compiles it and holds the committed file to it.
///
/// There are deliberately no sensitivity, deadzone, response-curve or axis-inversion
/// settings here. The tracker owns pose shaping: OpenTrack, a phone app or a headset
/// each already have those controls, and configuring them once there is what makes a
/// single profile behave the same across every game. The axis signs this game needs
/// are a fixed conversion applied at the engine boundary, not a knob.
/// </summary>
internal sealed class SonsOfTheForestConfig : HeadTrackingConfigData
{
    /// <summary>The game's name as data/games.json spells it.</summary>
    public const string DisplayName = "Sons of the Forest";

    public bool DebugFastBoot { get; set; }

    public static ConfigTable<SonsOfTheForestConfig> Table()
    {
        return HeadTrackingConfigTable.Create<SonsOfTheForestConfig>(
                ConfigConcepts.EnableOnStartup,
                ConfigConcepts.WorldSpaceYaw,
                ConfigConcepts.RotationEnabled,
                ConfigConcepts.LocalSmoothing,
                ConfigConcepts.RemoteSmoothing,
                ConfigConcepts.PositionEnabled,
                ConfigConcepts.PositionLimitX,
                ConfigConcepts.PositionLimitY,
                ConfigConcepts.PositionLimitYDown,
                ConfigConcepts.PositionLimitZ,
                ConfigConcepts.PositionLimitZBack,
                ConfigConcepts.ToggleKey,
                ConfigConcepts.CycleTrackingModeKey,
                ConfigConcepts.YawModeKey)
            .Select(ConfigConcepts.WorldSpaceYaw).Writable()
            .Select(ConfigConcepts.RotationEnabled).Writable()
            .Select(ConfigConcepts.PositionEnabled).Writable()
            .Local("Debug", "DebugFastBoot", c => c.DebugFastBoot, (c, v) => c.DebugFastBoot = v,
                new BoolCodec(),
                "true: skip the intro and splash videos on every scene load. For development;\n" +
                "leave it false.");
    }

    /// <summary>
    /// The config owner's options for <paramref name="path"/>, importing the published builds'
    /// .cfg at <paramref name="legacyPath"/> while it is absent. The mod passes
    /// <see cref="DefaultsFile.PerUser"/>, and a test a scratch file.
    /// </summary>
    public static ConfigOwnerOptions<SonsOfTheForestConfig> Options(string path, string legacyPath, DefaultsFile defaults)
    {
        return new ConfigOwnerOptions<SonsOfTheForestConfig>
        {
            Path = path,
            Table = Table(),
            Import = LegacyConfigImport.Create(),
            LegacySourcePath = legacyPath,
            Header = new RenderHeader(DisplayName),
            Defaults = defaults,
        };
    }
}
