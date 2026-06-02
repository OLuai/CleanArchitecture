param(
    [switch]$Force
)

. (Join-Path $PSScriptRoot '_common.ps1')

Initialize-DbToolingEnv

$args = @(
    '--project', $script:InfrastructureProject,
    '--startup-project', $script:StartupProject
)
if ($Force) { $args += '--force' }

dotnet ef migrations remove @args
