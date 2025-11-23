@echo off
REM Claude Code Portable Launcher
REM Place this file in any project folder and run it
REM It will automatically detect the current directory and launch claude-start

setlocal enabledelayedexpansion

REM Get the current directory where this script is located
set "PROJECT_DIR=%~dp0"
set "PROJECT_DIR=%PROJECT_DIR:~0,-1%"

echo.
echo ========================================
echo    Claude Code - Quick Launch
echo ========================================
echo.
echo Project Directory: %PROJECT_DIR%
echo.

REM Check if Windows Terminal is available
where wt.exe >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [+] Launching in Windows Terminal...
    echo.

    REM Launch Windows Terminal in the project directory and run claude-start
    wt.exe -d "%PROJECT_DIR%" cmd /k claude-start

) else (
    echo [!] Windows Terminal not found, using CMD...
    echo.

    REM Fallback to CMD
    start "" cmd /k "cd /d "%PROJECT_DIR%" && claude-start"
)

echo [✓] Launched successfully!
echo.
echo You can close this window now.
echo.

timeout /t 3 >nul
exit
