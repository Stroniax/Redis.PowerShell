#Requires -Version 7.0

<#

.SYNOPSIS
Builds the binary root module (C# project) and publishes it to the output directory.
Unlike most build scripts, this one is not used in the docker build process because
we are able to perform the build using a dotnet sdk image without the need for
PowerShell.

#>

[CmdletBinding()]
param(
    # Workspace root / directory of solution.
    [Parameter(Mandatory)]
    [string]
    $SolutionDirectory,

    [Parameter(Mandatory)]
    [string]
    $FullOutputPath,

    # Version to build. Used in dll version and module version.
    # Semantic version which may include -prerelease suffix.
    [Parameter(Mandatory, HelpMessage = 'Binary module semantic version, usually 0.0.1-dev for debugging')]
    [PSDefaultValue(Value = '0.0.1-dev')]
    [System.Management.Automation.SemanticVersion]
    $Version = '0.0.1-dev',

    # Configuration to build. Dotnet build arg.
    [Parameter(Mandatory, HelpMessage = 'DotNet build configuration')]
    [PSDefaultValue(Value = 'Debug')]
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration
)

$Source = Join-Path $SolutionDirectory -ChildPath 'src/Redis.PowerShell.Commands'
dotnet publish $Source /p:Version=$Version -c $Configuration -o $FullOutputPath | Write-Host -ForegroundColor Cyan