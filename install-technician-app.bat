@echo off
setlocal enabledelayedexpansion
title Warranty Support - Technician App Installer

echo =========================================================
echo   WARRANTY SUPPORT SYSTEM - TECHNICIAN APP INSTALLER
echo =========================================================
echo.

set "INSTALL_DIR=%LOCALAPPDATA%\WarrantyTechnicianApp"
echo Menginstal ke: %INSTALL_DIR%
echo.

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo [1/4] Menyalin file aplikasi Teknisi...
copy /Y "%~dp0release\windows-technician\TechnicianApp.exe" "%INSTALL_DIR%\TechnicianApp.exe" >nul
if exist "%~dp0release\windows-technician\appsettings.json" (
    copy /Y "%~dp0release\windows-technician\appsettings.json" "%INSTALL_DIR%\appsettings.json" >nul
)
if exist "%~dp0release\windows-technician\rustdesk.exe" (
    echo [*] Menyertakan RustDesk Portable...
    copy /Y "%~dp0release\windows-technician\rustdesk.exe" "%INSTALL_DIR%\rustdesk.exe" >nul
)

echo [2/4] Membuat shortcut di Desktop...
set "SHORTCUT_PATH=%USERPROFILE%\Desktop\Warranty Support Technician.lnk"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%SHORTCUT_PATH%'); $s.TargetPath = '%INSTALL_DIR%\TechnicianApp.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Save()"

echo [3/4] Membuat shortcut di Start Menu...
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Warranty Support Technician.lnk"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%START_MENU%'); $s.TargetPath = '%INSTALL_DIR%\TechnicianApp.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Save()"

echo [4/4] Mendaftarkan Protocol yourapp:// untuk 1-Klik Remote dari Browser...
reg add "HKCU\Software\Classes\yourapp" /ve /d "URL:yourapp Protocol" /f >nul
reg add "HKCU\Software\Classes\yourapp" /v "URL Protocol" /d "" /f >nul
reg add "HKCU\Software\Classes\yourapp\shell\open\command" /ve /d "\"%INSTALL_DIR%\TechnicianApp.exe\" \"%%1\"" /f >nul
echo [*] Protocol yourapp:// berhasil didaftarkan di Windows Registry.

echo.
echo =========================================================
echo   INSTALASI TEKNISI BERHASIL!
echo =========================================================
echo Shortcut: Desktop dan Start Menu telah dibuat.
echo Integrasi Browser: Tombol 'Koneksikan Remote' di web admin akan otomatis membuka aplikasi ini.
echo.
pause
