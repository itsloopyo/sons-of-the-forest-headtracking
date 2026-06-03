@echo off
:: ============================================
:: Sons of the Forest - Install
:: ============================================
:: Thin wrapper - install body lives in cameraunlock-core/scripts/install-body-bepinex.cmd.

:: --- CONFIG BLOCK ---
set "GAME_ID=sons-of-the-forest"
set "MOD_DISPLAY_NAME=Sons of the Forest Head Tracking"
set "MOD_DLLS=SonsOfTheForestHeadTracking.dll CameraUnlock.Core.dll"
set "MOD_INTERNAL_NAME=SonsOfTheForestHeadTracking"
set "MOD_VERSION=0.0.0"
set "STATE_FILE=.headtracking-state.json"
set "FRAMEWORK_TYPE=BepInEx"
set "BEPINEX_ARCH=x64"
set "BEPINEX_VENDOR_ZIP_NAME=BepInEx_UnityIL2CPP_x64.zip"
set "BEPINEX_SUBFOLDER="
set "MOD_CONTROLS=Controls:&echo   Home      or Ctrl+Shift+T - Recenter&echo   End       or Ctrl+Shift+Y - Toggle on/off&echo   Page Up   or Ctrl+Shift+G - Cycle tracking mode&echo   Page Down or Ctrl+Shift+H - Toggle yaw mode (world/local)"
:: --- END CONFIG BLOCK ---

set "WRAPPER_DIR=%~dp0"
set "_BODY=%WRAPPER_DIR%shared\install-body-bepinex.cmd"
if not exist "%_BODY%" set "_BODY=%WRAPPER_DIR%..\cameraunlock-core\scripts\install-body-bepinex.cmd"
if not exist "%_BODY%" (
    echo ERROR: install-body-bepinex.cmd not found in shared\ or ..\cameraunlock-core\scripts\.
    echo If this is a release ZIP, re-download it from GitHub ^(corrupt installer^).
    echo If this is the dev tree, run: git submodule update --init --recursive
    exit /b 1
)
call "%_BODY%" %*
exit /b %errorlevel%
