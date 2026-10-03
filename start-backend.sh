#!/bin/bash
export PATH="/home/alvin/.dotnet:/home/alvin/.local/bin:/home/alvin/.local/usr/bin:$PATH"
export LD_LIBRARY_PATH="/home/alvin/.local/usr/lib/x86_64-linux-gnu:/home/alvin/.local/usr/lib:$LD_LIBRARY_PATH"
export PHPRC="/home/alvin/.local/etc"

cd "$(dirname "$0")/backend"
echo "=== Menjalankan Backend Laravel API (Port 8000) ==="
echo "Endpoint: http://127.0.0.1:8000/api"
echo "Tekan Ctrl+C untuk menghentikan server."
echo ""
exec php -S 0.0.0.0:8000 -t public public/index.php
