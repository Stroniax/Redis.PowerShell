param(
    [switch]
    $Docker,

    [switch]
    $NoBuild,

    [switch]
    $UseCurrentSession
)

if ($Docker) {
    Get-Command Docker -ErrorAction Stop | Out-Null

    Push-Location $PSScriptRoot

    if (!$NoBuild) {
        docker compose build
    }
    docker compose run -i powershell
    docker compose down

    Pop-Location
    return
}

$DebugModulePath = "$PSScriptRoot/build/Debug/Redis.PowerShell"
if (!$NoBuild) {
    $DebugModulePath = & "$PSScriptRoot/build.ps1" -Configuration 'Debug' -Version '0.0.1-dev'
    if (!$?) {
        return;
    }
}

if (-not (Test-Path $DebugModulePath)) {
    Write-Error "Module not found at $DebugModulePath. Be sure to build the module first."
    return
}

if ($UseCurrentSession) {
    . "$PSScriptRoot/build/start-interactive.ps1" -ModulePath $DebugModulePath
}
else {
    $PowerShell = Get-Process -Id $PID | Select-Object -ExpandProperty Path
    & $PowerShell -NoProfile -NoExit -File "$PSScriptRoot/build/start-interactive.ps1" $DebugModulePath
}