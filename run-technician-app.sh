#!/bin/bash
export PATH="/home/alvin/.dotnet:/home/alvin/.local/bin:/home/alvin/.local/usr/bin:$PATH"
export DOTNET_ROOT="/home/alvin/.dotnet"

cd "$(dirname "$0")/desktop-app"
echo "=== Menjalankan Technician App Launcher (Avalonia UI) ==="
dotnet run --project TechnicianApp -- "$@"
