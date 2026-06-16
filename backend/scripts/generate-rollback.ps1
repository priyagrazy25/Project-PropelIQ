#!/usr/bin/env pwsh
# Generate rollback SQL scripts for EF Core migrations
# Usage: ./generate-rollback.ps1 -Module Identity -Migration V001_InitialSchema

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Identity", "Scheduling", "Clinical")]
    [string]$Module,

    [Parameter(Mandatory = $true)]
    [string]$Migration
)

$ErrorActionPreference = "Stop"

$projectMap = @{
    "Identity"   = "src/Modules/Identity/Identity.Infrastructure"
    "Scheduling" = "src/Modules/Scheduling/Scheduling.Infrastructure"
    "Clinical"   = "src/Modules/Clinical/Clinical.Infrastructure"
}

$project = $projectMap[$Module]
$outputDir = "scripts/migration-rollback"

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$outputFile = "$outputDir/${Module}_${Migration}_rollback_${timestamp}.sql"

Write-Host "Generating rollback script for $Module module, migration: $Migration..."

dotnet ef migrations script `
    $Migration `
    0 `
    --project $project `
    --startup-project src/Host `
    --output $outputFile `
    --idempotent

if ($LASTEXITCODE -eq 0) {
    Write-Host "Rollback script generated: $outputFile"
} else {
    Write-Error "Failed to generate rollback script."
}
