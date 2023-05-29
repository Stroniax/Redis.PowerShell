#Requires -Module PlatyPS

param(
    [Parameter(Mandatory)]
    [string]
    $SolutionDirectory
)

# Import into a separate process so that we do not lock the dll
Start-Job -ScriptBlock {
    # Import the module
    $Module = Import-Module $using:SolutionDirectory/build/Debug/Redis.PowerShell -PassThru
    if (!$?) {
        return
    }

    # Build the documentation
    $UpdateMarkdownHelp = @{
        Module            = $Module
        OutputFolder      = Join-Path $using:SolutionDirectory -ChildPath docs
        Force             = $true
        UpdateInputOutput = $true
        UseFullTypeName   = $true
        ExcludeDontShow   = $true
    }
    Update-MarkdownHelp @UpdateMarkdownHelp
} | Receive-Job -Wait -AutoRemoveJob
