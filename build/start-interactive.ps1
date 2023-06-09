<#
This script is the interactive PowerShell state to use when developing the module.
#>

param(
    [Parameter()]
    [string]
    $ModulePath,

    [Parameter()]
    [switch]
    $NoExit
)

Push-Location $HOME

if (-not ($ModulePath)) {
    $ModulePath = Split-Path -Path $PSScriptRoot -Parent
    $ModulePath = Join-Path $ModulePath -ChildPath 'build/Debug/Redis.PowerShell'
}
$Module = Import-Module $ModulePath -PassThru -ErrorAction Stop

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

if ($NoExit) {
    $Host.EnterNestedPrompt()
}