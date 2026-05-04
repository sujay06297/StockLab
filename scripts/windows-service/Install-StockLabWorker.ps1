[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$ServiceName = "StockLabWorker",
    [string]$DisplayName = "StockLab Worker",
    [string]$Description = "Runs StockLab scheduled stock sync and selection jobs.",
    [string]$Configuration = "Release",
    [string]$PublishPath = "C:\Services\StockLab.Worker",
    [string]$ProjectPath,
    [string]$LocalSettingsPath,
    [switch]$NoStart
)

$ErrorActionPreference = "Stop"

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$scriptRoot = $PSScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $repoRoot "src\StockLab.Worker\StockLab.Worker.csproj"
}

if ([string]::IsNullOrWhiteSpace($LocalSettingsPath)) {
    $LocalSettingsPath = Join-Path (Split-Path -Parent $ProjectPath) "appsettings.Local.json"
}

if (-not (Test-Path $ProjectPath)) {
    throw "Worker project was not found: $ProjectPath"
}

if (-not (Test-Administrator)) {
    throw "Run this script from an elevated PowerShell session."
}

$publishPathFull = [IO.Path]::GetFullPath($PublishPath)
$exePath = Join-Path $publishPathFull "StockLab.Worker.exe"
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService -and $existingService.Status -ne "Stopped") {
    Write-Host "Stopping existing service $ServiceName"
    if ($PSCmdlet.ShouldProcess($ServiceName, "stop service")) {
        Stop-Service -Name $ServiceName -Force
        $existingService.WaitForStatus("Stopped", [TimeSpan]::FromSeconds(30))
    }
}

Write-Host "Publishing StockLab.Worker to $publishPathFull"
if ($PSCmdlet.ShouldProcess($publishPathFull, "dotnet publish")) {
    dotnet publish $ProjectPath -c $Configuration -o $publishPathFull
    if ($LASTEXITCODE -ne 0) {
        if ($existingService -and -not $NoStart) {
            Write-Warning "Publish failed. Starting the existing service again."
            Start-Service -Name $ServiceName
        }
        throw "dotnet publish failed with exit code $LASTEXITCODE. Service update was not applied."
    }
}

if (Test-Path $LocalSettingsPath) {
    Write-Host "Copying local settings from $LocalSettingsPath"
    if ($PSCmdlet.ShouldProcess($publishPathFull, "copy appsettings.Local.json")) {
        Copy-Item -LiteralPath $LocalSettingsPath -Destination (Join-Path $publishPathFull "appsettings.Local.json") -Force
    }
}
else {
    Write-Warning "Local settings file was not found: $LocalSettingsPath"
    Write-Warning "Create it before starting the service if DB password or Discord webhook are required."
}

if (-not (Test-Path $exePath) -and -not $WhatIfPreference) {
    throw "Published worker executable was not found: $exePath"
}

if ($existingService) {
    Write-Host "Updating existing service $ServiceName"
    if ($PSCmdlet.ShouldProcess($ServiceName, "update service binPath and startup")) {
        sc.exe config $ServiceName binPath= "`"$exePath`"" start= auto DisplayName= "`"$DisplayName`"" | Out-Host
        sc.exe description $ServiceName "$Description" | Out-Host
    }
}
else {
    Write-Host "Creating service $ServiceName"
    if ($PSCmdlet.ShouldProcess($ServiceName, "create service")) {
        New-Service `
            -Name $ServiceName `
            -BinaryPathName "`"$exePath`"" `
            -DisplayName $DisplayName `
            -Description $Description `
            -StartupType Automatic | Out-Null
    }
}

if (-not $NoStart) {
    Write-Host "Starting service $ServiceName"
    if ($PSCmdlet.ShouldProcess($ServiceName, "start service")) {
        Start-Service -Name $ServiceName
        Get-Service -Name $ServiceName
    }
}
else {
    Get-Service -Name $ServiceName
}
