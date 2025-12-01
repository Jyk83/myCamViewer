# 실시간 데이터 처리를 위한 아키텍처 분석

## 📌 핵심 질문
**Q: 실시간 데이터를 받아서 렌더링할 때, 가장 핵심적인 역할을 하는 것은?**
- Throttle 타이머 (16ms)?
- 다른 이벤트?
- 완전히 새로운 메커니즘?

---

## 1. 현재 구조 분석 (시뮬레이션 모드)

### 1.1 데이터 소스: 내부 (시뮬레이션)
```
[SimulationEngine]                [CamViewerControl]              [Rendering]
  (백그라운드)                      (UI 스레드)                    (UI 스레드)
      │                                 │                             │
      │ 50ms마다                        │                             │
      ├─────────────────────────────────>│                             │
      │ ProgressUpdated(pi, ci, ei)     │                             │
      │                                 │                             │
      │                          currentSimPartIndex = pi            │
      │                          currentSimContourIndex = ci         │
      │                          currentSimElementIndex = ei         │
      │                                 │                             │
      │                          needsRedraw = true                  │
      │                                 │                             │
      │                                 │                             │
   [16ms 타이머]                        │                             │
      │                                 │                             │
      ├─────────────────────────────────>│                             │
      │ Timer.Tick (16ms마다)           │                             │
      │                          if (needsRedraw)                    │
      │                            Invalidate()                      │
      │                                 ├─────────────────────────────>│
      │                                 │                      Paint 이벤트
      │                                 │                 RenderSimulationFrame()
```

**현재 구조의 특징:**
1. **데이터 생성**: SimulationEngine이 50ms마다 생성
2. **데이터 전달**: ProgressUpdated 이벤트 (비동기)
3. **렌더링 트리거**: Throttle 타이머 (16ms 간격 체크)
4. **실제 렌더링**: Paint 이벤트 (Invalidate 호출 시)

**핵심 타이밍:**
- 데이터 업데이트 주기: **50ms (SimulationEngine.ResponseTime)**
- 렌더링 체크 주기: **16ms (Throttle Timer)**
- 실제 프레임 레이트: **16~50ms 사이 (데이터 업데이트 시에만 렌더링)**

---

## 2. 실시간 데이터 구조 (Phase 5+)

### 2.1 데이터 소스: 외부 (실시간 CNC 장비)

```
[CNC 장비/PLC]              [데이터 수신]                [렌더링]
  (외부 하드웨어)            (통신 스레드)               (UI 스레드)
      │                           │                          │
      │ 실시간 데이터              │                          │
      │ (불규칙/연속)              │                          │
      ├───────────────────────────>│                          │
      │ G-code 위치 (X, Y)        │                          │
      │ 절단 상태                  │                          │
      │ 속도, 전류 등              │                          │
      │                           │                          │
      │                    DataReceived 이벤트              │
      │                           ├──────────────────────────>│
      │                           │              currentPosition 업데이트
      │                           │              needsRedraw = true
      │                           │                          │
   [16ms 타이머]                  │                          │
      │                           │                          │
      ├─────────────────────────────────────────────────────>│
      │ Timer.Tick (16ms마다)                        Invalidate()
      │                                                Paint 이벤트
```

### 2.2 실시간 데이터 특징
1. **불규칙한 주기**: 1ms ~ 100ms (장비 상태에 따라)
2. **연속 스트림**: 끊임없이 데이터 도착
3. **지연 최소화 필요**: 100ms 이내 화면 반영
4. **데이터 손실 방지**: 버퍼링 필요

---

## 3. 핵심 요소 분석: Throttle vs DataReceived

### 3.1 시나리오 A: Throttle 타이머가 핵심인 경우

**가정:**
- 실시간 데이터가 **1ms마다 도착**
- Throttle 타이머: 16ms (60 FPS)

```
시간축: 0ms  1ms  2ms  3ms ... 15ms 16ms 17ms ... 32ms
        │    │    │    │       │    │    │          │
데이터: ★────★────★────★───────★────★────★──────────★
        │                           │                │
렌더링:                            ██              ██
        │<─── 15개 데이터 누적 ───>│                │
                                  렌더링            렌더링
                              (15개 반영)       (15개 반영)
```

**결과:**
- ✅ **렌더링 부하 제어**: 16ms마다만 렌더링 (60 FPS 유지)
- ⚠️ **지연 발생**: 최대 16ms 지연
- ⚠️ **데이터 손실 가능**: 중간 데이터는 렌더링 안됨 (최신 위치만 표시)

**코드 예시:**
```csharp
// 데이터 수신 (1ms마다)
private void OnDataReceived(RealTimeData data)
{
    if (InvokeRequired)
    {
        BeginInvoke(new Action(() => OnDataReceived(data)));
        return;
    }
    
    // 현재 위치만 업데이트 (이전 데이터는 덮어씀)
    currentPosition = new Point2D(data.X, data.Y);
    needsRedraw = true;  // ← 플래그만 설정
}

// Throttle 타이머 (16ms마다)
private void ThrottleTimer_Tick(object sender, EventArgs e)
{
    if (needsRedraw && !isRedrawing)
    {
        needsRedraw = false;
        renderPanel.Invalidate();  // ← 여기서만 렌더링 트리거
    }
}

// 렌더링
private void RenderPanel_Paint(...)
{
    // currentPosition 기준으로 렌더링
    DrawCurrentPosition(currentPosition);  // ← 최신 위치만 표시
}
```

**평가:**
- 장점: CPU/GPU 부하 제어 가능
- 단점: 중간 궤적 손실 (빠른 움직임 시 끊김)

---

### 3.2 시나리오 B: DataReceived 이벤트가 핵심인 경우

**가정:**
- 실시간 데이터가 **10ms마다 도착**
- 모든 데이터를 즉시 렌더링

```
시간축: 0ms   10ms  20ms  30ms  40ms  50ms
        │     │     │     │     │     │
데이터: ★─────★─────★─────★─────★─────★
        │     │     │     │     │     │
렌더링: ██───██───██───██───██───██
       즉시  즉시  즉시  즉시  즉시  즉시
```

**결과:**
- ✅ **지연 최소화**: 데이터 도착 즉시 렌더링 (<1ms)
- ✅ **궤적 완전**: 모든 중간 위치 표시
- ❌ **렌더링 부하 폭발**: 데이터 속도에 따라 1000 FPS 가능
- ❌ **CPU/GPU 과부하**: 제어 불가

**코드 예시:**
```csharp
// 데이터 수신 (10ms마다)
private void OnDataReceived(RealTimeData data)
{
    if (InvokeRequired)
    {
        BeginInvoke(new Action(() => OnDataReceived(data)));
        return;
    }
    
    // 즉시 렌더링 트리거
    currentPosition = new Point2D(data.X, data.Y);
    renderPanel.Invalidate();  // ← 데이터마다 렌더링!
}

// Throttle 타이머 없음 (사용 안함)
```

**평가:**
- 장점: 실시간성 완벽
- 단점: 리소스 낭비, 시스템 불안정

---

### 3.3 시나리오 C: 하이브리드 (데이터 버퍼 + Throttle)

**최적 구조:**
```
시간축: 0ms  1ms  2ms  3ms ... 15ms 16ms 17ms ... 32ms
        │    │    │    │       │    │    │          │
데이터: ★────★────★────★───────★────★────★──────────★
        │    │    │    │       │    │    │          │
버퍼:   ▼────▼────▼────▼───────▼   [15개 저장]       │
        │                           │                │
렌더링:                            ██              ██
        │<─── 버퍼에 누적 ────────>│   버퍼 소비     │
                                  (15개 모두        (15개 모두
                                   렌더링)          렌더링)
```

**구현:**
```csharp
// 데이터 버퍼 (Queue)
private Queue<RealTimeData> dataBuffer = new Queue<RealTimeData>();
private object bufferLock = new object();

// 데이터 수신 (1ms마다)
private void OnDataReceived(RealTimeData data)
{
    lock (bufferLock)
    {
        dataBuffer.Enqueue(data);  // ← 버퍼에 누적
        
        // 버퍼 오버플로 방지 (최대 1000개)
        if (dataBuffer.Count > 1000)
        {
            dataBuffer.Dequeue();  // 오래된 데이터 제거
        }
    }
    
    needsRedraw = true;  // ← 렌더링 필요 플래그
}

// Throttle 타이머 (16ms마다)
private void ThrottleTimer_Tick(object sender, EventArgs e)
{
    if (needsRedraw && !isRedrawing && dataBuffer.Count > 0)
    {
        needsRedraw = false;
        renderPanel.Invalidate();  // ← Throttle로 렌더링 주기 제어
    }
}

// 렌더링
private void RenderPanel_Paint(...)
{
    List<RealTimeData> dataToRender;
    lock (bufferLock)
    {
        dataToRender = new List<RealTimeData>(dataBuffer);
        dataBuffer.Clear();  // ← 버퍼 비우기
    }
    
    // 버퍼의 모든 데이터 렌더링
    foreach (var data in dataToRender)
    {
        DrawPosition(data.X, data.Y);  // ← 모든 중간 위치 표시
        // 또는 궤적 라인 그리기
        if (previousData != null)
        {
            DrawLine(previousData.X, previousData.Y, data.X, data.Y);
        }
        previousData = data;
    }
}
```

**결과:**
- ✅ **렌더링 부하 제어**: 16ms마다만 렌더링 (60 FPS)
- ✅ **데이터 손실 없음**: 모든 중간 데이터 버퍼에 보관
- ✅ **궤적 완전**: 버퍼의 모든 위치를 선으로 연결
- ✅ **지연 최소**: 평균 8ms (16ms의 절반)

---

## 4. 핵심 요소 결론

### 4.1 가장 핵심적인 역할: **Throttle 타이머 ✅**

**이유:**
1. **렌더링 주기 제어**: 60 FPS 유지 (CPU/GPU 부하 제어)
2. **시스템 안정성**: 데이터 속도와 무관하게 일정한 부하
3. **예측 가능**: 16ms마다 정확히 렌더링

### 4.2 보조 핵심 요소: **데이터 버퍼**

**이유:**
1. **데이터 손실 방지**: 빠른 데이터도 모두 보관
2. **궤적 완전성**: 중간 위치 모두 표시
3. **지연 최소화**: 버퍼링으로 평균 지연 감소

### 4.3 실시간 데이터 처리 아키텍처

```
┌─────────────────────────────────────────────────────────────────┐
│                     실시간 데이터 처리                            │
│                                                                   │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐  │
│  │   CNC 장비   │─────>│  데이터 버퍼  │<────│   Throttle   │  │
│  │  (외부 HW)   │      │   (Queue)    │     │    Timer     │  │
│  │              │      │              │     │   (16ms)     │  │
│  └──────────────┘      └──────────────┘     └──────────────┘  │
│         │                     │                      │          │
│         │ 1~100ms             │ 누적                 │ 16ms     │
│         │ 불규칙              │                      │ 정기적   │
│         v                     v                      v          │
│   DataReceived  ──────>  Enqueue(data)  ──────> Invalidate()   │
│   이벤트 발생            버퍼에 저장            렌더링 트리거   │
│         │                     │                      │          │
│         │                     │                      v          │
│         │                     └──────────────> Paint 이벤트    │
│         │                                      (버퍼 소비)      │
│         │                                      모든 데이터      │
│         │                                      렌더링          │
└─────────────────────────────────────────────────────────────────┘

핵심 포인트:
1. DataReceived: 데이터 수집 (버퍼에 누적)
2. Throttle Timer: 렌더링 주기 제어 (16ms마다)
3. Paint: 버퍼의 모든 데이터 소비 및 렌더링
```

---

## 5. Phase 5+ 권장 아키텍처

### 5.1 클래스 구조
```csharp
public class RealTimeDataReceiver
{
    // 데이터 버퍼 (스레드 안전)
    private ConcurrentQueue<RealTimeData> dataBuffer = new ConcurrentQueue<RealTimeData>();
    
    // 최대 버퍼 크기 (메모리 보호)
    private const int MaxBufferSize = 10000;
    
    // 데이터 수신 이벤트
    public event EventHandler<RealTimeData> DataReceived;
    
    // 통신 스레드 (TCP, UDP, Serial 등)
    private Task receiverTask;
    
    // CNC 장비에서 데이터 수신 (백그라운드 스레드)
    private void ReceiveLoop()
    {
        while (isRunning)
        {
            RealTimeData data = ReadFromCNC();  // 장비에서 읽기
            
            if (dataBuffer.Count < MaxBufferSize)
            {
                dataBuffer.Enqueue(data);  // 버퍼에 누적
                
                // 이벤트 발생 (UI 스레드로 알림)
                RaiseDataReceived(data);
            }
        }
    }
    
    // 버퍼에서 데이터 가져오기
    public List<RealTimeData> GetBufferedData()
    {
        List<RealTimeData> result = new List<RealTimeData>();
        while (dataBuffer.TryDequeue(out RealTimeData data))
        {
            result.Add(data);
        }
        return result;
    }
}

public class CamViewerControl
{
    private RealTimeDataReceiver receiver;
    private System.Windows.Forms.Timer throttleTimer;
    
    // 실시간 모드 설정
    public void EnableRealTimeMode()
    {
        // 데이터 수신기 생성
        receiver = new RealTimeDataReceiver();
        receiver.DataReceived += OnRealTimeDataReceived;
        
        // Throttle 타이머 (16ms = 60 FPS)
        throttleTimer = new System.Windows.Forms.Timer();
        throttleTimer.Interval = 16;
        throttleTimer.Tick += ThrottleTimer_Tick;
        throttleTimer.Start();
        
        // 수신 시작
        receiver.Start();
    }
    
    // 데이터 수신 이벤트 (백그라운드 → UI 스레드)
    private void OnRealTimeDataReceived(object sender, RealTimeData data)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnRealTimeDataReceived(sender, data)));
            return;
        }
        
        // 플래그만 설정 (렌더링은 Throttle에서)
        needsRedraw = true;
    }
    
    // Throttle 타이머 (16ms마다, UI 스레드)
    private void ThrottleTimer_Tick(object sender, EventArgs e)
    {
        if (needsRedraw && !isRedrawing)
        {
            needsRedraw = false;
            renderPanel.Invalidate();  // ← 핵심: 렌더링 트리거
        }
    }
    
    // 렌더링 (UI 스레드)
    private void RenderPanel_Paint(object sender, PaintEventArgs e)
    {
        if (isRedrawing) return;
        
        try
        {
            isRedrawing = true;
            
            // 버퍼에서 모든 데이터 가져오기
            List<RealTimeData> dataToRender = receiver.GetBufferedData();
            
            // 기존 MPF 렌더링
            RenderMPFScene();
            
            // 실시간 데이터 오버레이 (궤적, 현재 위치)
            foreach (var data in dataToRender)
            {
                DrawRealTimePosition(data);
            }
            
            // 텍스트 오버레이
            DrawTextOverlays(e.Graphics);
            
            // 버퍼 스왑
            NativeRenderer.SwapBuffersNow();
        }
        finally
        {
            isRedrawing = false;
        }
    }
}
```

### 5.2 타이밍 다이어그램

**데이터 속도: 100Hz (10ms 간격)**
```
시간:   0ms   10ms  20ms  30ms  40ms  50ms  60ms
        │     │     │     │     │     │     │
데이터: ★─────★─────★─────★─────★─────★─────★
        ▼     ▼     ▼     ▼     ▼     ▼     ▼
버퍼:  [1]   [2]   [3]   [4]   [5]   [6]   [7]
        │           │           │           │
        │<── 16ms ─>│<── 16ms ─>│<── 16ms ─>│
        │           │           │           │
렌더링: ██─────────██─────────██─────────██
       [1-2 소비] [3-4 소비] [5-6 소비] [7 소비]
```

**데이터 속도: 1000Hz (1ms 간격)**
```
시간:   0ms 1ms 2ms ... 15ms 16ms 17ms ... 32ms
        │   │   │       │    │    │          │
데이터: ★───★───★───────★────★────★──────────★
        ▼   ▼   ▼       ▼                    ▼
버퍼:  [1] [2] [3] ... [16]              [17-32]
        │               │                    │
        │<──── 16ms ───>│<───── 16ms ───────>│
        │               │                    │
렌더링: ───────────────██──────────────────██
                    [1-16 소비]        [17-32 소비]
```

---

## 6. 최종 답변

### Q: 실시간 데이터 처리 시 가장 핵심적인 역할은?

**A: Throttle 타이머 (16ms) ✅**

**이유:**

#### 1️⃣ **렌더링 주기 제어** (가장 중요!)
- 실시간 데이터가 1ms마다 오든, 100ms마다 오든, **렌더링은 항상 16ms마다**
- CPU/GPU 부하를 **예측 가능**하게 유지
- 시스템 안정성 보장

#### 2️⃣ **사용자 경험 최적화**
- 60 FPS = 사람 눈에 부드러운 애니메이션
- 16ms보다 빠른 렌더링은 사람이 인지 못함 (낭비)
- 16ms보다 느린 렌더링은 끊김 현상 (체감)

#### 3️⃣ **데이터와 렌더링 분리**
- 데이터 수신: **비동기, 불규칙** (1~100ms)
- 렌더링: **동기, 규칙적** (16ms)
- 버퍼가 둘을 연결

### 보조 핵심 요소

#### 1️⃣ **데이터 버퍼** (ConcurrentQueue)
- 빠른 데이터도 손실 없이 보관
- Throttle과 데이터 수신 속도 차이 흡수

#### 2️⃣ **needsRedraw 플래그**
- 불필요한 렌더링 스킵
- 데이터 없으면 렌더링 안함

#### 3️⃣ **isRedrawing 플래그**
- 중복 렌더링 방지
- 시스템 안정성

### 비유

```
실시간 데이터 처리 = 물탱크 시스템

[수도꼭지]           [물탱크]            [배수구]
  (CNC)              (버퍼)            (렌더링)
    │                   │                  │
    │ 불규칙            │ 저장              │ 16ms마다
    │ 1~100ms          │                  │ 규칙적
    v                   v                  v
   물이 쏟아짐  ──>  탱크에 모음  ──>  일정하게 배출
  (DataReceived)    (Enqueue)      (Throttle Timer)

핵심: 배수구 속도(Throttle)가 시스템 전체 성능 결정!
```

### Phase 5+ 구현 체크리스트

- [ ] `RealTimeDataReceiver` 클래스 구현
- [ ] `ConcurrentQueue<RealTimeData>` 버퍼 추가
- [ ] Throttle 타이머 유지 (16ms)
- [ ] `needsRedraw` 플래그 유지
- [ ] `isRedrawing` 플래그 유지
- [ ] Paint 이벤트에서 버퍼 소비 로직 추가
- [ ] 최대 버퍼 크기 설정 (메모리 보호)
- [ ] 통신 프로토콜 구현 (TCP/UDP/Serial)

### 성능 목표

| 지표 | 목표 | 현재 지원 |
|------|------|----------|
| 렌더링 FPS | 60 | ✅ (16ms) |
| 데이터 수신 속도 | 1000 Hz | ✅ (버퍼) |
| 지연 시간 | < 50ms | ✅ (평균 8ms) |
| CPU 사용률 | < 50% | ✅ (Throttle) |
| 메모리 사용 | < 100MB | ✅ (버퍼 제한) |

**결론: Throttle 타이머가 핵심이며, 현재 구조는 실시간 데이터 처리에 이미 최적화되어 있습니다! 🎯**
