@echo off
REM Phase 8.1 Build Script - Part/Contour Number Display
REM Builds Native DLL and C# WinForms application

echo ========================================
echo Phase 8.1 Build Script
echo ========================================
echo.

SET BUILD_CONFIG=Debug
SET PLATFORM=x64

REM Get script directory
SET SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%"

echo [Step 1/4] Building Native Renderer DLL...
echo ----------------------------------------
cd NativeRenderer

REM Create build directory
if not exist build mkdir build
cd build

REM Configure CMake
echo Configuring CMake...
echo Trying to auto-detect Visual Studio version...
echo.

REM Try to auto-detect Visual Studio generator
SET CMAKE_GENERATOR=
for %%G in (
    "Visual Studio 17 2022"
    "Visual Studio 16 2019"
    "Visual Studio 15 2017"
) do (
    echo Trying %%G...
    cmake .. -G %%G -A x64 >nul 2>&1
    if %ERRORLEVEL% EQU 0 (
        SET CMAKE_GENERATOR=%%G
        goto :generator_found
    )
)

echo ERROR: Could not find compatible Visual Studio version!
echo.
echo Please install one of:
echo - Visual Studio 2022 (recommended)
echo - Visual Studio 2019
echo - Visual Studio 2017
echo.
echo Make sure to install the "Desktop development with C++" workload
echo.
echo Or specify CMake generator manually:
echo   cmake .. -G "Your Generator" -A x64
echo.
pause
exit /b 1

:generator_found
echo.
echo Found Visual Studio: %CMAKE_GENERATOR%
echo Configuring with %CMAKE_GENERATOR%...
cmake .. -G %CMAKE_GENERATOR% -A x64

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: CMake configuration failed!
    echo.
    echo Possible solutions:
    echo - Install Visual Studio 2017 or later with C++ Desktop Development workload
    echo - Install CMake 3.10 or later
    echo - Check CMakeLists.txt for errors
    echo - Ensure OpenGL development libraries are installed
    pause
    exit /b 1
)

REM Build DLL
echo Building NativeRenderer.dll...
cmake --build . --config %BUILD_CONFIG%
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Native DLL build failed!
    echo Check compiler errors above.
    pause
    exit /b 1
)

echo.
echo Native DLL built successfully!
echo Location: %SCRIPT_DIR%NativeRenderer\build\bin\%BUILD_CONFIG%\NativeRenderer.dll
echo.

REM Go back to root
cd /d "%SCRIPT_DIR%"

echo [Step 2/4] Checking MSBuild...
echo ----------------------------------------

REM Find MSBuild
SET MSBUILD_PATH=
for %%i in (
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe"
    "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
) do (
    if exist %%i (
        SET MSBUILD_PATH=%%i
        goto :msbuild_found
    )
)

echo ERROR: MSBuild not found!
echo Please install Visual Studio 2017 or later
pause
exit /b 1

:msbuild_found
echo Found MSBuild: %MSBUILD_PATH%
echo.

echo [Step 3/4] Building C# WinForms Application...
echo ----------------------------------------
cd WinFormsApp

"%MSBUILD_PATH%" CamViewerPOC.csproj /p:Configuration=%BUILD_CONFIG% /p:Platform=%PLATFORM% /v:minimal
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: C# application build failed!
    echo Check compiler errors above.
    cd /d "%SCRIPT_DIR%"
    pause
    exit /b 1
)

echo.
echo C# application built successfully!
echo Location: %SCRIPT_DIR%WinFormsApp\bin\%PLATFORM%\%BUILD_CONFIG%\CamViewerPOC.exe
echo.

cd /d "%SCRIPT_DIR%"

echo [Step 4/4] Verifying Build Output...
echo ----------------------------------------

SET DLL_SOURCE=%SCRIPT_DIR%NativeRenderer\build\bin\%BUILD_CONFIG%\NativeRenderer.dll
SET DLL_DEST=%SCRIPT_DIR%WinFormsApp\bin\%PLATFORM%\%BUILD_CONFIG%\NativeRenderer.dll
SET EXE_PATH=%SCRIPT_DIR%WinFormsApp\bin\%PLATFORM%\%BUILD_CONFIG%\CamViewerPOC.exe

if exist "%DLL_SOURCE%" (
    echo [OK] Native DLL exists
) else (
    echo [ERROR] Native DLL not found: %DLL_SOURCE%
    pause
    exit /b 1
)

if exist "%EXE_PATH%" (
    echo [OK] Application EXE exists
) else (
    echo [ERROR] EXE not found: %EXE_PATH%
    pause
    exit /b 1
)

if exist "%DLL_DEST%" (
    echo [OK] Native DLL copied to output directory
) else (
    echo [WARNING] Native DLL not in output directory
    echo Copying DLL manually...
    copy "%DLL_SOURCE%" "%DLL_DEST%"
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] Failed to copy DLL
        pause
        exit /b 1
    )
    echo [OK] DLL copied successfully
)

echo.
echo ========================================
echo Build Completed Successfully!
echo ========================================
echo.
echo Output Directory: %SCRIPT_DIR%WinFormsApp\bin\%PLATFORM%\%BUILD_CONFIG%
echo.
echo Files:
echo - CamViewerPOC.exe
echo - NativeRenderer.dll
echo.
echo You can now run the application:
echo   %EXE_PATH%
echo.
echo Press any key to exit...
pause >nul
