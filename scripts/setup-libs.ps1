#!/usr/bin/env pwsh
# Populate src/SonsOfTheForestHeadTracking/libs/ with the compile-time references the mod
# needs.
#
# Sources are REPO FILES ONLY - never a game install, never the network. A contributor who
# owns Sons of the Forest and a CI runner that does not must compile against byte-identical
# references, otherwise a member missing from a stub passes locally and fails on push.
#
# - BepInEx 6 comes out of vendor/bepinex, the same archive install.cmd deploys, so the mod
#   is compiled against the exact loader build its users run and there is no second version
#   to keep in step. Il2CppInterop comes out of the NuGet cache at the version the
#   PackageReferences in SonsOfTheForestHeadTracking.csproj name, which is the version the
#   cameraunlock-core stubs below compile against.
#
# - The Unity module proxies cannot come from NuGet. unityengine.modules ships the real Mono
#   reference assemblies, and this mod compiles against the Il2CppInterop-generated shape
#   instead: engine classes derive from Il2CppSystem.Object and take an IntPtr constructor,
#   arrays are Il2CppReferenceArray<T>, and System.Type parameters are Il2CppSystem.Type.
#   They are compiled from cameraunlock-core/csharp/stubs/il2cpp, which every IL2CPP mod in
#   the fleet shares.
#
# - Sons.dll is the game's own assembly, stubbed down to the single member this mod uses
#   (TheForest.Utils.LocalPlayer.IsInWorld) in stubs/SonsStubs.cs.
#
# libs/ is wiped first so a local run reproduces the runner's empty-libs/ start and a stale
# DLL cannot survive into the build.
#
# Run order: dotnet restore -> setup -> dotnet build. The pixi `build` task wires this, and
# CI calls this same script through `pixi run package`.

$ErrorActionPreference = "Stop"

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$libsPath    = Join-Path $projectRoot "src/SonsOfTheForestHeadTracking/libs"

New-Item -ItemType Directory -Path $libsPath -Force | Out-Null
Get-ChildItem $libsPath -Filter '*.dll' -File | Remove-Item -Force

$vendorZip = Join-Path $projectRoot 'vendor/bepinex/BepInEx_UnityIL2CPP_x64.zip'
if (-not (Test-Path $vendorZip)) {
    throw "Bundled BepInEx vendor zip missing: $vendorZip. Run 'pixi run update-deps' to refresh."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($vendorZip)
try {
    foreach ($dll in @('BepInEx.Core.dll', 'BepInEx.Unity.IL2CPP.dll')) {
        $entry = $archive.Entries | Where-Object { $_.FullName -eq "BepInEx/core/$dll" }
        if (-not $entry) { throw "Vendored BepInEx zip has no BepInEx/core/$dll" }
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile(
            $entry, (Join-Path $libsPath $dll), $true)
    }
}
finally {
    $archive.Dispose()
}

$nugetRoot = (& dotnet nuget locals global-packages -l) -replace '^global-packages: ', ''
if (-not (Test-Path $nugetRoot)) {
    throw "NuGet global-packages root not found: $nugetRoot. Run 'dotnet restore' first."
}

$packageDlls = @(
    @{ Pkg = 'il2cppinterop.runtime/1.5.3';        Path = 'lib/net6.0/Il2CppInterop.Runtime.dll' },
    @{ Pkg = 'il2cppinterop.referencelibs/1.0.0';  Path = 'lib/net6.0/Il2Cppmscorlib.dll' }
)

foreach ($entry in $packageDlls) {
    $src = Join-Path $nugetRoot ("$($entry.Pkg)/$($entry.Path)")
    if (-not (Test-Path $src)) {
        throw "Missing NuGet asset: $src. Did 'dotnet restore' complete?"
    }
    Copy-Item $src $libsPath -Force
}

$stubProjects = @(
    'cameraunlock-core/csharp/stubs/il2cpp/UnityEngine.CoreModule/Stubs.UnityEngine.CoreModule.csproj',
    'cameraunlock-core/csharp/stubs/il2cpp/UnityEngine.InputLegacyModule/Stubs.UnityEngine.InputLegacyModule.csproj',
    'cameraunlock-core/csharp/stubs/il2cpp/UnityEngine.VideoModule/Stubs.UnityEngine.VideoModule.csproj',
    'stubs/Sons/Stubs.Sons.csproj'
)

foreach ($relative in $stubProjects) {
    $proj = Join-Path $projectRoot $relative
    if (-not (Test-Path $proj)) { throw "Stub project not found: $proj" }

    $output = & dotnet build $proj -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        $output | Write-Host
        throw "Failed to build stub project $relative"
    }

    $name = [System.IO.Path]::GetFileNameWithoutExtension($proj) -replace '^Stubs\.', ''
    $built = Join-Path (Split-Path -Parent $proj) "bin/Release/net6.0/$name.dll"
    if (-not (Test-Path $built)) { throw "Stub build produced no $name.dll at $built" }
    Copy-Item $built $libsPath -Force
}

$dlls = Get-ChildItem $libsPath -Filter '*.dll'
Write-Host "Populated $($dlls.Count) DLLs in $libsPath" -ForegroundColor Green
$dlls | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Gray }
