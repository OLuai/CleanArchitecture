# Instantiates the template in each supported mode, then builds and tests the result.
# Mirrors .github/workflows/test-templates.yml so the matrix can be reproduced locally.
param (
    [string[]]$ClientFramework = @("React", "None")
)

$outputPath = Join-Path (Split-Path $PSScriptRoot -Parent) "artifacts\template-tests"
$results = @()

function CreateAndTestProject {
    param (
        [string]$clientFramework
    )

    $name = $clientFramework
    $projectPath = Join-Path $outputPath $name

    try {
        if (Test-Path $projectPath) {
            Write-Host "Removing existing directory: $name"
            Remove-Item -Recurse -Force $projectPath
        }

        Write-Host "Creating project: $name"
        $startTime = Get-Date

        dotnet new ca-sln --client-framework $clientFramework --name CleanArchitecture --output $projectPath --no-update-check
        if ($LASTEXITCODE -ne 0) { throw "dotnet new ca-sln failed for $name" }

        $exitCode = 0
        Push-Location $projectPath
        try {
            if ($clientFramework -eq "None") {
                Write-Host "Checking API-only output: $name"
                if (Test-Path "./src/Web/ClientApp") { throw "ClientApp should not exist for $name" }
                if (Test-Path "./tests/Web.AcceptanceTests") { throw "Web.AcceptanceTests should not exist for $name" }
                $leaked = Get-ChildItem -Path src, tests -Recurse -File | Select-String -Pattern "UseApiOnly" -SimpleMatch
                if ($leaked) { throw "Conditional markers leaked for ${name}: $($leaked[0].Path)" }
            }

            Write-Host "Building: $name"
            dotnet build --configuration Release
            if ($LASTEXITCODE -ne 0) { throw "Build failed for $name" }

            if ($clientFramework -ne "None") {
                Write-Host "Building client app: $name"
                Push-Location "./src/Web/ClientApp"
                try {
                    npm ci
                    if ($LASTEXITCODE -ne 0) { throw "npm ci failed for $name" }
                    npm run build
                    if ($LASTEXITCODE -ne 0) { throw "npm build failed for $name" }
                } finally {
                    Pop-Location
                }

                Write-Host "Installing Playwright browsers: $name"
                pwsh artifacts/bin/Web.AcceptanceTests/release/playwright.ps1 install --with-deps chromium
                if ($LASTEXITCODE -ne 0) { throw "Playwright install failed for $name" }
            }

            Write-Host "Testing: $name"
            dotnet test --no-build --configuration Release
            if ($LASTEXITCODE -ne 0) { $exitCode = $LASTEXITCODE }
        } finally {
            Pop-Location
        }

        $endTime = Get-Date
        $duration = $endTime - $startTime

        $script:results += [PSCustomObject]@{
            ClientFramework = $clientFramework
            ExitCode        = $exitCode
            Status          = if ($exitCode -eq 0) { "Success" } else { "Failure" }
            Duration        = $duration.ToString("c")
        }
    } catch {
        Write-Host "An error occurred while processing: $name"
        Write-Host $_.Exception.Message
        $script:results += [PSCustomObject]@{
            ClientFramework = $clientFramework
            ExitCode        = -1
            Status          = "Error"
            Duration        = "00:00:00.0000000"
        }
    }
}

if (-not (Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

foreach ($cf in $ClientFramework) {
    CreateAndTestProject -clientFramework $cf
}

$results | Format-Table -Property ClientFramework, Status, Duration -AutoSize

if ($results | Where-Object { $_.Status -ne "Success" }) {
    exit 1
}
