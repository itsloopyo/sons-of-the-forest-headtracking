@echo off
:: ============================================
:: Sons of the Forest - Uninstall
:: ============================================
:: Thin wrapper - uninstall body lives in cameraunlock-core/scripts/uninstall-body.cmd.

:: --- CONFIG BLOCK ---
set "GAME_ID=sons-of-the-forest"
set "MOD_DISPLAY_NAME=Sons of the Forest Head Tracking"
set "MOD_DLLS=SonsOfTheForestHeadTracking.dll CameraUnlock.Core.dll"
set "MOD_INTERNAL_NAME=SonsOfTheForestHeadTracking"
set "STATE_FILE=.headtracking-state.json"
set "FRAMEWORK_TYPE=BepInEx"
set "LEGACY_DLLS=SonsOfTheForestHeadTracking.pdb CameraUnlock.Core.Unity.dll"

:: --- Loader-specific config (leave the ones that don't apply blank) ---
set "MANAGED_SUBFOLDER="
set "ASSEMBLY_DLL="
set "MANAGED_EXTRAS="
set "ASI_LOADER_NAME=winmm.dll"
:: --- END CONFIG BLOCK ---

set "WRAPPER_DIR=%~dp0"
set "_BODY=%WRAPPER_DIR%shared\uninstall-body.cmd"
if not exist "%_BODY%" set "_BODY=%WRAPPER_DIR%..\cameraunlock-core\scripts\uninstall-body.cmd"
if not exist "%_BODY%" (
    echo ERROR: uninstall-body.cmd not found in shared\ or ..\cameraunlock-core\scripts\.
    echo If this is a release ZIP, re-download it from GitHub ^(corrupt installer^).
    echo If this is the dev tree, run: git submodule update --init --recursive
    exit /b 1
)
call "%_BODY%" %*
exit /b %errorlevel%
