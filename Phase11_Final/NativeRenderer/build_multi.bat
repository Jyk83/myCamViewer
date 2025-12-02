@echo off
REM NativeRenderer Multi-Architecture Build Script
REM Builds x86 and x64 versions of NativeRenderer.dll

echo ====================================
echo NativeRenderer Multi-Architecture Build
echo Building x86 and x64 versions
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

REM Create build directories
if not exist "build" mkdir build
if not exist "build\x86" mkdir build\x86
if not exist "build\x64" mkdir build\x64

echo ====================================
echo [1/4] Building x86 version...
echo ====================================
cd build\x86
cmake ..\.. -G "Visual Studio 17 2022" -A Win32 -DCMAKE_BUILD_TYPE=Release
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] CMake configure failed for x86
    cd ..\..
    pause
    exit /b 1
)

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
echo [2/4] Building x64 version...
echo ====================================
cd build\x64
cmake ..\.. -G "Visual Studio 17 2022" -A x64 -DCMAKE_BUILD_TYPE=Release
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] CMake configure failed for x64
    cd ..\..
    pause
    exit /b 1
)

cmake --build . --config Release
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed for x64
    cd ..\..
    pause
    exit /b 1
)
cd ..\..

echo.
echo ====================================
echo [3/4] Copying DLLs...
echo ====================================

REM Create output directories
if not exist "bin" mkdir bin
if not exist "bin\x86" mkdir bin\x86
if not exist "bin\x64" mkdir bin\x64

REM Copy x86 DLL
if exist "build\x86\bin\Release\NativeRenderer.dll" (
    copy /Y "build\x86\bin\Release\NativeRenderer.dll" "bin\x86\NativeRenderer.dll"
    echo [OK] x86\NativeRenderer.dll copied
) else (
    echo [ERROR] x86 DLL not found
    pause
    exit /b 1
)

REM Copy x64 DLL
if exist "build\x64\bin\Release\NativeRenderer.dll" (
    copy /Y "build\x64\bin\Release\NativeRenderer.dll" "bin\x64\NativeRenderer.dll"
    echo [OK] x64\NativeRenderer.dll copied
) else (
    echo [ERROR] x64 DLL not found
    pause
    exit /b 1
)

echo.
echo ====================================
echo [4/4] Copying to RealtimeITagControl...
echo ====================================

REM Copy to RealtimeITagControl project
copy /Y "bin\x86\NativeRenderer.dll" "..\RealtimeITagControl\NativeRenderer_x86.dll"
copy /Y "bin\x64\NativeRenderer.dll" "..\RealtimeITagControl\NativeRenderer_x64.dll"

echo.
echo ====================================
echo Build Complete!
echo ====================================
echo.
echo Output files:
echo - NativeRenderer\bin\x86\NativeRenderer.dll
echo - NativeRenderer\bin\x64\NativeRenderer.dll
echo.
echo Copied to:
echo - RealtimeITagControl\NativeRenderer_x86.dll
echo - RealtimeITagControl\NativeRenderer_x64.dll
echo.
echo Next step: Update C# code to load correct DLL based on platform
echo.

pause
