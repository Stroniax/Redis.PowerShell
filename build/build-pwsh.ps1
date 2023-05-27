<#

.SYNOPSIS
Builds the PowerShell script module files.

#>

param(
    # Workspace root / directory of solution.
    [Parameter(Mandatory)]
    [string]
    $SolutionDirectory,

    # Directory to publish the module to.
    [Parameter(Mandatory)]
    [string]
    $FullOutputPath
)

if (-not (Test-Path $FullOutputPath)) {
    New-Item -ItemType Directory -Path $FullOutputPath | Out-Null
}

$SourcePath = Join-Path $SolutionDirectory -ChildPath 'src/Redis.PowerShell' | Resolve-Path
$Reset = $PSStyle.Reset

Get-ChildItem -Path $SourcePath -Recurse | ForEach-Object {
    $Source = $_.FullName.Replace($SourcePath.ProviderPath, '', 'OrdinalIgnoreCase')
    $Destination = Join-Path $FullOutputPath -ChildPath $Source
    if (-not (Test-Path $Destination)) {
        return [pscustomobject]@{ Path = $_.FullName; Destination = $Destination }
    }
    $SourceModified = $_.LastWriteTimeUtc
    $DestinationModified = (Get-Item $Destination).LastWriteTimeUtc

    if ($SourceModified -gt $DestinationModified) {
        return [pscustomobject]@{ Path = $_.FullName; Destination = $Destination }
    }
    else {
        $Color = $PSStyle.Foreground.Green
        $Message = "$Source up to date."
        Write-Host "$Color$Message$Reset"
    }
} | Copy-Item -Force -PassThru | ForEach-Object {
    $Color = $PSStyle.Foreground.BrightBlue
    $Subpath = $_.FullName.Replace($FullOutputPath, '', 'OrdinalIgnoreCase')
    $Message = "$Subpath updated."
    Write-Host "$Color$Message$Reset"
}