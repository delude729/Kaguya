@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\build-kaguya-icon.ps1"
if errorlevel 1 exit /b %errorlevel%
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:assets\kaguya.ico /win32manifest:src\Pet.manifest /out:KaguyaPet.exe /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll src\Kaguya.cs src\Native.cs src\Appearance.cs src\Checks.cs
if errorlevel 1 exit /b %errorlevel%
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\verify-kaguya-icon.ps1" -RefreshCache
exit /b %errorlevel%
