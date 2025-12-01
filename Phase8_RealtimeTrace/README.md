# CAM Viewer POC - WinCC Advanced v17 Compatible

이 프로젝트는 Siemens TIA Portal WinCC Advanced v17에 삽입 가능한 절단(Trace) 뷰어의 개념 증명(POC) 구현입니다.

## 프로젝트 구조

```
CamViewerPOC/
├── NativeRenderer/          # C++ OpenGL 렌더링 엔진 (DLL)
│   ├── renderer.h           # C API 헤더
│   ├── renderer.cpp         # OpenGL 구현
│   └── CMakeLists.txt       # CMake 빌드 설정
│
├── WinFormsApp/             # C# WinForms 사용자 컨트롤
│   ├── CamViewerControl.cs  # OpenGL 뷰어 컨트롤 (WinCC 삽입용)
│   ├── MainForm.cs          # 테스트용 메인 폼
│   ├── Program.cs           # 진입점
│   └── CamViewerPOC.csproj  # 프로젝트 파일
│
├── Docs/                    # 문서
├── build_native.bat         # C++ DLL 빌드 스크립트
├── build_csharp.bat         # C# 앱 빌드 스크립트
├── build_all.bat            # 전체 빌드 스크립트
└── README.md                # 이 파일
```

## 아키텍처 설계

### 계층 구조

```
┌─────────────────────────────────────────────┐
│    WinCC Advanced Runtime Environment      │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│      C# WinForms UserControl Layer         │
│  • UI 컨트롤 및 사용자 상호작용              │
│  • 데이터 로딩 및 관리                       │
│  • WinCC 태그 연동                          │
│  • P/Invoke 브리지                          │
└─────────────────┬───────────────────────────┘
                  │ P/Invoke (C API)
┌─────────────────▼───────────────────────────┐
│      C++ Native Renderer DLL               │
│  • OpenGL 컨텍스트 관리                      │
│  • 2D/3D 그래픽 렌더링                       │
│  • 성능 최적화된 렌더링 파이프라인            │
│  • VBO/IBO 관리                             │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│           OpenGL API                        │
└─────────────────────────────────────────────┘
```

### 주요 특징

1. **C# UI Layer**
   - WinForms UserControl로 WinCC에 쉽게 삽입 가능
   - 데이터 처리 및 관리 로직
   - 마우스/키보드 인터랙션 처리
   - WinCC 태그 시스템과 연동 가능

2. **C++ Rendering Layer**
   - 고성능 OpenGL 렌더링
   - 최적화된 그래픽 처리
   - C API를 통한 간단한 인터페이스 제공

3. **P/Invoke Bridge**
   - C#과 C++ 간 효율적인 통신
   - 단순한 C API로 복잡도 최소화
   - 타입 안정성 보장

## 빌드 요구사항

### C++ DLL 빌드
- **CMake** 3.15 이상
- **Visual Studio 2019/2022** (C++ 워크로드 포함)
- **Windows SDK**
- **OpenGL** (Windows에 기본 포함)

### C# 애플리케이션 빌드
- **.NET Framework 4.7.2** 이상
- **Visual Studio 2019/2022** 또는 MSBuild
- **Windows Forms** 컴포넌트

## 빌드 방법

### 방법 1: 자동 빌드 (권장)

```batch
# 모든 컴포넌트 빌드
build_all.bat
```

### 방법 2: 개별 빌드

```batch
# C++ DLL만 빌드
build_native.bat

# C# 애플리케이션만 빌드
build_csharp.bat
```

### 방법 3: Visual Studio 사용

1. **C++ DLL 빌드**:
   ```batch
   cd NativeRenderer
   mkdir build
   cd build
   cmake -G "Visual Studio 16 2019" -A x64 ..
   cmake --build . --config Release
   ```

2. **C# 애플리케이션 빌드**:
   - Visual Studio에서 `WinFormsApp/CamViewerPOC.csproj` 열기
   - Release 구성으로 빌드 (Ctrl+Shift+B)

## 실행 방법

### 독립 실행형 테스트

```batch
WinFormsApp\bin\Release\CamViewerPOC.exe
```

### WinCC Advanced에 통합

1. **UserControl 등록**:
   - `CamViewerControl.cs`를 WinCC 프로젝트에 참조
   - 또는 DLL로 빌드하여 등록

2. **DLL 배포**:
   - `NativeRenderer.dll`을 WinCC Runtime 실행 경로에 복사
   - 일반적으로 프로젝트의 `bin` 또는 `Runtime` 폴더

3. **WinCC 화면에 추가**:
   - Graphics Designer에서 UserControl 삽입
   - 필요한 태그와 연결

## 기능 데모

### 현재 구현된 기능

#### 기본 기능
- ✅ MPF 파일 로드 및 파싱
- ✅ 파트 및 컨투어 렌더링
- ✅ 줌 인/아웃 (마우스 휠)
- ✅ 패닝 (왼쪽/오른쪽/중간 마우스 버튼)
- ✅ 뷰 리셋
- ✅ 배경 이미지 지원
- ✅ 가공 시뮬레이션 (Contour/Element 레벨)

#### Phase 5 기능 (2024 신규)
- ✅ **컨투어/엘리먼트 선택** - 마우스 클릭으로 컨투어 및 개별 세그먼트 선택
- ✅ **다중 엘리먼트 선택** - Ctrl + 클릭으로 여러 엘리먼트 동시 선택
- ✅ **선택 하이라이트** - 선택된 항목을 노란색/마젠타로 강조 표시
- ✅ **파트 외곽선** - 6가지 점선 패턴으로 파트 경계 표시
- ✅ **사용자 정의 번호 위치** - 파트/컨투어 번호를 원하는 위치로 이동
- ✅ **번호 위치 저장/불러오기** - JSON 형식으로 위치 정보 영구 저장
- ✅ **캔버스 회전** - 0°, 90°, 180°, 270° 회전 지원
- ✅ **시뮬레이션 중 선택** - 가공 시뮬레이션과 선택 기능 동시 사용

### 사용법

#### 기본 조작
1. **Load MPF**: MPF 파일 로드
2. **Reset View**: 뷰 초기화 (줌/패닝 리셋)
3. **Render Settings**: 색상, 선 두께 등 렌더링 설정
4. **Contour Sim / Element Sim**: 가공 시뮬레이션 실행

#### Phase 5 기능 사용
1. **선택 모드**:
   - "컨투어 (Contour)": 컨투어 전체 선택
   - "엘리먼트 (Element)": 개별 세그먼트 선택
   - "다중 선택 (Multi-select)": Ctrl + 클릭으로 여러 엘리먼트 선택

2. **파트 외곽선**:
   - Render Settings → "파트 외곽선 표시" 체크
   - 색상, 두께, 점선 패턴 설정 가능

3. **번호 위치 조정**:
   - "파트 번호 위치 (Part #)": 파트 번호 이동
   - "컨투어 번호 위치 (Contour #)": 컨투어 번호 이동
   - "위치 저장/불러오기": JSON 파일로 위치 저장

4. **캔버스 회전**:
   - Render Settings → "캔버스 방향" → 각도 선택 (0°/90°/180°/270°)

### 마우스 컨트롤

- **휠**: 확대/축소
- **왼쪽 버튼 클릭**: 선택 (선택 모드 활성화 시) / 패닝 (일반 모드)
- **왼쪽 버튼 드래그**: 화면 이동 (패닝)
- **오른쪽/중간 버튼 드래그**: 화면 이동
- **Ctrl + 왼쪽 클릭**: 다중 엘리먼트 선택 (엘리먼트 모드 + 다중 선택 활성화 시)

## API 참조

### C++ Native API

```cpp
// 초기화 및 정리
int InitializeRenderer(void* windowHandle);
void CleanupRenderer();

// 뷰포트 관리
void ResizeViewport(int width, int height);

// 렌더링
void RenderFrame();

// 도형 그리기
void DrawRectangle(float x, float y, float width, float height, 
                   float r, float g, float b);
void DrawCircle(float x, float y, float radius, 
                float r, float g, float b);
void ClearShapes();

// 뷰 변환
void SetViewTransform(float zoom, float panX, float panY);
```

### C# Public API

```csharp
// 도형 추가
public void AddRectangle(float x, float y, float width, float height, Color color)
public void AddCircle(float x, float y, float radius, Color color)

// 장면 관리
public void ClearScene()
public void DrawSampleShapes()

// 뷰 제어
public void ResetView()
```

## Phase 5 문서

Phase 5 기능에 대한 상세한 정보는 다음 문서를 참조하세요:

- **[PHASE5_USER_GUIDE.md](./PHASE5_USER_GUIDE.md)** - 사용자 가이드 (사용법, FAQ, 문제 해결)
- **[PHASE5_IMPLEMENTATION_COMPLETE.md](./PHASE5_IMPLEMENTATION_COMPLETE.md)** - 기술 구현 세부사항

### Phase 5 주요 기능 요약

| 기능 | 설명 | 주요 클래스 |
|------|------|-------------|
| **선택 시스템** | 컨투어/엘리먼트 선택 및 하이라이트 | `SelectionManager` |
| **기하학 유틸리티** | Point-in-Polygon, 거리 계산, Bounding Box | `GeometryUtils` |
| **파트 외곽선** | 6가지 점선 패턴으로 파트 경계 표시 | `NativeRenderer` |
| **번호 위치 관리** | 사용자 정의 번호 위치 및 JSON 저장 | `NumberPositionManager` |
| **캔버스 회전** | 0°/90°/180°/270° 회전 지원 | `NativeRenderer` |

## 다음 단계

### Phase 6 계획 (실시간 데이터 처리)
- [ ] ITag 통신 구현
- [ ] Siemens WinCC Advanced DLL 통합
- [ ] 실시간 데이터 수신 및 처리
- [ ] 실시간 시각화 업데이트

### 단기 개선 사항
- [ ] 개별 번호 리셋 기능
- [ ] 선택 허용 오차 UI 설정
- [ ] 커스텀 점선 패턴 정의
- [ ] 선택 정보 내보내기
- [ ] 자동 프레임 맞춤 (Auto-fit)

### 성능 최적화
- [ ] VBO/IBO 최적화
- [ ] 셰이더 프로그램 구현
- [ ] LOD (Level of Detail) 시스템
- [ ] 컬링 및 클리핑

### WinCC 통합 강화
- [ ] WinCC 태그 바인딩 확장
- [ ] 알람 시각화
- [ ] 이벤트 로깅
- [ ] 작업자 인터페이스 최적화

## 문제 해결

### 일반적인 문제

#### DLL을 찾을 수 없음

**증상**: "NativeRenderer.dll not found" 오류

**해결책**:
1. DLL이 실행 파일과 같은 폴더에 있는지 확인
2. 올바른 플랫폼(x64)으로 빌드했는지 확인
3. Visual C++ Redistributable이 설치되어 있는지 확인

#### OpenGL 초기화 실패

**증상**: "Failed to initialize OpenGL renderer" 오류

**해결책**:
1. 그래픽 드라이버 업데이트
2. OpenGL 지원 여부 확인 (최소 OpenGL 2.1)
3. WinCC Runtime과 충돌 확인

#### 빌드 오류

**증상**: CMake 또는 MSBuild 오류

**해결책**:
1. Visual Studio가 올바르게 설치되었는지 확인
2. 필요한 워크로드(C++ Desktop, .NET Desktop)가 설치되었는지 확인
3. 경로에 한글이나 특수문자가 없는지 확인

### Phase 5 관련 문제

Phase 5 기능의 문제 해결은 **[PHASE5_USER_GUIDE.md](./PHASE5_USER_GUIDE.md)**의 "문제 해결" 섹션을 참조하세요:

- 선택 기능 문제
- 파트 외곽선 문제
- 번호 위치 설정 문제
- 캔버스 회전 문제
- 성능 저하 문제

## 라이선스

이 프로젝트는 POC(Proof of Concept)용입니다.

## 연락처

문제나 제안사항이 있으면 이슈를 등록해주세요.

---

## 버전 히스토리

### Phase 5 (2024) - 현재 버전
- ✅ 컨투어/엘리먼트 선택 시스템
- ✅ 파트 외곽선 (6가지 점선 패턴)
- ✅ 사용자 정의 번호 위치 (JSON 저장)
- ✅ 캔버스 회전 (0°/90°/180°/270°)
- ✅ 시뮬레이션 중 선택 지원

### Phase 4 (2024)
- MPF 파일 파싱 및 렌더링
- 가공 시뮬레이션 (Contour/Element)
- 렌더 설정 시스템
- 배경 이미지 지원

### Phase 1-3 (초기 개발)
- 기본 OpenGL 렌더링 엔진
- WinForms UserControl 구조
- P/Invoke 브리지
- 기본 뷰 컨트롤 (줌/패닝)

---

**버전**: 5.0.0 (Phase 5 완료)  
**최종 업데이트**: 2024  
**호환성**: Windows 10/11, WinCC Advanced v17, .NET Framework 4.7.2+  
**문서**: [사용자 가이드](./PHASE5_USER_GUIDE.md) | [구현 세부사항](./PHASE5_IMPLEMENTATION_COMPLETE.md)
