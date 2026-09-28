# Changelog

All notable changes to this project are documented here. Dev builds are
published as a rolling `dev` pre-release and track the Unreleased section
below; a dated entry is added when a versioned release is cut.

## [Unreleased]

**Settings have moved.** This version keeps its settings in `BepInEx\config\CameraUnlock.ini`. The first time it starts it reads your settings from the old `BepInEx\config\com.cameraunlock.sonsoftheforest.headtracking.cfg` into the new file, and leaves the old file as it was. BepInEx's ConfigurationManager no longer lists the settings: edit `CameraUnlock.ini` with any text editor. The entries under Changed have the details.

### Added
- A setting set to `default` in `CameraUnlock.ini` takes its value from `Defaults.ini`, which every head tracking mod that keeps its settings in `CameraUnlock.ini` reads. Head tracking mods that keep their settings in another file do not read it, and neither do earlier versions of this mod. Writing a value in place of `default` changes that setting for this game only. When the mod saves a setting that a hotkey changed in game, it writes the new value in place of `default`, so that setting no longer follows `Defaults.ini` in this game until you set it to `default` again.
- `Defaults.ini` is `%AppData%\CameraUnlock\Defaults.ini` on Windows; `$XDG_CONFIG_HOME/CameraUnlock/Defaults.ini` on Linux, or `~/.config/CameraUnlock/Defaults.ini` where `XDG_CONFIG_HOME` is not set, under Wine and Proton too; and `~/Library/Application Support/CameraUnlock/Defaults.ini` on macOS. The mod's log, where it writes one, names the file it read.
- When the mod starts and finds no `Defaults.ini`, it creates one holding the built-in values, unless Windows runs the game as a packaged app, or the game runs on Linux or macOS without Wine or Proton. The mod never changes `Defaults.ini` after that.
- The tracking mode hotkey (`Page Up` / `Ctrl+Shift+G`) and the yaw mode hotkey (`Page Down` / `Ctrl+Shift+H`) save their state to `CameraUnlock.ini`, so the next start begins with the mode you left. `End` still changes the session only.
- Head tracking for Sons of the Forest (Unity 2022.2 IL2CPP + HDRP) via
  BepInEx 6, built on CameraUnlock.Core.
- Decoupled look and aim: head moves the rendered view while game aim and
  raycasts read the clean `LocalPlayer` look state and game-owned camera
  rotation, so only the image gets the head pose.
- 6DOF tracking via CameraUnlock's split injection - rotation through the
  view matrix, position through the camera transform, applied per-frame to
  all live cameras in `LateUpdate`.
- Runtime tracking-mode cycling: 6DOF (rotation + position) -> rotation only
  -> position only.
- World-space (horizon-locked) and camera-local yaw modes, switchable at
  runtime.
- Gameplay gating on `LocalPlayer.IsInWorld` and `Time.timeScale`, with a clean
  view restore when gated.
- OpenTrack UDP receiver (port 4242) driving `HeadTrackingSession`:
  two-parameter smoothing (`LocalSmoothing` 0.0 / `RemoteSmoothing` 0.15,
  selected per connection from the packet source address), sample-rate
  interpolation, and tracking-loss hold.
- Hotkeys (with Ctrl+Shift chord alternatives): End to toggle, Page Up to
  cycle tracking mode, Page Down to toggle yaw mode.

### Changed
- Settings move to `BepInEx\config\CameraUnlock.ini`. Earlier versions of the mod kept these settings in `com.cameraunlock.sonsoftheforest.headtracking.cfg`, in the same folder. The first time this version starts and finds no `CameraUnlock.ini`, it reads your settings from `com.cameraunlock.sonsoftheforest.headtracking.cfg` and writes them into `CameraUnlock.ini`. It never changes `com.cameraunlock.sonsoftheforest.headtracking.cfg`, and does not read it again while `CameraUnlock.ini` exists.
- A setting that the defaults the README shows set to `default` is written as `default` when you never changed it from the default earlier versions used, because `com.cameraunlock.sonsoftheforest.headtracking.cfg` does not hold it or holds that default. It then follows `Defaults.ini`, so it takes the value `Defaults.ini` gives it, or the built-in value where `Defaults.ini` gives none, which can differ from the default earlier versions used. A setting you changed is written with the value imported for it, or as `default` where that value equals its default at that start.
- `RotationEnabled` and `PositionEnabled` are one setting here, the tracking mode, so both are written as `default` or neither is.
- Comments, and keys the mod never read, are not carried over. Nor are these, where your old file had them:
  - A sensitivity or axis inversion you changed from its default. Set these in your tracker instead.
  - A hotkey set to Ctrl, Shift or Alt on its own. That key goes down before the key of any chord made with it, so the hotkey is left unbound, and it keeps its Ctrl+Shift chord.
  - A hotkey set to a number that is not a key code Unity names (for example `ToggleKey = 999`). The hotkey is left unbound, the log says so, and it keeps its Ctrl+Shift chord.
- An older version of the mod reads `com.cameraunlock.sonsoftheforest.headtracking.cfg` and never reads `CameraUnlock.ini`, so a setting you change after updating is not in `com.cameraunlock.sonsoftheforest.headtracking.cfg`.
- Deleting only `CameraUnlock.ini` makes the next start read `com.cameraunlock.sonsoftheforest.headtracking.cfg` again. To go back to the defaults, replace everything in `CameraUnlock.ini` with the defaults the README shows. Every setting they set to `default` then follows `Defaults.ini`.
- BepInEx's ConfigurationManager no longer lists these settings. Edit `BepInEx\config\CameraUnlock.ini` with any text editor.
- Hotkeys are written as key names, and each hotkey lists every key that triggers it, the Ctrl+Shift chord included: `ToggleKey=End, Ctrl+Shift+Y`. The tracking mode hotkey is `CycleTrackingModeKey`, which was `PositionToggleKey`.
- A hotkey bound to a plain key no longer fires while Ctrl and Shift are both held, so Ctrl+Shift with that key reaches only a binding that names the chord.
- `PositionLimitYDown`, the downward limit, is a setting of its own. Earlier versions used `PositionLimitY` for both directions, and the import writes that value into both.
- On Linux and macOS without Wine or Proton, this version reads its settings and saves none: it creates no `CameraUnlock.ini`, reads your settings from `com.cameraunlock.sonsoftheforest.headtracking.cfg` again at every start while there is no `CameraUnlock.ini`, and a change made in game lasts until the game closes.
- Building no longer needs Sons of the Forest installed. `scripts/setup-libs.ps1`
  used to copy its compile references out of `<game>/BepInEx/core` and
  `<game>/BepInEx/interop`, so a contributor without the game, and CI, could not
  build at all. It now resolves BepInEx from the vendored loader archive,
  Il2CppInterop from NuGet, and the Il2CppInterop-shaped UnityEngine modules and
  `Sons` from checked-in stub sources. The built assembly's external references
  are byte-identical to the game-built one, member for member and signature for
  signature; only the Il2CppInterop.Runtime reference moves, from the 1.5.1 in a
  local game install to the 1.5.3 the vendored BepInEx actually ships.
- `.github/workflows/build.yml` builds and packages through `pixi run package`
  instead of only validating, which is what catches a stub drifting from the
  shipped interop shape.
- `BepInEx/LogOutput.log` no longer fills with head-tracking chatter. The pose
  dump that ran every 120 frames for the whole session now stops after 10
  lines, and the "tracking gated" line is written when the gate changes instead
  of every 300 frames. A long session used to add roughly 380 KB an hour of
  repeated state.
- The startup log now names the UDP port the mod listens on, so a report of
  "no tracking" can be diagnosed from the log alone.
- The mod no longer keeps a centre of its own and applies the tracker pose as
  absolute. Every tracker app centres itself, so a mod-side centre sat in series
  with the tracker's and the two drifted apart. Centre in your tracker app
  instead. The recentre hotkey and its `RecenterKey` config entry are gone with
  it.
- Replaced the single `Smoothing` config key with `LocalSmoothing` (default
  0.0) and `RemoteSmoothing` (default 0.15), selected per connection from the
  packet source address.
- Removed the `PositionSmoothing` key: position now uses the same
  connection-selected value as rotation.
- Removed the hidden 0.15 baseline smoothing floor, so local trackers get
  zero-latency tracking by default.

### Removed
- The sensitivity and axis inversion settings (`YawSensitivity`, `PitchSensitivity`, `RollSensitivity`, `InvertYaw`, `InvertPitch`, `InvertRoll`, `PositionSensitivityX/Y/Z`, `InvertPositionX`, `InvertPositionY`, `InvertTrackerZ`). Set these in your tracker app instead.
- With these settings at their shipped defaults the camera moves as it did before: the pitch and sideways-lean conversions the shipped `InvertPitch=true` and `InvertPositionX=true` made are now part of the mod.
