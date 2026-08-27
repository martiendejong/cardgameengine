#Requires -Version 5.1
<#
.SYNOPSIS
    Build, version-bump, and deploy Town Wars (Card Game Engine) to C:\deployed\townwars.

.DESCRIPTION
    This repo previously had no deploy/release process at all — publishing was a manual,
    undocumented "dotnet publish + copy" done by hand, with no version bump anywhere, which is
    why JengoAGI could never tell whether a deployed instance actually contained the latest
    changes (see JengoWork task 625). This script formalizes that process and makes the version
    bump an explicit, mandatory step in it.

    The VERSION file at the repo root is the single source of truth. Directory.Build.props reads
    it into every project's <Version>, so the published DLLs carry it too and GET /api/version can
    report back exactly what is actually running on the live instance — not just what the source
    checkout says, which is what catches a partial/stale deploy.

.PARAMETER Bump
    Which part of the semver VERSION to increment before deploying. Default: patch.

.PARAMETER SkipVersionBump
    Deploy the current VERSION as-is without incrementing it (e.g. re-deploying after a deploy
    failure with no code change).

.EXAMPLE
    ./deploy.ps1                  # bump patch version, build, publish, deploy
    ./deploy.ps1 -Bump minor      # bump minor version instead
    ./deploy.ps1 -SkipVersionBump # redeploy current version unchanged
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("major", "minor", "patch")]
    [string]$Bump = "patch",

    [switch]$SkipVersionBump
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$versionFile = Join-Path $repoRoot "VERSION"
$apiProject = Join-Path $repoRoot "src\CardGameEngine.Api\CardGameEngine.Api.csproj"
$frontendDir = Join-Path $repoRoot "frontend"
$publishDir = Join-Path $repoRoot "publish"
$deployTarget = "C:\deployed\townwars"
$serviceName = "TownWars"

# --- 1. Version bump -------------------------------------------------------
$currentVersion = (Get-Content $versionFile -Raw).Trim()

if ($SkipVersionBump) {
    $newVersion = $currentVersion
    Write-Host "Redeploying current version (no bump): $newVersion" -ForegroundColor Yellow
}
else {
    $parts = $currentVersion.Split('.')
    if ($parts.Count -ne 3) {
        throw "VERSION file content '$currentVersion' is not in MAJOR.MINOR.PATCH form"
    }
    [int]$major, [int]$minor, [int]$patch = $parts

    switch ($Bump) {
        "major" { $major++; $minor = 0; $patch = 0 }
        "minor" { $minor++; $patch = 0 }
        "patch" { $patch++ }
    }
    $newVersion = "$major.$minor.$patch"

    if ($PSCmdlet.ShouldProcess($versionFile, "Bump version $currentVersion -> $newVersion")) {
        Set-Content -Path $versionFile -Value $newVersion -NoNewline
        Write-Host "Version bumped: $currentVersion -> $newVersion" -ForegroundColor Green
    }
}

# --- 2. Build frontend -------------------------------------------------------
Write-Host "`n[1/3] Building frontend..." -ForegroundColor Cyan
Push-Location $frontendDir
try {
    $env:VITE_BASE_PATH = "/townwars/"
    npm install
    npm run build
}
finally {
    Remove-Item Env:\VITE_BASE_PATH -ErrorAction SilentlyContinue
    Pop-Location
}

# --- 3. Publish backend -------------------------------------------------------
Write-Host "`n[2/3] Publishing backend ($newVersion)..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish $apiProject -c Release -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

# Frontend build output is served as static files by the API (UseDefaultFiles/UseStaticFiles) —
# copy it into the publish output's wwwroot alongside the backend.
$publishWwwroot = Join-Path $publishDir "wwwroot"
New-Item -ItemType Directory -Path $publishWwwroot -Force | Out-Null
Copy-Item -Path (Join-Path $frontendDir "dist\*") -Destination $publishWwwroot -Recurse -Force

# Card/game definitions live at the repo root, outside every project folder, so
# `dotnet publish` never picks them up — GameDefinitionService loads them from a
# "definitions" dir beside the exe at runtime. Without this, definitions.json edits
# (new tags, balance changes, new cards) silently never reach the live site even
# though the compiled DLLs deploy fine.
Copy-Item -Path (Join-Path $repoRoot "definitions") -Destination $publishDir -Recurse -Force

# --- 4. Deploy -------------------------------------------------------
Write-Host "`n[3/3] Deploying v$newVersion to $deployTarget..." -ForegroundColor Cyan
if ($PSCmdlet.ShouldProcess($deployTarget, "Stop $serviceName, copy publish output, restart $serviceName")) {
    Stop-Service -Name $serviceName -ErrorAction SilentlyContinue
    Copy-Item -Path (Join-Path $publishDir "*") -Destination $deployTarget -Recurse -Force
    Start-Service -Name $serviceName
}

Write-Host "`nDeployed Town Wars v$newVersion to $deployTarget" -ForegroundColor Green
Write-Host "Verify: curl http://localhost/townwars/api/version (expect version=$newVersion)" -ForegroundColor Green
