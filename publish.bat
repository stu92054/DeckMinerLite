@echo off
setlocal enabledelayedexpansion

echo ========================================
echo DeckMinerLite Build Script
echo ========================================
echo.

set VERSION=1.4.3
set WIN_PACKAGE_NAME=DeckMinerLite-v1.4.3-win-x64
set LINUX_PACKAGE_NAME=DeckMinerLite-v1.4.3-linux-x64
set WIN_PUBLISH_DIR=..\publish\win-x64
set LINUX_PUBLISH_DIR=..\publish\linux-x64
set WIN_PACKAGE_DIR=..\publish\%WIN_PACKAGE_NAME%
set LINUX_PACKAGE_DIR=..\publish\%LINUX_PACKAGE_NAME%

echo [1/8] Cleaning old publish directories...
if exist "%WIN_PUBLISH_DIR%" (
    rmdir /s /q "%WIN_PUBLISH_DIR%"
)
if exist "%WIN_PACKAGE_DIR%" (
    rmdir /s /q "%WIN_PACKAGE_DIR%"
)
if exist "%LINUX_PUBLISH_DIR%" (
    rmdir /s /q "%LINUX_PUBLISH_DIR%"
)
if exist "%LINUX_PACKAGE_DIR%" (
    rmdir /s /q "%LINUX_PACKAGE_DIR%"
)
echo Done

echo.
echo [2/8] Running dotnet publish for Windows x64 (with WPF GUI)...
dotnet publish -c Release --framework net10.0-windows -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%WIN_PUBLISH_DIR%"

if errorlevel 1 (
    echo.
    echo [ERROR] Windows build failed!
    pause
    exit /b 1
)
echo Done

echo.
echo [3/8] Running dotnet publish for Linux x64 (CLI, no AOT - cross-compilation not supported)...
echo [HINT] Note: NativeAOT requires native Linux build. Building without AOT for now.
dotnet publish -c Release --framework net10.0 -r linux-x64 --self-contained -p:PublishSingleFile=true -o "%LINUX_PUBLISH_DIR%"

if errorlevel 1 (
    echo.
    echo [ERROR] Linux build failed!
    pause
    exit /b 1
)
echo Done

echo.
echo [4/8] Creating Windows package directory structure...
mkdir "%WIN_PACKAGE_DIR%"
mkdir "%WIN_PACKAGE_DIR%\config"
echo Done

echo.
echo [5/8] Copying Windows executable and game data...
copy "%WIN_PUBLISH_DIR%\DeckMinerLite.exe" "%WIN_PACKAGE_DIR%\"
xcopy /s /e /i /q "%WIN_PUBLISH_DIR%\GameData" "%WIN_PACKAGE_DIR%\GameData"
copy "%WIN_PUBLISH_DIR%\cardConfig.jsonc" "%WIN_PACKAGE_DIR%\"
copy "%WIN_PUBLISH_DIR%\task.jsonc" "%WIN_PACKAGE_DIR%\"
copy "..\config\default.yaml" "%WIN_PACKAGE_DIR%\config\"
copy "..\config\member-example.yaml" "%WIN_PACKAGE_DIR%\config\"
copy "..\config\member-test.yaml" "%WIN_PACKAGE_DIR%\config\"

echo.
echo [5.1/8] Packaging Python optimizer with PyInstaller...
echo [INFO] Checking if multi_optimizer_2.exe already exists...
if exist "..\dist\multi_optimizer_2.exe" (
    echo [INFO] Found pre-built multi_optimizer_2.exe, copying...
    copy "..\dist\multi_optimizer_2.exe" "%WIN_PACKAGE_DIR%\"
) else (
    echo [WARN] multi_optimizer_2.exe not found!
    echo [HINT] Please run: cd .. ^&^& pyinstaller --onefile multi_optimizer_2.py
    echo [HINT] Then re-run this publish script.
    echo.
    echo [SKIP] Continuing without Python optimizer...
    echo [NOTE] GUI will still work but multi-song optimization will be unavailable
)
echo Done

echo.
echo [5.2/8] Copying Musics.yaml for optimizer...
echo [INFO] Copying Musics.yaml from Data to GameData for packaged optimizer...
if exist "..\Data\Musics.yaml" (
    copy "..\Data\Musics.yaml" "%WIN_PACKAGE_DIR%\GameData\"
    echo [INFO] Musics.yaml copied successfully
) else (
    echo [WARN] Musics.yaml not found in Data directory!
)
echo Done

echo.
echo [6/8] Creating Linux package directory structure...
mkdir "%LINUX_PACKAGE_DIR%"
mkdir "%LINUX_PACKAGE_DIR%\config"
echo Done

echo.
echo [7/8] Copying Linux executable and game data...
copy "%LINUX_PUBLISH_DIR%\DeckMinerLite" "%LINUX_PACKAGE_DIR%\"
xcopy /s /e /i /q "%LINUX_PUBLISH_DIR%\GameData" "%LINUX_PACKAGE_DIR%\GameData"
copy "%LINUX_PUBLISH_DIR%\cardConfig.jsonc" "%LINUX_PACKAGE_DIR%\"
copy "%LINUX_PUBLISH_DIR%\task.jsonc" "%LINUX_PACKAGE_DIR%\"
copy "..\config\default.yaml" "%LINUX_PACKAGE_DIR%\config\"
copy "..\config\member-example.yaml" "%LINUX_PACKAGE_DIR%\config\"
copy "..\config\member-test.yaml" "%LINUX_PACKAGE_DIR%\config\"
echo Done

echo.
echo [8/8] Creating documentation files...
call :CreateReadmeWindows
call :CreateReadmeLinux
echo Done

echo.
echo ========================================
echo [SUCCESS] Build completed!
echo ========================================
echo.
echo Windows package: %WIN_PACKAGE_DIR%
echo Linux package: %LINUX_PACKAGE_DIR%
echo.
echo Next steps:
echo 1. Test Windows: cd "%WIN_PACKAGE_DIR%" ^&^& DeckMinerLite.exe --test-yaml --config config/member-test.yaml
echo 2. Test Linux: Transfer to Linux machine and run ./DeckMinerLite --test-yaml --config config/member-test.yaml
echo 3. Create ZIP packages manually
echo.

echo Windows files:
dir /b "%WIN_PACKAGE_DIR%"
echo.
echo Linux files:
dir /b "%LINUX_PACKAGE_DIR%"

echo.
pause
exit /b 0

:CreateReadmeWindows
> "%WIN_PACKAGE_DIR%\README.txt" (
echo DeckMinerLite - Quick Start Guide
echo ========================================
echo.
echo Quick Start
echo -----------
echo.
echo 1. Double-click DeckMinerLite.exe to launch GUI mode (Windows only)
echo.
echo 2. Or use command line for automation with custom config:
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
echo DeckMinerLite v1.4.3 - SukuShow Deck Calculator
echo ========================================
)
exit /b 0

:CreateReadmeLinux
> "%LINUX_PACKAGE_DIR%\README.txt" (
echo DeckMinerLite - Quick Start Guide (Linux)
echo ========================================
echo.
echo Quick Start
echo -----------
echo.
echo 1. Make executable: chmod +x DeckMinerLite
echo.
echo 2. Run with default config: ./DeckMinerLite
echo.
echo 3. Or use custom config:
echo    ./DeckMinerLite --config config/member-example.yaml
echo.
echo 4. Test your configuration:
echo    ./DeckMinerLite --test-yaml --config config/member-test.yaml
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
echo - Linux x64 (tested on Ubuntu 20.04+)
echo - No .NET runtime required (self-contained)
echo.
echo.
echo Support
echo -------
echo.
echo - GitHub: https://github.com/stu92054/SukuShow-Deck-Miner
echo - Documentation: docs/ directory
echo.
echo ========================================
echo DeckMinerLite v1.4.3 - SukuShow Deck Calculator
echo ========================================
)
exit /b 0
