# Phase 8.2 실시간 트레이스 구현 설계

## 🎯 목표

레이저 절단기의 **실시간 절단 진행 상황**을 시각화하여 작업자가 현재 절단 위치와 진행률을 직관적으로 파악할 수 있도록 합니다.

---

## 📋 핵심 기능

### 1. 실시간 경로 추적
- ✅ 현재 절단 중인 Part/Contour 표시
- ✅ 완료된 경로는 빨간색으로 표시
- ✅ 현재 진행 중인 세그먼트 하이라이트

### 2. 레이저 헤드 위치 표시
- ✅ 현재 레이저 헤드 위치 마커
- ✅ 실시간 좌표 업데이트 (X, Y WCS)
- ✅ 줌 레벨에 따른 마커 크기 조정

### 3. 진행 상황 UI
- ✅ Part/Contour 번호 표시
- ✅ 진행률 (%) 표시
- ✅ 현재 실행 중인 G-code 블록 표시

---

## 🏗️ 아키텍처 설계

### 데이터 흐름

```
외부 시스템 (Siemens PLC/OPC UA)
    │
    ↓
CommunicationManager (Phase 8.3/8.4)
    │
    ↓
TraceManager.UpdateProgress(part, contour, progress, x, y)
    │
    ↓
CamViewerControl.UpdateTrace(...)
    │
    ↓
NativeRenderer.UpdateCuttingProgress(...)
    │
    ↓
OpenGL Rendering (빨간색 경로 + 레이저 헤드)
```

---

## 📦 데이터 구조

### 1. CuttingProgressData (C# 클래스)

```csharp
namespace CamViewerPOC.Trace
{
    public class CuttingProgressData
    {
        // 현재 절단 중인 Part/Contour
        public int CurrentPart { get; set; }        // 1-based
        public int CurrentContour { get; set; }     // 1-based
        
        // 진행률 (0.0 ~ 1.0)
        public double Progress { get; set; }
        
        // 레이저 헤드 위치 (WCS)
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        
        // 현재 실행 중인 G-code 블록
        public string CurrentBlock { get; set; }
        
        // 시작 Part/Contour (추적 시작점)
        public int StartPart { get; set; }
        public int StartContour { get; set; }
        
        // 절단 방향
        public bool IsReverse { get; set; }
        
        // 상태
        public bool IsActive { get; set; }
    }
}
```

### 2. Native DLL 인터페이스

```cpp
// renderer.h
extern "C" {
    // 실시간 트레이스 시작
    RENDERER_API void StartCuttingTrace(
        int startPart,      // 1-based
        int startContour,   // 1-based
        bool isReverse
    );
    
    // 실시간 트레이스 업데이트
    RENDERER_API void UpdateCuttingTrace(
        int currentPart,    // 1-based
        int currentContour, // 1-based
        double progress,    // 0.0 ~ 1.0
        float posX,         // WCS X
        float posY          // WCS Y
    );
    
    // 실시간 트레이스 중지
    RENDERER_API void StopCuttingTrace();
    
    // 완료된 경로 렌더링
    RENDERER_API void DrawCompletedPath(
        int part,
        int contour,
        float r, float g, float b  // 색상
    );
    
    // 레이저 헤드 마커 렌더링
    RENDERER_API void DrawLaserHead(
        float posX,
        float posY,
        float scale  // 줌 레벨에 따른 크기
    );
}
```

---

## 🎨 렌더링 전략

### 1. 완료된 경로 (빨간색)

**HKCamInterface 참조:**
```cpp
// OpenGLCAMViewWnd.cpp: DrawPreviousCutElements()
// 이미 절단 완료된 Part/Contour를 빨간색으로 그림
void DrawCompletedContours(int startPart, int startContour, 
                            int currentPart, int currentContour)
{
    // startPart부터 currentPart까지 순회
    for (int p = startPart; p <= currentPart; p++)
    {
        for (int c = startContour; c <= currentContour; c++)
        {
            // 완료된 contour는 빨간색으로 렌더링
            DrawContour(p, c, RED_COLOR);
        }
    }
}
```

**구현 방법:**
- 기존 `DrawLine`, `DrawArc` 함수 활용
- 색상을 빨간색 (1.0f, 0.0f, 0.0f)으로 설정
- Z-order를 높여서 일반 경로 위에 표시

### 2. 현재 진행 중인 세그먼트

**HKCamInterface 참조:**
```cpp
// OpenGLCAMViewWnd.cpp: UpdateProgress()
// progress 값(0.0~1.0)에 따라 현재 세그먼트의 일부만 그림
void DrawPartialSegment(Segment seg, double progress)
{
    if (seg.type == LINE)
    {
        float endX = seg.startX + (seg.endX - seg.startX) * progress;
        float endY = seg.startY + (seg.endY - seg.startY) * progress;
        DrawLine(seg.startX, seg.startY, endX, endY, RED_COLOR);
    }
    else if (seg.type == ARC)
    {
        float sweepAngle = seg.sweepAngle * progress;
        DrawArc(seg.centerX, seg.centerY, seg.radius, 
                seg.startAngle, sweepAngle, RED_COLOR);
    }
}
```

**구현 방법:**
- Progress 값에 따라 세그먼트의 끝점 계산
- Line: 선형 보간 (lerp)
- Arc: 각도 보간

### 3. 레이저 헤드 마커

**HKCamInterface 참조:**
```cpp
// OpenGLCAMViewWnd.cpp: UpdateProgressView()
// 현재 위치에 십자선 마커 표시
void DrawLaserHeadMarker(float x, float y, float size)
{
    // 십자선 (+) 그리기
    glColor3f(1.0f, 0.0f, 0.0f);  // 빨간색
    glLineWidth(2.0f);
    
    // 가로선
    glBegin(GL_LINES);
    glVertex2f(x - size, y);
    glVertex2f(x + size, y);
    glEnd();
    
    // 세로선
    glBegin(GL_LINES);
    glVertex2f(x, y - size);
    glVertex2f(x, y + size);
    glEnd();
    
    // 중심 원
    DrawCircle(x, y, size * 0.2f, RED_COLOR);
}
```

**구현 방법:**
- 십자선 + 원형 마커
- 줌 레벨에 따라 크기 조정: `size = baseSize / zoomFactor`
- 항상 화면에 보이도록 Z-order 최상위

---

## 🔧 구현 단계

### Step 1: Native 렌더러 확장

**파일:** `NativeRenderer/renderer.h`, `renderer.cpp`

**추가 함수:**
```cpp
// 전역 변수
struct TraceState {
    bool isActive;
    int startPart;
    int startContour;
    int currentPart;
    int currentContour;
    double progress;
    float posX;
    float posY;
    bool isReverse;
} g_traceState;

// 함수 구현
void StartCuttingTrace(int startPart, int startContour, bool isReverse)
{
    g_traceState.isActive = true;
    g_traceState.startPart = startPart;
    g_traceState.startContour = startContour;
    g_traceState.currentPart = startPart;
    g_traceState.currentContour = startContour;
    g_traceState.progress = 0.0;
    g_traceState.isReverse = isReverse;
}

void UpdateCuttingTrace(int currentPart, int currentContour, 
                         double progress, float posX, float posY)
{
    if (!g_traceState.isActive) return;
    
    g_traceState.currentPart = currentPart;
    g_traceState.currentContour = currentContour;
    g_traceState.progress = progress;
    g_traceState.posX = posX;
    g_traceState.posY = posY;
}

void StopCuttingTrace()
{
    g_traceState.isActive = false;
}
```

### Step 2: C# TraceManager 클래스

**파일:** `WinFormsApp/Trace/TraceManager.cs` (신규)

```csharp
using CamViewerPOC.MPF;

namespace CamViewerPOC.Trace
{
    public class TraceManager
    {
        private CuttingProgressData _currentProgress;
        private MPFProgram _mpfProgram;
        private bool _isActive;
        
        public TraceManager(MPFProgram mpfProgram)
        {
            _mpfProgram = mpfProgram;
            _currentProgress = new CuttingProgressData();
            _isActive = false;
        }
        
        public void StartTrace(int startPart, int startContour, bool isReverse = false)
        {
            _currentProgress.StartPart = startPart;
            _currentProgress.StartContour = startContour;
            _currentProgress.CurrentPart = startPart;
            _currentProgress.CurrentContour = startContour;
            _currentProgress.Progress = 0.0;
            _currentProgress.IsReverse = isReverse;
            _currentProgress.IsActive = true;
            _isActive = true;
            
            // Native 렌더러 호출
            NativeRenderer.StartCuttingTrace(startPart, startContour, isReverse);
        }
        
        public void UpdateProgress(int part, int contour, double progress, 
                                    double posX, double posY, string currentBlock = "")
        {
            if (!_isActive) return;
            
            _currentProgress.CurrentPart = part;
            _currentProgress.CurrentContour = contour;
            _currentProgress.Progress = Math.Max(0.0, Math.Min(1.0, progress));
            _currentProgress.PositionX = posX;
            _currentProgress.PositionY = posY;
            _currentProgress.CurrentBlock = currentBlock;
            
            // Native 렌더러 업데이트
            NativeRenderer.UpdateCuttingTrace(part, contour, progress, 
                                               (float)posX, (float)posY);
        }
        
        public void StopTrace()
        {
            _isActive = false;
            _currentProgress.IsActive = false;
            
            NativeRenderer.StopCuttingTrace();
        }
        
        public CuttingProgressData GetCurrentProgress()
        {
            return _currentProgress;
        }
        
        public bool IsActive => _isActive;
    }
}
```

### Step 3: CamViewerControl 통합

**파일:** `WinFormsApp/CamViewerControl.cs`

```csharp
// 멤버 변수 추가
private TraceManager _traceManager;

// InitializeComponent() 내부
_traceManager = new TraceManager(MPFProgram);

// 공개 메서드
public void StartCuttingTrace(int startPart, int startContour, bool isReverse = false)
{
    _traceManager?.StartTrace(startPart, startContour, isReverse);
    Invalidate();  // 화면 갱신
}

public void UpdateCuttingProgress(int part, int contour, double progress, 
                                    double posX, double posY, string currentBlock = "")
{
    _traceManager?.UpdateProgress(part, contour, progress, posX, posY, currentBlock);
    Invalidate();  // 화면 갱신
}

public void StopCuttingTrace()
{
    _traceManager?.StopTrace();
    Invalidate();  // 화면 갱신
}
```

### Step 4: 진행 상황 UI 패널

**파일:** `WinFormsApp/Trace/TraceStatusPanel.cs` (신규)

```csharp
public class TraceStatusPanel : Panel
{
    private Label _lblPartContour;
    private Label _lblProgress;
    private Label _lblPosition;
    private Label _lblCurrentBlock;
    private ProgressBar _progressBar;
    
    public TraceStatusPanel()
    {
        InitializeComponents();
    }
    
    private void InitializeComponents()
    {
        // Part/Contour 라벨
        _lblPartContour = new Label
        {
            Text = "Part: -, Contour: -",
            Location = new Point(10, 10),
            AutoSize = true
        };
        
        // 진행률 라벨
        _lblProgress = new Label
        {
            Text = "Progress: 0%",
            Location = new Point(10, 35),
            AutoSize = true
        };
        
        // 진행률 바
        _progressBar = new ProgressBar
        {
            Location = new Point(10, 55),
            Size = new Size(250, 20),
            Minimum = 0,
            Maximum = 100
        };
        
        // 위치 라벨
        _lblPosition = new Label
        {
            Text = "Position: X: 0.000, Y: 0.000",
            Location = new Point(10, 80),
            AutoSize = true
        };
        
        // 현재 블록 라벨
        _lblCurrentBlock = new Label
        {
            Text = "Block: -",
            Location = new Point(10, 105),
            AutoSize = true,
            MaximumSize = new Size(250, 0),
            AutoEllipsis = true
        };
        
        Controls.Add(_lblPartContour);
        Controls.Add(_lblProgress);
        Controls.Add(_progressBar);
        Controls.Add(_lblPosition);
        Controls.Add(_lblCurrentBlock);
        
        Size = new Size(280, 140);
        BorderStyle = BorderStyle.FixedSingle;
    }
    
    public void UpdateStatus(CuttingProgressData progress)
    {
        if (progress == null || !progress.IsActive)
        {
            _lblPartContour.Text = "Part: -, Contour: -";
            _lblProgress.Text = "Progress: 0%";
            _progressBar.Value = 0;
            _lblPosition.Text = "Position: X: 0.000, Y: 0.000";
            _lblCurrentBlock.Text = "Block: -";
            return;
        }
        
        _lblPartContour.Text = $"Part: {progress.CurrentPart}, Contour: {progress.CurrentContour}";
        _lblProgress.Text = $"Progress: {progress.Progress * 100:F1}%";
        _progressBar.Value = (int)(progress.Progress * 100);
        _lblPosition.Text = $"Position: X: {progress.PositionX:F3}, Y: {progress.PositionY:F3}";
        _lblCurrentBlock.Text = $"Block: {progress.CurrentBlock}";
    }
}
```

---

## 🎨 색상 및 스타일

### 색상 정의

```csharp
public static class TraceColors
{
    // 완료된 경로
    public static Color CompletedPath = Color.FromArgb(255, 0, 0);  // 빨간색
    
    // 레이저 헤드 마커
    public static Color LaserHead = Color.FromArgb(255, 100, 0);    // 주황색
    
    // 현재 진행 중 세그먼트 (더 밝은 빨강)
    public static Color CurrentSegment = Color.FromArgb(255, 80, 80);
}
```

### 렌더링 순서 (Z-order)

1. **배경 (Workpiece)** - 가장 아래
2. **일반 경로 (회색)** - MPF 파일 경로
3. **완료된 경로 (빨간색)** - 이미 절단 완료
4. **레이저 헤드 마커 (주황색)** - 가장 위

---

## 🧪 테스트 시나리오

### 1. 시뮬레이션 모드 (Phase 8.3 이전)

```csharp
// 테스트용 시뮬레이션 코드
public void SimulateTrace()
{
    // Part 1, Contour 1 시작
    StartCuttingTrace(1, 1, false);
    
    // 진행률 시뮬레이션
    for (double progress = 0.0; progress <= 1.0; progress += 0.01)
    {
        // 예상 위치 계산 (실제로는 경로 데이터에서 추출)
        double x = /* 계산 */;
        double y = /* 계산 */;
        
        UpdateCuttingProgress(1, 1, progress, x, y, "G01 X... Y...");
        Thread.Sleep(100);  // 100ms마다 업데이트
    }
    
    StopCuttingTrace();
}
```

### 2. 실제 장비 연동 (Phase 8.3/8.4)

```csharp
// Itag 또는 OPC UA에서 데이터 수신
private void OnCuttingDataReceived(CuttingData data)
{
    UpdateCuttingProgress(
        data.PartNumber,
        data.ContourNumber,
        data.Progress,
        data.PositionX,
        data.PositionY,
        data.CurrentBlock
    );
}
```

---

## 📊 성능 고려사항

### 1. 렌더링 빈도
- **목표:** 60 FPS 유지
- **업데이트 간격:** 최소 16ms (1/60초)
- **전략:** Progress 데이터만 업데이트, 전체 재렌더링은 변경 시에만

### 2. 메모리 사용
- **완료된 경로:** 기존 MPF 데이터 재사용 (복사 없음)
- **TraceState:** 고정 크기 구조체 (~100 bytes)

### 3. OpenGL 최적화
- Display List 사용 (완료된 경로 캐싱)
- Vertex Buffer Object (VBO) 고려 (많은 경로 시)

---

## 📝 HKCamInterface 참조 매핑

| HKCamInterface | Phase 8.2 구현 | 설명 |
|----------------|----------------|------|
| `CVStartCutting()` | `StartCuttingTrace()` | 추적 시작 |
| `CVUpdateCutting()` | `UpdateCuttingTrace()` | 진행 상황 업데이트 |
| `CVStopCutting()` | `StopCuttingTrace()` | 추적 중지 |
| `DrawPreviousCutElements()` | `DrawCompletedPath()` | 완료된 경로 그리기 |
| `UpdateProgressView()` | `DrawLaserHead()` | 레이저 헤드 마커 |
| `m_iStartPart/Contour` | `startPart/Contour` | 시작 위치 |
| `m_iCurrPart/Contour` | `currentPart/Contour` | 현재 위치 |
| `m_cutDistance` | `progress` | 진행률 |

---

## 🚀 구현 우선순위

### Phase 8.2.1 (1주) - 핵심 기능
1. ✅ Native 렌더러 트레이스 함수 추가
2. ✅ TraceManager 클래스 구현
3. ✅ CamViewerControl 통합
4. ✅ 완료된 경로 렌더링
5. ✅ 레이저 헤드 마커

### Phase 8.2.2 (1주) - UI 및 테스트
6. ✅ TraceStatusPanel 구현
7. ✅ 시뮬레이션 모드 테스트
8. ✅ 문서화 및 사용 가이드

---

## 🎯 완료 조건

- [ ] Native 렌더러에 트레이스 함수 구현
- [ ] TraceManager 클래스 작동
- [ ] 완료된 경로가 빨간색으로 표시
- [ ] 레이저 헤드 위치 마커 표시
- [ ] 진행 상황 UI 패널 동작
- [ ] 시뮬레이션 모드 테스트 성공
- [ ] 60 FPS 이상 유지

---

**설계 완료일:** 2025-11-27  
**예상 구현 기간:** 2주  
**다음 단계:** Phase 8.3 - Itag 통신 구현

---

**참조 문서:**
- `HKCamInterface_Reference/OpenGLCAMViewWnd.cpp`
- `HKCamInterface_Reference/HKCAMInterfaceDLL.cpp`
- `PHASE8_ROADMAP.md`
