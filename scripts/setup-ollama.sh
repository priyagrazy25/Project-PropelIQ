#!/usr/bin/env bash
# Ollama installation and Phi-3-mini model setup (AIR-S01, AIR-O03).
# Supports Windows, macOS, and Linux.

set -euo pipefail

echo "=== Ollama Setup for Unified Patient Access Platform ==="

# Check if Ollama is already installed
if command -v ollama &> /dev/null; then
    echo "[OK] Ollama is already installed: $(ollama --version)"
else
    echo "[INFO] Installing Ollama..."
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        curl -fsSL https://ollama.com/install.sh | sh
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        echo "[INFO] Download Ollama from https://ollama.com/download/mac"
        echo "[INFO] Or install via: brew install ollama"
        exit 1
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
        echo "[INFO] Download Ollama from https://ollama.com/download/windows"
        exit 1
    fi
fi

# Start Ollama service if not running
if ! curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo "[INFO] Starting Ollama service..."
    ollama serve &
    sleep 3
fi

# Pull the primary model (Phi-3-mini)
echo "[INFO] Pulling Phi-3-mini model (active)..."
ollama pull phi3:mini

# Verify model is loaded
echo "[INFO] Verifying model..."
ollama list | grep -i "phi3"

# Test inference
echo "[INFO] Testing inference..."
echo "Hello, are you working?" | ollama run phi3:mini --nowordwrap 2>/dev/null | head -3

echo ""
echo "=== Ollama Setup Complete ==="
echo "Model: phi3:mini"
echo "Endpoint: http://localhost:11434"
echo "API test: curl http://localhost:11434/api/tags"
