<#
.SYNOPSIS
    Zips the ForeverTV addon for CurseForge and the GitHub release: artifacts/ForeverTV-<version>.zip.
.DESCRIPTION
    Copies addon/ForeverTV into a staging folder, sets "## Version:" in the TOC to VersionPrefix from
    Directory.Build.props and zips the folder so the archive holds ForeverTV/ at its root, ready for Interface\AddOns.
.EXAMPLE
    ./tools/Build-Addon.ps1
#>
[CmdletBinding()]
param([string]$OutDir)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1') -Force
$root = Get-RepoRoot
$version = Get-ReleaseVersion
if (-not $OutDir) { $OutDir = Join-Path $root 'artifacts' }

$stage = Join-Path ([IO.Path]::GetTempPath()) "forevertv-addon-$([guid]::NewGuid().ToString('n'))"
$target = Join-Path $stage 'ForeverTV'
try {
    Copy-Item (Join-Path $root 'addon/ForeverTV') $target -Recurse
    $tocPath = Join-Path $target 'ForeverTV.toc'
    $toc = (Get-Content $tocPath -Raw) -replace '(?m)^## Version:.*$', "## Version: $version"
    Set-Content $tocPath $toc -NoNewline -Encoding utf8NoBOM

    New-Item -ItemType Directory -Force $OutDir | Out-Null
    $zip = Join-Path $OutDir "ForeverTV-$version.zip"
    Compress-Archive -Path $target -DestinationPath $zip -Force
    Write-Host "Addon:     $zip"
    $zip
}
finally {
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
}
