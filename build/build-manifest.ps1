#Requires -Version 7.0

param(
    [Parameter(Mandatory)]
    [string]
    $SolutionDirectory,

    [Parameter(Mandatory)]
    [string]
    $FullOutputPath,

    # Use 'Release' to build a Release configuration of the binary and publish the
    # .psm1 files to the output directory. 'Debug' will build a Debug binary configuration
    # and will reference the source module files in the .psd1 without copying them directly,
    # to allow for debugging.
    [Parameter(Mandatory, HelpMessage = "Use 'Debug' to support breakpoints (reference source .psm1 files directly)")]
    [ValidateSet('Debug', 'Release')]
    [PSDefaultValue(Value = 'Debug')]
    [string]
    $Configuration,

    [Parameter(Mandatory, HelpMessage = 'Module semantic version, usually 0.0.1-dev for debugging')]
    [PSDefaultValue(Value = '0.0.1-dev')]
    [System.Management.Automation.SemanticVersion]
    $Version
)

if (-not (Test-Path $FullOutputPath)) {
    New-Item -ItemType Directory -Path $FullOutputPath | Out-Null
}

# The manifest uses compiled information from the dll and psm1 files.
# Therefore if any of those files are newer than the existing manifest file,
# we need to rebuild the manifest. Otherwise it is up-to-date.

$ManifestPath = Join-Path $FullOutputPath 'Redis.PowerShell.psd1'

$ScriptSourceDirectory = if ($Configuration -eq 'Debug')
{ Join-Path $SolutionDirectory -ChildPath 'src/Redis.PowerShell' } else
{ $FullOutputPath }

if (Test-Path $ManifestPath) {
    $CurrentManifest = Get-Item $ManifestPath
    $RebuildManifest = $false
    # in Release configuration, source files are published to output
    foreach ($Item in Get-ChildItem -Path $FullOutputPath -Recurse) {
        if ($Item.LastWriteTimeUtc -gt $CurrentManifest.LastWriteTimeUtc) {
            $RebuildManifest = $true
            break
        }
    }
    # in Debug configuration, the psd1 references source psm1 files directly
    if (!$RebuildManifest -and ($ScriptSourceDirectory -ne $FullOutputPath)) {
        foreach ($Item in Get-ChildItem -Path $ScriptSourceDirectory -Filter '*.psm1') {
            if ($Item.LastWriteTimeUtc -gt $CurrentManifest.LastWriteTimeUtc) {
                $RebuildManifest = $true
                break
            }
        }
    }

    if (!$RebuildManifest) {
        # Manifest is up-to-date
        Write-Host "$($PSStyle.Foreground.Green)Manifest is up to date.$($PSStyle.Reset)"
        return $CurrentManifest
    }
    Write-Host "$($PSStyle.Foreground.BrightBlue)Manifest is out of date. Rebuilding...$($PSStyle.Reset)"
}

# Get the commands in a separate process so that we do not lock the dll
$Commands = Start-Job -ScriptBlock {
    Import-Module $using:FullOutputPath/Redis.PowerShell.Commands.dll -PassThru | ForEach-Object { $_.ExportedCommands.Values } | Select-Object Name, CommandType
    Get-ChildItem $using:ScriptSourceDirectory -Filter *.psm1 | Import-Module -PassThru | ForEach-Object { $_.ExportedCommands.Values } | Select-Object Name, CommandType
} | Receive-Job -Wait -AutoRemoveJob

if (!$?) {
    # Getting the commands failed
    return;
}

$FileList = Get-ChildItem -Path $FullOutputPath -Recurse -Name
if ($ScriptSourceDirectory -ne $FullOutputPath) {
    $FileList += Get-ChildItem -Path $ScriptSourceDirectory -Filter '*.psm1'
}
$ScriptSourceNameParam = @{}
if ($ScriptSourceDirectory -eq $FullOutputPath) {
    $ScriptSourceNameParam['Name'] = $true
}

# Create the manifest file
$NewManifest = @{
    Path               = Join-Path $FullOutputPath 'Redis.PowerShell.psd1'
    RootModule         = 'Redis.PowerShell.Commands.dll'
    RequiredAssemblies = @( Get-ChildItem -Path $FullOutputPath -Filter '*.dll' -Name)
    NestedModules      = [string[]]@( Get-ChildItem -Path $ScriptSourceDirectory -Filter '*.psm1' @ScriptSourceNameParam )
    FunctionsToExport  = @( $Commands | ForEach-Object { if ($_.CommandType.Value -eq 'Function') { $_.Name } } )
    CmdletsToExport    = @( $Commands | ForEach-Object { if ($_.CommandType.Value -eq 'Cmdlet') { $_.Name } } )
    AliasesToExport    = @( $Commands | ForEach-Object { if ($_.CommandType.Value -eq 'Alias') { $_.Name } } )
    TypesToProcess     = @( Get-ChildItem -Path $FullOutputPath -Filter '*.types.ps1xml' -Name)
    FormatsToProcess   = @( Get-ChildItem -Path $FullOutputPath -Filter '*.formats.ps1xml' -Name)
    ModuleVersion      = $Version
    Author             = 'Caleb Frederickson'
    CompanyName        = 'Kenai Peninsula Borough School District'
    Description        = 'PowerShell API to connect to Redis'
    Copyright          = 'Kenai Peninsula Borough School District (c) 2023'
    FileList           = $FileList
    Tags               = @( 'Redis', 'Cache', 'StackExchange.Redis' )
    ProjectUri         = 'https://www.github.com/Stroniax/Redis.PowerShell'
    LicenseUri         = 'https://raw.githubusercontent.com/Stroniax/Redis.PowerShell/master/LICENSE.txt'
    IconUri            = 'https://raw.githubusercontent.com/Stroniax/Redis.PowerShell/master/src/Redis.PowerShell/Redis.PowerShell.svg'
}

if ($Version.PreReleaseLabel) {
    $NewManifest['PreRelease'] = $Version.PreReleaseLabel
}

New-ModuleManifest @NewManifest

Get-Item $NewManifest['Path']