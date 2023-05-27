<#
.SYNOPSIS

Builds the docker image for the Redis PowerShell module. The image is designed for
testing with a redis image running in parallel (using docker compose) and running
the module on a non-Windows OS. The image has no reason to be published.

#>
param(
    # Workspace root / directory of solution.
    [Parameter(Mandatory)]
    [string]
    $SolutionDirectory,

    # Configuration to build. Dotnet build arg.
    [Parameter()]
    [string]
    $Configuration = 'Release',

    # Version to build. Used in dll version and module version.
    # Semantic version which may include -prerelease suffix.
    [Parameter()]
    [System.Management.Automation.SemanticVersion]
    $Version = '1.0.0'
)

Push-Location $SolutionDirectory

docker build . -t redis.powershell --build-arg Configuration=$Configuration --build-arg Version=$Version

Pop-Location