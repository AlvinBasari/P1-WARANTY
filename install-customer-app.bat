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
copy /Y "%~dp0release\windows-client\CustomerApp.exe" "%INSTALL_DIR%\CustomerApp.exe" >nul
if exist "%~dp0release\windows-client\appsettings.json" (
    copy /Y "%~dp0release\windows-client\appsettings.json" "%INSTALL_DIR%\appsettings.json" >nul
)
if exist "%~dp0release\windows-client\rustdesk.exe" (
    echo [*] Menyertakan RustDesk Portable...
    copy /Y "%~dp0release\windows-client\rustdesk.exe" "%INSTALL_DIR%\rustdesk.exe" >nul
)

echo [2/3] Membuat shortcut di Desktop...
set "SHORTCUT_PATH=%USERPROFILE%\Desktop\Warranty Support Customer.lnk"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%SHORTCUT_PATH%'); $s.TargetPath = '%INSTALL_DIR%\CustomerApp.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Save()"

echo [3/3] Membuat shortcut di Start Menu...
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Warranty Support Customer.lnk"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%START_MENU%'); $s.TargetPath = '%INSTALL_DIR%\CustomerApp.exe'; $s.WorkingDirectory = '%INSTALL_DIR%'; $s.Save()"

echo.
echo =========================================================
echo   INSTALASI BERHASIL!
echo =========================================================
echo Shortcut telah dibuat di Desktop Anda.
echo.
pause
