# 에러 진단 가이드

InitializeOpenGL()에서 예외가 발생했을 때의 진단 방법입니다.

## 🔍 즉시 확인 사항

### 1. 에러 메시지 확인

업데이트된 코드는 이제 더 상세한 에러 메시지를 표시합니다:

- **Exception Type**: 어떤 종류의 에러인지
- **Message**: 구체적인 에러 메시지
- **Stack Trace**: 어디서 에러가 발생했는지

### 2. 진단 도구 사용

프로그램을 다시 빌드하고 실행하면:

1. **"Diagnostics" 버튼**이 추가되어 있습니다 (오렌지색)
2. 이 버튼을 클릭하면:
   - DLL 존재 여부
   - DLL 아키텍처 (x86 vs x64)
   - 프로세스 아키텍처
   - OpenGL 지원 여부
   - 모든 DLL 목록

이 정보를 확인하세요!

---

## 🐛 일반적인 에러 및 해결 방법

### 에러 1: DllNotFoundException

**증상**:
```
NativeRenderer.dll not found!
```

**해결책**:
```cmd
# DLL이 올바른 위치에 있는지 확인
dir WinFormsApp\bin\Release\NativeRenderer.dll

# 없으면 복사
copy NativeRenderer\build\bin\Release\NativeRenderer.dll WinFormsApp\bin\Release\
```

### 에러 2: BadImageFormatException

**증상**:
```
Platform mismatch error!
The NativeRenderer.dll architecture doesn't match this application.
```

**원인**: 
- DLL은 x64 (64-bit)로 빌드됨
- 애플리케이션이 x86 (32-bit)로 실행됨

**해결책**:

#### Option A: Visual Studio에서 플랫폼 변경

1. Visual Studio에서 CamViewerPOC.sln 열기
2. 상단 툴바에서 "Any CPU" 또는 "x86" → **"x64"** 선택
3. 또는 Configuration Manager:
   - Build → Configuration Manager
   - Active solution platform → x64 선택
   - 없으면 New → x64 추가
4. 다시 빌드

#### Option B: 프로젝트 설정 변경

1. 프로젝트 속성 열기 (Alt+Enter)
2. Build 탭
3. Platform target: **x64** 선택
4. 저장 후 빌드

#### Option C: 명령줄 빌드

```cmd
msbuild CamViewerPOC.csproj /p:Configuration=Release /p:Platform=x64
```

### 에러 3: OpenGL 초기화 실패

**증상**:
```
Failed to initialize OpenGL renderer
```

**가능한 원인**:

1. **그래픽 드라이버 구버전**
   - 해결: 최신 그래픽 드라이버 설치
   - NVIDIA: https://www.nvidia.com/Download/index.aspx
   - AMD: https://www.amd.com/en/support
   - Intel: https://www.intel.com/content/www/us/en/support/detect.html

2. **가상 머신 또는 원격 데스크톱**
   - 해결: 물리적 컴퓨터에서 실행
   - 또는 VM에 3D 가속 활성화

3. **OpenGL 지원 안 됨**
   - 확인: Diagnostics 버튼으로 opengl32.dll 확인
   - 해결: Windows 시스템 파일 복구
   ```cmd
   sfc /scannow
   ```

### 에러 4: Invalid Window Handle

**증상**:
```
Invalid window handle. Panel not created properly.
```

**원인**: renderPanel이 완전히 초기화되기 전에 OpenGL 초기화 시도

**해결책**:

#### CamViewerControl.cs 수정:

```csharp
private void CamViewerControl_Load(object sender, EventArgs e)
{
    if (!DesignMode)
    {
        // Panel이 완전히 생성될 때까지 대기
        renderPanel.CreateControl();
        
        // 약간의 지연 후 초기화
        this.BeginInvoke(new Action(() =>
        {
            InitializeOpenGL();
        }));
    }
}
```

---

## 🔧 상세 진단 단계

### 단계 1: DLL 확인

```cmd
cd WinFormsApp\bin\Release
dir *.dll
```

**예상 출력**:
```
NativeRenderer.dll      (반드시 있어야 함!)
```

### 단계 2: DLL 아키텍처 확인

PowerShell에서:
```powershell
[System.Reflection.Assembly]::LoadFile("C:\full\path\to\NativeRenderer.dll").GetName().ProcessorArchitecture
```

**예상 출력**: `Amd64` (64-bit)

### 단계 3: 애플리케이션 아키텍처 확인

Task Manager에서:
- CamViewerPOC.exe 실행
- 상세 정보 탭
- Platform 열 보기
- **64비트**로 표시되어야 함

### 단계 4: OpenGL 지원 확인

다운로드 및 실행: [OpenGL Extensions Viewer](https://www.realtech-vr.com/home/glview)

최소 요구사항: **OpenGL 2.1** 이상

---

## 📋 진단 정보 수집

에러가 계속 발생하면 다음 정보를 수집하세요:

### 1. Diagnostics 버튼 클릭
   - "Copy to Clipboard" 클릭
   - 정보 저장

### 2. 에러 메시지
   - 전체 에러 메시지 복사
   - Stack Trace 포함

### 3. 시스템 정보
   ```cmd
   systeminfo | findstr /B /C:"OS Name" /C:"OS Version" /C:"System Type"
   ```

### 4. Visual Studio 버전
   - Help → About Microsoft Visual Studio

---

## 🚀 빠른 테스트

다시 빌드하고 테스트하세요:

### 1. 프로젝트 정리

```cmd
cd WinFormsApp
rmdir /s /q bin obj
cd ..
```

### 2. 다시 빌드

```cmd
# Visual Studio 사용
CamViewerPOC.sln 열기 → Build → Rebuild Solution

# 또는 명령줄
msbuild WinFormsApp\CamViewerPOC.csproj /p:Configuration=Release /p:Platform=x64 /t:Rebuild
```

### 3. DLL 복사 확인

```cmd
copy NativeRenderer\build\bin\Release\NativeRenderer.dll WinFormsApp\bin\x64\Release\
```
(플랫폼이 x64인 경우 경로가 달라질 수 있음)

### 4. 실행 및 테스트

```cmd
WinFormsApp\bin\Release\CamViewerPOC.exe
```

또는

```cmd
WinFormsApp\bin\x64\Release\CamViewerPOC.exe
```

### 5. Diagnostics 버튼 클릭

모든 정보가 OK인지 확인

---

## 💡 업데이트된 기능

### 향상된 에러 메시지

이제 에러 메시지가 다음을 포함합니다:

1. **DllNotFoundException**: DLL 경로와 예상 위치
2. **BadImageFormatException**: 아키텍처 불일치 상세 정보
3. **일반 Exception**: Exception Type, Message, Stack Trace

### 진단 도구

- **Diagnostics 버튼**: 시스템 및 DLL 정보 확인
- **복사 기능**: 진단 정보를 클립보드에 복사

---

## 📞 추가 지원

위 방법으로 해결되지 않으면:

1. **Diagnostics 버튼** 클릭 → **Copy to Clipboard**
2. **에러 메시지** 전체 복사
3. 위 정보와 함께 문의

---

## ✅ 체크리스트

문제 해결 전 확인:

- [ ] NativeRenderer.dll이 실행 파일과 같은 폴더에 있음
- [ ] DLL과 애플리케이션 모두 x64 (64-bit)
- [ ] 최신 그래픽 드라이버 설치됨
- [ ] OpenGL 2.1 이상 지원됨
- [ ] Visual C++ Redistributable 설치됨
- [ ] Diagnostics 도구 실행 완료

---

**다시 빌드하고 Diagnostics 버튼으로 확인해보세요!** 🔍
