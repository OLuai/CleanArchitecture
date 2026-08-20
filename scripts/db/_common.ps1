# Shared helpers for EF Core scripts. Dot-source from sibling scripts.
#
# Connection string contract (matches the shared Aspire-managed Postgres container):
#   Host=localhost ; Port=<the port ca-shared-postgres is published on>
#   Database=CleanArchitectureDb ; Username=postgres ; Password=$env:CA_SHARED_PG_PWD
#
# The port is asked of Docker rather than hard-coded. The AppHost requests
# Services.Shared.PostgresHostPort, but the container is persistent and reused by name, and a
# container keeps the port mapping it was created with: one created before the fixed port was
# declared - or by an older revision of the template - stays on whatever port Docker assigned it.
# A hard-coded 5431 therefore pointed at nothing while the real instance was up.
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

# Mirrors Services.Shared in src/Shared/Services.cs.
$script:SharedPostgresContainer  = 'ca-shared-postgres'
$script:SharedPostgresDataVolume = 'ca-shared-pg-data'
$script:DeclaredPostgresHostPort = 5431

function Get-CleanArchitecturePostgresPort {
    $ports = $null
    try {
        $filter = 'name=^{0}$' -f $script:SharedPostgresContainer
        $ports = docker ps --filter $filter --format '{{.Ports}}'
    } catch {
        Write-Warning "Docker could not be reached: $($_.Exception.Message)"
    }

    if ($ports) {
        # e.g. "127.0.0.1:62574->5432/tcp". A container can list several bindings (IPv4 and IPv6);
        # any of them reaches the same instance, so the first match will do.
        $match = [regex]::Match((@($ports) -join "`n"), ':(?<port>\d+)->5432/tcp')
        if ($match.Success) {
            $port = [int]$match.Groups['port'].Value

            if ($port -ne $script:DeclaredPostgresHostPort) {
                Write-Warning ("'{0}' is published on port {1}, not the declared {2}. The container predates the fixed host port and keeps the mapping it was created with. These scripts follow it, so nothing is broken; to move it, recreate the container with 'docker rm -f {0}' - the {3} volume keeps every database." -f `
                    $script:SharedPostgresContainer, $port, $script:DeclaredPostgresHostPort, $script:SharedPostgresDataVolume)
            }

            return $port
        }
    }

    Write-Warning ("Could not read the published port of '{0}' from Docker - is the container running? Falling back to {1}." -f `
        $script:SharedPostgresContainer, $script:DeclaredPostgresHostPort)

    return $script:DeclaredPostgresHostPort
}

function Get-CleanArchitectureConnectionString {
    $password = $env:CA_SHARED_PG_PWD
    if ([string]::IsNullOrWhiteSpace($password)) {
        Write-Warning "CA_SHARED_PG_PWD environment variable is not set. Falling back to 'postgres' (works only if the shared container was created with that password)."
        $password = 'postgres'
    }

    $port = Get-CleanArchitecturePostgresPort

    return "Host=localhost;Port=$port;Database=CleanArchitectureDb;Username=postgres;Password=$password"
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
