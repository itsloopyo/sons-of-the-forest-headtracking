#!/usr/bin/env pwsh
#Requires -Version 5.1
# Thin wrapper - dev-deploy orchestration lives in
# cameraunlock-core/powershell/DevDeploy.psm1.

param(
    [Parameter(Position=0)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [Parameter(Mandatory=$false, Position=1)]
    [string]$GivenPath,
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$RemainingArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

Import-Module (Join-Path $projectRoot "cameraunlock-core\powershell\DevDeploy.psm1") -Force
Import-Module (Join-Path $projectRoot "cameraunlock-core\powershell\ModDeployment.psm1") -Force
$buildOutput = Join-Path $projectRoot "src\SonsOfTheForestHeadTracking\bin\$Configuration\net6.0"
$vendorZip = Join-Path $projectRoot "vendor\bepinex\BepInEx_UnityIL2CPP_x64.zip"
$result = Invoke-DevDeployBepInEx `
    -GameId 'sons-of-the-forest' `
    -GameDisplayName 'Sons of the Forest' `
    -BuildOutputPath $buildOutput `
    -ModDllName 'SonsOfTheForestHeadTracking.dll' `
    -ExtraDlls @('CameraUnlock.Core.dll') `
    -GivenPath $GivenPath `
    -EnsureLoader `
    -MajorVersion 6 `
    -VendorZip $vendorZip

Write-DeploymentSuccess `
    -ModName "Head Tracking mod" `
    -DeployPath $result.DeployedDllPath `
    -RecenterKey "Home" `
    -ToggleKey "End"
