@echo off
set "PATH=%USERPROFILE%\.dotnet;%PATH%"
set "DOTNET_ROOT=%USERPROFILE%\.dotnet"
echo === Menjalankan Customer Desktop App (Avalonia UI) ===
"%USERPROFILE%\.dotnet\dotnet.exe" run --project "%~dp0desktop-app\CustomerApp"
