# Phase 14: 성능 최적화, 기능 추가, 안정성 향상 제안서

## 📋 전체 프로젝트 분석

### 프로젝트 파일 구조 (27개 파일)
```
RealtimeITagControl/
├── CamViewerCore.cs                    # 핵심 렌더링 및 트레이스 로직
├── RealtimeITagControl.cs              # ITag 통신 및 UI 통합
├── IITagManager.cs                     # ITag 인터페이스
├── ITagManager.cs                      # ITag 구현체
├── LogHelper.cs                        # 로깅 유틸리티
├── TagDefinitions.cs                   # Tag 정의
├── MPF/                                # MPF 파일 파싱
│   ├── Commands.cs
│   ├── Contour.cs
│   ├── MPFParser.cs
│   ├── MPFProgram.cs
│   ├── Part.cs
│   ├── PathSegment.cs
│   └── Point2D.cs
├── Rendering/                          # 렌더링 관련
│   ├── LabelPositionCalculator.cs
│   ├── NativeTextRenderer.cs
│   └── RenderSettings.cs
├── Selection/                          # 선택 기능
│   ├── GeometryUtils.cs
│   ├── NumberPositionManager.cs
│   └── SelectionManager.cs
├── Simulation/                         # 시뮬레이션
│   └── SimulationEngine.cs
├── Trace/                              # 트레이스 기능
│   ├── CuttingProgressData.cs
│   ├── CuttingProgressManager.cs       # ✅ 사용 중 (Phase 13 핵심)
│   ├── TraceManager.cs                 # ⚠️ 검토 필요
│   ├── TraceStatusPanel.cs             # ⚠️ 검토 필요
│   └── TraceTestForm.cs                # ✅ 사용 중 (디버깅용)
└── UI/                                 # UI 컴포넌트
    ├── ContourColorLegendForm.cs
    └── ProgramInfoPanel.cs
```

---

## 🎯 1. 성능 최적화 (Performance Optimization)

### 1.1 ⚠️ 사용되지 않는 코드 제거

#### 📁 Trace 폴더 분석 결과

| 파일 | 사용 여부 | 상태 | 비고 |
|------|----------|------|------|
| **CuttingProgressManager.cs** | ✅ 사용 중 | 유지 | Phase 13 핵심, 엘리먼트 단위 진행 추적 |
| **CuttingProgressData.cs** | ✅ 사용 중 | 유지 | CuttingProgressManager 데이터 모델 |
| **TraceManager.cs** | ⚠️ 부분 사용 | 검토 | NativeRenderer 직접 호출, 실제 추적 안 함 |
| **TraceStatusPanel.cs** | ❌ 미사용 | 제거 대상 | UI 컴포넌트, 실제 사용 안 됨 |
| **TraceTestForm.cs** | ✅ 사용 중 | 유지 | 디버깅/테스트용 폼 |

#### 🔍 상세 분석

##### 1. TraceManager.cs
```csharp
// 현재 상태: CamViewerCore에 인스턴스만 생성, 실제 메서드 호출 없음
private TraceManager traceManager = null;

// 문제점:
// - StartTrace(), UpdateProgress(), StopTrace() 호출되지 않음
// - NativeRenderer를 직접 호출 (StartCuttingTrace, UpdateCuttingTrace, StopCuttingTrace)
// - CamViewerCore.cs가 이미 NativeRenderer를 직접 사용
// - 중간 레이어 역할만 하고 실제 로직 없음

// 제안:
// ❓ 완전 제거 vs 실제 사용으로 전환
// - Option A: TraceManager 제거, CamViewerCore가 직접 Native 호출
// - Option B: TraceManager를 실제 추적 로직으로 활용 (리팩토링)
```

##### 2. TraceStatusPanel.cs
```csharp
// 현재 상태: 정의만 있고 실제 사용 안 됨
// - RealtimeITagControl.cs에서 인스턴스 생성 없음
// - UI에 표시되지 않음
// - 이벤트 연결 없음

// 제안: ❌ 완전 제거
// - 대체: ProgramInfoPanel이 이미 상태 표시 역할
```

#### 🗑️ 제거 대상 상세

##### Trace/TraceStatusPanel.cs - 8,649 bytes (미사용 UI)
```csharp
// 이 파일은 완전히 미사용
// - CamViewerCore.cs: 인스턴스 없음
// - RealtimeITagControl.cs: 인스턴스 없음
// - UI에 표시되지 않음

제거 효과:
- 코드: -200줄 이상
- 메모리: UI 컴포넌트 로드 불필요
- 유지보수: 혼란 감소
```

##### Trace/TraceManager.cs - 부분 제거 또는 리팩토링
```csharp
// 옵션 1: 완전 제거 (추천)
// - NativeRenderer 호출 부분을 CamViewerCore로 이동
// - 중간 레이어 제거로 호출 경로 단순화
제거 효과:
- 코드: -221줄
- 성능: 함수 호출 오버헤드 감소
- 명확성: 호출 경로 단순화

// 옵션 2: 리팩토링 (개선)
// - 실제 추적 로직 구현
// - DrawLaserHeadMarker() 활용
// - 히스토리 추적 기능 추가
```

---

### 1.2 📊 렌더링 성능 최적화

#### 현재 렌더링 흐름
```
UpdateViewer() → Invalidate() → OnPaint()
    ↓
DrawMPFProgram()
    ↓
모든 Part/Contour/Element 순회 (매번 전체 그리기)
    ↓
NativeRenderer.DrawLine/DrawArc 수백~수천 번 호출
```

#### 제안: 렌더링 캐싱 전략

##### A. Dirty Region 최적화
```csharp
// 현재: 항상 전체 화면 다시 그리기
renderPanel.Invalidate();

// 개선: 변경된 영역만 다시 그리기
// 1. 진행 중인 엘리먼트의 Bounding Box만 Invalidate
Rectangle dirtyRect = GetElementBoundingBox(partIdx, contourIdx, elementIdx);
renderPanel.Invalidate(dirtyRect);

// 2. 진행 완료된 엘리먼트는 다시 그리지 않기 (캐싱)
Dictionary<(int, int, int), bool> renderedElements;

성능 향상:
- 렌더링 호출: 100% → 1~5% (진행 중 엘리먼트만)
- 프레임률: ~30 FPS → ~60 FPS 이상
```

##### B. Display List 패턴
```csharp
// OpenGL Display List 또는 Vertex Buffer Object (VBO) 활용
// 1. MPF 로드 시 전체 경로를 GPU에 업로드 (1회)
// 2. 렌더링 시 색상만 변경 (매우 빠름)

구현 예시:
public void LoadMPFToGPU(MPFProgram program)
{
    // Phase 14: GPU 버퍼에 전체 경로 업로드
    int displayListId = NativeRenderer.CreateDisplayList(program);
    // 이후 렌더링 시 색상만 변경
}

public void Render()
{
    // 기본 색상으로 전체 그리기 (GPU에서 처리, 매우 빠름)
    NativeRenderer.DrawDisplayList(displayListId);
    
    // 진행 중/완료 엘리먼트만 색상 덮어쓰기
    foreach (var completed in completedElements)
    {
        NativeRenderer.DrawElement(completed, CuttingCompletedColor);
    }
}

성능 향상:
- CPU 사용량: 80% 감소
- 렌더링 속도: 10배 이상 향상
```

##### C. 멀티스레딩 렌더링
```csharp
// Phase 14: Background worker에서 렌더링 준비
// UI 스레드는 최종 화면만 표시

private BackgroundWorker renderWorker;

public void StartBackgroundRendering()
{
    renderWorker = new BackgroundWorker();
    renderWorker.DoWork += (s, e) =>
    {
        // 백그라운드에서 렌더링 데이터 준비
        var renderData = PrepareRenderData();
        e.Result = renderData;
    };
    renderWorker.RunWorkerCompleted += (s, e) =>
    {
        // UI 스레드에서 최종 표시
        DisplayRenderData((RenderData)e.Result);
    };
    renderWorker.RunWorkerAsync();
}

성능 향상:
- UI 응답성: 즉각 반응
- 렌더링: 백그라운드 처리
```

---

### 1.3 🧠 메모리 사용량 최적화

#### A. MPF 데이터 구조 최적화
```csharp
// 현재: 모든 PathSegment를 메모리에 유지
public class Contour
{
    public List<PathSegment> PathSegments;  // 수천 개
}

// 개선 1: Lazy Loading
public class Contour
{
    private List<PathSegment> _cachedSegments;
    private bool _isLoaded;
    
    public List<PathSegment> PathSegments
    {
        get
        {
            if (!_isLoaded)
            {
                LoadPathSegments();  // 필요할 때만 로드
                _isLoaded = true;
            }
            return _cachedSegments;
        }
    }
}

// 개선 2: Struct 사용 (값 타입)
// 작은 객체는 class → struct로 변경
public struct Point2D  // 이미 struct ✅
{
    public double X;
    public double Y;
}

// 개선 3: Object Pooling
// 자주 생성/삭제되는 객체는 풀에서 재사용
private ObjectPool<PathSegment> segmentPool;

메모리 절감:
- Lazy Loading: 50% 감소 (사용하지 않는 데이터)
- Struct 사용: 20% 감소 (작은 객체)
- Object Pooling: GC 압력 80% 감소
```

#### B. 이벤트 구독 메모리 누수 방지
```csharp
// 현재: 이벤트 구독 후 해제 안 함
progressManager.ProgressUpdated += CuttingProgressManager_ProgressUpdated;

// 개선: WeakEventManager 사용
WeakEventManager<CuttingProgressManager, CuttingProgressEventArgs>
    .AddHandler(progressManager, "ProgressUpdated", CuttingProgressManager_ProgressUpdated);

// 또는 Dispose 시 명시적 해제 (Phase 13에서 이미 구현됨 ✅)
protected override void Dispose(bool disposing)
{
    if (progressManager != null)
    {
        progressManager.ProgressUpdated -= CuttingProgressManager_ProgressUpdated;
    }
}

메모리 누수 방지:
- 이벤트 구독: 자동 해제
- 순환 참조: 방지
```

---

## 🎨 2. 기능 추가 (Feature Enhancement)

### 2.1 🎥 새로운 시각화 기능

#### A. 레이저 헤드 실시간 위치 표시
```csharp
// Phase 14: 실시간 레이저 헤드 위치 시각화
// TraceManager.DrawLaserHeadMarker() 활용

public void UpdateLaserHead(double x, double y)
{
    // 1. 십자선 마커
    NativeRenderer.DrawCrosshair(x, y, size: 5.0f, color: Orange);
    
    // 2. 궤적 추적 (Trail)
    laserTrail.Add(new Point2D(x, y));
    if (laserTrail.Count > 100)  // 최근 100개만 유지
    {
        laserTrail.RemoveAt(0);
    }
    DrawTrail(laserTrail, fadeColor: Orange);
    
    // 3. 속도 벡터 표시
    if (laserTrail.Count >= 2)
    {
        var velocity = CalculateVelocity(laserTrail);
        DrawVelocityVector(x, y, velocity, color: Yellow);
    }
}

시각화 효과:
- 레이저 위치: 십자선으로 명확히 표시
- 궤적: 잔상 효과로 이동 경로 확인
- 속도: 화살표로 이동 방향 표시
```

#### B. 진행 상태 히트맵 (Progress Heatmap)
```csharp
// Phase 14: 엘리먼트별 진행 속도 시각화
// 빠른 부분: 초록색, 느린 부분: 빨간색

public void DrawProgressHeatmap()
{
    foreach (var element in elements)
    {
        double speed = element.Length / element.CuttingTime;
        Color heatColor = GetHeatmapColor(speed);  // 속도 → 색상
        DrawElement(element, heatColor);
    }
}

Color GetHeatmapColor(double speed)
{
    // 속도 범위: 0 ~ maxSpeed
    // 색상: 빨강 (느림) → 노랑 → 초록 (빠름)
    double ratio = speed / maxSpeed;
    return Color.FromHsv(ratio * 120, 1.0, 1.0);  // HSV 색상
}

활용:
- 절단 속도 분석
- 병목 구간 식별
- 최적화 포인트 발견
```

#### C. 3D 뷰 (높이 표시)
```csharp
// Phase 14: Z축 정보 활용 (있을 경우)
// 높이 차이를 색상으로 표시

public void Draw3DView()
{
    foreach (var segment in segments)
    {
        double z = segment.ZHeight;
        Color heightColor = GetHeightColor(z);
        DrawSegment(segment, heightColor);
    }
}

// 또는 간단한 Isometric View
public void DrawIsometricView()
{
    // X, Y 좌표를 45도 회전하여 3D 효과
    float isoX = (x - y) * cos(45);
    float isoY = (x + y) * sin(45) - z * heightScale;
    DrawLine(isoX, isoY, ...);
}

시각화 효과:
- 높이 차이: 색상으로 표시
- 3D 효과: Isometric 뷰
- 깊이 인식: 더 나은 공간 이해
```

#### D. 애니메이션 효과
```csharp
// Phase 14: 부드러운 전환 애니메이션

public void AnimateProgress(double from, double to, int durationMs)
{
    var timer = new System.Windows.Forms.Timer();
    timer.Interval = 16;  // 60 FPS
    double elapsed = 0;
    
    timer.Tick += (s, e) =>
    {
        elapsed += 16;
        double progress = elapsed / durationMs;
        if (progress >= 1.0)
        {
            progress = 1.0;
            timer.Stop();
        }
        
        // Easing 함수로 부드러운 전환
        double currentValue = EaseInOutCubic(from, to, progress);
        UpdateDisplay(currentValue);
    };
    
    timer.Start();
}

double EaseInOutCubic(double from, double to, double t)
{
    double range = to - from;
    return t < 0.5
        ? from + range * 4 * t * t * t
        : from + range * (1 - Math.Pow(-2 * t + 2, 3) / 2);
}

효과:
- 진행률: 부드러운 증가
- 색상: 그라데이션 전환
- 확대/축소: 부드러운 줌
```

---

### 2.2 🖱️ UI/UX 개선

#### A. 컨텍스트 메뉴 (우클릭)
```csharp
// Phase 14: 엘리먼트/컨투어에 우클릭 메뉴 추가

private ContextMenuStrip elementContextMenu;

private void InitializeContextMenu()
{
    elementContextMenu = new ContextMenuStrip();
    elementContextMenu.Items.Add("여기서부터 트레이스 시작", null, StartTraceFromHere);
    elementContextMenu.Items.Add("엘리먼트 정보 보기", null, ShowElementInfo);
    elementContextMenu.Items.Add("이 컨투어 숨기기", null, HideContour);
    elementContextMenu.Items.Add(new ToolStripSeparator());
    elementContextMenu.Items.Add("줌 맞추기", null, ZoomToElement);
}

private void renderPanel_MouseClick(object sender, MouseEventArgs e)
{
    if (e.Button == MouseButtons.Right)
    {
        var element = GetElementAtPosition(e.Location);
        if (element != null)
        {
            selectedElement = element;
            elementContextMenu.Show(renderPanel, e.Location);
        }
    }
}

기능:
- 빠른 트레이스 시작
- 정보 확인
- 선택적 표시/숨기기
- 자동 줌
```

#### B. 키보드 단축키
```csharp
// Phase 14: 키보드 단축키 추가

protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
{
    switch (keyData)
    {
        case Keys.Space:
            // 시뮬레이션 일시정지/재개
            ToggleSimulation();
            return true;
            
        case Keys.R:
            // 뷰 리셋
            ResetView();
            return true;
            
        case Keys.F:
            // 자동 맞춤
            AutoFitView();
            return true;
            
        case Keys.Control | Keys.Z:
            // 실행 취소
            Undo();
            return true;
            
        case Keys.Add:
        case Keys.Oemplus:
            // 확대
            ZoomIn();
            return true;
            
        case Keys.Subtract:
        case Keys.OemMinus:
            // 축소
            ZoomOut();
            return true;
    }
    
    return base.ProcessCmdKey(ref msg, keyData);
}

단축키:
- Space: 일시정지/재개
- R: 뷰 리셋
- F: 자동 맞춤
- Ctrl+Z: 실행 취소
- +/-: 확대/축소
```

#### C. 툴팁 (Hover 정보)
```csharp
// Phase 14: 마우스 오버 시 정보 표시

private ToolTip elementTooltip = new ToolTip();

private void renderPanel_MouseMove(object sender, MouseEventArgs e)
{
    var element = GetElementAtPosition(e.Location);
    if (element != null)
    {
        string info = $"Part {element.PartIndex}, Contour {element.ContourIndex}\n" +
                     $"Element {element.ElementIndex}\n" +
                     $"Length: {element.Length:F2} mm\n" +
                     $"Type: {element.Type}\n" +
                     $"Progress: {element.Progress:P0}";
        
        elementTooltip.Show(info, renderPanel, e.X + 15, e.Y + 15, 3000);
    }
    else
    {
        elementTooltip.Hide(renderPanel);
    }
}

정보:
- Part/Contour/Element 번호
- 길이
- 타입 (직선/원호)
- 진행률
```

---

### 2.3 📊 데이터 분석 기능

#### A. 진행 상태 통계
```csharp
// Phase 14: 실시간 통계 정보

public class ProgressStatistics
{
    public int TotalElements { get; set; }
    public int CompletedElements { get; set; }
    public int InProgressElements { get; set; }
    public int PendingElements { get; set; }
    
    public double TotalLength { get; set; }
    public double CompletedLength { get; set; }
    public double RemainingLength { get; set; }
    
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
    
    public double AverageSpeed { get; set; }  // mm/s
    public double CurrentSpeed { get; set; }
    
    public double CompletionPercentage => 
        TotalLength > 0 ? (CompletedLength / TotalLength) * 100 : 0;
}

public ProgressStatistics CalculateStatistics()
{
    var stats = new ProgressStatistics();
    
    foreach (var part in mpfProgram.Parts)
    {
        foreach (var contour in part.Contours)
        {
            foreach (var element in contour.Elements)
            {
                stats.TotalElements++;
                stats.TotalLength += element.Length;
                
                if (progressManager.IsElementCompleted(part.Index, contour.Index, element.Index))
                {
                    stats.CompletedElements++;
                    stats.CompletedLength += element.Length;
                }
                else if (progressManager.IsElementInProgress(part.Index, contour.Index, element.Index))
                {
                    stats.InProgressElements++;
                    double progress = progressManager.GetElementProgress(part.Index, contour.Index, element.Index);
                    stats.CompletedLength += element.Length * progress;
                }
                else
                {
                    stats.PendingElements++;
                }
            }
        }
    }
    
    stats.RemainingLength = stats.TotalLength - stats.CompletedLength;
    stats.ElapsedTime = DateTime.Now - traceStartTime;
    stats.AverageSpeed = stats.CompletedLength / stats.ElapsedTime.TotalSeconds;
    stats.EstimatedTimeRemaining = TimeSpan.FromSeconds(stats.RemainingLength / stats.AverageSpeed);
    
    return stats;
}

표시:
- 진행률: 42.5% (150 / 353 elements)
- 완료: 125.3 mm / 294.7 mm
- 남은 시간: 약 5분 30초
- 현재 속도: 12.3 mm/s
```

#### B. 진행 그래프
```csharp
// Phase 14: 진행률 시간별 그래프

public class ProgressChart
{
    private List<(DateTime Time, double Progress)> history = new List<(DateTime, double)>();
    
    public void AddDataPoint(double progress)
    {
        history.Add((DateTime.Now, progress));
        
        // 최근 10분만 유지
        var cutoff = DateTime.Now.AddMinutes(-10);
        history.RemoveAll(x => x.Time < cutoff);
    }
    
    public void DrawChart(Graphics g, Rectangle bounds)
    {
        if (history.Count < 2) return;
        
        // X축: 시간, Y축: 진행률 (0~100%)
        var points = new List<PointF>();
        
        double minTime = history[0].Time.Ticks;
        double maxTime = history[history.Count - 1].Time.Ticks;
        double timeRange = maxTime - minTime;
        
        foreach (var data in history)
        {
            float x = bounds.X + (float)((data.Time.Ticks - minTime) / timeRange * bounds.Width);
            float y = bounds.Bottom - (float)(data.Progress * bounds.Height);
            points.Add(new PointF(x, y));
        }
        
        // 선 그래프 그리기
        g.DrawLines(Pens.Blue, points.ToArray());
    }
}

활용:
- 진행 속도 추이 확인
- 정체 구간 파악
- 예상 완료 시간 예측
```

---

## 🛡️ 3. 안정성 향상 (Stability Enhancement)

### 3.1 🔍 에러 핸들링 강화

#### A. 구조화된 예외 처리
```csharp
// 현재: 빈 catch 블록 (Phase 13에서 일부 개선됨)
catch (Exception ex)
{
    // 빈 블록 또는 LogHelper만
}

// Phase 14: 구조화된 예외 처리
public class CamViewerException : Exception
{
    public ErrorCode Code { get; }
    public string Context { get; }
    
    public CamViewerException(ErrorCode code, string message, string context)
        : base(message)
    {
        Code = code;
        Context = context;
    }
}

public enum ErrorCode
{
    MPFParseError,
    RenderingError,
    ITagConnectionError,
    TraceError,
    NativeRendererError,
    InvalidOperation
}

// 사용 예시
try
{
    LoadMPFFile(path);
}
catch (CamViewerException ex) when (ex.Code == ErrorCode.MPFParseError)
{
    MessageBox.Show($"MPF 파일 파싱 오류:\n{ex.Message}\n\n컨텍스트: {ex.Context}",
                    "파싱 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
    LogHelper.Log("CamViewerCore", $"MPF Parse Error: {ex.Message} (Context: {ex.Context})");
}
catch (CamViewerException ex)
{
    // 기타 CamViewer 오류
    HandleCamViewerError(ex);
}
catch (Exception ex)
{
    // 예상치 못한 오류
    LogHelper.Log("CamViewerCore", $"Unexpected error: {ex.Message}\n{ex.StackTrace}");
    throw;  // 재throw로 상위로 전파
}

개선 효과:
- 명확한 오류 분류
- 적절한 오류 메시지
- 디버깅 용이
```

#### B. 재시도 로직 (Retry Logic)
```csharp
// Phase 14: ITag 연결 재시도

public async Task<bool> ConnectWithRetryAsync(int maxRetries = 3, int delayMs = 1000)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            LogHelper.Log("ITagManager", $"Connection attempt {attempt}/{maxRetries}");
            
            bool success = await ConnectAsync();
            if (success)
            {
                LogHelper.Log("ITagManager", "Connection successful");
                return true;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log("ITagManager", $"Connection attempt {attempt} failed: {ex.Message}");
            
            if (attempt < maxRetries)
            {
                LogHelper.Log("ITagManager", $"Retrying in {delayMs}ms...");
                await Task.Delay(delayMs);
            }
            else
            {
                LogHelper.Log("ITagManager", "All connection attempts failed");
                throw new CamViewerException(
                    ErrorCode.ITagConnectionError,
                    $"Failed to connect after {maxRetries} attempts",
                    $"Last error: {ex.Message}"
                );
            }
        }
    }
    
    return false;
}

안정성:
- 일시적 네트워크 오류 극복
- 자동 재연결
- 사용자 경험 향상
```

#### C. 상태 검증 (State Validation)
```csharp
// Phase 14: 메서드 실행 전 상태 검증

public void StartSimulation()
{
    // 상태 검증
    ValidateState(
        condition: currentProgram != null,
        errorCode: ErrorCode.InvalidOperation,
        message: "MPF 파일을 먼저 로드하세요."
    );
    
    ValidateState(
        condition: !isDisposed,
        errorCode: ErrorCode.InvalidOperation,
        message: "이미 Dispose된 객체입니다."
    );
    
    ValidateState(
        condition: simulationEngine.State != SimulationState.Running,
        errorCode: ErrorCode.InvalidOperation,
        message: "시뮬레이션이 이미 실행 중입니다."
    );
    
    // 정상 실행
    simulationEngine.Start();
    redrawTimer.Start();
}

private void ValidateState(bool condition, ErrorCode errorCode, string message)
{
    if (!condition)
    {
        throw new CamViewerException(errorCode, message, $"Method: {nameof(StartSimulation)}");
    }
}

안정성:
- 잘못된 상태에서 실행 방지
- 명확한 오류 메시지
- 디버깅 용이
```

---

### 3.2 🔒 리소스 관리 개선

#### A. using 패턴 적용
```csharp
// 현재: 수동 Dispose
var timer = new Timer();
try
{
    timer.Start();
    // ...
}
finally
{
    timer.Stop();
    timer.Dispose();
}

// Phase 14: using 패턴
using (var timer = new Timer())
{
    timer.Start();
    // ...
}  // 자동 Dispose

// C# 8.0 이상: using declaration
using var timer = new Timer();
timer.Start();
// 스코프 종료 시 자동 Dispose

리소스 누수 방지:
- 자동 정리
- 예외 발생 시에도 안전
```

#### B. 비동기 Dispose 패턴
```csharp
// Phase 14: IAsyncDisposable 구현

public class CamViewerControl : UserControl, IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        if (isDisposed) return;
        
        try
        {
            // 비동기 정리 작업
            await StopAsyncOperationsAsync();
            
            // 동기 정리 작업
            DisposeCore();
            
            isDisposed = true;
        }
        catch (Exception ex)
        {
            LogHelper.Log("CamViewerCore", $"DisposeAsync error: {ex.Message}");
        }
    }
    
    private async Task StopAsyncOperationsAsync()
    {
        // ITag 연결 종료 (비동기)
        if (itagManager != null)
        {
            await itagManager.DisconnectAsync();
        }
        
        // 진행 중인 작업 대기
        if (renderWorker != null && renderWorker.IsBusy)
        {
            renderWorker.CancelAsync();
            await Task.Delay(100);  // 취소 대기
        }
    }
    
    private void DisposeCore()
    {
        // 기존 Dispose 로직
        simulationEngine?.Dispose();
        redrawTimer?.Dispose();
        // ...
    }
    
    // 기존 Dispose도 유지 (호환성)
    protected override void Dispose(bool disposing)
    {
        DisposeAsync().AsTask().Wait();
        base.Dispose(disposing);
    }
}

안정성:
- 비동기 정리 지원
- 리소스 누수 방지
- 깔끔한 종료
```

---

### 3.3 🧪 테스트 및 검증

#### A. 단위 테스트 추가
```csharp
// Phase 14: 핵심 로직 단위 테스트

[TestFixture]
public class CuttingProgressManagerTests
{
    [Test]
    public void UpdateProgress_ValidInput_ReturnsTrue()
    {
        // Arrange
        var mpfProgram = LoadTestMPF();
        var manager = new CuttingProgressManager();
        manager.SetProgram(mpfProgram);
        manager.StartCuttingProgress(1, 1);
        
        // Act
        bool result = manager.UpdateProgress(1, 1, 2, 0.5);
        
        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(0.5, manager.GetElementProgress(1, 1, 2));
    }
    
    [Test]
    public void UpdateProgress_InvalidElement_ReturnsFalse()
    {
        // Arrange
        var manager = new CuttingProgressManager();
        
        // Act
        bool result = manager.UpdateProgress(99, 99, 99, 0.5);
        
        // Assert
        Assert.IsFalse(result);
    }
}

테스트 커버리지:
- CuttingProgressManager: 핵심 로직
- MPFParser: 파싱 정확성
- GeometryUtils: 계산 정확성
```

#### B. 통합 테스트
```csharp
// Phase 14: 전체 시나리오 테스트

[TestFixture]
public class RealtimeTraceIntegrationTests
{
    [Test]
    public void RealtimeTrace_FullScenario_WorksCorrectly()
    {
        // 1. MPF 로드
        var camViewer = new CamViewerControl();
        camViewer.LoadMPFFile("test.mpf");
        
        // 2. 트레이스 시작
        bool started = camViewer.StartTrace(1, 1);
        Assert.IsTrue(started);
        
        // 3. 진행 업데이트
        bool updated = camViewer.UpdateCuttingProgress(1, 1, 0.5, 10.0, 20.0, "G01");
        Assert.IsTrue(updated);
        
        // 4. 렌더링 검증
        Assert.IsTrue(camViewer.IsElementInProgress(1, 1, 0));
        
        // 5. 트레이스 종료
        camViewer.StopCuttingTrace();
        Assert.IsFalse(camViewer.IsTracing);
    }
}

테스트 범위:
- MPF 로드 → 트레이스 → 렌더링 → 종료
- 전체 흐름 검증
```

#### C. 성능 테스트
```csharp
// Phase 14: 렌더링 성능 측정

[TestFixture]
public class RenderingPerformanceTests
{
    [Test]
    public void Rendering_LargeMPF_CompletesInTime()
    {
        // Arrange
        var camViewer = new CamViewerControl();
        camViewer.LoadMPFFile("large_10000_elements.mpf");
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        camViewer.Invalidate();
        camViewer.Update();  // 강제 렌더링
        stopwatch.Stop();
        
        // Assert
        Assert.Less(stopwatch.ElapsedMilliseconds, 100);  // 100ms 이하
    }
}

성능 기준:
- 렌더링: < 100ms (10,000 elements)
- MPF 파싱: < 500ms (대용량 파일)
- 메모리: < 200MB (일반적인 MPF)
```

---

## 📊 4. 우선순위 및 로드맵

### Phase 14.1 (즉시 적용) - 1주
✅ **성능 최적화 (Critical)**
1. ❌ TraceStatusPanel.cs 제거 (-8,649 bytes, -200줄)
2. ⚠️ TraceManager.cs 검토 및 결정 (제거 or 리팩토링)
3. ✅ Dirty Region 최적화 (진행 중 엘리먼트만 렌더링)

### Phase 14.2 (단기) - 2주
🎨 **UI/UX 개선**
1. 레이저 헤드 실시간 위치 표시
2. 컨텍스트 메뉴 (우클릭)
3. 키보드 단축키
4. 툴팁 (Hover 정보)

### Phase 14.3 (중기) - 1개월
🛡️ **안정성 향상**
1. 구조화된 예외 처리
2. 재시도 로직
3. 상태 검증
4. 단위 테스트 추가

### Phase 14.4 (장기) - 2개월
🚀 **고급 기능**
1. Display List 패턴 (GPU 최적화)
2. 진행 상태 히트맵
3. 3D 뷰
4. 애니메이션 효과

---

## 📝 다음 단계

1. **즉시 실행**: TraceStatusPanel.cs 제거
2. **검토**: TraceManager.cs 사용 여부 결정
3. **우선순위 결정**: 위 제안 중 원하는 기능 선택
4. **단계별 구현**: Phase 14.1부터 시작

---

**Date**: 2026-01-15  
**Phase**: 14 (계획)  
**Status**: 제안서 작성 완료
