#!/bin/bash
# Script to build and package Windows Client standalone application and installer bundle

set -e

PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
OUTPUT_DIR="$PROJECT_ROOT/release/windows-client"

echo "========================================================="
echo "  Membangun Standalone Windows Client (CustomerApp.exe)  "
echo "========================================================="

# 1. Compile & Publish Windows Single-File Executable
echo "[1/4] Mengompilasi binary Windows x64 self-contained..."
export PATH="/home/alvin/.dotnet:/home/alvin/.local/bin:$PATH"
export DOTNET_ROOT="/home/alvin/.dotnet"

dotnet publish "$PROJECT_ROOT/desktop-app/CustomerApp/CustomerApp.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUTPUT_DIR"

# 2. Generate appsettings.json for Server IP Configuration
echo "[2/4] Membuat template konfigurasi appsettings.json..."
SERVER_IP=$(ip -4 addr show | grep -oP '(?<=inet\s)\d+(\.\d+){3}' | grep -v '^127\.' | head -n 1)
if [ -z "$SERVER_IP" ]; then
  SERVER_IP=$(hostname -I | tr ' ' '\n' | grep -v '^127\.' | head -n 1)
fi
if [ -z "$SERVER_IP" ]; then
  SERVER_IP="192.168.42.243"
fi

cat << EOF > "$OUTPUT_DIR/appsettings.json"
{
  "ServerUrl": "http://$SERVER_IP:8000/api",
  "AppName": "Warranty Support System - Customer Client",
  "Version": "1.0.0"
}
EOF

# 3. Create Windows 1-Click Setup Script (setup-windows.bat)
echo "[3/4] Menyertakan script instalasi otomatis Windows (setup-windows.bat)..."
cat << 'EOF' > "$OUTPUT_DIR/setup-windows.bat"
@echo off
setlocal enabledelayedexpansion
title Warranty Support - Customer Client Installer

echo =========================================================
echo   WARRANTY SUPPORT SYSTEM - CUSTOMER APP INSTALLER
echo =========================================================
echo.

set "INSTALL_DIR=%LOCALAPPDATA%\WarrantyCustomerApp"
echo Menginstal ke: %INSTALL_DIR%
echo.

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo [1/3] Menyalin file aplikasi...
copy /Y "%~dp0CustomerApp.exe" "%INSTALL_DIR%\CustomerApp.exe" >nul
copy /Y "%~dp0appsettings.json" "%INSTALL_DIR%\appsettings.json" >nul

echo [2/3] Membuat shortcut di Desktop...
set "SHORTCUT_PATH=%USERPROFILE%\Desktop\Warranty Support Customer.lnk"
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%SHORTCUT_PATH%'); $s.TargetPath = '%INSTALL_DIR%\CustomerApp.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Save()"

echo [3/3] Membuat shortcut di Start Menu...
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Warranty Support Customer.lnk"
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%START_MENU%'); $s.TargetPath = '%INSTALL_DIR%\CustomerApp.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Save()"

echo.
echo =========================================================
echo   INSTALASI BERHASIL!
echo =========================================================
echo Shortcut telah dibuat di Desktop Anda.
echo.
set /p RUN_NOW="Jalankan Customer App sekarang? (Y/N): "
if /i "%RUN_NOW%"=="Y" (
    start "" "%INSTALL_DIR%\CustomerApp.exe"
)
exit /b 0
EOF

# 4. Create ZIP Bundle for easy transfer
echo "[4/4] Mengompresi paket distribusi menjadi ZIP..."
cd "$PROJECT_ROOT/release"
rm -f WarrantyCustomerApp-Windows-x64.zip
zip -r WarrantyCustomerApp-Windows-x64.zip windows-client/ -x "*.pdb"

echo ""
echo "========================================================="
echo "  BUILD SUKSES!"
echo "  Paket Distribusi Windows Siap di: release/WarrantyCustomerApp-Windows-x64.zip"
echo "  Folder Executable di: release/windows-client/"
echo "========================================================="
