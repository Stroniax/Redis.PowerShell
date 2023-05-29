[CmdletBinding(DefaultParameterSetName = 'Path')]
param(
    [Parameter()]
    [string]
    $SolutionDirectory = (Split-Path -Path $PSScriptRoot -Parent),

    [Parameter(Mandatory)]
    [System.Management.Automation.SemanticVersion]
    $Version,

    [Parameter(ParameterSetName = 'Path')]
    [string]
    $OutputPath,

    [Parameter(ParameterSetName = 'NuGet')]
    [string]
    $Repository,

    [Parameter(ParameterSetName = 'NuGet')]
    [string]
    $NuGetApiKey
)

# Build the Redis.PowerShell module
$IntermediatePath = Join-Path -Path $SolutionDirectory -ChildPath 'build\intermediate'

if (Test-Path $IntermediatePath) {
    Remove-Item -Path $IntermediatePath -Recurse -Force
}

New-Item -Path $IntermediatePath -ItemType Directory | Out-Null

$BuildPath = Join-Path $SolutionDirectory -ChildPath 'build.ps1'

# Build module
& $BuildPath -OutputPath $IntermediatePath -Version $Version -Configuration Release

# Run tests
Write-Warning "Tests are not yet implemented."

# Copy to output path
if ($PSCmdlet.ParameterSetName -eq 'Path') {
    if (!$OutputPath) {
        # Set the output path to the user's module directory
        $OutputPath = Join-Path -Path $env:PSModulePath.Split(';')[0] -ChildPath "Redis.PowerShell/$Version/"
        if (Test-Path $OutputPath) {
            Write-Warning "Replacing existing module at $OutputPath. Publishing the module does not remove existing items at the destination unless they are being replaced."
        }
    }
    Copy-Item -Path $IntermediatePath -Destination $OutputPath -Recurse -Force
}

# Publish to NuGet
if ($PSCmdlet.ParameterSetName -eq 'NuGet') {
    $PublishModuleParameters = @{
        Path        = $IntermediatePath
        NuGetApiKey = $NuGetApiKey
        Repository  = $Repository
    }
    if ($NuGetApiKey) {
        $PublishModuleParameters['NuGetApiKey'] = $NuGetApiKey
    }

    Publish-Module @PublishModuleParameters
}

Remove-Item -Path $IntermediatePath -Force -Recurse