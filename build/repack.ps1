# Packs the template into a versioned .nupkg and installs it in place of any previous install,
# so `dotnet new ca-sln` keeps using that snapshot whatever branch this folder is on afterwards.
#
#   .\build\repack.ps1                  # version 1.0.<commit count>, "-dirty" with local changes
#   .\build\repack.ps1 -Version 1.2.0
param (
    [string]$Version
)

$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$packageId = "OLuai.CleanArchitecture.Template"
$outputPath = Join-Path $root "artifacts\packages"

Push-Location $root
try {
    if (-not $Version) {
        $commits = (git rev-list --count HEAD).Trim()
        $Version = "1.0.$commits"
        if (git status --porcelain) { $Version = "$Version-dirty" }
    }

    dotnet pack build/Template.csproj -p:PackageVersion=$Version -o $outputPath --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed" }

    # Replace both kinds of previous install: this package, and an install from this folder.
    # Both register ca-sln, and dotnet new refuses two templates with the same short name.
    # Either may be absent; Windows PowerShell would turn their stderr into a terminating error.
    & { $ErrorActionPreference = "Continue"; dotnet new uninstall $packageId *> $null }
    & { $ErrorActionPreference = "Continue"; dotnet new uninstall $root *> $null }

    $package = Join-Path $outputPath "$packageId.$Version.nupkg"
    dotnet new install $package --force
    if ($LASTEXITCODE -ne 0) { throw "dotnet new install failed" }

    Write-Host "Installed $packageId $Version"
} finally {
    Pop-Location
}
