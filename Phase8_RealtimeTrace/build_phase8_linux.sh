#!/bin/bash
# Phase 8.1 Build Script for Linux
# This script builds the Native Renderer library on Linux
# Note: The C# WinForms application requires Windows to build

set -e  # Exit on error

echo "========================================"
echo "Phase 8.1 Linux Build Script"
echo "========================================"
echo ""
echo "⚠️  Note: This builds only the Native Renderer library."
echo "    The C# WinForms application must be built on Windows."
echo ""

BUILD_CONFIG="Debug"

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

echo "[Step 1/2] Building Native Renderer Library..."
echo "----------------------------------------"
cd NativeRenderer

# Check for required tools
if ! command -v cmake &> /dev/null; then
    echo "❌ ERROR: CMake not found!"
    echo "   Install CMake: sudo apt-get install cmake"
    exit 1
fi

if ! command -v make &> /dev/null; then
    echo "❌ ERROR: Make not found!"
    echo "   Install Make: sudo apt-get install build-essential"
    exit 1
fi

# Check for OpenGL development libraries
if ! ldconfig -p | grep -q libGL.so; then
    echo "⚠️  WARNING: OpenGL libraries may not be installed"
    echo "   Install OpenGL: sudo apt-get install libgl1-mesa-dev libglu1-mesa-dev"
fi

# Check for X11 development libraries
if ! ldconfig -p | grep -q libX11.so; then
    echo "⚠️  WARNING: X11 libraries may not be installed"
    echo "   Install X11: sudo apt-get install libx11-dev"
fi

# Create build directory
mkdir -p build
cd build

echo ""
echo "Configuring CMake..."
cmake .. -DCMAKE_BUILD_TYPE=$BUILD_CONFIG

if [ $? -ne 0 ]; then
    echo ""
    echo "❌ ERROR: CMake configuration failed!"
    echo ""
    echo "Possible solutions:"
    echo "  1. Install CMake 3.10+: sudo apt-get install cmake"
    echo "  2. Install OpenGL: sudo apt-get install libgl1-mesa-dev libglu1-mesa-dev"
    echo "  3. Install X11: sudo apt-get install libx11-dev"
    echo "  4. Install build tools: sudo apt-get install build-essential"
    echo ""
    exit 1
fi

echo ""
echo "Building Native Renderer..."
cmake --build . --config $BUILD_CONFIG

if [ $? -ne 0 ]; then
    echo ""
    echo "❌ ERROR: Build failed!"
    echo ""
    exit 1
fi

echo ""
echo "✅ Native Renderer build complete!"
echo ""
echo "Output: NativeRenderer/build/lib/libNativeRenderer.so"
echo ""

echo "[Step 2/2] Build Summary"
echo "----------------------------------------"
echo "✅ Native Renderer library built successfully"
echo "⚠️  C# WinForms application must be built on Windows"
echo ""
echo "To build the complete application on Windows:"
echo "  1. Open 'CamViewerPOC.sln' in Visual Studio 2017+"
echo "  2. Or run 'build_phase8.bat' script"
echo ""
echo "========================================"
echo "Build Process Complete!"
echo "========================================"
