#!/bin/bash
export PATH="/home/alvin/.dotnet:/home/alvin/.local/bin:/home/alvin/.local/usr/bin:$PATH"

cd "$(dirname "$0")/frontend-admin"
echo "=== Menjalankan Frontend Admin Panel (Port 5173) ==="
npm run dev -- --host
