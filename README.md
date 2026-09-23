# Sons of the Forest Head Tracking

![Sons of the Forest running with this mod](https://raw.githubusercontent.com/itsloopyo/sons-of-the-forest-headtracking/main/assets/readme-clip.gif)

An unofficial head tracking mod for Sons of the Forest that moves the view with your head while your mouse or controller keeps aiming, driven by OpenTrack over UDP, with no VR headset required.

> [!CAUTION]
> **Experimental prototype - expect missing core features.**
> Head-tracked rotation and 6DOF position are working in-game, but on-screen
> reticle compensation is not yet implemented. Comfort tuning and edge cases
> are still in progress.

## Features

- **Decoupled look and aim** - head tracking moves the camera; aim stays on your mouse/controller
- **6DOF positional tracking** - lean and peek with head position
- **Works with any OpenTrack compatible tracker** - free options available for PC, iOS and Android

## Requirements

- [Sons of the Forest](https://store.steampowered.com/app/1326470/Sons_Of_The_Forest/) (Steam) - a legitimately purchased copy
- A tracking source - [OpenTrack](https://github.com/opentrack/opentrack) with a webcam, a phone tracking app, or a VR headset
- Windows 10/11, 64-bit

## Installation

### Lopari

Download [Lopari](https://lopari.app), choose **Sons of the Forest**, and click
**Play with head tracking**.

### Standalone Installer

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

The mod listens for OpenTrack pose data on UDP port `4242`, on every network
interface. One datagram is six little-endian 64-bit floats in the order
`x, y, z, yaw, pitch, roll`: position in centimetres, rotation in degrees, 48
bytes in total. Anything that sends that to that port drives the view.
OpenTrack's **UDP over network** output sends exactly this, and the steps below
set it up.

1. Install [OpenTrack](https://github.com/opentrack/opentrack/releases).
2. Pick a tracker under **Input**, using the notes below.
3. Set **Output** to **UDP over network**, host `127.0.0.1`, port `4242`.
4. Press **Start**. Tracking and the game can start in either order.

### Webcam

OpenTrack ships a `neuralnet tracker` input that reads a plain webcam. Select it
under **Input**, pick your camera in its settings, and use the output settings
above. How well it tracks depends on your camera and your lighting, so try it
before buying anything.

### Phone

A phone app can reach the mod directly, with no OpenTrack on the PC, if it sends
the datagram described above. Point it at this PC's IP address (run `ipconfig`
to find it) on port `4242`. Not every phone tracker speaks this protocol, so
check yours for an OpenTrack or UDP output option first. [Headcam](https://headcam.app)
sends it, and I wrote it so decent tracking is free for anyone who already owns
a phone.

Sending direct works when the app filters its own signal on the device. The
mod's smoothing is sized to take the edge off a clean signal rather than to
rescue a noisy one, so a raw feed sent direct will jitter. If it does, point the
app at OpenTrack's **UDP over network** *input* on some other port, say 5252,
and let OpenTrack's filters and curves clean it up before its output forwards to
`127.0.0.1:4242`.

Anything arriving from outside `127.0.0.0/8` counts as a remote connection and
is smoothed with `RemoteSmoothing` rather than `LocalSmoothing`. That includes a
tracker on this very PC that sends to the machine's own LAN address, because the
mod reads the source address and not the machine.

### Headset or other hardware

If your device has an OpenTrack input driver, select it under **Input** and use
the same output settings. OpenTrack's own **Input** list is the authority on
what it can read; the mod only ever sees what OpenTrack sends.

### Centring

Centring belongs to your tracker. The mod subtracts no centre of its own: it
applies the pose it receives exactly as it arrives, so a stream of zeros holds
the view where the game itself puts it. Press the centre control in your tracker
(OpenTrack's **Center** bind, or the CENTER button in Headcam) and the tracker
zeroes its own output, which leaves the view centred with the mod doing nothing.

That is why there is no centre hotkey here and nothing to re-centre in game. Two
centres in series would drift apart, because each side re-centres at moments the
other cannot see, and you would end up pressing twice to centre once. If the
view sits off to one side, centre it in the tracker.

## Controls

Two equivalent binding sets - use whichever your keyboard has:

| Action              | Nav-cluster | Chord           |
|---------------------|-------------|-----------------|
| Toggle tracking     | `End`       | `Ctrl+Shift+Y`  |
| Cycle tracking mode | `Page Up`   | `Ctrl+Shift+G`  |
| Toggle yaw mode     | `Page Down` | `Ctrl+Shift+H`  |

The mod applies the pose your tracker sends and keeps no centre of its own. To
recentre, use the centre control in your tracker app: Center in opentrack,
CENTER in Headcam, or the equivalent in whatever you run.

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

[Smoothing]
# Picked per connection from the packet source address. Both cover rotation
# and position. 0 = no smoothing, 1 = heavy.
# Tracker running on this machine (loopback):
LocalSmoothing = 0
# Tracker on a remote network device, e.g. a phone over WiFi:
RemoteSmoothing = 0.15

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
# Default on: converts OpenTrack axes to Unity (verified for Sons of the Forest).
InvertPositionX = true
InvertPositionY = false
InvertTrackerZ = false

[Hotkeys]
ToggleKey = End
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

- Raise `RemoteSmoothing` (phone or other network tracker) or `LocalSmoothing` (tracker on this PC) in the config file, try 0.3 to 0.5.
- Phone trackers over WiFi are the most jitter-prone; raise `RemoteSmoothing` further or use a wired tracker.

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

Prerequisites: Windows and [pixi](https://pixi.sh). No game install is needed: `scripts/setup-libs.ps1` resolves every compile reference from the vendored BepInEx archive, NuGet, and the checked-in stub sources.

```powershell
git clone --recursive https://github.com/itsloopyo/sons-of-the-forest-headtracking
cd sons-of-the-forest-headtracking
pixi run build
pixi run package
```

Output: `release/SonsOfTheForestHeadTracking-v<version>-installer.zip`.

## Community & Support

- Discord: [Loop's Head Tracking Hangout](https://discord.com/invite/dxyZdyFNT9) - setup help, bug reports, and new-release announcements
- [Lopari](https://lopari.app) - free Windows launcher with one-click install and launch for the released head-tracking mods
- [Headcam](https://headcam.app) - free app that turns your iPhone or Android phone into the head tracker

## License

MIT License - see [LICENSE](LICENSE) for details.

## Credits

- Sons of the Forest by Endnight Games
- [BepInEx](https://github.com/BepInEx/BepInEx) - IL2CPP mod loader
- [HarmonyX](https://github.com/BepInEx/HarmonyX) and [Il2CppInterop](https://github.com/BepInEx/Il2CppInterop) - runtime patching and IL2CPP interop, loaded via BepInEx
- [OpenTrack](https://github.com/opentrack/opentrack) - head tracking protocol

## Disclaimer

This mod is not affiliated with, endorsed by, or supported by Endnight Games. Use at your own risk.
