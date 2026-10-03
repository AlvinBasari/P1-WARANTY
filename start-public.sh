#!/bin/bash
export PATH="/home/alvin/.dotnet:/home/alvin/.local/bin:/home/alvin/.local/usr/bin:$PATH"

cd "$(dirname "$0")/frontend-public"
echo "=== Menjalankan Portal Publik QR Code (Port 5174) ==="
npm run dev -- --host
