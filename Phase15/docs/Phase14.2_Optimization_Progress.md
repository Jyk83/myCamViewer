# Phase 14.2: 렌더링 최적화 - 중간 진행 보고서

**작성일**: 2026-01-15  
**상태**: 진행 중 (50% 완료)  
**목표**: 2가지 렌더링 최적화 방식 구현 및 성능 비교

---

## ✅ **완료된 작업 (1-3단계)**

### **1. 환경변수 시스템 구축**

#### **OpenGLRenderMode.cs**
```csharp
public enum OpenGLRenderMode
{
    DirtyRegion = 1,        // 화면 영역만 갱신
    ReducedRendering = 2    // 렌더링 대상 축소
}

public static class OpenGLSettings
{
    public static OpenGLRenderMode CurrentMode { get; }
    // 환경변수 HMI_OPENGL_TYPE에서 로드
}
```

**기능:**
- 환경변수 `HMI_OPENGL_TYPE` 읽기
- 기본값: DirtyRegion (1)
- 런타임 모드 전환 가능

---

### **2. Dirty Region 최적화 (TYPE=1)**

#### **DirtyRegionCalculator.cs**
```csharp
public class DirtyRegionCalculator
{
    // 컨투어 영역 계산
    public Rectangle CalculateContourRegion(int partNo, int contourNo);
    
    // 진행 영역 계산 (현재 + 이전 컨투어)
    public Rectangle CalculateProgressRegion(int partNo, int contourNo);
    
    // 월드 좌표 → 화면 좌표 변환
    private Point WorldToScreen(float worldX, float worldY);
}
```

**동작 방식:**
1. 진행 중인 컨투어의 월드 좌표 바운딩 박스 계산
2. 화면 좌표로 변환
3. 마진 추가 (렌더링 두께 고려)
4. `renderPanel.Invalidate(dirtyRect)` 호출

**예상 성능:**
- 화면 복사: 전체 → 일부
- 성능 향상: 1.5-2배

---

### **3. Progressive Rendering Manager (TYPE=2)**

#### **ProgressiveRenderingManager.cs**
```csharp
public class ProgressiveRenderingManager
{
    // 진행 상태 업데이트
    public void UpdateProgress(int partNo, int contourNo, double progress);
    
    // 렌더링할 컨투어 목록
    public List<ContourRenderInfo> GetContoursToRender();
    
    // 전체 렌더링 필요 여부
    public bool ShouldRenderAll();
}

public class ContourRenderInfo
{
    public int PartNo { get; set; }
    public int ContourNo { get; set; }
    public Contour ContourObject { get; set; }
    public bool IsCompleted { get; set; }   // 완료 (빨강)
    public bool IsCurrent { get; set; }      // 진행 중 (빨강+파랑)
    public double Progress { get; set; }     // 0.0 ~ 1.0
}
```

**동작 방식:**
1. 진행 중인 파트/컨투어 추적
2. 완료된 컨투어 + 진행 중 컨투어만 목록 반환
3. 나머지 컨투어는 렌더링하지 않음 ✅

**예상 성능:**
- 렌더링 대상: 10개 → 2개
- 성능 향상: 5배

---

### **4. CamViewerCore.cs 통합**

#### **초기화 코드**
```csharp
// InitializeOpenGL()
dirtyRegionCalculator = new DirtyRegionCalculator();
progressiveRenderingManager = new ProgressiveRenderingManager();
LogHelper.Log("CamViewerCore", 
    $"렌더링 모드: {OpenGLSettings.GetModeDescription(OpenGLSettings.CurrentMode)}");
```

#### **진행 상태 업데이트**
```csharp
private void CuttingProgressManager_ProgressUpdated(object sender, CuttingProgressEventArgs e)
{
    // Progressive Manager 업데이트
    if (progressiveRenderingManager != null)
    {
        progressiveRenderingManager.UpdateProgress(e.PartNumber, e.ContourNumber, e.Progress);
    }

    // 모드별 Invalidate
    if (OpenGLSettings.CurrentMode == OpenGLRenderMode.DirtyRegion)
    {
        InvalidateProgressRegion(e.PartNumber, e.ContourNumber);
    }
    else
    {
        Invalidate();
    }
}
```

#### **Dirty Region Invalidate**
```csharp
private void InvalidateDirtyRegion(Rectangle dirtyRect)
{
    if (OpenGLSettings.CurrentMode == OpenGLRenderMode.DirtyRegion)
    {
        renderPanel.Invalidate(dirtyRect);
    }
    else
    {
        Invalidate();
    }
}

private void InvalidateProgressRegion(int partNo, int contourNo)
{
    Rectangle dirtyRect = dirtyRegionCalculator.CalculateProgressRegion(partNo, contourNo);
    InvalidateDirtyRegion(dirtyRect);
}
```

---

## ⏳ **남은 작업 (4-7단계)**

### **5. RenderMPFScene/RenderSimulationFrame 수정**

**목표:** Progressive Rendering 적용

```csharp
private void RenderMPFScene()
{
    // 현재: 모든 컨투어 렌더링
    foreach (var part in currentProgram.Parts)
    {
        foreach (var contour in part.Contours)
        {
            RenderContour(contour);
        }
    }
}

// 개선: 진행 컨투어까지만
private void RenderMPFSceneProgressive()
{
    if (OpenGLSettings.CurrentMode == OpenGLRenderMode.ReducedRendering)
    {
        var contoursToRender = progressiveRenderingManager.GetContoursToRender();
        
        foreach (var info in contoursToRender)
        {
            RenderContourWithProgress(info);
        }
    }
    else
    {
        // 기존 방식
        RenderAllContours();
    }
}
```

**예상 소요 시간:** 1-2시간

---

### **6. CuttingProgressManager 최적화**

**목표:** 미사용 함수 제거, API 정리

**확인 대상:**
- CompleteLastElement()
- CompleteLastContour()
- GetProgress()

**예상 소요 시간:** 30분

---

### **7. 성능 측정 시스템**

**목표:** FPS 카운터 및 렌더링 시간 측정

```csharp
public class PerformanceMonitor
{
    private Stopwatch renderTimer = new Stopwatch();
    private Queue<double> frameTime = new Queue<double>();
    
    public void StartFrame();
    public void EndFrame();
    public double GetAverageFPS();
    public double GetAverageRenderTime();
}
```

**UI 추가:**
- ProgramInfoPanel에 성능 표시
- FPS: XX
- Render Time: XX ms

**예상 소요 시간:** 1시간

---

## 📊 **예상 성능 비교**

| 방식 | OpenGL 렌더링 | 화면 복사 | 총 시간 | FPS | 향상 |
|------|--------------|---------|---------|-----|------|
| **현재 (기존)** | 10ms (10개) | 6ms (전체) | 16ms | 62 | - |
| **TYPE=1 (Dirty Region)** | 10ms (10개) | 0.004ms (일부) | 10ms | 100 | 1.6배 |
| **TYPE=2 (Progressive)** | 2ms (2개) | 6ms (전체) | 8ms | 125 | 2배 |
| **TYPE=1+2 (이상적)** | 2ms (2개) | 0.004ms (일부) | 2ms | 500 | 8배 |

---

## 🎯 **다음 단계**

### **우선순위:**
1. ✅ 환경변수 시스템 (완료)
2. ✅ Dirty Region (완료)
3. ✅ Progressive Manager (완료)
4. ⏳ RenderMPFScene 수정 (진행 중)
5. ⏳ CuttingProgressManager 최적화 (대기)
6. ⏳ 성능 측정 시스템 (대기)

### **예상 잔여 시간:**
- 2-3시간

---

## 📝 **파일 변경 사항**

### **신규 파일:**
- `Rendering/OpenGLRenderMode.cs`
- `Rendering/DirtyRegionCalculator.cs`
- `Rendering/ProgressiveRenderingManager.cs`

### **수정 파일:**
- `CamViewerCore.cs`
  - Dirty Region 통합
  - Progressive Rendering Manager 통합
  - CuttingProgressManager_ProgressUpdated 수정
- `RealtimeITagControl.csproj`
  - 신규 파일 3개 추가

---

## 🚀 **테스트 시나리오**

### **환경변수 설정:**
```batch
# TYPE 1 테스트
set HMI_OPENGL_TYPE=1
실행 → Dirty Region 확인

# TYPE 2 테스트
set HMI_OPENGL_TYPE=2
실행 → Progressive Rendering 확인
```

### **검증 항목:**
- ✅ 환경변수 읽기 정상
- ✅ 모드별 로그 출력
- ⏳ Dirty Region 영역 계산
- ⏳ Progressive 컨투어 목록
- ⏳ 성능 향상 측정

---

## 🎊 **예상 효과**

- 코드 정리: 958줄 삭제 (Phase 14.1)
- 성능 향상: 1.6배 ~ 8배 (모드별)
- 유지보수성: 명확한 모드 분리
- 확장성: 새로운 최적화 방식 추가 용이
