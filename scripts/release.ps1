#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
    Automated release workflow for Sons of the Forest Head Tracking mod.

.DESCRIPTION
    This script:
    1. Updates version in csproj and plugin source
    2. Builds and updates prebuilt DLLs
    3. Commits all changes
    4. Creates and pushes a git tag to trigger CI release

.PARAMETER Version
    The version to release (e.g., "1.0.0", "1.2.3")

.EXAMPLE
    pixi run release 1.0.0

.NOTES
    Run via: pixi run release <version>
#>
param(
    [Parameter(Position=0)]
    [string]$Version = "",
    # Ship a release even when there are no user-facing commits since the
    # last tag (writes a maintenance changelog entry instead of aborting).
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$csprojPath = Join-Path $projectDir "src\SonsOfTheForestHeadTracking\SonsOfTheForestHeadTracking.csproj"

# Reads and writes raw UTF-8 (no BOM) so CRLF line endings in .cmd files survive untouched.
function Update-VersionInFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Pattern,
        [Parameter(Mandatory)][string]$Replacement,
        [Parameter(Mandatory)][string]$Label
    )
    $content = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($Path))
    if ($content -notmatch $Pattern) {
        Write-Host "Error: $Label not found in $Path" -ForegroundColor Red
        exit 1
    }
    $updated = $content -replace $Pattern, $Replacement
    if ($updated -eq $content) { return }
    [System.IO.File]::WriteAllText($Path, $updated, [System.Text.UTF8Encoding]::new($false))
    Write-Host "  Updated $Label" -ForegroundColor Gray
}

Import-Module (Join-Path $projectDir "cameraunlock-core\powershell\ReleaseWorkflow.psm1") -Force

# Mirrors New-ChangelogFromCommits' insertion so a -Force maintenance entry
# lands in the same place with the same shape.
function Add-MaintenanceChangelogEntry {
    param([string]$Path, [string]$NewVersion)
    $date = Get-Date -Format 'yyyy-MM-dd'
    $entry = "## [$NewVersion] - $date`n`n### Changed`n`n- Maintenance release (no user-facing changes).`n`n"
    $changelog = Get-Content $Path -Raw
    if ($changelog -match '(?s)(# Changelog.*?)(## \[)') {
        $changelog = $changelog -replace '(?s)(# Changelog.*?\n\n)', "`$1$entry"
    } else {
        $changelog = $changelog -replace '(?s)(# Changelog.*?\n)', "`$1$entry"
    }
    $changelog = $changelog.TrimEnd() + "`n"
    Set-Content $Path $changelog -NoNewline
}

Write-Host "=== Sons of the Forest Head Tracking Release ===" -ForegroundColor Cyan
Write-Host ""

$currentVersion = Get-CsprojVersion $csprojPath

# If no version provided, show current and exit
if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-Host "Current version: " -NoNewline -ForegroundColor Yellow
    Write-Host $currentVersion -ForegroundColor White
    Write-Host ""
    Write-Host "Usage: " -NoNewline -ForegroundColor Yellow
    Write-Host "pixi run release <major|minor|patch|nightly|X.Y.Z>" -ForegroundColor White
    Write-Host ""
    Write-Host "Example: " -NoNewline -ForegroundColor Yellow
    Write-Host "pixi run release patch" -ForegroundColor White
    exit 0
}

if ($Version -eq 'nightly') {
    & (Join-Path $scriptDir 'release-nightly.ps1')
    exit $LASTEXITCODE
}

# Resolve major/minor/patch into a concrete version (or accept literal X.Y.Z)
try {
    $Version = Resolve-ReleaseVersion -Argument $Version -CurrentVersion $currentVersion
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$tagName = "v$Version"

# Check if we're on main branch
$currentBranch = git rev-parse --abbrev-ref HEAD
if ($currentBranch -ne "main") {
    Write-Host "Error: Must be on 'main' branch to release (currently on '$currentBranch')" -ForegroundColor Red
    exit 1
}

# Check for uncommitted changes (prebuilt/ is excluded since the release overwrites it)
$status = git status --porcelain -- ':!prebuilt/'
if ($status) {
    Write-Host "Error: Working directory has uncommitted changes" -ForegroundColor Red
    Write-Host $status -ForegroundColor Gray
    Write-Host "Please commit or stash changes before releasing" -ForegroundColor Yellow
    exit 1
}

# Check if tag already exists
$existingTag = git tag -l $tagName
if ($existingTag) {
    Write-Host "Error: Tag '$tagName' already exists" -ForegroundColor Red
    exit 1
}

Write-Host "Current version: $currentVersion" -ForegroundColor Gray
Write-Host "New version:     $Version" -ForegroundColor Green
Write-Host ""

# Step 1: Generate CHANGELOG from commits since last tag. This is the gate
# that aborts when there are no user-facing commits, so run it BEFORE
# mutating any version files or building - a failure here then leaves a clean
# tree instead of stranding a half-applied version bump with no tag.
Write-Host "Generating CHANGELOG from commits..." -ForegroundColor Cyan
$changelogPath = Join-Path $projectDir "CHANGELOG.md"
$hasExistingTags = git tag -l
if (-not $hasExistingTags) {
    # First release - write a basic changelog entry
    $date = Get-Date -Format 'yyyy-MM-dd'
    $firstEntry = "# Changelog`n`n## [$Version] - $date`n`nFirst release.`n"
    Set-Content $changelogPath $firstEntry
    Write-Host "  First release - wrote initial CHANGELOG entry" -ForegroundColor Gray
} else {
    try {
        $changelogArgs = @{
            ChangelogPath = $changelogPath
            Version = $Version
            ArtifactPaths = @(
                "src/SonsOfTheForestHeadTracking/",
                "cameraunlock-core",
                "scripts/install.cmd",
                "scripts/uninstall.cmd",
                "prebuilt/"
            )
        }
        New-ChangelogFromCommits @changelogArgs
    } catch {
        if (-not $Force) {
            Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "No user-facing changes to release. Re-run with -Force for a maintenance release." -ForegroundColor Yellow
            exit 1
        }
        Write-Host "No user-facing commits since last tag - writing maintenance entry (-Force)." -ForegroundColor Yellow
        Add-MaintenanceChangelogEntry -Path $changelogPath -NewVersion $Version
    }
}

# Step 2: Update version in csproj
Write-Host "Updating version to $Version..." -ForegroundColor Cyan
Set-CsprojVersion $csprojPath $Version

# Step 3: Update version in plugin source, pixi.toml, and install.cmd CONFIG BLOCK
# (the latter so the state file written at install time records the correct version).
$pluginPath = Join-Path $projectDir "src\SonsOfTheForestHeadTracking\Plugin.cs"
Update-VersionInFile -Path $pluginPath `
    -Pattern 'PluginVersion = "[^"]+"' -Replacement "PluginVersion = `"$Version`"" `
    -Label "Plugin.cs PluginVersion"

$pixiTomlPath = Join-Path $projectDir "pixi.toml"
Update-VersionInFile -Path $pixiTomlPath `
    -Pattern '(?m)^version = "[^"]+"' -Replacement "version = `"$Version`"" `
    -Label "pixi.toml version"

$installCmdPath = Join-Path $projectDir "scripts\install.cmd"
Update-VersionInFile -Path $installCmdPath `
    -Pattern 'set "MOD_VERSION=[^"]+"' -Replacement "set `"MOD_VERSION=$Version`"" `
    -Label "install.cmd MOD_VERSION"

$manifestPath = Join-Path $projectDir "launcher-manifest.json"
Update-VersionInFile -Path $manifestPath `
    -Pattern '(?m)^(    "version":\s*)"[^"]+"' -Replacement "`${1}`"$Version`"" `
    -Label "launcher-manifest.json version"

# Step 4: Build and update prebuilt DLLs
Write-Host "Building release..." -ForegroundColor Cyan
Push-Location $projectDir
pixi run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

$prebuiltDir = Join-Path $projectDir "prebuilt"
if (-not (Test-Path $prebuiltDir)) {
    New-Item -ItemType Directory -Path $prebuiltDir -Force | Out-Null
}
# Allowlist, not a wildcard: prebuilt/ is committed to a public repo, and a
# wildcard would sweep in any third-party/game-derived DLL that ever lands in
# the build output. Keep this list in sync with the packagers' $modDlls.
$buildOutputDir = Join-Path $projectDir "src\SonsOfTheForestHeadTracking\bin\Release\net6.0"
$prebuiltDlls = @("SonsOfTheForestHeadTracking.dll", "CameraUnlock.Core.dll")
foreach ($dll in $prebuiltDlls) {
    $src = Join-Path $buildOutputDir $dll
    if (-not (Test-Path $src)) {
        Write-Host "Expected build output not found: $src" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Copy-Item $src $prebuiltDir -Force
}
Write-Host "  Updated prebuilt DLLs" -ForegroundColor Gray
Pop-Location

# Step 5: Commit
Write-Host "Committing changes..." -ForegroundColor Cyan
git add $csprojPath
git add $pluginPath
git add $pixiTomlPath
git add $installCmdPath
git add $manifestPath
git add "$projectDir/prebuilt"
git add $changelogPath
git commit -m "Release v$Version"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Commit failed!" -ForegroundColor Red
    exit 1
}

# Step 6: Create tag
Write-Host "Creating tag $tagName..." -ForegroundColor Cyan
git tag -a $tagName -m "Release $tagName"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Tag creation failed!" -ForegroundColor Red
    exit 1
}

# Step 7: Push
Write-Host "Pushing to GitHub..." -ForegroundColor Cyan
git push origin main
if ($LASTEXITCODE -ne 0) {
    Write-Host "Push of main failed! The tag $tagName was created locally but not pushed." -ForegroundColor Red
    Write-Host "Fix the push issue, then run: git push origin main; git push origin $tagName" -ForegroundColor Yellow
    exit 1
}
git push origin $tagName
if ($LASTEXITCODE -ne 0) {
    Write-Host "Push of tag $tagName failed! main was pushed but the release was not triggered." -ForegroundColor Red
    Write-Host "Fix the push issue, then run: git push origin $tagName" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Release $tagName initiated!" -ForegroundColor Green
Write-Host ""
Write-Host "The GitHub Actions release workflow will now:" -ForegroundColor Yellow
Write-Host "  - Package the prebuilt DLLs" -ForegroundColor White
Write-Host "  - Create GitHub release with artifacts" -ForegroundColor White
Write-Host ""
Write-Host "Watch progress at:" -ForegroundColor Yellow
Write-Host "  https://github.com/itsloopyo/sons-of-the-forest-headtracking/actions" -ForegroundColor Cyan
