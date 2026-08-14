param (
    [string]$Target = "Default",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$solution = "CleanArchitecture.slnx"
#if (!UseApiOnly)
$clientApps = @("./src/Web/ClientApp")
#endif

Write-Host "Building solution..."
dotnet build $solution --configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

#if (!UseApiOnly)
foreach ($clientApp in $clientApps) {
    if (Test-Path $clientApp) {
        Write-Host "Installing client dependencies ($clientApp)..."
        Push-Location $clientApp
        try {
            npm ci
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            npm run build
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        } finally {
            Pop-Location
        }
    }
}
#endif

Write-Host "Testing solution..."
if ($Target -eq "Basic") {
    $testProjects = Get-ChildItem -Path "tests" -Filter "*.csproj" -Recurse |
        Where-Object { $_.Name -notlike "*AcceptanceTests*" }
    foreach ($project in $testProjects) {
        dotnet test $project.FullName --no-build --configuration $Configuration
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
} else {
    dotnet test $solution --no-build --configuration $Configuration
    exit $LASTEXITCODE
}
