# This script is similar to the Dockerfile, but it is used to build the module locally
#Requires -Version 7.0

[CmdletBinding(DefaultParameterSetName = 'Path')]
param(
    [Parameter()]
    [string]
    $Configuration = 'Release',

    [Parameter()]
    [System.Management.Automation.SemanticVersion]
    $Version = '1.0.0',

    [Parameter(ParameterSetName = 'Path')]
    [string]
    $OutputPath,

    [Parameter(ParameterSetName = 'Docker')]
    [switch]
    $Docker
)

if ($Docker) {
    & "$PSScriptRoot/build/build-docker.ps1" -SolutionDirectory $PSScriptRoot -Configuration $Configuration -Version $Version
    return
}

if (!$OutputPath) {
    $FullOutputPath = "$PSScriptRoot/build/$Configuration/Redis.PowerShell/$Version"
}
else {
    $FullOutputPath = "$OutputPath/Redis.PowerShell/$Version"
}

if (-not (Test-Path $FullOutputPath)) {
    New-Item -ItemType Directory -Path $FullOutputPath | Out-Null
}

# Compile documentation
$BuildDocs = @{
    'FilePath' = Join-Path $PSScriptRoot 'build/build-docs.ps1'
    'ArgumentList' = $PSScriptRoot, $FullOutputPath
}
$BuildDocsJob = Start-ThreadJob @BuildDocs

# Build binary module
$BuildDll = @{
    'FilePath' = Join-Path $PSScriptRoot 'build/build-dll.ps1'
    'ArgumentList' = $PSScriptRoot, $FullOutputPath, $Version, $Configuration
}
$BuildDllJob = Start-ThreadJob @BuildDll

# Build script module
$BuildPwsh = @{
    'FilePath' = Join-Path $PSScriptRoot 'build/build-pwsh.ps1'
    'ArgumentList' = $PSScriptRoot, $FullOutputPath
}
$BuildPwshJob = Start-ThreadJob @BuildPwsh

# Wait for parallel work to complete
$ReceiveJobs = @{
    'Job' = $BuildDocsJob, $BuildDllJob, $BuildPwshJob
    'Wait' = $true
    'AutoRemoveJob' = $true
    'ErrorAction' = 'Stop'
}
Receive-Job @ReceiveJobs | Out-Null

& "$PSScriptRoot/build/build-manifest.ps1" -SolutionDirectory $PSScriptRoot -FullOutputPath $FullOutputPath -Version $Version
