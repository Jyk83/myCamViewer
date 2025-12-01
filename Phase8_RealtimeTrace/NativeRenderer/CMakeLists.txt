cmake_minimum_required(VERSION 3.10)
project(NativeRenderer)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Platform detection
if(WIN32)
    message(STATUS "Building for Windows platform")
    set(PLATFORM_WINDOWS TRUE)
elseif(UNIX AND NOT APPLE)
    message(STATUS "Building for Linux platform")
    set(PLATFORM_LINUX TRUE)
elseif(APPLE)
    message(STATUS "Building for macOS platform")
    set(PLATFORM_MACOS TRUE)
endif()

# Source files
set(SOURCES
    renderer.cpp
    TextRenderer.cpp
)

set(HEADERS
    renderer.h
    TextRenderer.h
)

# Create shared library (DLL on Windows, .so on Linux)
add_library(NativeRenderer SHARED ${SOURCES} ${HEADERS})

# Define export macro
target_compile_definitions(NativeRenderer PRIVATE RENDERER_EXPORTS)

# Link OpenGL
find_package(OpenGL REQUIRED)
target_link_libraries(NativeRenderer PRIVATE OpenGL::GL)

# Platform-specific settings
if(PLATFORM_WINDOWS)
    # Windows: Link opengl32 and gdi32
    target_link_libraries(NativeRenderer PRIVATE opengl32 gdi32)
    
    # Windows: Set DLL output directory
    set_target_properties(NativeRenderer PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
        LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
        ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
    )
elseif(PLATFORM_LINUX)
    # Linux: Link X11, GL, and GLU
    find_package(X11 REQUIRED)
    target_link_libraries(NativeRenderer PRIVATE ${X11_LIBRARIES} GL GLU)
    target_include_directories(NativeRenderer PRIVATE ${X11_INCLUDE_DIR})
    
    # Linux: Set .so output directory
    set_target_properties(NativeRenderer PROPERTIES
        LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
    )
endif()

# Compiler warnings
if(MSVC)
    target_compile_options(NativeRenderer PRIVATE /W4)
else()
    target_compile_options(NativeRenderer PRIVATE -Wall -Wextra)
endif()

# Print build information
message(STATUS "CMake Generator: ${CMAKE_GENERATOR}")
message(STATUS "Build Type: ${CMAKE_BUILD_TYPE}")
message(STATUS "C++ Standard: ${CMAKE_CXX_STANDARD}")
