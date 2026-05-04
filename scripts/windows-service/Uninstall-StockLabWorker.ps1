[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$ServiceName = "StockLabWorker"
)

$ErrorActionPreference = "Stop"

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    throw "Run this script from an elevated PowerShell session."
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Service $ServiceName does not exist."
    return
}

if ($service.Status -ne "Stopped") {
    Write-Host "Stopping service $ServiceName"
    if ($PSCmdlet.ShouldProcess($ServiceName, "stop service")) {
        Stop-Service -Name $ServiceName -Force
        $service.WaitForStatus("Stopped", [TimeSpan]::FromSeconds(30))
    }
}

Write-Host "Deleting service $ServiceName"
if ($PSCmdlet.ShouldProcess($ServiceName, "delete service")) {
    sc.exe delete $ServiceName | Out-Host
}
