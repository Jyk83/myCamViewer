@echo off
REM Master build script - Builds both Native DLL and C# Application

echo ========================================
echo CAM Viewer POC - Master Build Script
echo ========================================
echo.

REM Step 1: Build Native Renderer DLL
echo [1/2] Building Native C++ Renderer...
call build_native.bat
if errorlevel 1 (
    echo Native build failed!
    exit /b 1
)

echo.
echo.

REM Step 2: Build C# WinForms Application
echo [2/2] Building C# WinForms Application...
call build_csharp.bat
if errorlevel 1 (
    echo C# build failed!
    exit /b 1
)

echo.
echo ========================================
echo All builds completed successfully!
echo ========================================
echo.
echo You can now run: WinFormsApp\bin\Release\CamViewerPOC.exe
echo.

pause
