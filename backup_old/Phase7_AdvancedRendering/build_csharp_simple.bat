@echo off
REM Simple C# Build Script using Developer Command Prompt
REM This script should be run from "Developer Command Prompt for VS"

echo ========================================
echo Building C# WinForms Application
echo ========================================
echo.

cd WinFormsApp

REM Use MSBuild from PATH (Developer Command Prompt)
msbuild CamViewerPOC.csproj /p:Configuration=Release /p:Platform="Any CPU" /t:Rebuild

if errorlevel 1 (
    echo.
    echo ========================================
    echo Build FAILED!
    echo ========================================
    echo.
    echo If MSBuild is not found, please:
    echo 1. Open "Developer Command Prompt for VS 2022" (or VS 2019)
    echo 2. Navigate to this project folder
    echo 3. Run this script again
    echo.
    echo Or use Visual Studio IDE to build the project.
    echo.
    cd ..
    pause
    exit /b 1
)

cd ..

echo.
echo ========================================
echo Build completed successfully!
echo Executable location: WinFormsApp\bin\Release\CamViewerPOC.exe
echo ========================================
echo.

pause
