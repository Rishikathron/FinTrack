#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build and run the FinTrack Docker Compose stack.

.DESCRIPTION
    Builds all container images and starts the services.
      - API:      http://localhost:8080
      - UI:       http://localhost:80
      - Telegram: runs as a background worker

    Development (default): Uses local Ollama on host, no API keys needed.
    Production (-Prod):    Uses OpenRouter, requires OPENROUTER_API_KEY in .env.

.PARAMETER Down
    Stop and remove all containers, networks, and volumes.

.PARAMETER Build
    Force rebuild all images before starting.

.PARAMETER Prod
    Run in Production mode (OpenRouter AI, skips docker-compose.override.yml).
    Requires OPENROUTER_API_KEY and TELEGRAM_BOT_TOKEN in .env file.

.EXAMPLE
    .\run-docker.ps1                # Start in Development (Ollama)
    .\run-docker.ps1 -Prod          # Start in Production (OpenRouter)
    .\run-docker.ps1 -Build -Prod   # Rebuild and start (Production)
    .\run-docker.ps1 -Down          # Tear everything down
#>
param(
    [switch]$Down,
    [switch]$Build,
    [switch]$Prod
)

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot

# Choose compose files based on environment
if ($Prod) {
    $composeArgs = @("-f", "docker-compose.yml")
    $envLabel = "Production (OpenRouter)"

    # Verify secrets exist
    if (Test-Path ".env") {
        $envContent = Get-Content ".env" -Raw
        if ($envContent -notmatch 'OPENROUTER_API_KEY=\S+') {
            Write-Host "ERROR: OPENROUTER_API_KEY is missing or empty in .env" -ForegroundColor Red
            Write-Host "Copy .env.example to .env and fill in your keys." -ForegroundColor Yellow
            Pop-Location
            return
        }
    } else {
        Write-Host "ERROR: .env file not found." -ForegroundColor Red
        Write-Host "Copy .env.example to .env and fill in your keys." -ForegroundColor Yellow
        Pop-Location
        return
    }
} else {
    $composeArgs = @("-f", "docker-compose.yml", "-f", "docker-compose.override.yml")
    $envLabel = "Development (Ollama)"
}

if ($Down) {
    Write-Host "Stopping FinTrack stack..." -ForegroundColor Yellow
    docker compose @composeArgs down -v
    Pop-Location
    return
}

if ($Build) {
    Write-Host "Building images..." -ForegroundColor Cyan
    docker compose @composeArgs build --no-cache
}

Write-Host "Starting FinTrack stack ($envLabel)..." -ForegroundColor Green
docker compose @composeArgs up -d --build

Write-Host ""
Write-Host "FinTrack is running! [$envLabel]" -ForegroundColor Green
Write-Host "   API:       http://localhost:8080" -ForegroundColor White
Write-Host "   UI:        http://localhost:80" -ForegroundColor White
Write-Host "   Telegram:  running in background" -ForegroundColor White
Write-Host ""
Write-Host "View logs:    docker compose logs -f" -ForegroundColor DarkGray
Write-Host "Stop stack:   .\run-docker.ps1 -Down" -ForegroundColor DarkGray

Pop-Location
