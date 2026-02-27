# Phase 8 CAM Viewer - Build Guide

## 🎯 Overview

Phase 8 CAM Viewer는 **C# WinForms 애플리케이션**과 **Native C++ OpenGL 렌더러**로 구성된 하이브리드 프로젝트입니다.

**플랫폼 요구사항:**
- **완전한 빌드**: Windows (Visual Studio 필요)
- **Native 렌더러만**: Linux/macOS (CMake + 컴파일러)

---

## 🖥️ Windows 빌드 (권장)

### 필수 요구사항

1. **Visual Studio 2017 이상** (2022 권장)
   - "Desktop development with C++" 워크로드 설치
   - C++/CLI 지원 (선택사항)

2. **CMake 3.10 이상**
   - Visual Studio에 포함됨
   - 또는 독립 설치: https://cmake.org/download/

3. **.NET Framework 4.5 이상**
   - Windows 10/11에 기본 포함

### 빌드 방법

#### Option 1: 자동 빌드 스크립트 (권장)

```batch
cd Phase8_RealtimeTrace
build_phase8.bat
```

스크립트 동작:
1. ✅ Visual Studio 버전 자동 감지 (2022 → 2019 → 2017)
2. ✅ CMake로 Native DLL 빌드
3. ✅ MSBuild로 C# 애플리케이션 빌드
4. ✅ 출력 파일 검증 및 복사

#### Option 2: Visual Studio에서 직접 빌드

```batch
# 1. Native 렌더러 빌드
cd NativeRenderer
mkdir build && cd build
cmake .. -G "Visual Studio 17 2022" -A x64
cmake --build . --config Debug

# 2. C# 애플리케이션 빌드
cd ../../
start CamViewerPOC.sln
# Visual Studio에서 F5 (빌드 후 실행)
```

#### Option 3: MSBuild 명령줄

```batch
# Visual Studio 개발자 명령 프롬프트에서 실행
msbuild CamViewerPOC.sln /p:Configuration=Debug /p:Platform=x64
```

### 출력 파일

```
WinFormsApp\bin\x64\Debug\
├── CamViewerPOC.exe          # 메인 애플리케이션
├── NativeRenderer.dll        # Native OpenGL 렌더러
└── [기타 종속 DLL들]
```

### 문제 해결

#### 문제: "Visual Studio 15 2017 could not find any instance"

**원인:** CMake가 Visual Studio를 찾을 수 없음

**해결책:**
1. Visual Studio 2017+ 설치 확인
2. "Desktop development with C++" 워크로드 설치 확인
3. `build_phase8.bat` 실행 (자동 감지 기능 있음)

#### 문제: "MSBuild not found"

**원인:** Visual Studio MSBuild가 경로에 없음

**해결책:**
1. "Visual Studio 개발자 명령 프롬프트" 사용
2. 또는 `build_phase8.bat` 실행 (자동 검색 기능 있음)

#### 문제: "NativeRenderer.dll not found"

**원인:** DLL이 실행 파일 위치에 복사되지 않음

**해결책:**
```batch
copy NativeRenderer\build\bin\Debug\NativeRenderer.dll WinFormsApp\bin\x64\Debug\
```

---

## 🐧 Linux 빌드 (Native 렌더러만)

### 필수 요구사항

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y \
    build-essential \
    cmake \
    libgl1-mesa-dev \
    libglu1-mesa-dev \
    libx11-dev

# Fedora/RHEL
sudo dnf install -y \
    gcc-c++ \
    cmake \
    mesa-libGL-devel \
    mesa-libGLU-devel \
    libX11-devel
```

### 빌드 방법

```bash
cd Phase8_RealtimeTrace
chmod +x build_phase8_linux.sh
./build_phase8_linux.sh
```

### 출력 파일

```
NativeRenderer/build/lib/
└── libNativeRenderer.so      # Shared library
```

### 제한사항

⚠️ **Linux에서는 C# WinForms 애플리케이션을 빌드할 수 없습니다.**
- Native 렌더러 라이브러리만 빌드 가능
- 개발/테스트 목적으로만 사용
- 실제 애플리케이션은 Windows에서 빌드 필요

---

## 🍎 macOS 빌드 (실험적)

### 필수 요구사항

```bash
# Homebrew로 설치
brew install cmake

# Xcode Command Line Tools
xcode-select --install
```

### 빌드 방법

```bash
cd Phase8_RealtimeTrace/NativeRenderer
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Debug
cmake --build .
```

### 제한사항

⚠️ **macOS에서는 완전한 지원이 제공되지 않습니다.**
- Windows-specific API 사용 (wglUseFontOutlinesW 등)
- C# WinForms는 macOS에서 실행 불가
- 코드 검증 목적으로만 사용

---

## 📦 프로젝트 구조

```
Phase8_RealtimeTrace/
├── NativeRenderer/              # C++ OpenGL 렌더러
│   ├── CMakeLists.txt          # CMake 빌드 설정
│   ├── renderer.h/cpp          # 메인 렌더러
│   └── TextRenderer.h/cpp      # 텍스트 렌더링 (Phase 8.1)
│
├── WinFormsApp/                 # C# WinForms 애플리케이션
│   ├── CamViewerPOC.csproj     # C# 프로젝트 파일
│   ├── CamViewerControl.cs     # 메인 뷰어 컨트롤
│   ├── MPF/                    # MPF 파서
│   ├── Rendering/              # 렌더링 유틸리티
│   └── Selection/              # 선택 시스템
│
├── HKCamInterface_Reference/    # 참조 코드 (C++)
│
├── build_phase8.bat            # Windows 빌드 스크립트
├── build_phase8_linux.sh       # Linux 빌드 스크립트
├── CamViewerPOC.sln            # Visual Studio 솔루션
└── BUILD_GUIDE.md              # 이 문서
```

---

## 🔧 개발 환경 설정

### Visual Studio 권장 확장

- **ReSharper** (선택사항): C# 코드 분석
- **Visual Assist** (선택사항): C++ 코드 분석
- **Productivity Power Tools**: 일반 생산성 향상

### Visual Studio Code (Linux)

```bash
# 확장 설치
code --install-extension ms-vscode.cpptools
code --install-extension ms-vscode.cmake-tools
```

---

## 📊 빌드 시간 예상

| 환경 | Native DLL | C# App | 전체 |
|------|-----------|---------|------|
| **VS 2022** | ~30초 | ~20초 | **~50초** |
| **VS 2019** | ~40초 | ~25초 | **~65초** |
| **VS 2017** | ~50초 | ~30초 | **~80초** |
| **Linux** | ~20초 | N/A | **~20초** |

*Intel i7, 16GB RAM, SSD 기준*

---

## 🚀 빌드 후 실행

### Windows

```batch
# 방법 1: 직접 실행
cd WinFormsApp\bin\x64\Debug
CamViewerPOC.exe

# 방법 2: Visual Studio에서 F5
# 방법 3: 빌드 스크립트에서 자동 실행 옵션
```

### 실행 전 확인사항

1. ✅ `NativeRenderer.dll`이 EXE와 같은 폴더에 있는지 확인
2. ✅ OpenGL 드라이버가 설치되어 있는지 확인
3. ✅ 샘플 MPF 파일 준비 (SampleMPF/ 폴더 참조)

---

## 🐛 일반적인 빌드 오류

### 1. "Cannot open include file: 'GL/gl.h'"

**원인:** OpenGL 헤더 파일 없음

**해결:**
- Windows: Visual Studio에서 "Desktop development with C++" 설치
- Linux: `sudo apt-get install libgl1-mesa-dev`

### 2. "LNK1104: cannot open file 'opengl32.lib'"

**원인:** OpenGL 라이브러리 링크 실패

**해결:**
- Windows SDK 설치 확인
- Visual Studio Installer에서 "Windows 10 SDK" 설치

### 3. "error MSB4019: The imported project ... was not found"

**원인:** .NET Framework 타겟팅 팩 없음

**해결:**
```batch
# Visual Studio Installer에서 설치
# ".NET Framework 4.5 targeting pack"
```

### 4. "wglUseFontOutlinesW: identifier not found"

**원인:** Windows.h 헤더 누락

**해결:**
```cpp
// renderer.cpp 상단에 추가
#ifdef _WIN32
#include <windows.h>
#endif
```

---

## 📚 추가 리소스

- **CMake 문서**: https://cmake.org/documentation/
- **MSBuild 참조**: https://docs.microsoft.com/msbuild
- **OpenGL 튜토리얼**: https://learnopengl.com/
- **C# WinForms 가이드**: https://docs.microsoft.com/dotnet/desktop/winforms/

---

## 🆘 도움 요청

빌드 문제가 해결되지 않으면:

1. 빌드 로그 전체 내용 확인
2. `BUILD_TROUBLESHOOTING.md` 문서 참조
3. Visual Studio 출력 창 확인 (보기 → 출력)
4. CMake 출력 로그 확인 (`NativeRenderer/build/CMakeOutput.log`)

---

**마지막 업데이트:** 2025-11-27  
**버전:** Phase 8.1  
**작성자:** GenSpark AI Developer
