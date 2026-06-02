param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Name
)

. (Join-Path $PSScriptRoot '_common.ps1')

Initialize-DbToolingEnv

dotnet ef migrations add $Name `
    --project $script:InfrastructureProject `
    --startup-project $script:StartupProject `
    --output-dir Migrations
