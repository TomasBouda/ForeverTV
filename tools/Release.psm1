# Shared helpers for the release scripts (docs/decisions/0010-release-pipeline.md).

$script:Root = Split-Path -Parent $PSScriptRoot

function Get-RepoRoot { $script:Root }

<# The one version of app, addon and extension: VersionPrefix in Directory.Build.props. #>
function Get-ReleaseVersion {
    [xml]$props = Get-Content (Join-Path $script:Root 'Directory.Build.props')
    $version = ($props.Project.PropertyGroup | Where-Object { $_.VersionPrefix } | Select-Object -First 1).VersionPrefix
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "VersionPrefix '$version' is not major.minor.patch." }
    $version
}

<# Everything between this version's "## x.y.z" heading and the next heading in CHANGELOG.md. #>
function Get-ChangelogSection([string]$Version, [switch]$AllowMissing) {
    $lines = [System.Collections.Generic.List[string]]::new()
    $inSection = $false
    foreach ($line in Get-Content (Join-Path $script:Root 'CHANGELOG.md') -Encoding utf8) {
        if ($line -match '^## ') {
            if ($inSection) { break }
            $inSection = $line -match ('^## ' + [regex]::Escape($Version) + '(\s|$)')
            continue
        }
        if ($inSection) { $lines.Add($line) }
    }
    $text = ($lines -join "`n").Trim()
    if (-not $text -and -not $AllowMissing) { throw "CHANGELOG.md has no '## $Version' section; a release needs one." }
    $text
}

Export-ModuleMember -Function Get-RepoRoot, Get-ReleaseVersion, Get-ChangelogSection
