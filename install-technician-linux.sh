#!/bin/bash
set -e

echo "========================================================="
echo "   WARRANTY SYSTEM - SETUP TEKNISI & RUSTDESK (LINUX)    "
echo "========================================================="
echo ""

APP_DIR="$(cd "$(dirname "$0")" && pwd)"

# 1. Daftarkan Custom Protocol Handler yourapp://
echo "[1/3] Mendaftarkan protocol yourapp:// untuk integrasi browser..."
mkdir -p ~/.local/share/applications

cat << EOF > ~/.local/share/applications/yourapp-launcher.desktop
[Desktop Entry]
Name=Warranty Technician Remote Launcher
Exec=$APP_DIR/run-technician-app.sh %u
Type=Application
Terminal=false
MimeType=x-scheme-handler/yourapp;
EOF

xdg-mime default yourapp-launcher.desktop x-scheme-handler/yourapp 2>/dev/null || true
update-desktop-database ~/.local/share/applications 2>/dev/null || true
echo "✓ Protocol yourapp:// terdaftar."

# 2. Cek & Install RustDesk
echo ""
echo "[2/3] Memeriksa RustDesk..."
if command -v rustdesk >/dev/null 2>&1; then
    echo "✓ RustDesk sudah terpasang di sistem ($(rustdesk --version 2>/dev/null || echo 'OK'))."
else
    echo "(!) RustDesk belum ditemukan. Mengunduh dan menginstal RustDesk..."
    RUSTDESK_DEB="/tmp/rustdesk.deb"
    RUSTDESK_URL="https://github.com/rustdesk/rustdesk/releases/download/1.3.1/rustdesk-1.3.1-x86_64.deb"
    
    if command -v wget >/dev/null 2>&1; then
        wget -q --show-progress "$RUSTDESK_URL" -O "$RUSTDESK_DEB"
    elif command -v curl >/dev/null 2>&1; then
        curl -L "$RUSTDESK_URL" -o "$RUSTDESK_DEB"
    fi

    if [ -f "$RUSTDESK_DEB" ]; then
        echo "Menginstal paket .deb RustDesk (memerlukan hak sudo)..."
        sudo apt-get update && sudo apt-get install -y "$RUSTDESK_DEB"
        rm -f "$RUSTDESK_DEB"
        echo "✓ RustDesk berhasil diinstal!"
    else
        echo "[Peringatan] Gagal mengunduh RustDesk otomatis. Anda dapat memasangnya manual dari: https://rustdesk.com"
    fi
fi

# 3. Verifikasi Build TechnicianApp
echo ""
echo "[3/3] Memverifikasi build Technician Desktop App..."
if [ -d "$APP_DIR/desktop-app/TechnicianApp" ]; then
    chmod +x "$APP_DIR/run-technician-app.sh" 2>/dev/null || true
    echo "✓ Script run-technician-app.sh siap dijalankan."
fi

echo ""
echo "========================================================="
echo "   SETUP SELESAI! APLIKASI TEKNISI SIAP DIGUNAKAN       "
echo "========================================================="
