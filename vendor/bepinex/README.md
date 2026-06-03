# bepinex (vendored - IL2CPP bleeding edge)

Bundled copy of BepInEx 6 IL2CPP, the install-time source of truth for Sons of the Forest.
Refresh manually with `pixi run update-deps`, then commit.

## Snapshot

- Asset: `BepInEx_UnityIL2CPP_x64.zip`
- Build: `6.0.0-be.755` (BepInEx CI build server, NOT GitHub releases)
- Upstream URL: https://builds.bepinex.dev/projects/bepinex_be/755/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.755%2B3fab71a.zip
- SHA-256: `3616d6a67f5f595973ec4aa7bd7edaf7f799d5bb9926f7146a6dcc7b4abf478f`
- Fetched at: 2026-05-31T01:09:37.6929792+01:00

BepInEx 6 IL2CPP is bleeding edge and only published on builds.bepinex.dev. update-deps
auto-bumps to whatever the latest IL2CPP-win-x64 build is. If you need to pin to a
specific build (because a newer one regresses for SotF), hardcode the build number in
update-deps.ps1.
