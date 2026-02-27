# 빌드 가이드

이 문서는 CAM Viewer POC 프로젝트를 처음부터 빌드하는 상세 과정을 설명합니다.

## 사전 준비

### 필수 소프트웨어

#### 1. Visual Studio 2019 또는 2022

**다운로드**: [Visual Studio](https://visualstudio.microsoft.com/)

**필수 워크로드**:
- ✅ .NET 데스크톱 개발
- ✅ C++를 사용한 데스크톱 개발

**개별 구성 요소** (Installer에서 선택):
- ✅ Windows 10 SDK (최신 버전)
- ✅ MSVC v142/v143 - C++ 빌드 도구
- ✅ CMake 도구
- ✅ .NET Framework 4.7.2 타겟 팩

#### 2. CMake (선택사항)

Visual Studio에 CMake가 포함되지 않은 경우:

**다운로드**: [CMake](https://cmake.org/download/)

**설치 옵션**:
- ✅ "Add CMake to system PATH for all users" 선택

**설치 확인**:
```cmd
cmake --version
```

### 선택 소프트웨어

- **Git**: 버전 관리 (이미 설치됨)
- **Visual Studio Code**: 코드 편집 (선택)

## 빌드 방법

### 방법 1: 자동 빌드 스크립트 (권장)

#### Windows 배치 파일 사용

1. **프로젝트 루트로 이동**:
   ```cmd
   cd C:\path\to\CamViewerPOC
   ```

2. **전체 빌드 실행**:
   ```cmd
   build_all.bat
   ```

   이 스크립트는 자동으로:
   - C++ DLL 빌드
   - C# 애플리케이션 빌드
   - DLL을 올바른 위치에 복사

#### 개별 컴포넌트 빌드

**C++ DLL만**:
```cmd
build_native.bat
```

**C# 앱만**:
```cmd
build_csharp.bat
```

### 방법 2: Visual Studio GUI 사용

#### A. C++ DLL 빌드

1. **CMake GUI 실행**:
   ```cmd
   cd NativeRenderer
   mkdir build
   cd build
   cmake-gui ..
   ```

2. **설정**:
   - Source: `NativeRenderer` 폴더 선택
   - Build: `NativeRenderer/build` 폴더 선택
   - Configure → Visual Studio 16 2019 (또는 17 2022) 선택
   - Platform: x64
   - Generate

3. **Visual Studio에서 빌드**:
   - `NativeRenderer/build/NativeRenderer.sln` 열기
   - Solution Configuration을 **Release**로 변경
   - Solution Platform을 **x64**로 변경
   - Build → Build Solution (Ctrl+Shift+B)

4. **결과 확인**:
   ```
   NativeRenderer/build/bin/Release/NativeRenderer.dll
   ```

#### B. C# 애플리케이션 빌드

1. **솔루션 열기**:
   - Visual Studio 실행
   - `WinFormsApp/CamViewerPOC.csproj` 열기

2. **빌드 설정**:
   - Configuration: **Release**
   - Platform: **Any CPU**

3. **빌드**:
   - Build → Build Solution (Ctrl+Shift+B)

4. **DLL 복사** (자동으로 안 되는 경우):
   ```cmd
   copy NativeRenderer\build\bin\Release\NativeRenderer.dll WinFormsApp\bin\Release\
   ```

5. **결과 확인**:
   ```
   WinFormsApp/bin/Release/CamViewerPOC.exe
   WinFormsApp/bin/Release/NativeRenderer.dll
   ```

### 방법 3: 명령줄 수동 빌드

#### A. C++ DLL 수동 빌드

```cmd
REM 1. NativeRenderer 폴더로 이동
cd NativeRenderer

REM 2. 빌드 디렉토리 생성
mkdir build
cd build

REM 3. CMake 구성 (Visual Studio 2019)
cmake -G "Visual Studio 16 2019" -A x64 ..

REM 또는 Visual Studio 2022의 경우:
REM cmake -G "Visual Studio 17 2022" -A x64 ..

REM 4. 빌드
cmake --build . --config Release

REM 5. 결과 확인
dir bin\Release\NativeRenderer.dll
```

#### B. C# 애플리케이션 수동 빌드

```cmd
REM 1. MSBuild 경로 찾기
REM Visual Studio 2022:
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

REM Visual Studio 2019:
REM set MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"

REM 2. WinFormsApp 폴더로 이동
cd WinFormsApp

REM 3. 빌드
%MSBUILD% CamViewerPOC.csproj /p:Configuration=Release /p:Platform="Any CPU"

REM 4. DLL 복사
copy ..\NativeRenderer\build\bin\Release\NativeRenderer.dll bin\Release\

REM 5. 결과 확인
dir bin\Release
```

## 빌드 후 검증

### 1. 파일 확인

다음 파일들이 존재해야 합니다:

```
WinFormsApp/bin/Release/
├── CamViewerPOC.exe          # 메인 실행 파일
├── CamViewerPOC.exe.config   # 앱 설정
├── NativeRenderer.dll        # C++ 렌더링 엔진 ⚠️ 중요!
└── (기타 .NET 어셈블리)
```

### 2. 실행 테스트

```cmd
cd WinFormsApp\bin\Release
CamViewerPOC.exe
```

**예상 결과**:
- 윈도우 창이 열림
- 샘플 도형이 표시됨
- 마우스 휠로 줌 가능
- 오른쪽 버튼으로 패닝 가능

### 3. 의존성 확인

**도구**: [Dependencies Walker](https://www.dependencywalker.com/) 또는 `dumpbin`

```cmd
REM Visual Studio 개발자 명령 프롬프트에서
dumpbin /dependents NativeRenderer.dll
```

**예상 종속성**:
- `KERNEL32.dll`
- `USER32.dll`
- `GDI32.dll`
- `OPENGL32.dll`
- `VCRUNTIME140.dll` (Visual C++ Runtime)

## 문제 해결

### 문제 1: CMake를 찾을 수 없음

**증상**:
```
'cmake'은(는) 내부 또는 외부 명령, 실행할 수 있는 프로그램, 또는
배치 파일이 아닙니다.
```

**해결책**:
1. Visual Studio Installer 실행
2. 설치된 제품 → 수정
3. 개별 구성 요소 → "Windows용 C++ CMake 도구" 체크
4. 수정 클릭

또는 독립 실행형 CMake 설치:
- [cmake.org](https://cmake.org/download/)에서 다운로드
- 설치 시 "Add to PATH" 선택

### 문제 2: MSBuild를 찾을 수 없음

**증상**:
```
'msbuild'은(는) 내부 또는 외부 명령이 아닙니다.
```

**해결책**:
1. **Visual Studio 개발자 명령 프롬프트** 사용:
   - 시작 메뉴 → Visual Studio 2022 → Developer Command Prompt

2. 또는 경로 수동 추가:
   ```cmd
   set PATH=%PATH%;C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin
   ```

### 문제 3: OpenGL 헤더를 찾을 수 없음

**증상**:
```
fatal error C1083: Cannot open include file: 'gl/GL.h'
```

**해결책**:
1. Windows SDK 설치 확인
2. Visual Studio Installer → 개별 구성 요소:
   - "Windows 10 SDK" 체크
3. 수정 후 재빌드

### 문제 4: NativeRenderer.dll 로드 실패

**증상**:
- 실행 시 "DLL not found" 오류
- 또는 "Entry point not found" 오류

**해결책 A**: DLL 위치 확인
```cmd
dir WinFormsApp\bin\Release\NativeRenderer.dll
```

없으면 수동 복사:
```cmd
copy NativeRenderer\build\bin\Release\NativeRenderer.dll WinFormsApp\bin\Release\
```

**해결책 B**: Visual C++ Redistributable 설치
- [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist) 다운로드
- vc_redist.x64.exe 설치

**해결책 C**: 의존성 확인
```cmd
dumpbin /dependents NativeRenderer.dll
```

### 문제 5: 빌드는 성공하지만 실행 시 충돌

**증상**:
- 프로그램이 시작 직후 종료
- "Application has stopped working" 메시지

**디버깅**:
1. **Debug 빌드로 재빌드**:
   ```cmd
   cmake --build . --config Debug
   ```

2. **Visual Studio에서 디버깅**:
   - F5 또는 Debug → Start Debugging
   - 충돌 지점 확인

3. **이벤트 뷰어 확인**:
   - Windows 로그 → 응용 프로그램
   - 오류 메시지 확인

### 문제 6: 플랫폼 불일치

**증상**:
```
BadImageFormatException: 잘못된 형식의 프로그램을 로드하려고 했습니다.
```

**원인**: C++ DLL(x64)과 C# 앱(Any CPU → x86) 불일치

**해결책**:
1. C# 프로젝트를 **x64**로 빌드:
   ```cmd
   msbuild CamViewerPOC.csproj /p:Configuration=Release /p:Platform=x64
   ```

2. 또는 프로젝트 속성 변경:
   - 프로젝트 → 속성 → Build
   - Platform target: **x64** 선택

## 배포 준비

### 1. Release 빌드 생성

```cmd
REM 1. 클린 빌드
cd NativeRenderer\build
cmake --build . --config Release --target clean
cmake --build . --config Release

cd ..\..\WinFormsApp
msbuild CamViewerPOC.csproj /t:Clean
msbuild CamViewerPOC.csproj /p:Configuration=Release /p:Platform="Any CPU"
```

### 2. 필수 파일 수집

```
Release/
├── CamViewerPOC.exe
├── CamViewerPOC.exe.config
├── NativeRenderer.dll
└── vcredist_x64.exe (선택)
```

### 3. 설치 프로그램 생성 (선택)

**도구 옵션**:
- **WiX Toolset**: MSI 패키지 생성
- **Inno Setup**: 간단한 설치 프로그램
- **NSIS**: 커스터마이징 가능한 설치 프로그램

## CI/CD 통합

### GitHub Actions 예제

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup MSBuild
      uses: microsoft/setup-msbuild@v1
    
    - name: Setup CMake
      uses: lukka/get-cmake@latest
    
    - name: Build C++ DLL
      run: |
        cd NativeRenderer
        mkdir build
        cd build
        cmake -G "Visual Studio 17 2022" -A x64 ..
        cmake --build . --config Release
    
    - name: Build C# App
      run: |
        cd WinFormsApp
        msbuild CamViewerPOC.csproj /p:Configuration=Release
    
    - name: Upload Artifacts
      uses: actions/upload-artifact@v2
      with:
        name: CamViewerPOC
        path: WinFormsApp/bin/Release/
```

## 참고 자료

- [CMake Documentation](https://cmake.org/documentation/)
- [MSBuild Reference](https://docs.microsoft.com/en-us/visualstudio/msbuild/)
- [Visual Studio C++ Projects](https://docs.microsoft.com/en-us/cpp/build/)

---

**문서 버전**: 1.0  
**최종 업데이트**: 2024
