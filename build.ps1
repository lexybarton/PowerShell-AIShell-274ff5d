## Copyright (c) Microsoft Corporation.
## Licensed under the MIT License.

[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $Configuration = "Debug",

    [Parameter()]
    [switch]
    $Clean
)

try {
    Write-Host "Building ShellCopilot..."
    Push-Location "$PSScriptRoot/shell/ShellCopilot.App"

    $outPath = "$PSScriptRoot/out/ShellCopilot.App"

    if ($Clean) {
        if (Test-Path $outPath) {
            Write-Verbose "Deleting $outPath"
            Remove-Item -recurse -force -path $outPath
        }

        dotnet clean
    }

    dotnet publish --output $outPath --configuration $Configuration
    Write-Host "ShellCopilot built successfully, output path: $outPath"
    
}
finally {
    Pop-Location
}
