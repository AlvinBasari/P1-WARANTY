@echo off
set "PATH=%USERPROFILE%\.dotnet;%PATH%"
set "DOTNET_ROOT=%USERPROFILE%\.dotnet"
echo === Menjalankan Technician App Launcher (Avalonia UI) ===
"%USERPROFILE%\.dotnet\dotnet.exe" run --project "%~dp0desktop-app\TechnicianApp" -- %*
