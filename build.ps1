# This script is similar to the Dockerfile, but it is used to build the module locally
#Requires -Version 7.0

[CmdletBinding(DefaultParameterSetName = 'Path')]
param(
    # Use 'Release' to build a Release configuration of the binary and publish the
    # .psm1 files to the output directory. 'Debug' will build a Debug binary configuration
    # and will reference the source module files in the .psd1 without copying them directly,
    # to allow for debugging.
    [Parameter()]
    [string]
    $Configuration = 'Debug',

    [Parameter()]
    [System.Management.Automation.SemanticVersion]
    $Version = '0.0.1-dev',

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
    if ($Configuration -eq 'Debug') {
        $FullOutputPath = Join-Path $PSScriptRoot 'build/Debug/Redis.PowerShell'
    }
    else {
        $FullOutputPath = Join-Path $PSScriptRoot 'build/Release/Redis.PowerShell/' $Version
    }
}
else {
    if ($OutputPath -match '.*[\\/]?redis\.powershell(?:\/[\d.a-zA-Z-]+)?[\\/]?$') {
        $FullOutputPath = $OutputPath
    }
    else {
        Write-Warning 'Output path must end with "Redis.PowerShell" or "Redis.PowerShell/<version>". The required path will be appended to the provided output path.'
        $FullOutputPath = "$OutputPath/Redis.PowerShell/$Version"
    }
}

if (-not (Test-Path $FullOutputPath)) {
    New-Item -ItemType Directory -Path $FullOutputPath | Out-Null
}

# Compile documentation
$BuildDocs = @{
    'FilePath'     = Join-Path $PSScriptRoot 'build/build-docs.ps1'
    'ArgumentList' = $PSScriptRoot, $FullOutputPath
}
$BuildDocsJob = Start-ThreadJob @BuildDocs

# Build binary module
$BuildDll = @{
    'FilePath'     = Join-Path $PSScriptRoot 'build/build-dll.ps1'
    'ArgumentList' = $PSScriptRoot, $FullOutputPath, $Version, $Configuration
}
$BuildDllJob = Start-ThreadJob @BuildDll

# Build script module
$BuildPwsh = @{
    'FilePath'     = Join-Path $PSScriptRoot 'build/build-pwsh.ps1'
    'ArgumentList' = $PSScriptRoot, $FullOutputPath, $Configuration
}
$BuildPwshJob = Start-ThreadJob @BuildPwsh

# Wait for parallel work to complete
$ReceiveJobs = @{
    'Job'           = $BuildDocsJob, $BuildDllJob, $BuildPwshJob
    'Wait'          = $true
    'AutoRemoveJob' = $true
    'ErrorAction'   = 'Stop'
}
Receive-Job @ReceiveJobs | Out-Null

$BuildManifest = @{
    SolutionDirectory = $PSScriptRoot
    FullOutputPath    = $FullOutputPath
    Version           = $Version
    Configuration     = $Configuration
}
& "$PSScriptRoot/build/build-manifest.ps1" @BuildManifest
