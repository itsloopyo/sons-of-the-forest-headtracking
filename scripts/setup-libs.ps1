#!/usr/bin/env pwsh
# Populate src/SonsOfTheForestHeadTracking/libs/ with compile-time references
# copied from the game's own BepInEx install:
#
# - BepInEx/core/      -> BepInEx + Il2CppInterop runtime DLLs (exact versions the
#                         mod will run against).
# - BepInEx/interop/   -> Il2CppInterop-generated proxy assemblies for UnityEngine
#                         and the game's own code (Sons.dll, Endnight.dll). These are
#                         the ONLY Unity references that bind correctly at runtime
#                         under IL2CPP - NuGet UnityEngine reference assemblies
#                         typecheck but throw MissingMethodException for events,
#                         arrays, and System.Type parameters.
#
# The game must have been launched at least once with BepInEx installed so the
# interop assemblies exist.
#
# Run order: setup-libs -> dotnet restore -> dotnet build.

$ErrorActionPreference = "Stop"

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$libsPath    = Join-Path $projectRoot "src/SonsOfTheForestHeadTracking/libs"

$coreDlls = @(
    'BepInEx.Core.dll',
    'BepInEx.Unity.IL2CPP.dll',
    'BepInEx.Unity.Common.dll',
    'Il2CppInterop.Runtime.dll',
    '0Harmony.dll'
)

$interopDlls = @(
    'Il2Cppmscorlib.dll',
    'Il2CppSystem.dll',
    'Il2CppSystem.Core.dll',
    'UnityEngine.dll',
    'UnityEngine.CoreModule.dll',
    'UnityEngine.InputLegacyModule.dll',
    'UnityEngine.PhysicsModule.dll',
    'UnityEngine.VideoModule.dll',
    'UnityEngine.AnimationModule.dll',
    'UnityEngine.UI.dll',
    'Sons.dll',
    'Endnight.dll'
)

$allDlls = $coreDlls + $interopDlls
$missing = $allDlls | Where-Object { -not (Test-Path (Join-Path $libsPath $_)) }
if (-not $missing) {
    Write-Host "libs/ already populated with all $($allDlls.Count) references; skipping copy from game install." -ForegroundColor Green
    return
}

Import-Module (Join-Path $projectRoot 'cameraunlock-core/powershell/GamePathDetection.psm1') -Force
$gamePath = Find-GamePath -GameId 'sons-of-the-forest'
if (-not $gamePath) {
    throw "Sons of the Forest install not found. Pass the game path or install the game."
}

$coreDir    = Join-Path $gamePath 'BepInEx/core'
$interopDir = Join-Path $gamePath 'BepInEx/interop'

if (-not (Test-Path $interopDir)) {
    throw "Interop assemblies not found at $interopDir. Launch the game once with BepInEx installed so Il2CppInterop generates them."
}

New-Item -ItemType Directory -Path $libsPath -Force | Out-Null

foreach ($dll in $coreDlls) {
    $src = Join-Path $coreDir $dll
    if (-not (Test-Path $src)) { throw "Missing BepInEx core DLL: $src" }
    Copy-Item $src $libsPath -Force
}

foreach ($dll in $interopDlls) {
    $src = Join-Path $interopDir $dll
    if (-not (Test-Path $src)) { throw "Missing interop DLL: $src" }
    Copy-Item $src $libsPath -Force
}

$dlls = Get-ChildItem $libsPath -Filter '*.dll'
Write-Host "Populated $($dlls.Count) DLLs in $libsPath" -ForegroundColor Green
$dlls | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Gray }
