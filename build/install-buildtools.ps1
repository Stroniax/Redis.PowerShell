# Obviously, PowerShell is already required to build the module.
# Since this is a PowerShell script, it's assumed that the user
# has at least PowerShell 5.1 installed.
# (Who even uses PowerShell 5.0 anymore?)

# However, most build tools here require PowerShell 7.0 or greater,
# so we may need to install it.

# We also need the PlatyPS module installed.

# To build the binaries we need the .net 5.0 or greater SDK.

# Docker is optional and is a self-contained build environment so we
# don't need to bother installing it - if the user has it, they can
# run the full build pipeline from within docker.

throw [System.NotImplementedException]::new('Install-BuildTools is not implemented, but the required tools are indicated in the comments of the script. Consider manually installing the tools required based on the comments.')