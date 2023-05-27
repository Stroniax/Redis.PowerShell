# Use dotnet sdk to restore and build the csproj

FROM mcr.microsoft.com/dotnet/sdk:7.0 as dotnet-build

# NuGet restore
COPY ./src/Redis.PowerShell.Commands/Redis.PowerShell.Commands.csproj ./src/Redis.PowerShell.Commands/Redis.PowerShell.Commands.csproj
RUN dotnet restore ./src/Redis.PowerShell.Commands/Redis.PowerShell.Commands.csproj

# dotnet publish
COPY ./src/Redis.PowerShell.Commands ./src/Redis.PowerShell.Commands
ARG VERSION=1.0.0
ARG CONFIGURATION=Release
RUN dotnet publish ./src/Redis.PowerShell.Commands/Redis.PowerShell.Commands.csproj -o ./out -c ${CONFIGURATION} /p:Version=${VERSION}

# Use PlatyPS to build the docs

FROM mcr.microsoft.com/powershell:latest as platyps-build
SHELL ["pwsh", "-Command"]

RUN Install-Module -Name PlatyPS -Force -SkipPublisherCheck -AcceptLicense
RUN Import-Module PlatyPS

COPY ./docs ./docs

RUN New-ExternalHelp -Path ./docs -OutputPath ./out -Force

# Use powershell to build the script module

FROM mcr.microsoft.com/powershell:latest as pwsh-build
SHELL ["pwsh", "-Command"]

# build the module
COPY ./src/Redis.PowerShell ./out
COPY --from=dotnet-build ./out ./out
COPY --from=platyps-build ./out ./out

ARG VERSION=1.0.0
ENV PSREDIS_VERSION=$VERSION

RUN \
    $ErrorActionPreference = 'Stop';\
    # Load compiled cmdlets in a separate process so that the dll isn't locked
    $Cmdlets = Start-Job -ScriptBlock { Import-Module './out/Redis.PowerShell.Commands.dll' -PassThru | % { $_.ExportedCommands.Values } | Select Name, CommandType } | Receive-Job -Wait -AutoRemoveJob; \
    $Functions = Start-Job -ScriptBlock { Get-ChildItem ./out/*.psm1 | Import-Module -PassThru | % { $_.ExportedCommands.Values } | Select Name, CommandType } | Receive-Job -Wait -AutoRemoveJob; \
    $Commands = $Cmdlets + $Functions; \
    New-ModuleManifest \
    -Path ./out/Redis.PowerShell.psd1 \
    -RootModule 'Redis.PowerShell.Commands.dll' \
    -RequiredAssemblies @( Get-ChildItem -Path ./out -Filter '*.dll' -Name) \
    -NestedModules @( Get-ChildItem -Path ./out -Filter '*.psm1' -Name) \
    -FunctionsToExport @( $Commands | % { if ($_.CommandType -eq 'Function') { $_.Name }} ) \
    -CmdletsToExport @( $Commands | % { if ($_.CommandType -eq 'Cmdlet') { $_.Name }} ) \
    -AliasesToExport @( $Commands | % { if ($_.CommandType -eq 'Alias') { $_.Name }} ) \
    -TypesToProcess @( Get-ChildItem -Path ./out -Filter '*.types.ps1xml' -Name) \
    -FormatsToProcess @( Get-ChildItem -Path ./out -Filter '*.formats.ps1xml' -Name) \
    -ModuleVersion ([System.Management.Automation.SemanticVersion]$env:PSREDIS_VERSION) \
    -Author 'Caleb Frederickson' \
    -CompanyName 'Kenai Peninsula Borough School District' \
    -Description 'PowerShell API to connect to Redis' \
    -Copyright 'Kenai Peninsula Borough School District (c) 2023' \
    -FileList @( Get-ChildItem -Path ./out -Recurse -Name ) \
    -Tags @( 'Redis', 'Cache', 'StackExchange.Redis' ) \
    -ProjectUri 'https://www.github.com/Stroniax/Redis.PowerShell' \
    -LicenseUri 'https://raw.githubusercontent.com/Stroniax/Redis.PowerShell/master/LICENSE' \
    -IconUri 'https://raw.githubusercontent.com/Stroniax/Redis.PowerShell/master/src/Redis.PowerShell/Redis.PowerShell.svg'
#    -Prerelease ([System.Management.Automation.SemanticVersion]$VERSION).PreReleaseLabel\

# Use powershell to run the module
FROM mcr.microsoft.com/powershell:latest as runtime
SHELL ["pwsh", "-Command"]
ENV REDIS_PSModulePath=/usr/local/share/powershell/Modules
RUN $env:REDIS_PSModulePath = ($env:PSModulePath -split [System.IO.Path]::PathSeparator)[-1]

COPY --from=pwsh-build ./out ${REDIS_PSModulePath}/Redis.PowerShell

ENTRYPOINT pwsh -noexit -command {\
    $Module = Import-Module Redis.PowerShell -PassThru -ErrorAction Stop;\
    function Prompt {\
    $LastCommand = Get-History -Count 1;\
    $ElapsedTime = $LastCommand.EndExecutionTime - $LastCommand.StartExecutionTime;\
    $ModuleText = "$($Module.Name)/$($Module.Version)";\
    if ($Module.PrivateData.PSData.Prerelease) {\
    $ModuleText += "-$($Module.PrivateData.PSData.Prerelease)";\
    }; \
    "$($PSStyle.Foreground.BrightBlack)" +\
    "$($ElapsedTime.ToString('mm\:ss\.fff')) @ $pwd" +\
    "$($PSStyle.Reset)`n" +\
    "[$($PSStyle.Foreground.Yellow)$PID$($PSStyle.Reset)] " +\
    "$ModuleText> "\
    }\
    }