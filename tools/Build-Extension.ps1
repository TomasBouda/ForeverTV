<#
.SYNOPSIS
    Zips the browser extension for the Chrome Web Store and the GitHub release:
    artifacts/ForeverTV-extension-<version>.zip.
.DESCRIPTION
    Takes extension/ without its tests, sets "version" in manifest.json to VersionPrefix from Directory.Build.props
    and zips it with manifest.json at the root of the archive, as the store expects.
.EXAMPLE
    ./tools/Build-Extension.ps1
#>
[CmdletBinding()]
param([string]$OutDir)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1') -Force
$root = Get-RepoRoot
$version = Get-ReleaseVersion
if (-not $OutDir) { $OutDir = Join-Path $root 'artifacts' }

$stage = Join-Path ([IO.Path]::GetTempPath()) "forevertv-extension-$([guid]::NewGuid().ToString('n'))"
try {
    Copy-Item (Join-Path $root 'extension') $stage -Recurse
    Remove-Item (Join-Path $stage 'test') -Recurse -Force -ErrorAction SilentlyContinue
    $manifestPath = Join-Path $stage 'manifest.json'
    $manifest = Get-Content $manifestPath -Raw
    $manifest = [regex]::Replace($manifest, '"version"\s*:\s*"[^"]*"', "`"version`": `"$version`"", 1)
    Set-Content $manifestPath $manifest -NoNewline -Encoding utf8NoBOM

    New-Item -ItemType Directory -Force $OutDir | Out-Null
    $zip = Join-Path $OutDir "ForeverTV-extension-$version.zip"
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
    Write-Host "Extension: $zip"
    $zip
}
finally {
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
}
