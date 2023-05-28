#Requires -Version 7.0

param(
    [Parameter(Mandatory)]
    [string]
    $SolutionDirectory,

    [Parameter(Mandatory)]
    [string]
    $FullOutputPath,

    [Parameter(Mandatory)]
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

if (Test-Path $ManifestPath) {
    $CurrentManifest = Get-Item $ManifestPath
    $NewerFiles = Get-ChildItem -Path $FullOutputPath -Recurse | Where-Object LastWriteTimeUtc -gt $CurrentManifest.LastWriteTimeUtc | Select-Object -First 1
    if (!$NewerFiles) {
        # Manifest is up-to-date
        Write-Host "$($PSStyle.Foreground.Green)Manifest is up-to-date.$($PSStyle.Reset)"
        return $CurrentManifest
    }
    Write-Host "$($PSStyle.Foreground.BrightBlue)Manifest is out-of-date. Rebuilding...$($PSStyle.Reset)"
}

# Get the commands in a separate process so that we do not lock the dll
$Commands = Start-Job -ScriptBlock {
    Import-Module $using:FullOutputPath/Redis.PowerShell.Commands.dll -PassThru | ForEach-Object { $_.ExportedCommands.Values } | Select-Object Name, CommandType
    Get-ChildItem $using:FullOutputPath -Filter *.psm1 | Import-Module -PassThru | ForEach-Object { $_.ExportedCommands.Values } | Select-Object Name, CommandType
} | Receive-Job -Wait -AutoRemoveJob

if (!$?) {
    # Getting the commands failed
    return;
}

# Create the manifest file
$NewManifest = @{
    Path               = Join-Path $FullOutputPath 'Redis.PowerShell.psd1'
    RootModule         = 'Redis.PowerShell.Commands.dll'
    RequiredAssemblies = @( Get-ChildItem -Path $FullOutputPath -Filter '*.dll' -Name)
    NestedModules      = @( Get-ChildItem -Path $FullOutputPath -Filter '*.psm1' -Name)
    FunctionsToExport  = @( $Commands | ForEach-Object { if ($_.CommandType -eq 'Function') { $_.Name } } )
    CmdletsToExport    = @( $Commands | ForEach-Object { if ($_.CommandType -eq 'Cmdlet') { $_.Name } } )
    AliasesToExport    = @( $Commands | ForEach-Object { if ($_.CommandType -eq 'Alias') { $_.Name } } )
    TypesToProcess     = @( Get-ChildItem -Path $FullOutputPath -Filter '*.types.ps1xml' -Name)
    FormatsToProcess   = @( Get-ChildItem -Path $FullOutputPath -Filter '*.formats.ps1xml' -Name)
    ModuleVersion      = $Version
    Author             = 'Caleb Frederickson'
    CompanyName        = 'Kenai Peninsula Borough School District'
    Description        = 'PowerShell API to connect to Redis'
    Copyright          = 'Kenai Peninsula Borough School District (c) 2023'
    FileList           = @( Get-ChildItem -Path $FullOutputPath -Recurse -Name )
    Tags               = @( 'Redis', 'Cache', 'StackExchange.Redis' )
    ProjectUri         = 'https://www.github.com/Stroniax/Redis.PowerShell'
    LicenseUri         = 'https://raw.githubusercontent.com/Stroniax/Redis.PowerShell/master/LICENSE.txt'
    IconUri            = 'https://raw.githubusercontent.com/Stroniax/Redis.PowerShell/master/src/Redis.PowerShell/Redis.PowerShell.svg'
    Prerelease         = $Version.PreReleaseLabel
}

$NewManifest.Keys.Where({ !$NewManifest[$_] }).ForEach({ $NewManifest.Remove($_) })

New-ModuleManifest @NewManifest

Get-Item $NewManifest['Path']