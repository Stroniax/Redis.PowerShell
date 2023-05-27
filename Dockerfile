# Use dotnet sdk to restore and build the csproj

FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-dll

# NuGet restore
COPY ./src/Redis.PowerShell.Commands/Redis.PowerShell.Commands.csproj ./src/Redis.PowerShell.Commands/Redis.PowerShell.Commands.csproj
RUN dotnet restore ./src/Redis.PowerShell.Commands/Redis.PowerShell.Commands.csproj

# dotnet publish
COPY ./src/Redis.PowerShell.Commands ./src/Redis.PowerShell.Commands
ARG VERSION=1.0.0
ARG CONFIGURATION=Release
RUN dotnet publish ./src/Redis.PowerShell.Commands/Redis.PowerShell.Commands.csproj -o ./out -c ${CONFIGURATION} /p:Version=${VERSION}

# Use PlatyPS to build the docs

FROM mcr.microsoft.com/powershell:latest as build-docs
SHELL ["pwsh", "-Command"]

RUN Install-Module -Name PlatyPS -Force -SkipPublisherCheck -AcceptLicense
RUN Import-Module PlatyPS

COPY ./build/build-docs.ps1 ./build-docs.ps1
COPY ./docs ./docs

RUN & ./build-docs.ps1 -SolutionDirectory '.' -FullOutputPath './out'

# Use powershell to build the script module

FROM mcr.microsoft.com/powershell:latest as build-pwsh
SHELL ["pwsh", "-Command"]

# build the script module
COPY ./build/build-pwsh.ps1 ./build-pwsh.ps1
COPY ./src/Redis.PowerShell ./src/Redis.PowerShell

RUN & ./build-pwsh.ps1 -SolutionDirectory '.' -FullOutputPath './out'

# Use PowerShell to build the manifest
FROM mcr.microsoft.com/powershell:latest as build-manifest
SHELL ["pwsh", "-Command"]

# build the manifest
COPY ./build/build-manifest.ps1 ./build-manifest.ps1
COPY --from=build-pwsh ./out ./out
COPY --from=build-dll ./out ./out
COPY --from=build-docs ./out ./out

ARG VERSION=1.0.0

RUN & ./build-manifest.ps1 -SolutionDirectory '.' -FullOutputPath './out' -Version ${VERSION}

# Use powershell to run the module
FROM mcr.microsoft.com/powershell:latest as runtime
SHELL ["pwsh", "-Command"]

# identify the path at which to install the module
ENV REDIS_PSModulePath=/usr/local/share/powershell/Modules
RUN $env:REDIS_PSModulePath = ($env:PSModulePath -split [System.IO.Path]::PathSeparator)[-1]

# install the run script
COPY ./build/start-interactive.ps1 ./start-interactive.ps1

# install the module
COPY --from=build-manifest ./out ${REDIS_PSModulePath}/Redis.PowerShell

# start the interactive shell
ENTRYPOINT pwsh -NoProfile -NoExit -File ./start-interactive.ps1 'Redis.PowerShell'