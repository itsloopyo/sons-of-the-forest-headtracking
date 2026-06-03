#!/usr/bin/env pwsh
#Requires -Version 5.1
# Bump vendored BepInEx 6 IL2CPP bleeding-edge build to the latest published on
# builds.bepinex.dev and rewrite vendor/bepinex/{LICENSE,README.md}. Manual:
# dev runs this when they want a fresh upstream bump, then commits the result.
# CI never refreshes.
# See AGENTS.md "Vendoring Third-Party Dependencies".
#
# BepInEx 6 IL2CPP is bleeding edge and only published on builds.bepinex.dev,
# NOT on GitHub releases. Refresh-VendoredLoader's GitHub mode does not apply.
# We scrape the build server's project index directly.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference    = 'SilentlyContinue'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir

$out = Join-Path $projectDir 'vendor/bepinex'
if (-not (Test-Path $out)) { New-Item -ItemType Directory -Path $out -Force | Out-Null }

$headers = @{ "User-Agent" = "CameraUnlock-HeadTracking" }

Write-Host "Discovering latest BepInEx 6 IL2CPP build from builds.bepinex.dev..." -ForegroundColor Cyan
$idx = Invoke-WebRequest -Uri 'https://builds.bepinex.dev/projects/bepinex_be' -UseBasicParsing -Headers $headers -TimeoutSec 30
# Find IL2CPP win-x64 hrefs and pick the highest build number.
$hrefs = [regex]::Matches($idx.Content, 'href="(/projects/bepinex_be/(\d+)/(BepInEx-Unity\.IL2CPP-win-x64-6\.0\.0-be\.\d+(?:%2B|\+)[a-f0-9]+\.zip))"')
if (-not $hrefs.Count) { throw "Could not find any IL2CPP-win-x64 builds on builds.bepinex.dev" }
$best = $hrefs | ForEach-Object {
    [pscustomobject]@{
        Build = [int]$_.Groups[2].Value
        Path  = $_.Groups[1].Value
        Asset = $_.Groups[3].Value
    }
} | Sort-Object -Property Build -Descending | Select-Object -First 1

$assetUrl = "https://builds.bepinex.dev$($best.Path)"
$zip = Join-Path $out 'BepInEx_UnityIL2CPP_x64.zip'
$tmpZip = "$zip.tmp"
Write-Host "  Fetching build $($best.Build) ($($best.Asset))" -ForegroundColor DarkGray
Invoke-WebRequest -Uri $assetUrl -OutFile $tmpZip -UseBasicParsing -Headers $headers -TimeoutSec 120
$sha = (Get-FileHash $tmpZip -Algorithm SHA256).Hash.ToLower()

$readmePath = Join-Path $out 'README.md'
if (Test-Path $readmePath) {
    $existing = Get-Content $readmePath -Raw
    $match = [regex]::Match($existing, '(?m)^- SHA-256:\s*`([0-9a-f]+)`')
    if ($match.Success -and $match.Groups[1].Value.ToLower() -eq $sha) {
        Remove-Item $tmpZip -Force
        Write-Host ""
        Write-Host "vendor/bepinex already at build $($best.Build) (sha $($sha.Substring(0,12))...). No changes." -ForegroundColor DarkGray
        return
    }
}

Move-Item -Path $tmpZip -Destination $zip -Force

Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/BepInEx/BepInEx/master/LICENSE' -OutFile (Join-Path $out 'LICENSE') -UseBasicParsing -Headers $headers -TimeoutSec 30

$readme = @"
# bepinex (vendored - IL2CPP bleeding edge)

Bundled copy of BepInEx 6 IL2CPP, the install-time source of truth for Sons of the Forest.
Refresh manually with ``pixi run update-deps``, then commit.

## Snapshot

- Asset: ``BepInEx_UnityIL2CPP_x64.zip``
- Build: ``6.0.0-be.$($best.Build)`` (BepInEx CI build server, NOT GitHub releases)
- Upstream URL: $assetUrl
- SHA-256: ``$sha``
- Fetched at: $((Get-Date).ToString('o'))

BepInEx 6 IL2CPP is bleeding edge and only published on builds.bepinex.dev. update-deps
auto-bumps to whatever the latest IL2CPP-win-x64 build is. If you need to pin to a
specific build (because a newer one regresses for SotF), hardcode the build number in
update-deps.ps1.
"@
Set-Content -Path $readmePath -Value $readme -Encoding UTF8

Write-Host ""
Write-Host "vendor/bepinex refreshed (build $($best.Build), sha $($sha.Substring(0,12))...). Review and commit." -ForegroundColor Green
