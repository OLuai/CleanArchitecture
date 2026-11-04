<#
.SYNOPSIS
Builds the web and migration images on this machine, ships them over SSH to a Docker host on
the local network, and runs deploy.sh there.

.DESCRIPTION
No registry involved: the images are saved to a tar, copied with scp and loaded with docker load
on the host. The host keeps its own .env (never copied from here); deploy.sh is run with the
cloudflared override, so the stack is reached through the Cloudflare Tunnel, not Traefik.

Prerequisites: Docker Desktop here, OpenSSH client (built into Windows), and a host prepared by
setup-host.sh: Docker, the deployment user (key-based login, docker group) and $RemoteDir/.env
(see .env.example; its REGISTRY / IMAGE_NAMESPACE / IMAGE_TAG are overridden by this script).

.EXAMPLE
.\deploy\Deploy-Preprod.ps1 -Target deploy@192.168.1.50

.EXAMPLE
.\deploy\Deploy-Preprod.ps1 -Target deploy@ubuntu-preprod -SkipBuild   # images already built for this commit
#>
[CmdletBinding()]
param (
    # user@host of the Docker machine: the deployment user created by setup-host.sh.
    [Parameter(Mandatory = $true)]
    [string]$Target,

    # Directory on the host holding .env; deploy.sh and the compose files are copied there.
    [string]$RemoteDir = "/opt/cleanarchitecture-preprod",

    # Env file name inside $RemoteDir.
    [string]$EnvFile = ".env",

    # Image tag; defaults to the short commit SHA, suffixed with -dirty when the tree is modified.
    [string]$Tag,

    # Image name prefix, passed to deploy.sh over the host's .env (REGISTRY / IMAGE_NAMESPACE).
    [string]$Registry = "local",
    [string]$Namespace = "cleanarchitecture",

    # Reuse the images already built locally for this tag.
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$deployDir = Join-Path $repo "deploy"

# Native commands report failure through $LASTEXITCODE only; wrap each one so the first failure
# stops the deployment instead of letting a later step run against a broken state.
function Invoke-Step {
    param ([string]$Name, [scriptblock]$Action)
    Write-Host "==> $Name"
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "$Name failed (exit code $LASTEXITCODE)." }
}

Push-Location $repo
try {
    if (-not $Tag) {
        $Tag = (git rev-parse --short HEAD).Trim()
        if ($LASTEXITCODE -ne 0) { throw "Not a git checkout: pass -Tag explicitly." }
        if (git status --porcelain) { $Tag = "$Tag-dirty" }
    }

    $webImage = "$Registry/$Namespace/web:$Tag"
    $migrationImage = "$Registry/$Namespace/migration:$Tag"
    $remoteTar = "/tmp/cleanarchitecture-$Tag.tar"
    $localTar = Join-Path $env:TEMP "cleanarchitecture-$Tag.tar"

    Write-Host "Deploying $Tag to $Target ($RemoteDir)"

    # The host must already be prepared: .env is the one thing this script never writes.
    Invoke-Step "Checking $RemoteDir/$EnvFile on the host" {
        ssh $Target "test -f '$RemoteDir/$EnvFile'"
    }

    if (-not $SkipBuild) {
        Invoke-Step "Building $webImage" {
            docker build -f (Join-Path $deployDir "Dockerfile.web") -t $webImage .
        }
        Invoke-Step "Building $migrationImage" {
            docker build -f (Join-Path $deployDir "Dockerfile.migration") -t $migrationImage .
        }
    }

    # A clean tag names one immutable build, so a host that already holds both images needs no
    # transfer. A -dirty tag may mean anything: always ship it.
    ssh $Target "docker image inspect '$webImage' '$migrationImage' >/dev/null 2>&1"
    $hostHasImages = ($LASTEXITCODE -eq 0) -and -not $Tag.EndsWith("-dirty")

    if ($hostHasImages) {
        Write-Host "==> Host already has $Tag, skipping transfer"
    } else {
        # A tar file and scp rather than `docker save | ssh docker load`: Windows PowerShell 5.1
        # re-encodes what flows through its pipeline and corrupts binary streams.
        Invoke-Step "Saving images to $localTar" {
            docker save -o $localTar $webImage $migrationImage
        }
        try {
            Invoke-Step "Copying images to the host" {
                scp $localTar "${Target}:$remoteTar"
            }
        } finally {
            Remove-Item $localTar -ErrorAction SilentlyContinue
        }
        Invoke-Step "Loading images on the host" {
            ssh $Target "docker load -i '$remoteTar' && rm -f '$remoteTar'"
        }
    }

    # deploy.sh runs under bash on the host: ship LF copies whatever this checkout holds.
    $staging = Join-Path $env:TEMP "cleanarchitecture-deploy-$Tag"
    New-Item -ItemType Directory -Force $staging | Out-Null
    try {
        $utf8 = New-Object System.Text.UTF8Encoding($false)
        foreach ($name in @("docker-compose.yml", "docker-compose.cloudflared.yml", "deploy.sh")) {
            $content = [System.IO.File]::ReadAllText((Join-Path $deployDir $name)) -replace "`r`n", "`n"
            [System.IO.File]::WriteAllText((Join-Path $staging $name), $content, $utf8)
        }
        Invoke-Step "Copying compose files and deploy.sh to $RemoteDir" {
            scp (Join-Path $staging "docker-compose.yml") (Join-Path $staging "docker-compose.cloudflared.yml") (Join-Path $staging "deploy.sh") "${Target}:$RemoteDir/"
        }
    } finally {
        Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
    }

    # Shell variables win over .env in compose interpolation, so the registry, namespace and tag
    # of the images just loaded are imposed here: whatever the host's .env says about images
    # (a GHCR setup, for instance) cannot send compose looking for them elsewhere.
    Invoke-Step "Running deploy.sh on the host" {
        ssh $Target "cd '$RemoteDir' && chmod +x deploy.sh && REGISTRY='$Registry' IMAGE_NAMESPACE='$Namespace' IMAGE_TAG='$Tag' SKIP_PULL=1 COMPOSE_EXTRA_FILES=docker-compose.cloudflared.yml COMPOSE_EXTRA_SERVICES=cloudflared ./deploy.sh '$EnvFile'"
    }

    $publicHost = (ssh $Target "grep -E '^PUBLIC_HOST=' '$RemoteDir/$EnvFile' | cut -d= -f2-")
    if ($publicHost) {
        Write-Host "Deployed ${Tag}: https://$($publicHost.Trim())"
    } else {
        Write-Host "Deployed $Tag."
    }
} finally {
    Pop-Location
}
