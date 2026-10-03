#!/bin/bash
# Protocol Handler Registration Script (yourapp://)

APP_DIR="$(cd "$(dirname "$0")" && pwd)"
BIN_PATH="$APP_DIR/run-technician-app.sh"

echo "=== Mendaftarkan Custom Protocol yourapp:// pada Linux ==="

mkdir -p ~/.local/share/applications

cat << EOF > ~/.local/share/applications/yourapp-launcher.desktop
[Desktop Entry]
Name=Warranty Technician Remote Launcher
Exec=$BIN_PATH %u
Type=Application
Terminal=false
MimeType=x-scheme-handler/yourapp;
EOF

xdg-mime default yourapp-launcher.desktop x-scheme-handler/yourapp 2>/dev/null || true
update-desktop-database ~/.local/share/applications 2>/dev/null || true

echo "✓ Protokol yourapp:// berhasil didaftarkan!"
echo ""
echo "=== Panduan Registrasi di Windows (Registry) ==="
echo "Buat file 'register_protocol.reg' dengan konten:"
echo '---------------------------------------------------'
echo 'Windows Registry Editor Version 5.00'
echo ''
echo '[HKEY_CLASSES_ROOT\yourapp]'
echo '@="URL:yourapp Protocol"'
echo '"URL Protocol"=""'
echo ''
echo '[HKEY_CLASSES_ROOT\yourapp\shell\open\command]'
echo '@="\"C:\\Path\\To\\TechnicianApp.exe\" \"%1\""'
echo '---------------------------------------------------'
