# 빠른 시작 가이드

CAM Viewer POC를 빠르게 빌드하고 실행하는 방법입니다.

## 5분 안에 시작하기

### 1단계: 요구사항 확인 (1분)

필수:
- ✅ Windows 10/11 (64-bit)
- ✅ Visual Studio 2019 또는 2022

확인 방법:
```cmd
REM Visual Studio 설치 확인
where msbuild
```

### 2단계: 프로젝트 다운로드 (1분)

```cmd
REM 프로젝트 폴더로 이동
cd C:\Projects\CamViewerPOC
```

### 3단계: 빌드 (2분)

```cmd
REM 자동 빌드 실행
build_all.bat
```

**예상 출력**:
```
========================================
Building Native Renderer DLL
========================================
[CMake configuration...]
[Build completed]

========================================
Building C# WinForms Application
========================================
[MSBuild...]
[Build completed]

All builds completed successfully!
```

### 4단계: 실행 (1분)

```cmd
WinFormsApp\bin\Release\CamViewerPOC.exe
```

**예상 결과**:
- 윈도우가 열림
- 샘플 도형이 표시됨 (사각형, 원형)
- 마우스 휠로 줌 가능
- 오른쪽 버튼으로 드래그해서 이동 가능

## 기본 사용법

### 버튼 설명

| 버튼 | 기능 |
|------|------|
| **Add Rectangle** | 랜덤 위치/색상의 사각형 추가 |
| **Add Circle** | 랜덤 위치/색상의 원형 추가 |
| **Clear Scene** | 모든 도형 제거 |
| **Reset View** | 뷰 초기화 (줌/위치 리셋) |
| **Sample Shapes** | 미리 정의된 샘플 도형 표시 |

### 마우스 조작

| 조작 | 기능 |
|------|------|
| **마우스 휠** | 확대/축소 (줌) |
| **오른쪽 버튼 드래그** | 화면 이동 (패닝) |
| **중간 버튼 드래그** | 화면 이동 (패닝) |

## 문제 해결

### "DLL not found" 오류

**증상**: 프로그램이 시작되지 않고 "NativeRenderer.dll not found" 메시지

**해결책**:
```cmd
REM DLL 수동 복사
copy NativeRenderer\build\bin\Release\NativeRenderer.dll WinFormsApp\bin\Release\
```

### "OpenGL initialization failed" 오류

**증상**: 프로그램이 시작되지만 빈 화면 또는 오류 메시지

**해결책**:
1. 그래픽 드라이버 업데이트
2. OpenGL 지원 확인:
   ```cmd
   REM OpenGL 버전 확인 도구 다운로드 및 실행
   REM https://www.realtech-vr.com/home/glview
   ```

### 빌드 실패

**증상**: build_all.bat 실행 시 오류

**해결책**:
1. Visual Studio가 설치되어 있는지 확인
2. 필수 워크로드 설치:
   - .NET 데스크톱 개발
   - C++ 데스크톱 개발
3. Visual Studio Installer에서 확인 가능

## 다음 단계

### 코드 수정하기

**도형 색상 변경** (`WinFormsApp/CamViewerControl.cs`):
```csharp
public void DrawSampleShapes()
{
    NativeRenderer.ClearShapes();
    
    // 색상 변경: RGB 값 (0.0 ~ 1.0)
    NativeRenderer.DrawRectangle(-0.5f, -0.3f, 0.4f, 0.3f, 
                                 1.0f, 0.0f, 0.0f);  // 빨강
}
```

**도형 위치 변경**:
```csharp
// x, y: -1.0 (좌/하) ~ 1.0 (우/상)
NativeRenderer.DrawCircle(0.0f, 0.0f, 0.2f,  // 중앙
                          0.0f, 1.0f, 0.0f);  // 녹색
```

### Visual Studio에서 열기

**C++ 프로젝트**:
```cmd
cd NativeRenderer\build
start NativeRenderer.sln
```

**C# 프로젝트**:
```cmd
cd WinFormsApp
start CamViewerPOC.csproj
```

### WinCC에 통합하기

자세한 내용은 다음 문서 참조:
- [WINCC_INTEGRATION.md](Docs/WINCC_INTEGRATION.md)

## 프로젝트 구조 이해

```
CamViewerPOC/
│
├── NativeRenderer/          ← C++ OpenGL 렌더링 엔진
│   ├── renderer.h           ← API 인터페이스
│   └── renderer.cpp         ← 구현
│
├── WinFormsApp/             ← C# 사용자 인터페이스
│   ├── CamViewerControl.cs  ← 메인 뷰어 (WinCC 삽입용)
│   └── MainForm.cs          ← 테스트용 폼
│
└── Docs/                    ← 상세 문서
    ├── ARCHITECTURE.md      ← 설계 문서
    ├── BUILD_GUIDE.md       ← 빌드 상세 가이드
    └── WINCC_INTEGRATION.md ← WinCC 통합 가이드
```

## 자주 묻는 질문 (FAQ)

**Q: Visual Studio가 없는데 빌드할 수 있나요?**  
A: Visual Studio Build Tools만 설치해도 가능합니다.
   [다운로드](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022)

**Q: 32-bit Windows에서 실행 가능한가요?**  
A: 아니요, 64-bit 전용입니다. WinCC Advanced v17도 64-bit 전용입니다.

**Q: OpenGL 대신 DirectX를 사용할 수 있나요?**  
A: 가능합니다. renderer.cpp를 DirectX로 재작성하면 됩니다. API는 동일하게 유지됩니다.

**Q: WinCC v16에서도 동작하나요?**  
A: .NET Framework 버전 조정이 필요할 수 있습니다. 테스트 필요합니다.

**Q: Linux에서 빌드 가능한가요?**  
A: 현재는 Windows 전용입니다. WinCC가 Windows 전용이므로 필요성이 낮습니다.

## 추가 자료

### 상세 문서
- [README.md](README.md) - 전체 개요
- [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md) - 프로젝트 요약
- [ARCHITECTURE.md](Docs/ARCHITECTURE.md) - 아키텍처 설계
- [BUILD_GUIDE.md](Docs/BUILD_GUIDE.md) - 상세 빌드 가이드
- [WINCC_INTEGRATION.md](Docs/WINCC_INTEGRATION.md) - WinCC 통합

### 외부 참고 자료
- [OpenGL Tutorial](https://learnopengl.com/)
- [WinForms Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)
- [P/Invoke Tutorial](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)

## 도움 받기

문제가 해결되지 않으면:
1. 이슈 등록
2. 빌드 로그 첨부
3. 환경 정보 제공 (OS, VS 버전 등)

---

**즐거운 코딩 되세요!** 🚀

문서 버전: 1.0  
최종 업데이트: 2024
