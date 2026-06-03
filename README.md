# Sons of the Forest Head Tracking

Head tracking for Sons of the Forest using any OpenTrack-compatible tracker (webcam, phone, or VR headset): move your head to look around while your mouse or controller keeps aiming, no VR required.

<!-- ![Mod GIF](https://raw.githubusercontent.com/itsloopyo/sons-of-the-forest-headtracking/main/assets/readme-clip.gif) -->

> [!CAUTION]
> **Experimental prototype - expect missing core features.**
> Head-tracked rotation and 6DOF position are working in-game, but on-screen
> reticle compensation is not yet implemented. Comfort tuning and edge cases
> are still in progress.

## Features

- **Decoupled look and aim** - head tracking moves the camera; aim stays on your mouse/controller
- **6DOF positional tracking** - lean and peek with head position

## Requirements

- [Sons of the Forest](https://store.steampowered.com/app/1326470/Sons_Of_The_Forest/) (Steam) - a legitimately purchased copy
- A tracking source - [OpenTrack](https://github.com/opentrack/opentrack) with a webcam, a phone tracking app, or a VR headset
- Windows 10/11, 64-bit

## Installation

1. Download the latest `SonsOfTheForestHeadTracking-vX.Y.Z-installer.zip` from the [Releases page](https://github.com/itsloopyo/sons-of-the-forest-headtracking/releases).
2. Extract it anywhere.
3. Double-click `install.cmd`. It locates your Steam install of Sons of the Forest, installs BepInEx 6 IL2CPP (bundled) if you don't already have it, and deploys the mod to `BepInEx/plugins/`.
4. Configure OpenTrack to output UDP to `127.0.0.1:4242` (see [Setting Up OpenTrack](#setting-up-opentrack)).
5. Launch the game.

If the installer can't find your game, point it at your install folder either way:

```bat
:: Option 1: pass the path as an argument
install.cmd "D:\Games\Sons Of The Forest"

:: Option 2: set an environment variable, then run install.cmd
set SONS_OF_THE_FOREST_PATH=D:\Games\Sons Of The Forest
install.cmd
```

### Manual Installation

1. Install [BepInEx 6 IL2CPP x64](https://github.com/BepInEx/BepInEx) into the Sons of the Forest game folder. The installer ZIP bundles a known-good copy at `vendor/bepinex/BepInEx_UnityIL2CPP_x64.zip`; extract it to the game root.
2. Launch the game once so BepInEx initializes.
3. Copy `SonsOfTheForestHeadTracking.dll` and `CameraUnlock.Core.dll` into `<game folder>/BepInEx/plugins/`.

Alternatively, the Nexus ZIP (`SonsOfTheForestHeadTracking-vX.Y.Z-nexus.zip`) contains only the plugin files: extract it over the game folder. You must already have BepInEx 6 IL2CPP installed.

## Setting Up OpenTrack

In OpenTrack:

- Output: `UDP over network`
- Address: `127.0.0.1`, Port: `4242`
- Input: whichever tracker you use (see below)

### VR Headset Setup

If you have a VR headset but want to play on a flat screen:

1. Connect the headset to your PC (Quest: Air Link or Virtual Desktop).
2. Start SteamVR.
3. In OpenTrack, set Input to `SteamVR` and start tracking.

### Webcam Setup

1. In OpenTrack, set Input to `neuralnet tracker`.
2. Select your webcam and start tracking.

### Phone App Setup

Phone apps (SmoothTrack, Smartphone Head Tracker, and similar) can send directly to the game:

- Point the app at your PC's LAN IP address, port `4242`. Most apps smooth the data themselves, so no OpenTrack relay is needed.
- If you want OpenTrack's curve mapping and filtering, send the phone data to OpenTrack instead and have OpenTrack output UDP to `127.0.0.1:4242`.

## Controls

Two equivalent binding sets - use whichever your keyboard has:

| Action              | Nav-cluster | Chord           |
|---------------------|-------------|-----------------|
| Recenter            | `Home`      | `Ctrl+Shift+T`  |
| Toggle tracking     | `End`       | `Ctrl+Shift+Y`  |
| Cycle tracking mode | `Page Up`   | `Ctrl+Shift+G`  |
| Toggle yaw mode     | `Page Down` | `Ctrl+Shift+H`  |

`Page Up` / `Ctrl+Shift+G` cycles tracking mode:

1. Normal head-tracked gameplay
2. Positional tracking disabled, rotational tracking enabled
3. Rotational tracking disabled, positional tracking enabled
4. Back to normal

## Configuration

The config file is created after the first launch with the mod installed:

`<game folder>/BepInEx/config/com.cameraunlock.sonsoftheforest.headtracking.cfg`

```ini
[General]
# Enable head tracking automatically when the game starts.
EnabledOnStartup = true
# true = horizon-locked yaw (rotates around world up). false = camera-local yaw.
WorldSpaceYaw = true

[Sensitivity]
YawSensitivity = 1
PitchSensitivity = 1
RollSensitivity = 1
# Rotation smoothing (0 = none, 1 = heavy). A 0.15 floor is applied internally.
Smoothing = 0

[CoordinateTransform]
# Flip an axis if it moves the wrong way.
InvertYaw = false
# Default on: converts OpenTrack pitch to Unity.
InvertPitch = true
InvertRoll = false

[Position]
# Positional (6DOF) tracking - lean and move your head to shift the camera.
PositionEnabled = true
PositionSensitivityX = 1
PositionSensitivityY = 1
PositionSensitivityZ = 1
# Maximum displacement in meters.
PositionLimitX = 0.3
PositionLimitY = 0.2
PositionLimitZ = 0.4
# Backward lean limit (small, prevents clipping into the player).
PositionLimitZBack = 0.1
PositionSmoothing = 0.15
# Default on: converts OpenTrack axes to Unity (verified for Sons of the Forest).
InvertPositionX = true
InvertPositionY = false
InvertPositionZ = true

[Hotkeys]
ToggleKey = End
RecenterKey = Home
YawModeKey = PageDown
PositionToggleKey = PageUp
```

## Troubleshooting

**Mod not loading**

- Check `<game folder>/BepInEx/LogOutput.log` for a "loaded successfully" line. If the file or line is absent, BepInEx did not load: re-run `install.cmd`.
- The first launch after installing BepInEx takes noticeably longer while it generates interop assemblies. Let it finish.

**No tracking response**

- Confirm OpenTrack is started (the octopus moves) and its output is `UDP over network` to `127.0.0.1:4242`.
- For phone trackers, point the app at this PC's LAN IP (not 127.0.0.1) and allow UDP port 4242 through Windows Firewall.
- Press `End` (or `Ctrl+Shift+Y`) in case tracking was toggled off.

**Jittery / unstable tracking**

- Raise `Smoothing` in the config file (try 0.3 to 0.5).
- Phone trackers over WiFi are the most jitter-prone; raise smoothing further or use a wired tracker.

**Wrong rotation axis**

- Toggle `InvertYaw` / `InvertPitch` / `InvertRoll` in the config file.
- If yaw feels wrong when looking up or down at extreme angles, toggle between world-locked and camera-local yaw with `Page Down` (or `Ctrl+Shift+H`).

**Crosshair drifts when looking around**

- Aim stays on the mouse by design; on-screen reticle compensation is not yet implemented in this prototype.

## Updating

Download the new release and run `install.cmd` again. Your config is preserved.

## Uninstalling

Run `uninstall.cmd`. This removes the mod DLLs. BepInEx is only removed if the installer put it there; use `uninstall.cmd /force` to remove it anyway.

## Building from Source

Prerequisites: Windows, [pixi](https://pixi.sh), and a local Sons of the Forest install (the build references the game's IL2CPP interop assemblies).

```powershell
git clone --recursive https://github.com/itsloopyo/sons-of-the-forest-headtracking
cd sons-of-the-forest-headtracking
pixi run build
pixi run package
```

Output: `release/SonsOfTheForestHeadTracking-v<version>-installer.zip`.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Credits

- Sons of the Forest by Endnight Games
- [BepInEx](https://github.com/BepInEx/BepInEx) - IL2CPP mod loader
- [HarmonyX](https://github.com/BepInEx/HarmonyX) and [Il2CppInterop](https://github.com/BepInEx/Il2CppInterop) - runtime patching and IL2CPP interop, loaded via BepInEx
- [OpenTrack](https://github.com/opentrack/opentrack) - head tracking protocol

## Disclaimer

This mod is not affiliated with, endorsed by, or supported by Endnight Games. Use at your own risk.
