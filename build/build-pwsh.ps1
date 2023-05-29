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
    $FullOutputPath,

    # Build configuration. Module (.psm1) files are not copied for Debug builds
    # because it is expected that their absolute paths will be referenced by the
    # manifest file so that breakpoints can still be hit.
    # For Release builds, the .psm1 files are copied to the output directory.
    [Parameter(Mandatory, HelpMessage = "Use 'Debug' to support breakpoints, or 'Release' to copy .psm1 files to output")]
    [ValidateSet('Debug', 'Release')]
    [PSDefaultValue(Value = 'Debug')]
    [string]
    $Configuration
)

if (-not (Test-Path $FullOutputPath)) {
    New-Item -ItemType Directory -Path $FullOutputPath | Out-Null
}

$SourcePath = Join-Path $SolutionDirectory -ChildPath 'src/Redis.PowerShell' | Resolve-Path
$Reset = $PSStyle.Reset

Get-ChildItem -Path $SourcePath -Recurse | ForEach-Object {
    # in Debug configuration, ignore .psm1 files since they will be referenced
    # by the manifest file directly to support debug symbols
    $Source = $_.FullName.Replace($SourcePath.ProviderPath, '', 'OrdinalIgnoreCase')

    if ($Configuration -eq 'Debug' -and $_.Extension -eq '.psm1') {
        Write-Host "$($PSStyle.Foreground.Magenta)$Source not published for Debug configuration.$($PSStyle.Reset)"
        return
    }

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