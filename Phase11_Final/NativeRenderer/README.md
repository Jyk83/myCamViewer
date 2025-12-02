# NativeRenderer - OpenGL 렌더링 엔진

## 개요
Phase8에서 개발된 OpenGL 기반 고성능 렌더링 엔진입니다.
C++로 작성되었으며, .NET/C#에서 P/Invoke를 통해 호출됩니다.

## Architecture Mismatch 문제 해결

### 문제 상황
```
Platform mismatch error!
The NativeRenderer.dll architecture doesn't match this application.
- DLL is x64 (64-bit)
- Application must be x64 or AnyCPU
```

### 원인
- NativeRenderer.dll이 x64 전용으로 빌드됨
- WinCC Runtime이 32비트 프로세스로 실행될 경우 로드 실패

### 해결 방법

#### 옵션 1: x86 버전 빌드 (가장 간단, 추천) ⭐

`build_x86.bat` 스크립트를 실행하여 x86 버전만 빌드합니다:

```cmd
cd NativeRenderer
build_x86.bat
```

이 스크립트는:
- Visual Studio 2022/2019/2017을 자동으로 탐색
- x86 버전 빌드: `bin\x86\NativeRenderer.dll`
- RealtimeITagControl 폴더로 자동 복사
- 32비트 WinCC Runtime과 완벽하게 호환

**이 방법을 사용하면 Architecture Mismatch 문제가 완전히 해결됩니다.**

#### 옵션 2: Multi-Architecture 빌드 (고급)

`build_multi.bat` 스크립트를 실행하여 x86과 x64 두 버전을 모두 빌드합니다:

```cmd
cd NativeRenderer
build_multi.bat
```

이 스크립트는:
- x86 버전: `bin\x86\NativeRenderer.dll`
- x64 버전: `bin\x64\NativeRenderer.dll`

두 파일을 모두 생성하고 RealtimeITagControl 폴더로 복사합니다.
런타임에 플랫폼에 맞는 DLL을 동적으로 로드하는 추가 코드가 필요합니다.

#### 옵션 3: WinCC를 64비트 모드로 실행

WinCC Runtime을 64비트 프로세스로 실행하면 x64 DLL을 사용할 수 있습니다.
(대부분의 WinCC 환경은 32비트로 실행됨)

## 빌드 요구사항

### Windows
- Visual Studio 2017 이상 (C++ 컴파일러 포함)
- CMake 3.10 이상
- OpenGL 개발 라이브러리 (Windows SDK 포함)

### Linux (개발/테스트용)
```bash
sudo apt-get install build-essential cmake
sudo apt-get install libgl1-mesa-dev libglu1-mesa-dev libx11-dev
```

## 빌드 방법

### Windows (단일 아키텍처)

#### x64 빌드
```cmd
mkdir build
cd build
cmake .. -G "Visual Studio 17 2022" -A x64 -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
```

#### x86 빌드
```cmd
mkdir build
cd build
cmake .. -G "Visual Studio 17 2022" -A Win32 -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
```

### Linux
```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
make
```

## 빌드 출력

### Windows
- DLL: `build/bin/Release/NativeRenderer.dll`
- Import Library: `build/lib/Release/NativeRenderer.lib`

### Linux
- Shared Object: `build/lib/libNativeRenderer.so`

## 파일 구조

```
NativeRenderer/
├── CMakeLists.txt          # CMake 빌드 설정
├── build_multi.bat         # Windows 멀티 아키텍처 빌드 스크립트
├── renderer.h              # 렌더러 헤더
├── renderer.cpp            # 렌더러 구현
├── TextRenderer.h          # 텍스트 렌더링 헤더
├── TextRenderer.cpp        # 텍스트 렌더링 구현
└── README.md               # 이 파일
```

## P/Invoke 사용법

C#에서 다음과 같이 사용합니다:

```csharp
[DllImport("NativeRenderer.dll", CallingConvention = CallingConvention.Cdecl)]
public static extern int InitializeRenderer(IntPtr windowHandle);

[DllImport("NativeRenderer.dll", CallingConvention = CallingConvention.Cdecl)]
public static extern void RenderFrame();
```

## 주요 기능

### 기본 렌더링
- `InitializeRenderer()`: OpenGL 컨텍스트 초기화
- `CleanupRenderer()`: OpenGL 리소스 정리
- `ResizeViewport()`: 뷰포트 크기 조정
- `RenderFrame()`: 프레임 렌더링

### 도형 그리기
- `DrawLine()`: 선 그리기
- `DrawArc()`: 호 그리기
- `DrawPoint()`: 점 그리기
- `DrawRectangle()`: 사각형 그리기 (외곽선)
- `DrawFilledRectangle()`: 채워진 사각형
- `DrawDashedRectangle()`: 점선 사각형

### MPF 렌더링
- `BeginMPFRender()`: MPF 렌더링 시작
- `EndMPFRender()`: MPF 렌더링 종료
- `SetViewTransform()`: 뷰 변환 (Pan/Zoom)

### 텍스트 렌더링 (Phase 8.1)
- `InitializeTextRenderer()`: 텍스트 렌더러 초기화
- `DrawPartNumber()`: Part 번호 표시
- `DrawContourNumber()`: Contour 번호 표시
- `CleanupTextRenderer()`: 텍스트 렌더러 정리

### 실시간 Trace (Phase 8.2)
- `StartCuttingTrace()`: 절단 추적 시작
- `UpdateCuttingTrace()`: 절단 추적 업데이트
- `StopCuttingTrace()`: 절단 추적 종료
- `DrawLaserHeadMarker()`: 레이저 헤드 마커 표시

## WinCC 배포 시 주의사항

1. **적절한 아키텍처 선택**
   - WinCC Runtime이 32비트면 x86 DLL 사용
   - WinCC Runtime이 64비트면 x64 DLL 사용

2. **DLL 위치**
   - `RealtimeITagControl.dll`과 같은 폴더에 배치
   - 또는 System32/SysWOW64 경로에 배치

3. **의존성 확인**
   - OpenGL32.dll (Windows 기본 제공)
   - GDI32.dll (Windows 기본 제공)
   - Visual C++ Redistributable (필요시)

## 문제 해결

### Architecture Mismatch 에러
→ WinCC Runtime 아키텍처와 일치하는 DLL 사용

### DLL을 찾을 수 없음
→ DLL을 실행 파일과 같은 폴더에 배치

### OpenGL 초기화 실패
→ 그래픽 드라이버 업데이트

### 텍스트 렌더링 깨짐
→ 폰트 파일 경로 확인 (Phase 8.1)

## 라이선스
이 프로젝트는 Phase8 Realtime Trace 프로젝트의 일부입니다.

## 문의
Phase11 프로젝트 관련 문의: GitHub Issues
