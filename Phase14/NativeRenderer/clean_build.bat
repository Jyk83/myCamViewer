@echo off
REM Clean all build artifacts

echo ====================================
echo Cleaning NativeRenderer Build
echo ====================================
echo.

if exist "build" (
    echo [INFO] Removing build directory...
    rmdir /S /Q "build"
    echo [OK] build\ removed
)

if exist "bin" (
    echo [INFO] Removing bin directory...
    rmdir /S /Q "bin"
    echo [OK] bin\ removed
)

echo.
echo ====================================
echo Clean Complete!
echo ====================================
echo.
echo All build artifacts have been removed.
echo You can now run build_x86.bat for a fresh build.
echo.

pause
