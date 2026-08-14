# Shared helpers for EF Core scripts. Dot-source from sibling scripts.
#
# Connection string contract (matches the shared Aspire-managed Postgres container):
#   Host=localhost ; Port=5431 ; Database=CleanArchitectureDb ; Username=postgres
#   Password=$env:CA_SHARED_PG_PWD
#
# CA_SHARED_PG_PWD is machine-wide and deliberately not named after the project: every solution
# generated from this template targets the same PostgreSQL container, each with its own database
# inside it, so they all authenticate with the same password. Set it once:
#
#   setx CA_SHARED_PG_PWD '<the password the container was created with>'
#
# then reopen the shell. The AppHost reads the same variable.
#
# The connection string is exported as ConnectionStrings__CleanArchitectureDb so that the
# IDesignTimeDbContextFactory in src/Infrastructure picks it up without launching a host.

$ErrorActionPreference = 'Stop'

$script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$script:InfrastructureProject = Join-Path $RepoRoot 'src\Infrastructure\Infrastructure.csproj'
$script:StartupProject       = Join-Path $RepoRoot 'src\Migration\Migration.csproj'

function Get-CleanArchitectureConnectionString {
    $password = $env:CA_SHARED_PG_PWD
    if ([string]::IsNullOrWhiteSpace($password)) {
        Write-Warning "CA_SHARED_PG_PWD environment variable is not set. Falling back to 'postgres' (works only if the shared container was created with that password)."
        $password = 'postgres'
    }
    return "Host=localhost;Port=5431;Database=CleanArchitectureDb;Username=postgres;Password=$password"
}

function Initialize-DbToolingEnv {
    $env:ConnectionStrings__CleanArchitectureDb = Get-CleanArchitectureConnectionString

    Push-Location $RepoRoot
    try {
        dotnet tool restore | Out-Null
    } finally {
        Pop-Location
    }
}
