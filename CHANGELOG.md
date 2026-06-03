# Changelog

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
- Gameplay gating on `LocalPlayer.IsInWorld` and `Time.timeScale`, with
  auto-recenter on entering the world and a clean view restore when gated.
- OpenTrack UDP receiver (port 4242) driving `HeadTrackingSession`:
  smoothing (0.15 baseline floor), sample-rate interpolation, auto-recenter
  on first connection, and tracking-loss hold.
- Hotkeys (with Ctrl+Shift chord alternatives): End to toggle, Home to
  recenter, Page Up to cycle tracking mode, Page Down to toggle yaw mode.
