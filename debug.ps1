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

# Enter a new PowerShell session, load the module, and set custom prompt
# (Identical to Docker image)

$PowerShell = Get-Process -Id $PID | Select-Object -ExpandProperty Path

$DebugModulePath = & "$PSScriptRoot/build.ps1" -Configuration 'Debug' -Version '1.0.0'
if (!$?) {
    return;
}

$TempFile = New-TemporaryFile
$TempFile = $TempFile | Rename-Item -NewName "$($TempFile.BaseName).ps1" -PassThru

@'
Push-Location $HOME

$Module = Import-Module $args[0] -PassThru -ErrorAction Stop;

function Prompt {
    $LastCommand = Get-History -Count 1;
    [TimeSpan]$ElapsedTime = $LastCommand.EndExecutionTime - $LastCommand.StartExecutionTime;
    $ModuleText = "$($Module.Name)/$($Module.Version)";
    if ($Module.PrivateData.PSData.Prerelease) {
        $ModuleText += "-$($Module.PrivateData.PSData.Prerelease)";
    }; 
    "$($PSStyle.Foreground.BrightBlack)" +
    "$($ElapsedTime.ToString('mm\:ss\.fff'))`n@ $pwd" +
    "$($PSStyle.Reset)`n" +
    "[$($PSStyle.Foreground.Yellow)$PID$($PSStyle.Reset)] " +
    "$ModuleText> "
}

# Remove this temporary PowerShell init file
Remove-Item $MyInvocation.MyCommand.Path
'@ | Out-File $TempFile

if ($UseCurrentSession) {
    # WARNING: if replacing this with direct script call,
    # be sure to pull out the Remove-Item command
    . $TempFile $DebugModulePath
}
else {
    & $PowerShell -NoProfile -NoExit -File $TempFile $DebugModulePath
}