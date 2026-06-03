#!/usr/bin/env pwsh
#Requires -Version 5.1
# Repo consistency and integrity checks for Sons of the Forest Head Tracking.
# Run via `pixi run test`. Exits 1 if any check fails, 0 if all pass.
#
# These checks cover the regression classes that have actually bitten the
# CameraUnlock mod catalogue: version drift between the csproj / plugin source /
# installer, DLL-list drift between install and uninstall, LF line endings
# silently breaking .cmd files, and a tampered or corrupted vendored loader.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

$failures = New-Object System.Collections.Generic.List[string]

function Test-Check {
    param([string]$Name, [bool]$Condition, [string]$Detail)
    if ($Condition) {
        Write-Host "  PASS  $Name" -ForegroundColor Green
    } else {
        Write-Host "  FAIL  $Name" -ForegroundColor Red
        Write-Host "        $Detail" -ForegroundColor Yellow
        $script:failures.Add("${Name}: $Detail")
    }
}

function Get-FirstMatch {
    param([string]$Path, [string]$Pattern)
    $content = Get-Content $Path -Raw
    if ($content -match $Pattern) { return $matches[1] }
    return $null
}

Write-Host "=== Sons of the Forest Head Tracking - Repo Validation ===" -ForegroundColor Cyan
Write-Host ""

$csprojPath    = Join-Path $projectRoot "src\SonsOfTheForestHeadTracking\SonsOfTheForestHeadTracking.csproj"
$pluginPath    = Join-Path $projectRoot "src\SonsOfTheForestHeadTracking\Plugin.cs"
$pixiPath      = Join-Path $projectRoot "pixi.toml"
$installPath   = Join-Path $projectRoot "scripts\install.cmd"
$uninstallPath = Join-Path $projectRoot "scripts\uninstall.cmd"
$packagePath   = Join-Path $projectRoot "scripts\package-release.ps1"
$deployPath    = Join-Path $projectRoot "scripts\deploy.ps1"
$vendorDir     = Join-Path $projectRoot "vendor\bepinex"
$coreRoot      = Join-Path $projectRoot "cameraunlock-core"

# --- 1. Version consistency ---------------------------------------------------

Write-Host "Version consistency" -ForegroundColor Cyan

$csprojVersion  = Get-FirstMatch $csprojPath '<Version>([^<]+)</Version>'
$pluginVersion  = Get-FirstMatch $pluginPath 'PluginVersion = "([^"]+)"'
$installVersion = Get-FirstMatch $installPath 'set "MOD_VERSION=([^"]+)"'
$pixiVersion    = Get-FirstMatch $pixiPath '(?m)^version = "([^"]+)"'

Test-Check "csproj <Version> present" ($null -ne $csprojVersion) "No <Version> element in $csprojPath"
Test-Check "Plugin.cs PluginVersion present" ($null -ne $pluginVersion) "No PluginVersion constant in $pluginPath"
Test-Check "install.cmd MOD_VERSION present" ($null -ne $installVersion) "No MOD_VERSION in $installPath"

Test-Check "Plugin.cs matches csproj ($csprojVersion)" ($pluginVersion -eq $csprojVersion) `
    "Plugin.cs has '$pluginVersion', csproj has '$csprojVersion'"
Test-Check "install.cmd matches csproj ($csprojVersion)" ($installVersion -eq $csprojVersion) `
    "install.cmd has '$installVersion', csproj has '$csprojVersion'"
Test-Check "pixi.toml matches csproj ($csprojVersion)" ($pixiVersion -eq $csprojVersion) `
    "pixi.toml has '$pixiVersion', csproj has '$csprojVersion'"

# --- 2. Mod DLL list consistency ----------------------------------------------

Write-Host ""
Write-Host "Mod DLL list consistency" -ForegroundColor Cyan

$installDlls   = (Get-FirstMatch $installPath 'set "MOD_DLLS=([^"]+)"') -split '\s+'
$uninstallDlls = (Get-FirstMatch $uninstallPath 'set "MOD_DLLS=([^"]+)"') -split '\s+'

Test-Check "install.cmd and uninstall.cmd MOD_DLLS match" `
    (($installDlls -join ' ') -eq ($uninstallDlls -join ' ')) `
    "install.cmd: '$($installDlls -join ' ')' vs uninstall.cmd: '$($uninstallDlls -join ' ')'"

$packageContent = Get-Content $packagePath -Raw
$deployContent  = Get-Content $deployPath -Raw
foreach ($dll in $installDlls) {
    Test-Check "package-release.ps1 stages $dll" ($packageContent -match [regex]::Escape($dll)) `
        "$dll is in install.cmd MOD_DLLS but not referenced by package-release.ps1"
    Test-Check "deploy.ps1 deploys $dll" ($deployContent -match [regex]::Escape($dll)) `
        "$dll is in install.cmd MOD_DLLS but not referenced by deploy.ps1"
}

# --- 3. .cmd files are CRLF ----------------------------------------------------

Write-Host ""
Write-Host ".cmd line endings (must be CRLF)" -ForegroundColor Cyan

foreach ($cmdFile in (Get-ChildItem (Join-Path $projectRoot "scripts") -Filter "*.cmd")) {
    $bytes = [System.IO.File]::ReadAllBytes($cmdFile.FullName)
    $loneLf = $false
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        if ($bytes[$i] -eq 0x0A -and ($i -eq 0 -or $bytes[$i - 1] -ne 0x0D)) { $loneLf = $true; break }
    }
    Test-Check "$($cmdFile.Name) is CRLF" (-not $loneLf) `
        "$($cmdFile.Name) contains LF-only line endings; .cmd files silently fail on Windows. Run unix2dos on it."
}

# --- 4. Installer wrapper / shared body contract -------------------------------

Write-Host ""
Write-Host "Installer contract" -ForegroundColor Cyan

$gameId          = Get-FirstMatch $installPath 'set "GAME_ID=([^"]+)"'
$uninstallGameId = Get-FirstMatch $uninstallPath 'set "GAME_ID=([^"]+)"'
$vendorZipName   = Get-FirstMatch $installPath 'set "BEPINEX_VENDOR_ZIP_NAME=([^"]+)"'

Test-Check "install.cmd and uninstall.cmd GAME_ID match" ($gameId -eq $uninstallGameId) `
    "install.cmd: '$gameId' vs uninstall.cmd: '$uninstallGameId'"

$gamesJsonPath = Join-Path $coreRoot "data\games.json"
Test-Check "cameraunlock-core submodule checked out" (Test-Path $gamesJsonPath) `
    "Missing $gamesJsonPath. Run: git submodule update --init --recursive"

if (Test-Path $gamesJsonPath) {
    $gamesJson = Get-Content $gamesJsonPath -Raw | ConvertFrom-Json
    $gameEntry = $gamesJson.games.PSObject.Properties.Name -contains $gameId
    Test-Check "GAME_ID '$gameId' exists in games.json" $gameEntry `
        "install.cmd GAME_ID '$gameId' has no entry in cameraunlock-core/data/games.json"
}

$bodyPath = Join-Path $coreRoot "scripts\install-body-bepinex.cmd"
Test-Check "install-body-bepinex.cmd exists in submodule" (Test-Path $bodyPath) `
    "install.cmd dispatches to $bodyPath which is missing"

# --- 5. Vendored BepInEx integrity ---------------------------------------------

Write-Host ""
Write-Host "Vendored loader integrity" -ForegroundColor Cyan

$vendorZip    = Join-Path $vendorDir $vendorZipName
$vendorReadme = Join-Path $vendorDir "README.md"

Test-Check "vendored zip exists ($vendorZipName)" (Test-Path $vendorZip) `
    "install.cmd expects vendor/bepinex/$vendorZipName but it is missing. Run: pixi run update-deps"
Test-Check "vendor/bepinex/README.md exists" (Test-Path $vendorReadme) `
    "vendor/bepinex/README.md (version + SHA-256 record) is missing. Run: pixi run update-deps"

if ((Test-Path $vendorZip) -and (Test-Path $vendorReadme)) {
    $recordedSha = Get-FirstMatch $vendorReadme '(?m)SHA-256:\s*``?([0-9a-fA-F]{64})'
    $actualSha = (Get-FileHash $vendorZip -Algorithm SHA256).Hash.ToLower()
    Test-Check "vendored zip SHA-256 matches recorded hash" ($recordedSha -and ($actualSha -eq $recordedSha.ToLower())) `
        "vendor/bepinex/README.md records '$recordedSha' but the zip hashes to '$actualSha'. The vendored loader was modified outside update-deps.ps1."

    $vendorBuild = Get-FirstMatch $vendorReadme '6\.0\.0-be\.(\d+)'
    Test-Check "vendor README records a BepInEx build number" ($null -ne $vendorBuild) `
        "vendor/bepinex/README.md does not mention a '6.0.0-be.<n>' version. Run: pixi run update-deps"

    if ($null -ne $vendorBuild) {
        $noticesPath = Join-Path $projectRoot "THIRD-PARTY-NOTICES.md"
        $noticesContent = Get-Content $noticesPath -Raw
        Test-Check "THIRD-PARTY-NOTICES.md references vendored build (be.$vendorBuild)" `
            ($noticesContent -match [regex]::Escape("6.0.0-be.$vendorBuild")) `
            "THIRD-PARTY-NOTICES.md does not mention BepInEx 6.0.0-be.$vendorBuild (vendored). Update the notices after update-deps."
    }
}

# --- 6. No forbidden files tracked in git --------------------------------------

Write-Host ""
Write-Host "Tracked-file hygiene" -ForegroundColor Cyan

Push-Location $projectRoot
try {
    $trackedFiles = git ls-files
} finally {
    Pop-Location
}

$forbiddenPatterns = @(
    @{ Pattern = '(^|/)libs/.*\.dll$';   Reason = 'game-derived reference DLLs must never be committed' },
    @{ Pattern = '(^|/)(bin|obj)/';      Reason = 'build output must never be committed' },
    @{ Pattern = '^\.claude/';           Reason = 'AI tooling state must never be committed' },
    @{ Pattern = '^\.pixi/';             Reason = 'pixi environment must never be committed' },
    @{ Pattern = '^release/';            Reason = 'release output must never be committed' },
    @{ Pattern = '\.user$';              Reason = 'IDE user settings must never be committed' }
)

foreach ($rule in $forbiddenPatterns) {
    $hits = @($trackedFiles | Where-Object { $_ -match $rule.Pattern })
    Test-Check "no tracked files matching $($rule.Pattern)" ($hits.Count -eq 0) `
        "$($rule.Reason): $($hits -join ', ')"
}

# --- Summary --------------------------------------------------------------------

Write-Host ""
if ($failures.Count -gt 0) {
    Write-Host "=== $($failures.Count) check(s) FAILED ===" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "=== All checks passed ===" -ForegroundColor Green
exit 0
