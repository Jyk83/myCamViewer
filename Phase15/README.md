# Phase11: Final WinCC ITag Realtime MPF Viewer

## 개요
**실제 장비용 WinCC Advanced ITag 기반 MPF 렌더링 & 트레이스 프로그램**

Phase8 (OpenGL 렌더링) + Phase9 (ITag 통신) + Phase10 (레이아웃) 완전 통합 버전

## 핵심 기능

### 1. WinCC ITag 통신
- **자동 연결**: UserControl Load 시 자동 ITag 서버 연결
- **Cyclic Read**: 500ms 주기로 Tag 읽기
- **Tag 정의**:
  - `HMI_VIEW_WORK_DIR`: MPF 파일 디렉토리
  - `HMI_VIEW_WORK_MPF_NAME`: MPF 파일명
  - `HMI_VIEW_WORK_STATUS`: 작업 상태 (0:완료, 1:진행, 2:일시정지, 3:에러)
  - `HMI_VIEW_ACT_LINE_CODE`: 현재 실행 라인 코드
  - `HMI_VIEW_ACT_LINE_NUM`: 현재 실행 라인 번호
  - `HMI_VIEW_SEARCH_PART`: 현재 Part 번호
  - `HMI_VIEW_SEARCH_CONT`: 현재 Contour 번호
  - `HMI_VIEW_DIR_TYPE`: 절단 방향 (0:정방향, 1:역방향)

### 2. OpenGL 고품질 렌더링 (Phase8 완전 이식)
- **NativeRenderer.dll**: OpenGL 기반 하드웨어 가속 렌더링
- **Phase8 렌더링 로직 100% 이식**:
  - Workpiece 배경 (Exterior + Interior)
  - Part Origin 정확한 위치
  - Arc 정확한 호 렌더링 (원이 아님!)
  - Lead-in 경로 (노란색, 얇은 선)
  - Piercing Point (빨간 원)
  - Marking 경로 (노란색, CuttingType=10)
  - Cutting Path 상태별 색상:
    - **Gray**: 미시작 (NotStarted)
    - **Red**: 진행 중 (InProgress)
    - **Cyan**: 완료 (Completed)

### 3. Part/Contour Number 표시 (OpenGL Text)
- Part Number: 파트 중심에 큰 숫자 표시
- Contour Number: 컨투어 시작점에 작은 숫자 표시
- RenderSettings에서 크기, 색상 설정 가능

### 4. Realtime Trace
- WorkStatus에 따라 절단 진행 상황 실시간 추적
- 현재 Part/Contour 강조 표시
- 완료된 경로는 Cyan으로 변경

### 5. Pan/Zoom (Phase8 방식)
- **마우스 드래그**: Pan (화면 이동)
- **마우스 휠**: Zoom (확대/축소)
- **Zoom 범위**: 0.1 ~ 30000.0 (극한 확대 지원)
- **AutoFit**: MPF 로드 시 자동 화면 맞춤

### 6. Simulation
- "Simulation" 버튼으로 시작/중지
- 가상으로 절단 과정 시뮬레이션

### 7. Element Selection
- "Element Select" 버튼으로 모드 전환
- 마우스 클릭으로 Part/Contour 선택

### 8. RenderSettings (JSON 설정)
- `RenderSettings.json` 파일로 모든 렌더링 파라미터 설정
- 색상, 선 굵기, 표시 옵션 등 커스터마이징

## 프로젝트 구조

```
Phase11_Final/
├── RealtimeITagControl/
│   ├── RealtimeITagControl.cs     # 메인 UserControl (레이아웃 + ITag 통신)
│   ├── CamViewerCore.cs           # Phase8 OpenGL 렌더링 로직 (완전 이식)
│   ├── NativeRenderer.dll         # OpenGL DLL
│   ├── ITagManager.cs             # ITag 통신 관리
│   ├── TagDefinitions.cs          # Tag 정의
│   │
│   ├── MPF/                       # MPF 파싱
│   │   ├── MPFParser.cs
│   │   ├── MPFProgram.cs
│   │   ├── Commands.cs
│   │   └── Point2D.cs
│   │
│   ├── Rendering/                 # 렌더링 설정
│   │   ├── RenderSettings.cs
│   │   └── NativeTextRenderer.cs
│   │
│   ├── Simulation/                # Phase8 시뮬레이션
│   │   └── SimulationEngine.cs
│   │
│   ├── Selection/                 # Phase8 선택 기능
│   │   ├── SelectionManager.cs
│   │   ├── NumberPositionManager.cs
│   │   └── GeometryUtils.cs
│   │
│   ├── Trace/                     # Phase8 트레이스
│   │   ├── TraceManager.cs
│   │   └── CuttingProgressData.cs
│   │
│   └── UI/                        # UI 컴포넌트
│       └── ProgramInfoPanel.cs
│
├── build.bat                      # 빌드 스크립트
├── Siemens.Runtime.ControlDev.dll # WinCC 참조 DLL
└── README.md                      # 이 파일
```

## 빌드 방법

### ⚠️ 중요: NativeRenderer.dll Architecture 문제

**Architecture Mismatch 에러가 발생하는 경우:**
```
Platform mismatch error!
The NativeRenderer.dll architecture doesn't match this application.
- DLL is x64 (64-bit)
- Application must be x64 or AnyCPU
```

**해결 방법:**

#### 옵션 1: WinCC Runtime 아키텍처 확인 (추천)

1. WinCC Runtime이 32비트인지 64비트인지 확인
2. 해당 아키텍처의 NativeRenderer.dll 사용

#### 옵션 2: x86 버전 NativeRenderer.dll 빌드

```batch
cd Phase11_Final\NativeRenderer
mkdir build\x86
cd build\x86
cmake ..\.. -G "Visual Studio 17 2022" -A Win32 -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
copy bin\Release\NativeRenderer.dll ..\..\..\RealtimeITagControl\
```

#### 옵션 3: Multi-Architecture 빌드 (x86 + x64)

```batch
cd Phase11_Final\NativeRenderer
build_multi.bat
```

이 스크립트는 x86과 x64 두 버전을 모두 빌드하여:
- `NativeRenderer_x86.dll`
- `NativeRenderer_x64.dll`

두 파일을 RealtimeITagControl 폴더에 복사합니다.

상세한 내용은 [`NativeRenderer/README.md`](NativeRenderer/README.md)를 참조하세요.

### C# 프로젝트 빌드

```batch
cd Phase11_Final
build.bat
```

빌드 결과: `RealtimeITagControl\bin\Release\RealtimeITagControl.dll`

**빌드 설정:**
- **Platform**: AnyCPU
- **Prefer32Bit**: false
- **Framework**: .NET Framework 4.7.2

이 설정은 WinCC Runtime 환경에 자동으로 적응합니다:
- x64 WinCC → 64비트 프로세스로 실행 → NativeRenderer_x64.dll 로드
- x86 WinCC → 32비트 프로세스로 실행 → NativeRenderer_x86.dll 로드

## WinCC Graphics Designer에 임포트

1. WinCC Runtime Advanced 프로젝트 열기
2. Graphics Designer에서 "User Controls" 추가
3. `RealtimeITagControl.dll` 선택
4. Toolbox에 `RealtimeITagControl` 추가
5. 화면에 드래그 & 드롭

## ITag 설정

WinCC에서 다음 Tag를 생성하고 연결:

```
HMI_VIEW_WORK_DIR        : String
HMI_VIEW_WORK_MPF_NAME   : String
HMI_VIEW_WORK_STATUS     : Int (0=완료, 1=진행, 2=일시정지, 3=에러)
HMI_VIEW_ACT_LINE_CODE   : String
HMI_VIEW_ACT_LINE_NUM    : Int
HMI_VIEW_SEARCH_PART     : Int (현재 Part 번호)
HMI_VIEW_SEARCH_CONT     : Int (현재 Contour 번호)
HMI_VIEW_DIR_TYPE        : Int (0=정방향, 1=역방향)
```

## RenderSettings 커스터마이징

`RenderSettings.json` 예시:

```json
{
    "WorkpieceExteriorColor": "#000000",
    "WorkpieceInteriorColor": "#0A3A3A",
    "LeadInColor": "#FFFF00",
    "PiercingPointColor": "#FF0000",
    "MarkingColor": "#FFFF00",
    "CuttingCompletedColor": "#00FFFF",
    "CuttingInProgressColor": "#FF0000",
    "CuttingPendingColor": "#808080",
    "PartNumberSize": 50.0,
    "ContourNumberSize": 30.0,
    "InitialZoomMultiplier": 0.005,
    "ShowPartNumbers": true,
    "ShowContourNumbers": true,
    "ShowPartOrigin": false,
    "ShowWorkpieceBoundary": false
}
```

## 주요 변경 사항 (vs Phase10)

### ✅ 추가된 기능
1. **OpenGL 렌더링 100% 이식** (Phase8 CamViewerCore)
2. **Part/Contour Number OpenGL 텍스트 표시**
3. **Simulation 엔진** (Phase8 이식)
4. **Element Selection** (Phase8 이식)
5. **TraceManager** (Realtime Trace 고도화)
6. **Zoom 최대값 30000.0** (극한 확대)
7. **정확한 Arc 렌더링** (호가 아닌 원 문제 해결)

### 🗑️ 제거된 기능
1. **ITag Test Form** (시뮬레이션 완료, 불필요)
2. **GDI+ 렌더링 코드** (OpenGL로 완전 대체)

### 🔧 수정된 부분
1. **Pan 방향 수정** (Phase8 방식, 마우스 드래그 방향 정상화)
2. **좌표계 완전 재구성** (Workpiece 좌상단 기준)
3. **네임스페이스 통일** (`RealtimeITagControl`)

## 기술 스택

- **언어**: C# (.NET Framework 4.7.2)
- **렌더링**: OpenGL (NativeRenderer.dll)
- **UI**: WinForms (UserControl)
- **통신**: Siemens WinCC Advanced ITag
- **빌드**: MSBuild (x86)

## 성능

- **렌더링 FPS**: 60fps (OpenGL)
- **ITag Cyclic Read**: 500ms
- **메모리 사용량**: ~50MB (MPF 로드 시)
- **Zoom 범위**: 0.1 ~ 30000.0

## 라이선스

내부 프로젝트용 (비공개)

## 개발 이력

- **Phase1-7**: 기초 개발
- **Phase8**: OpenGL 렌더링 완성
- **Phase9**: ITag 통신 추가
- **Phase10**: WinCC DLL 레이아웃
- **Phase11**: 완전 통합 (Final)
  - Phase8 렌더링 100% 이식
  - Phase9 ITag 통신 유지
  - Phase10 레이아웃 유지
  - Simulation, Selection 추가
  - GDI+ 코드 완전 제거
  - ITag Test Form 제거

---

**Phase11: 실제 장비에 배포 가능한 완성 버전** 🎉
