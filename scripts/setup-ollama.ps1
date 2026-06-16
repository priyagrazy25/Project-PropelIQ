# Ollama Setup for Windows
# Ollama installation and Phi-3-mini model setup (AIR-S01, AIR-O03).

Write-Host "=== Ollama Setup for Unified Patient Access Platform ===" -ForegroundColor Cyan

# Check if Ollama is installed
$ollamaPath = Get-Command ollama -ErrorAction SilentlyContinue
if ($ollamaPath) {
    Write-Host "[OK] Ollama is installed at: $($ollamaPath.Source)" -ForegroundColor Green
} else {
    Write-Host "[INFO] Ollama not found. Download from: https://ollama.com/download/windows" -ForegroundColor Yellow
    Write-Host "[INFO] After installation, re-run this script." -ForegroundColor Yellow
    exit 1
}

# Check if Ollama service is running
try {
    $null = Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -TimeoutSec 3
    Write-Host "[OK] Ollama service is running." -ForegroundColor Green
} catch {
    Write-Host "[INFO] Starting Ollama service..." -ForegroundColor Yellow
    Start-Process ollama -ArgumentList "serve" -WindowStyle Hidden
    Start-Sleep -Seconds 3
}

# Pull primary model
Write-Host "[INFO] Pulling Phi-3-mini model..." -ForegroundColor Yellow
& ollama pull phi3:mini

# Verify
Write-Host "[INFO] Verifying installed models..." -ForegroundColor Yellow
& ollama list

Write-Host ""
Write-Host "=== Ollama Setup Complete ===" -ForegroundColor Cyan
Write-Host "Model: phi3:mini"
Write-Host "Endpoint: http://localhost:11434"
