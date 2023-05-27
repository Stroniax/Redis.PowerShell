# This script is similar to the Dockerfile, but it is used to build the module locally
#Requires -Version 7.0

[CmdletBinding(DefaultParameterSetName = 'Path')]
param(
    [Parameter()]
    [string]$Configuration = 'Release',

    [Parameter()]
    [System.Management.Automation.SemanticVersion]$Version = '1.0.0',

    [Parameter(ParameterSetName = 'Path')]
    [string]
    $OutputPath,

    [Parameter(ParameterSetName = 'Docker')]
    [switch]
    $Docker
)

if ($Docker) {
    Push-Location $PSScriptRoot

    docker build . -t redis.powershell --build-arg Configuration=$Configuration --build-arg Version=$Version

    Pop-Location
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
$DocsJob = Start-ThreadJob -ScriptBlock {
    Import-Module -Name PlatyPS -ErrorAction Stop

    # Check if the source docs have been updated after the last build
    $Source = Get-ChildItem -Path "$using:PSScriptRoot/docs" -Recurse -Filter *.md
    $Output = Join-Path $using:FullOutputPath -ChildPath 'Redis.PowerShell-help.xml'

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

    if ($RebuildDocs -and $Docssource) {
        New-ExternalHelp -Path $DocsSource -OutputPath $Output -Force
        Write-Host 'Updated docs' -ForegroundColor Magenta
    }
    else {
        Write-Host 'Docs are up to date' -ForegroundColor Green
    }
}

# Build binary module
$BinaryJob = Start-ThreadJob -ScriptBlock {
    dotnet publish $using:PSScriptRoot/src/Redis.PowerShell.Commands /p:Version=$using:Version -c $using:Configuration -o $using:FullOutputPath | Write-Host -ForegroundColor Cyan
}

# Build script module
$ScriptModuleJob = Start-ThreadJob -ScriptBlock {
    $SourcePath = Join-Path $using:PSScriptRoot -ChildPath 'src/Redis.PowerShell' | Resolve-Path
    Get-ChildItem -Path "$using:PSScriptRoot/src/Redis.PowerShell" -Recurse | ForEach-Object {
        $Source = $_.FullName.Replace($SourcePath.ProviderPath, '', 'OrdinalIgnoreCase')
        $Destination = Join-Path $using:FullOutputPath -ChildPath $Source
        if (-not (Test-Path $Destination)) {
            return [pscustomobject]@{ Path = $_.FullName; Destination = $Destination }
        }
        $SourceModified = $_.LastWriteTimeUtc
        $DestinationModified = (Get-Item $Destination).LastWriteTimeUtc
        if ($SourceModified -gt $DestinationModified) {
            Write-Host "Updating $Destination" -ForegroundColor DarkRed
            return [pscustomobject]@{ Path = $_.FullName; Destination = $Destination }
        }
        Write-Host "$Source is up to date" -ForegroundColor Green
    } | Copy-Item -Force
}

# Wait for parallel work to complete
Receive-Job -Job $DocsJob, $BinaryJob, $ScriptModuleJob -Wait -AutoRemoveJob | Out-Null

# Get the commands in a separate process so that we do not lock the dll
$Commands = Start-Job -ScriptBlock {
    Import-Module $using:FullOutputPath/Redis.PowerShell.Commands.dll -PassThru | ForEach-Object { $_.ExportedCommands.Values } | Select-Object Name, CommandType
    Get-ChildItem $using:FullOutputPath -Filter *.psm1 | Import-Module -PassThru | ForEach-Object { $_.ExportedCommands.Values } | Select-Object Name, CommandType
} | Receive-Job -Wait -AutoRemoveJob

# Create the manifest file
$NewManifest = @{
    Path               = "$FullOutputPath/Redis.PowerShell.psd1"
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
    LicenseUri         = 'https://raw.githubusercontent.com/Stroniax/Redis.PowerShell/master/LICENSE'
    IconUri            = 'https://raw.githubusercontent.com/Stroniax/Redis.PowerShell/master/src/Redis.PowerShell/Redis.PowerShell.svg'
    Prerelease         = $Version.PreReleaseLabel
}

$NewManifest.Keys.Where({ !$NewManifest[$_] }).ForEach({ $NewManifest.Remove($_) })
New-ModuleManifest @NewManifest

Get-Item $NewManifest['Path']