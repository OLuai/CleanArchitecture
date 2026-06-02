param(
    [Parameter(Position = 0)]
    [string]$TargetMigration
)

. (Join-Path $PSScriptRoot '_common.ps1')

Initialize-DbToolingEnv

$args = @(
    '--project', $script:InfrastructureProject,
    '--startup-project', $script:StartupProject
)
if ($TargetMigration) { $args += $TargetMigration }

dotnet ef database update @args
