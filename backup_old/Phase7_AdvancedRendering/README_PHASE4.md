# Phase 4: Graphics Enhancement (그래픽 고도화)

## 개요

Phase3 기능에 고급 렌더링 기능을 추가한 버전입니다.
- 사용자 정의 색상 및 크기
- 개선된 마우스 컨트롤
- 렌더링 설정 UI

## 새로운 기능

### 1. 마우스 컨트롤 개선
- **좌클릭 드래그**: 캔버스 이동 (기존: 우클릭/중클릭만)
- 마우스 클릭 위치를 중심으로 자연스러운 이동
- 휠: 줌 인/아웃 (기존과 동일)

### 2. 파트 원점 표시 옵션
- 기본적으로 파트 원점(녹색 점) 숨김
- 렌더링 설정에서 표시/숨김 토글 가능

### 3. 사용자 정의 색상 시스템
모든 렌더링 요소의 색상을 사용자가 정의 가능:

| 요소 | 기본 색상 | 설명 |
|------|----------|------|
| **워크피스 경계** | 흰색 | 작업 영역 테두리 |
| **파트 원점** | 녹색 | 파트 기준점 (기본 숨김) |
| **피어싱 포인트** | 빨강 | 레이저 시작 위치 |
| **리드인 경로** | 노랑 | 진입 경로 |
| **절단 완료** | 청록색 | 완료된 절단 경로 |
| **절단 진행 중** | 빨강 | 현재 절단 중 (시뮬레이션) |
| **절단 대기** | 회색 | 미래 경로 미리보기 |

### 4. 사용자 정의 크기 시스템
모든 렌더링 요소의 크기를 픽셀 단위로 조정 가능:

| 요소 | 기본 크기 | 범위 |
|------|----------|------|
| **워크피스 경계 두께** | 2.0px | 0.5 ~ 10.0px |
| **파트 원점 크기** | 8.0px | 1 ~ 20px |
| **피어싱 포인트 크기** | 6.0px | 1 ~ 20px |
| **리드인 경로 두께** | 1.5px | 0.5 ~ 10.0px |
| **절단 완료 두께** | 2.0px | 0.5 ~ 10.0px |
| **절단 진행 중 두께** | 2.5px | 0.5 ~ 10.0px |
| **절단 대기 두께** | 1.0px | 0.5 ~ 10.0px |

## 새로운 UI 구성

### 렌더링 설정 대화상자
**위치**: 시뮬레이션 패널 → "Render Settings (렌더링 설정)" 버튼 (보라색)

**설정 항목**:
1. **색상 설정**
   - 각 요소별 ColorPicker를 통한 색상 선택
   - 실시간 버튼 색상 미리보기

2. **크기 설정**
   - NumericUpDown을 통한 정밀 조정
   - 0.5px 단위 증감

3. **표시 옵션**
   - "파트 원점 표시" 체크박스

4. **액션 버튼**
   - **OK**: 설정 저장 및 적용
   - **Cancel**: 변경 취소
   - **Reset**: 기본값으로 복원

## 아키텍처

### 새로운 클래스

#### RenderSettings (Singleton)
```csharp
namespace CamViewerPOC.Rendering
{
    public class RenderSettings
    {
        public static RenderSettings Instance { get; }
        
        // Color properties
        public Color WorkpieceBoundaryColor { get; set; }
        public Color PiercingPointColor { get; set; }
        public Color LeadInColor { get; set; }
        public Color CuttingCompletedColor { get; set; }
        public Color CuttingInProgressColor { get; set; }
        public Color CuttingPendingColor { get; set; }
        
        // Size properties
        public float WorkpieceBoundaryWidth { get; set; }
        public float PiercingPointSize { get; set; }
        public float LeadInWidth { get; set; }
        public float CuttingCompletedWidth { get; set; }
        public float CuttingInProgressWidth { get; set; }
        public float CuttingPendingWidth { get; set; }
        
        // Visibility
        public bool ShowPartOrigin { get; set; }
        
        public void ResetToDefaults();
        public float[] GetColorAsFloat(Color color);
    }
}
```

#### RenderSettingsForm
```csharp
public partial class RenderSettingsForm : Form
{
    // Color buttons with ColorPicker
    // NumericUpDown for sizes
    // CheckBox for visibility
    // OK/Cancel/Reset buttons
}
```

### 수정된 클래스

#### CamViewerControl
- RenderSettings 적용
- DrawWorkpieceBoundary() - 설정 색상/크기 사용
- DrawPart() - 파트 원점 표시 옵션 처리
- DrawContour() - 설정 색상/크기 사용
- RedrawSimulation() - 시뮬레이션 색상 설정 적용
- 마우스 이벤트: 좌클릭 팬 기능 추가

#### MainForm
- btnRenderSettings 버튼 추가 (보라색, Y=683)
- BtnRenderSettings_Click() 이벤트 핸들러
- 시뮬레이션 로그 크기 조정 (440px → 390px)

## 빌드 및 실행

### 필수 조건
- Visual Studio 2019 이상
- .NET Framework 4.7.2
- CMake (NativeRenderer 빌드용)

### 빌드 명령
```powershell
cd D:\Genspark\Phase4_Graphics

# NativeRenderer 빌드
cd NativeRenderer
cmake -B build -G "Visual Studio 16 2019" -A x64
cmake --build build --config Release
cd ..

# WinFormsApp 빌드
dotnet build WinFormsApp/WinFormsApp.csproj -c Release

# 실행
.\WinFormsApp\bin\x64\Release\CamViewerPOC.exe
```

## 검증 체크리스트

### Phase 1~3 기능 (모두 유지)
- [x] MPF 파싱 및 렌더링
- [x] 시뮬레이션 기능
- [x] 파일 탐색기

### Phase 4 신규 기능
- [ ] **좌클릭 드래그로 캔버스 이동**
- [ ] **파트 원점 기본 숨김** (렌더링 설정에서 토글 가능)
- [ ] **렌더링 설정 대화상자 열기**
  - [ ] 색상 버튼 클릭 시 ColorPicker 표시
  - [ ] 색상 변경 후 OK 시 즉시 반영
  - [ ] 크기 조정 (NumericUpDown)
  - [ ] Reset 버튼으로 기본값 복원
- [ ] **사용자 정의 색상으로 렌더링**
  - [ ] 워크피스 경계
  - [ ] 피어싱 포인트
  - [ ] 리드인 경로
  - [ ] 절단 경로 (완료/진행/대기)
- [ ] **사용자 정의 크기로 렌더링**
  - [ ] 선 두께 변경 확인
  - [ ] 점 크기 변경 확인

## 변경 사항 요약

### 새로운 파일
```
Phase4_Graphics/
└── WinFormsApp/
    └── Rendering/
        ├── RenderSettings.cs         (싱글톤, 설정 저장)
        └── RenderSettingsForm.cs     (설정 UI)
```

### 수정된 파일
```
Phase4_Graphics/
└── WinFormsApp/
    ├── CamViewerControl.cs    (RenderSettings 적용, 좌클릭 팬)
    └── MainForm.cs             (설정 버튼 추가)
```

## 기술 세부사항

### Singleton Pattern
RenderSettings는 싱글톤으로 구현되어 전역 설정 공유:
```csharp
RenderSettings settings = RenderSettings.Instance;
float[] color = settings.GetColorAsFloat(settings.PiercingPointColor);
```

### Color to Float Conversion
OpenGL은 0.0~1.0 범위를 사용하므로 변환 필요:
```csharp
public float[] GetColorAsFloat(Color color)
{
    return new float[] { 
        color.R / 255.0f, 
        color.G / 255.0f, 
        color.B / 255.0f 
    };
}
```

### Mouse Panning Enhancement
좌클릭도 팬 기능 활성화:
```csharp
if (e.Button == MouseButtons.Left || 
    e.Button == MouseButtons.Middle || 
    e.Button == MouseButtons.Right)
{
    isPanning = true;
}
```

## 이전 Phase와의 차이점

| 기능 | Phase 3 | Phase 4 |
|------|---------|---------|
| 마우스 팬 | 우클릭/중클릭만 | 좌클릭 추가 |
| 파트 원점 | 항상 표시 | 기본 숨김, 옵션 |
| 색상 | 하드코딩 | 사용자 정의 |
| 크기 | 하드코딩 | 사용자 정의 |
| 설정 UI | 없음 | 전용 대화상자 |

## 향후 개선 가능 항목
- [ ] 설정 저장/로드 (JSON 파일)
- [ ] 프리셋 시스템 (Dark/Light 테마 등)
- [ ] 실시간 미리보기 (설정 변경 시)
- [ ] 단축키 지원 (Ctrl+R로 설정 열기)
- [ ] 색상 히스토리
