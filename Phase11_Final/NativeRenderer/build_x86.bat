@echo off
REM NativeRenderer x86 Build Script
REM Builds x86 version of NativeRenderer.dll only

echo ====================================
echo NativeRenderer x86 Build
echo ====================================
echo.

REM Check if CMake is installed
where cmake >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] CMake not found in PATH
    echo Please install CMake and add it to PATH
    pause
    exit /b 1
)

REM Create build directory
if not exist "build" mkdir build
if not exist "build\x86" mkdir build\x86

echo ====================================
echo [1/3] Configuring x86 build...
echo ====================================
cd build\x86

REM Try Visual Studio 2022 first
cmake ..\.. -G "Visual Studio 17 2022" -A Win32 -DCMAKE_BUILD_TYPE=Release
if %ERRORLEVEL% NEQ 0 (
    echo [WARN] Visual Studio 2022 not found, trying 2019...
    cmake ..\.. -G "Visual Studio 16 2019" -A Win32 -DCMAKE_BUILD_TYPE=Release
    if %ERRORLEVEL% NEQ 0 (
        echo [WARN] Visual Studio 2019 not found, trying 2017...
        cmake ..\.. -G "Visual Studio 15 2017" -A Win32 -DCMAKE_BUILD_TYPE=Release
        if %ERRORLEVEL% NEQ 0 (
            echo [ERROR] CMake configure failed - No Visual Studio found
            echo.
            echo Please install one of the following:
            echo - Visual Studio 2017 or later
            echo - Visual Studio Build Tools
            echo.
            cd ..\..
            pause
            exit /b 1
        )
    )
)

echo.
echo ====================================
echo [2/3] Building x86 Release...
echo ====================================
cmake --build . --config Release
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed for x86
    cd ..\..
    pause
    exit /b 1
)
cd ..\..

echo.
echo ====================================
echo [3/3] Copying DLL...
echo ====================================

REM Create output directory
if not exist "bin" mkdir bin
if not exist "bin\x86" mkdir bin\x86

REM Copy x86 DLL
if exist "build\x86\bin\Release\NativeRenderer.dll" (
    copy /Y "build\x86\bin\Release\NativeRenderer.dll" "bin\x86\NativeRenderer.dll"
    echo [OK] x86\NativeRenderer.dll copied
) else (
    echo [ERROR] x86 DLL not found at build\x86\bin\Release\NativeRenderer.dll
    dir /s /b build\x86\*.dll
    pause
    exit /b 1
)

REM Copy to RealtimeITagControl project as main NativeRenderer.dll
copy /Y "bin\x86\NativeRenderer.dll" "..\RealtimeITagControl\NativeRenderer.dll"
echo [OK] Copied to RealtimeITagControl\NativeRenderer.dll

echo.
echo ====================================
echo x86 Build Complete!
echo ====================================
echo.
echo Output files:
echo - NativeRenderer\bin\x86\NativeRenderer.dll
echo - RealtimeITagControl\NativeRenderer.dll (x86)
echo.
echo This DLL is compiled for 32-bit (x86) architecture.
echo It will work with:
echo - AnyCPU (.NET application on 32-bit OS)
echo - x86 (.NET application)
echo - 32-bit WinCC Runtime
echo.

pause
