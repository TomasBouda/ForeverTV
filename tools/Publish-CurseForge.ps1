<#
.SYNOPSIS
    Uploads the addon zip to CurseForge (docs/decisions/0010-release-pipeline.md).
.DESCRIPTION
    The project comes from "## X-Curse-Project-ID" in addon/ForeverTV/ForeverTV.toc, the version from
    Directory.Build.props, the changelog from that version's section of CHANGELOG.md, the game version from the
    newest WoW Forever (1.60.x) version CurseForge knows. The API token is read from CURSEFORGE_API_TOKEN, never
    from a parameter or a file.

    Until the CurseForge project exists (no project ID in the TOC, or no token) the script warns and returns, so a
    release still publishes the app and the other stores. -Require turns that into an error.
    -DryRun builds the zip and prints the metadata without calling CurseForge.
.EXAMPLE
    ./tools/Publish-CurseForge.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [ValidateSet('alpha', 'beta', 'release')][string]$ReleaseType = 'release',
    [string]$OutDir,
    [switch]$Require,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1') -Force
$root = Get-RepoRoot
$version = Get-ReleaseVersion
$api = 'https://wow.curseforge.com/api'

function Skip([string]$why) {
    if ($Require) { throw $why }
    Write-Warning "CurseForge skipped: $why"
    if ($env:GITHUB_STEP_SUMMARY) { "CurseForge skipped: $why" | Add-Content $env:GITHUB_STEP_SUMMARY }
}

$projectId = $null
foreach ($line in Get-Content (Join-Path $root 'addon/ForeverTV/ForeverTV.toc')) {
    if ($line -match '^##\s*X-Curse-Project-ID:\s*(\d+)\s*$') { $projectId = $Matches[1] }
}
$changelog = Get-ChangelogSection $version -AllowMissing:$DryRun
$zip = & (Join-Path $PSScriptRoot 'Build-Addon.ps1') -OutDir $OutDir | Select-Object -Last 1

$metadata = [ordered]@{
    changelog     = $changelog
    changelogType = 'markdown'
    displayName   = "ForeverTV $version"
    releaseType   = $ReleaseType
}

Write-Host "Addon:     ForeverTV $version (CurseForge project $(if ($projectId) { $projectId } else { 'not set' }))"
Write-Host "File:      $zip ($([math]::Round((Get-Item $zip).Length / 1kb)) kB)"
Write-Host "Changelog:`n$changelog"
if ($DryRun) { Write-Host 'Dry run: nothing uploaded.'; return }

if (-not $projectId) { Skip 'addon/ForeverTV/ForeverTV.toc has no "## X-Curse-Project-ID".'; return }
$token = $env:CURSEFORGE_API_TOKEN
if (-not $token) { Skip 'CURSEFORGE_API_TOKEN is not set.'; return }
$headers = @{ 'X-Api-Token' = $token }

# Assigned first: Invoke-RestMethod hands a JSON array to a pipeline as one object.
$versions = Invoke-RestMethod -Uri "$api/game/versions" -Headers $headers
$hit = $versions |
    Where-Object { $_.gameVersionTypeID -eq 88568 -and $_.name -match '^1\.60\.\d+$' } |
    Sort-Object { [version]$_.name }, id | Select-Object -Last 1
if (-not $hit) { throw 'CurseForge lists no 1.60.x game version.' }
Write-Host "Game:      $($hit.name)"
$metadata.gameVersions = @($hit.id)

$result = Invoke-RestMethod -Method Post -Uri "$api/projects/$projectId/upload-file" -Headers $headers -Form @{
    metadata = ($metadata | ConvertTo-Json -Compress -Depth 6)
    file     = Get-Item -LiteralPath $zip
}
Write-Host "Uploaded:  CurseForge file $($result.id) (waits for CurseForge approval before it is public)"
