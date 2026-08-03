# bepinex (vendored - IL2CPP bleeding edge)

Bundled copy of BepInEx 6 IL2CPP, the install-time source of truth for Sons of the Forest.
Refresh manually with `pixi run update-deps`, then commit.

## Snapshot

- Asset: `BepInEx_UnityIL2CPP_x64.zip`
- Build: `6.0.0-be.785` (BepInEx CI build server, NOT GitHub releases)
- Upstream URL: https://builds.bepinex.dev/projects/bepinex_be/785/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.785%2B6abdba4.zip
- SHA-256: `2a7cbf74d26abe4765c3e662db1721b923bac39849ebfef2ca5dc7de7e2d9b7f`
- Fetched at: 2026-08-03T11:49:38.9030273+01:00

BepInEx 6 IL2CPP is bleeding edge and only published on builds.bepinex.dev. update-deps
auto-bumps to whatever the latest IL2CPP-win-x64 build is. If you need to pin to a
specific build (because a newer one regresses for SotF), hardcode the build number in
update-deps.ps1.
