@echo off
title X360CE Universal Game Injector
cd /d "%~dp0"

echo ============================================================
echo           X360CE ONE-CLICK GAME INJECTOR & SETUP
echo ============================================================
echo.
echo Launching automated setup engine...
echo.

powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0Inject_Games.ps1"

echo.
echo Press any key to exit...
pause >nul
