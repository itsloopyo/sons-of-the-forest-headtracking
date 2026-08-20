# Changelog

## [0.1.0] - 2026-08-20

### Fixed

- show full control set in pixi install via shared -Controls
- migrate to the per-connection smoothing pair in cameraunlock-core
- restore the forward lean budget; InvertPositionZ becomes InvertTrackerZ
- drop the mod-side centre and cut log noise

### Other

- Hello world
- Ship launcher-manifest.json in installer ZIP and stamp version on release

All notable changes to this project are documented here. Dev builds are
published as a rolling `dev` pre-release and track the Unreleased section
below; a dated entry is added when a versioned release is cut.

## [Unreleased]

### Added
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
