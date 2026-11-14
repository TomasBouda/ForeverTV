<#
.SYNOPSIS
    Uploads the browser extension to the Chrome Web Store and submits it for review
    (docs/decisions/0010-release-pipeline.md).
.DESCRIPTION
    Uses the Chrome Web Store API v2 with an OAuth client and refresh token:
      CWS_CLIENT_ID, CWS_CLIENT_SECRET, CWS_REFRESH_TOKEN   secrets
      CWS_PUBLISHER_ID, CWS_EXTENSION_ID                     IDs from the Developer Dashboard
    all read from environment variables, never from parameters or files.

    The API cannot create an item: the first version is uploaded by hand in the Developer Dashboard (with the
    listing, privacy fields and screenshots). Until the IDs and secrets are set the script warns and returns, so a
    release still publishes the app and the addon. -Require turns that into an error.
    -DryRun builds the zip without calling the store.
.EXAMPLE
    ./tools/Publish-ChromeWebStore.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [string]$OutDir,
    [switch]$Require,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Release.psm1') -Force
$version = Get-ReleaseVersion

function Skip([string]$why) {
    if ($Require) { throw $why }
    Write-Warning "Chrome Web Store skipped: $why"
    if ($env:GITHUB_STEP_SUMMARY) { "Chrome Web Store skipped: $why" | Add-Content $env:GITHUB_STEP_SUMMARY }
}

$zip = & (Join-Path $PSScriptRoot 'Build-Extension.ps1') -OutDir $OutDir | Select-Object -Last 1
Write-Host "Extension: ForeverTV $version, $zip ($([math]::Round((Get-Item $zip).Length / 1kb)) kB)"
if ($DryRun) { Write-Host 'Dry run: nothing uploaded.'; return }

$missing = 'CWS_CLIENT_ID', 'CWS_CLIENT_SECRET', 'CWS_REFRESH_TOKEN', 'CWS_PUBLISHER_ID', 'CWS_EXTENSION_ID' |
    Where-Object { -not [Environment]::GetEnvironmentVariable($_) }
if ($missing) { Skip "not configured ($($missing -join ', ') missing)."; return }

$token = (Invoke-RestMethod -Method Post -Uri 'https://oauth2.googleapis.com/token' -Body @{
    client_id     = $env:CWS_CLIENT_ID
    client_secret = $env:CWS_CLIENT_SECRET
    refresh_token = $env:CWS_REFRESH_TOKEN
    grant_type    = 'refresh_token'
}).access_token
$headers = @{ Authorization = "Bearer $token" }
$item = "publishers/$($env:CWS_PUBLISHER_ID)/items/$($env:CWS_EXTENSION_ID)"

$upload = Invoke-RestMethod -Method Post -Uri "https://chromewebstore.googleapis.com/upload/v2/${item}:upload" `
    -Headers $headers -InFile $zip -ContentType 'application/zip'
Write-Host "Upload:    $($upload.uploadState)"
for ($i = 0; $upload.uploadState -eq 'IN_PROGRESS' -and $i -lt 30; $i++) {
    Start-Sleep -Seconds 5
    $upload = Invoke-RestMethod -Uri "https://chromewebstore.googleapis.com/v2/${item}:fetchStatus" -Headers $headers
    Write-Host "Upload:    $($upload.lastAsyncUploadState ?? $upload.uploadState)"
    if ($upload.lastAsyncUploadState) { $upload | Add-Member -Force uploadState $upload.lastAsyncUploadState }
}
if ($upload.uploadState -ne 'SUCCEEDED') { throw "Upload did not succeed: $($upload | ConvertTo-Json -Depth 6 -Compress)" }

$publish = Invoke-RestMethod -Method Post -Uri "https://chromewebstore.googleapis.com/v2/${item}:publish" -Headers $headers
Write-Host "Submitted: $($publish.state ?? ($publish | ConvertTo-Json -Compress)) (Google reviews it before it is live)"
