<#
.SYNOPSIS
    Automated CI/CD Deployment Script for RxIntel Backend on Windows Server (IIS).
.DESCRIPTION
    1. Validates repository and target branch.
    2. Builds and publishes Rxintel.AI in Release mode.
    3. Safely engages app_offline.htm in IIS to release file locks.
    4. Syncs published binaries to live IIS directory WITHOUT overwriting:
       - appsettings.json
       - appsettings.Development.json (and any appsettings.*.json)
       - web.config
    5. Cleans up app_offline.htm and optionally recycles IIS AppPool.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$TargetBranch = "Develop",

    [Parameter(Mandatory = $false)]
    [string]$IisSitePath = "C:\inetpub\wwwroot\RxIntel_API",

    [Parameter(Mandatory = $false)]
    [string]$AppPoolName = "RxIntel_API_Pool",

    [Parameter(Mandatory = $false)]
    [string]$RepoDir = $PSScriptRoot + "\..",

    [Parameter(Mandatory = $false)]
    [string]$TempPublishDir = "C:\temp\RxIntel_Backend_Publish",

    [Parameter(Mandatory = $false)]
    [switch]$SkipGitPull = $false
)

$ErrorActionPreference = "Stop"

function Write-Step ([string]$msg) {
    Write-Host "`n>>> [CI/CD] $msg" -ForegroundColor Cyan
}

function Write-Success ([string]$msg) {
    Write-Host ">>> [SUCCESS] $msg" -ForegroundColor Green
}

function Write-WarningMsg ([string]$msg) {
    Write-Host ">>> [WARNING] $msg" -ForegroundColor Yellow
}

function Write-Failure ([string]$msg) {
    Write-Host ">>> [ERROR] $msg" -ForegroundColor Red
}

try {
    Write-Step "Starting RxIntel Backend Deployment for branch: '$TargetBranch'..."
    Write-Host "Target IIS Path: $IisSitePath"
    Write-Host "Repository Dir:  $RepoDir"

    # 1. Verify Directories
    if (-not (Test-Path $RepoDir)) {
        throw "Repository directory '$RepoDir' not found."
    }

    if (-not (Test-Path $IisSitePath)) {
        Write-WarningMsg "IIS Site Path '$IisSitePath' does not exist. Creating directory..."
        New-Item -ItemType Directory -Path $IisSitePath -Force | Out-Null
    }

    # 2. Update Source from Git
    if (-not $SkipGitPull) {
        Write-Step "Updating repository working tree to latest '$TargetBranch'..."
        Push-Location $RepoDir
        try {
            git fetch origin $TargetBranch
            git checkout $TargetBranch
            git reset --hard "origin/$TargetBranch"
        }
        finally {
            Pop-Location
        }
    }

    # 3. Clean Temp Publish Directory
    if (Test-Path $TempPublishDir) {
        Remove-Item -Path $TempPublishDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $TempPublishDir -Force | Out-Null

    # 4. Build and Publish .NET Web App
    $csprojPath = Join-Path $RepoDir "Rxintel.AI\Rxintel.AI.csproj"
    if (-not (Test-Path $csprojPath)) {
        throw "Could not find project file at '$csprojPath'."
    }

    Write-Step "Publishing Rxintel.AI project in Release configuration..."
    dotnet publish "$csprojPath" -c Release -o "$TempPublishDir" --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }
    Write-Success "Build and publish succeeded."

    # 5. Place app_offline.htm to release IIS file locks
    Write-Step "Placing app_offline.htm in IIS directory..."
    $appOfflinePath = Join-Path $IisSitePath "app_offline.htm"
    $offlineHtml = @"
<!DOCTYPE html>
<html>
<head><title>Application Updating</title></head>
<body style="font-family: Arial, sans-serif; text-align: center; padding-top: 100px;">
    <h2>RxIntel API is updating...</h2>
    <p>Please wait a moment while the latest version is deployed.</p>
</body>
</html>
"@
    Set-Content -Path $appOfflinePath -Value $offlineHtml -Encoding UTF8
    Start-Sleep -Seconds 2

    # 6. Copy Published Artifacts with ROBOCOPY
    # EXCLUDE appsettings.json, appsettings.*.json, and web.config from being overwritten
    Write-Step "Syncing new binaries to IIS directory (protecting appsettings.json and web.config)..."
    
    # Robocopy flags:
    # /E   : Copy subdirectories, including empty ones.
    # /XF  : Exclude files matching names (PROTECTED CONFIGS).
    # /XD  : Exclude directories.
    # /R:2 : Retry twice on locked file.
    # /W:2 : Wait 2 seconds between retries.
    # /NP  : No progress percentage in log.
    $excludeFiles = @("appsettings.json", "appsettings.Development.json", "appsettings.*.json", "web.config")
    
    $robocopyArgs = @(
        "$TempPublishDir",
        "$IisSitePath",
        "/E",
        "/R:2",
        "/W:2",
        "/NP",
        "/XF"
    ) + $excludeFiles

    & robocopy.exe @robocopyArgs | Out-Null
    $roboExit = $LASTEXITCODE

    # Robocopy exit codes < 8 indicate success (0=no change, 1=copied, 2=extra, 3=copied+extra, etc.)
    if ($roboExit -ge 8) {
        throw "Robocopy failed with exit code $roboExit."
    }
    Write-Success "Binaries synced successfully. Server appsettings.json and web.config remained untouched."

    # 7. Remove app_offline.htm to bring API back online
    Write-Step "Removing app_offline.htm and restoring API..."
    if (Test-Path $appOfflinePath) {
        Remove-Item -Path $appOfflinePath -Force
    }

    # 8. Recycle IIS AppPool if WebAdministration is present
    if (![string]::IsNullOrWhiteSpace($AppPoolName)) {
        try {
            if (Get-Module -ListAvailable -Name WebAdministration) {
                Import-Module WebAdministration -ErrorAction SilentlyContinue
                if (Test-Path "IIS:\AppPools\$AppPoolName") {
                    Write-Step "Recycling IIS Application Pool '$AppPoolName'..."
                    Restart-WebAppPool -Name $AppPoolName
                    Write-Success "AppPool '$AppPoolName' recycled."
                }
            }
        }
        catch {
            Write-WarningMsg "Could not automatically recycle AppPool '$AppPoolName': $_"
        }
    }

    # 9. Clean Temp Directory
    if (Test-Path $TempPublishDir) {
        Remove-Item -Path $TempPublishDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Success "=========================================================="
    Write-Success " RxIntel Backend Deployment Completed Successfully! "
    Write-Success " Target Branch: $TargetBranch | IIS Path: $IisSitePath"
    Write-Success " Protected: appsettings.json, appsettings.*.json, web.config"
    Write-Success "=========================================================="
    exit 0
}
catch {
    Write-Failure "Deployment FAILED: $_"
    # Ensure app_offline.htm is cleaned up even on failure
    $fallbackOffline = Join-Path $IisSitePath "app_offline.htm"
    if (Test-Path $fallbackOffline) {
        Remove-Item -Path $fallbackOffline -Force -ErrorAction SilentlyContinue
    }
    exit 1
}
