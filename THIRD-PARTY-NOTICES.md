# Third-Party Notices

This mod itself is licensed under the MIT License - see [LICENSE](LICENSE).

It includes or depends on the following third-party components.

## BepInEx 6 (IL2CPP bleeding edge)

- **Version:** 6.0.0-be.755
- **License:** LGPL-2.1
- **Upstream:** https://github.com/BepInEx/BepInEx
- **Usage:** Mod loader and plugin framework hosting this mod inside the game process.
- **Bundled:** yes. Bundled in the release ZIP and used as the install-time source.

LGPL-2.1 source obligations are satisfied by linking to the upstream
repository; we ship the upstream binary unmodified.

---

## HarmonyX

- **Version:** As bundled with BepInEx 6.0.0-be.755
- **License:** MIT
- **Upstream:** https://github.com/BepInEx/HarmonyX
- **Usage:** Runtime patching library referenced at build time and loaded as a dependency of BepInEx; not modified.
- **Bundled:** yes. Ships inside the bundled BepInEx package.

---

## Il2CppInterop

- **Version:** As bundled with BepInEx 6.0.0-be.755
- **License:** LGPL-3.0
- **Upstream:** https://github.com/BepInEx/Il2CppInterop
- **Usage:** IL2CPP managed interop layer; this mod compiles against its generated proxy assemblies and loads it as a dependency of BepInEx 6 IL2CPP; not modified.
- **Bundled:** yes. Ships inside the bundled BepInEx package.

---

## OpenTrack

- **Version:** N/A (UDP wire protocol only)
- **License:** ISC
- **Upstream:** https://github.com/opentrack/opentrack
- **Usage:** The mod implements the OpenTrack UDP wire protocol to receive head pose data; no OpenTrack source or binaries are redistributed.
- **Bundled:** no.

---

## Game credit

Sons of the Forest is developed by Endnight Games. This mod is an
unofficial fan project and is not affiliated with or endorsed by
Endnight Games. A legitimately purchased copy of the game is required.
