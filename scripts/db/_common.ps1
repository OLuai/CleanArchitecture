# Shared helpers for EF Core scripts. Dot-source from sibling scripts.
#
# Connection string contract (matches the Aspire AppHost-managed Postgres container):
#   Host=localhost ; Port=5431 ; Database=CleanArchitectureDb ; Username=postgres
#   Password=$env:CLEANARCHITECTURE_PG_PWD  (set this once for your machine, e.g.:
#                                    setx CLEANARCHITECTURE_PG_PWD '<the password you gave to
#                                    dotnet user-secrets set Parameters:postgres-password>'
#                                    and reopen the shell)
#
# The connection string is exported as ConnectionStrings__CleanArchitectureDb so that the
# IDesignTimeDbContextFactory in src/Infrastructure picks it up without launching a host.

$ErrorActionPreference = 'Stop'

$script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$script:InfrastructureProject = Join-Path $RepoRoot 'src\Infrastructure\Infrastructure.csproj'
$script:StartupProject       = Join-Path $RepoRoot 'src\Migration\Migration.csproj'

function Get-CleanArchitectureConnectionString {
    $pwd = $env:CLEANARCHITECTURE_PG_PWD
    if ([string]::IsNullOrWhiteSpace($pwd)) {
        Write-Warning "CLEANARCHITECTURE_PG_PWD environment variable is not set. Falling back to 'postgres' (works only if you chose that password for the AppHost parameter)."
        $pwd = 'postgres'
    }
    return "Host=localhost;Port=5431;Database=CleanArchitectureDb;Username=postgres;Password=$pwd"
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
