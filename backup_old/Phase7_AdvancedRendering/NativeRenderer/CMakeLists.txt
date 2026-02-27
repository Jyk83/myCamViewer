cmake_minimum_required(VERSION 3.15)
project(NativeRenderer)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Source files
set(SOURCES
    renderer.cpp
)

set(HEADERS
    renderer.h
)

# Create shared library (DLL)
add_library(NativeRenderer SHARED ${SOURCES} ${HEADERS})

# Define export macro
target_compile_definitions(NativeRenderer PRIVATE RENDERER_EXPORTS)

# Link OpenGL
find_package(OpenGL REQUIRED)
target_link_libraries(NativeRenderer PRIVATE OpenGL::GL)

# Windows specific settings
if(WIN32)
    target_link_libraries(NativeRenderer PRIVATE opengl32)
endif()

# Output directory
set_target_properties(NativeRenderer PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
)
