#Requires -Module PlatyPS
<#

.SYNOPSIS
Creates a published help documentation file using the PlatyPS module
and the source markdown documentation in the /docs directory.

#>
param(
    # Workspace root / directory of solution.
    [Parameter(Mandatory)]
    [string]
    $SolutionDirectory,

    # Directory to publish the documentation to.
    [Parameter(Mandatory)]
    [string]
    $FullOutputPath
)

if (-not (Test-Path $FullOutputPath)) {
    New-Item -ItemType Directory -Path $FullOutputPath | Out-Null
}

Import-Module -Name PlatyPS -ErrorAction Stop

# Check if the source docs have been updated after the last build
$SourceDirectory = Join-Path $SolutionDirectory -ChildPath 'docs'
$Source = Get-ChildItem -Path $SourceDirectory -Recurse -Filter *.md
$Output = Join-Path $FullOutputPath -ChildPath 'Redis.PowerShell-help.xml'

if (Test-Path $Output) {
    $RebuildDocs = $false
    $BuildTime = (Get-Item $Output).LastWriteTimeUTc
    foreach ($Item in $Source) {
        if ($Item.LastWriteTimeUtc -gt $BuildTime) {
            $RebuildDocs = $true
            break
        }
    }
}
else {
    $RebuildDocs = $true
}

if ($RebuildDocs -and $Source) {
    New-ExternalHelp -Path $Source -OutputPath $Output -Force
    $Color = $PSStyle.Foreground.BrightBlue
    $Message = 'Documentation updated.'
}
else {
    $Color = $PSStyle.Foreground.Green
    $Message = 'Documentation up to date.'  
}
$Reset = $PSStyle.Reset
Write-Host "$Color$Message$Reset"
