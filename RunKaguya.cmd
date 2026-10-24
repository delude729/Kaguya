@echo off
setlocal
cd /d "%~dp0"
if not exist "%~dp0KaguyaPet.exe" (
  echo KaguyaPet.exe was not found. Run build.cmd first.
  pause
  exit /b 1
)
start "" /D "%~dp0" "%~dp0KaguyaPet.exe"
