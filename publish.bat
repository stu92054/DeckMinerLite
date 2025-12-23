@echo off
setlocal enabledelayedexpansion

echo ========================================
echo DeckMinerLite Build Script
echo ========================================
echo.

set VERSION=1.0.0
set PACKAGE_NAME=DeckMinerLite-v1.0-win-x64
set PUBLISH_DIR=..\publish\win-x64
set PACKAGE_DIR=..\publish\%PACKAGE_NAME%

echo [1/6] Cleaning old publish directories...
if exist "%PUBLISH_DIR%" (
    rmdir /s /q "%PUBLISH_DIR%"
)
if exist "%PACKAGE_DIR%" (
    rmdir /s /q "%PACKAGE_DIR%"
)
echo Done

echo.
echo [2/6] Running dotnet publish...
dotnet publish -c Release -r win-x64 --self-contained -p:PublishAot=false -p:PublishSingleFile=true -p:PublishTrimmed=false -o "%PUBLISH_DIR%"

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed!
    pause
    exit /b 1
)
echo Done

echo.
echo [3/6] Creating package directory structure...
mkdir "%PACKAGE_DIR%"
mkdir "%PACKAGE_DIR%\config"
echo Done

echo.
echo [4/6] Copying executable and game data...
copy "%PUBLISH_DIR%\DeckMinerLite.exe" "%PACKAGE_DIR%\"
xcopy /s /e /i /q "%PUBLISH_DIR%\GameData" "%PACKAGE_DIR%\GameData"
copy "%PUBLISH_DIR%\cardConfig.jsonc" "%PACKAGE_DIR%\"
copy "%PUBLISH_DIR%\task.jsonc" "%PACKAGE_DIR%\"
echo Done

echo.
echo [5/6] Copying configuration files...
copy "..\config\default.yaml" "%PACKAGE_DIR%\config\"
copy "..\config\member-example.yaml" "%PACKAGE_DIR%\config\"
copy "..\config\member-test.yaml" "%PACKAGE_DIR%\config\"
echo Done

echo.
echo [6/6] Creating documentation files...
call :CreateReadme
echo Done

echo.
echo ========================================
echo [SUCCESS] Build completed!
echo ========================================
echo.
echo Output directory: %PACKAGE_DIR%
echo.
echo Next steps:
echo 1. Test executable: cd "%PACKAGE_DIR%" ^&^& DeckMinerLite.exe --test-yaml --config config/member-test.yaml
echo 2. Create ZIP package manually
echo.

echo File list:
dir /s "%PACKAGE_DIR%"

echo.
pause
exit /b 0

:CreateReadme
> "%PACKAGE_DIR%\README.txt" (
echo DeckMinerLite - Quick Start Guide
echo ========================================
echo.
echo Quick Start
echo -----------
echo.
echo 1. Double-click DeckMinerLite.exe to run with default config
echo.
echo 2. Or use custom config:
echo    DeckMinerLite.exe --config config/member-example.yaml
echo.
echo 3. Test your configuration:
echo    DeckMinerLite.exe --test-yaml --config config/member-test.yaml
echo.
echo.
echo Configuration Files
echo -------------------
echo.
echo - config/default.yaml        Default configuration
echo - config/member-example.yaml Example configuration with comments
echo - config/member-test.yaml    Test configuration
echo.
echo.
echo Command Line Options
echo --------------------
echo.
echo --config [file]     Specify configuration file
echo --test-yaml         Test YAML configuration
echo --version           Show version information
echo --help              Show help message
echo.
echo.
echo Output
echo ------
echo.
echo - log/              Log files
echo - output/           Deck calculation results in CSV format
echo - temp/             Temporary files can be deleted
echo.
echo.
echo System Requirements
echo -------------------
echo.
echo - Windows 10/11 x64
echo - No .NET runtime required self-contained
echo.
echo.
echo Support
echo -------
echo.
echo - GitHub: https://github.com/stu92054/SukuShow-Deck-Miner
echo - Documentation: docs/ directory
echo.
echo ========================================
echo DeckMinerLite v1.0 - SukuShow Deck Calculator
echo ========================================
)
exit /b 0
