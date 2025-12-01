@echo off
REM Build script for Native C++ Renderer DLL
REM Requires CMake and Visual Studio installed

echo ========================================
echo Building Native Renderer DLL
echo ========================================

cd NativeRenderer

REM Create build directory
if not exist build mkdir build
cd build

REM Configure CMake (adjust generator based on your Visual Studio version)
REM For Visual Studio 2019:
cmake -G "Visual Studio 16 2019" -A x64 ..

REM For Visual Studio 2022:
REM cmake -G "Visual Studio 17 2022" -A x64 ..

if errorlevel 1 (
    echo CMake configuration failed!
    pause
    exit /b 1
)

REM Build Release configuration
cmake --build . --config Release

if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo DLL location: NativeRenderer\build\bin\Release\NativeRenderer.dll
echo ========================================
echo.

REM Copy DLL to WinForms output directory for testing
if exist "bin\Release\NativeRenderer.dll" (
    if not exist "..\..\WinFormsApp\bin\Release" mkdir "..\..\WinFormsApp\bin\Release"
    copy /Y "bin\Release\NativeRenderer.dll" "..\..\WinFormsApp\bin\Release\"
    echo DLL copied to WinForms Release directory
)

cd ..\..
pause
