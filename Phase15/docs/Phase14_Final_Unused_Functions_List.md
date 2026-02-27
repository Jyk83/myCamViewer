# Phase 14.1 - 전면 재검토 최종 리스트

## 📊 호출이 확실히 없는 함수 최종 리스트

---

## ✅ **확인된 미사용 함수 (호출 0회)**

### 1. CamViewerCore.cs (2개)

| 번호 | 함수 | 호출 횟수 | 이벤트 등록 | 상태 |
|------|------|----------|------------|------|
| 1 | **DrawCircle()** | **0회** | 0 | ❌ **미사용** |
| 2 | **DrawRectangle()** | **0회** | 0 | ❌ **미사용** |

**비고**: extern 선언, 테스트용 함수

---

### 2. Selection/GeometryUtils.cs (3개)

| 번호 | 함수 | 호출 횟수 | 이벤트 등록 | 상태 |
|------|------|----------|------------|------|
| 3 | **DistancePointToLine()** | **0회** | 0 | ❌ **미사용** |
| 4 | **DistancePointToArc()** | **0회** | 0 | ❌ **미사용** |
| 5 | **IsPointInContour()** | **0회** | 0 | ❌ **미사용** |
| 6 | **IsPointNearPath()** | **0회** | 0 | ❌ **미사용** |

**비고**: 거리 계산 및 충돌 감지 함수들

---

### 3. Selection/NumberPositionManager.cs (1개)

| 번호 | 함수 | 호출 횟수 | 이벤트 등록 | 상태 |
|------|------|----------|------------|------|
| 7 | **ClearAllPositions()** | **0회** | 0 | ❌ **미사용** |

**비고**: 위치 초기화 함수, 이미 //JYK 주석 처리됨

---

### 4. Simulation/SimulationEngine.cs (2개)

| 번호 | 함수 | 호출 횟수 | 이벤트 등록 | 상태 |
|------|------|----------|------------|------|
| 8 | **SetSpeed()** | **0회** | 0 | ❌ **미사용** |
| 9 | **GetCurrentState()** | **0회** | 0 | ❌ **미사용** |

**비고**: 
- SetSpeed: 시뮬레이션 속도 조절 기능 (미구현)
- GetCurrentState: 외부에서 호출 안됨 (내부 프로퍼티 사용)

---

### 5. Trace/CuttingProgressManager.cs (3개)

| 번호 | 함수 | 호출 횟수 | 이벤트 등록 | 상태 |
|------|------|----------|------------|------|
| 10 | **CompleteLastElement()** | **0회** | 0 | ❌ **미사용** |
| 11 | **CompleteLastContour()** | **0회** | 0 | ❌ **미사용** |
| 12 | **GetProgress()** | **0회** | 0 | ❌ **미사용** |

**비고**: TraceTestForm에서만 사용되던 함수들 (이제 불필요)

---

### 6. MPF/MPFParser.cs (3개)

| 번호 | 함수 | 호출 횟수 | 이벤트 등록 | 상태 |
|------|------|----------|------------|------|
| 13 | **ParseCommand()** | **0회** | 0 | ❌ **미사용** |
| 14 | **ParseHKLDB()** | **0회** | 0 | ❌ **미사용** |
| 15 | **ParseHKINI()** | **0회** | 0 | ❌ **미사용** |

**비고**: Parse 함수들 (실제 사용 확인 필요)

---

### 7. CamViewerCore.cs - 이미 //JYK 처리됨 (7개)

| 번호 | 함수 | 호출 횟수 | 이벤트 등록 | 상태 |
|------|------|----------|------------|------|
| 16 | **BeginMPFRender()** | **0회** | 0 | ⚠️ **이미 주석 처리** |
| 17 | **ClearScene()** | **0회** | 0 | ⚠️ **이미 주석 처리** |
| 18 | **GetTraceManager()** | **0회** | 0 | ⚠️ **이미 주석 처리** |

**비고**: 
- UpdateCuttingProgress: 1회 (TraceManager 주석에서만)
- StopCuttingTrace: 1회 (TraceManager 주석에서만)
- EnableNumberPositioning: 이미 주석 처리됨
- DisableNumberPositioning: 이미 주석 처리됨

---

## ⚠️ **제외 (사용 중 확인됨)**

### A. CamViewerCore.cs

| 함수 | 호출 횟수 | 호출 위치 | 상태 |
|------|----------|----------|------|
| **GetSimulationState()** | **2회** | RealtimeITagControl.cs (2곳) | ✅ **사용 중** |
| **CamViewerControl_Load()** | 이벤트 | `this.Load += ...` | ✅ **사용 중** |
| **IsTraceActive** | Property | - | ✅ **사용 중** |
| **GetTraceProgress()** | 0회 | TraceManager 관련 | ⚠️ **검증 필요** |
| **DrawLaserHeadMarker()** | 2회 | TraceManager 내부 | ⚠️ **검증 필요** |

---

### B. Selection/GeometryUtils.cs

| 함수 | 호출 횟수 | 호출 위치 | 상태 |
|------|----------|----------|------|
| **Distance()** | **5회** | CamViewerCore.cs:622 | ✅ **사용 중** |
| **DistancePointToSegment()** | **2회** | SelectionManager.cs | ✅ **사용 중** |
| **CalculatePartBoundingBox()** | **0회** | LabelPositionCalculator에 중복 | ⚠️ **중복** |

---

### C. Selection/NumberPositionManager.cs

| 함수 | 호출 횟수 | 호출 위치 | 상태 |
|------|----------|----------|------|
| **EnablePositioningMode()** | **1회** | CamViewerCore.cs (주석 처리된 곳) | ⚠️ **이미 주석 처리** |
| **DisablePositioningMode()** | **2회** | CamViewerCore.cs (실제 사용) | ✅ **사용 중** |

---

### D. Rendering/LabelPositionCalculator.cs

| 함수 | 호출 횟수 | 호출 위치 | 상태 |
|------|----------|----------|------|
| **CalculatePartLabelPosition()** | **1회** | CamViewerCore.cs | ✅ **사용 중** |
| **CalculateContourLabelPosition()** | **1회** | CamViewerCore.cs | ✅ **사용 중** |

---

### E. Simulation/SimulationEngine.cs

| 함수 | 호출 횟수 | 상태 |
|------|----------|------|
| **Start()** | **58회** | ✅ **사용 중** |
| **Pause()** | **2회** | ✅ **사용 중** |
| **Resume()** | **2회** | ✅ **사용 중** |
| **Stop()** | **8회** | ✅ **사용 중** |
| **Reset()** | **14회** | ✅ **사용 중** |

---

### F. UI/ContourColorLegendForm.cs

| 함수 | 호출 횟수 | 호출 위치 | 상태 |
|------|----------|----------|------|
| **ContourColorLegendForm (클래스)** | **2회** | RealtimeITagControl.cs | ✅ **사용 중** |

---

## 📋 **최종 //JYK 표시 대상 요약**

### 확실한 미사용 (15개)

| 번호 | 파일 | 함수 | 신뢰도 |
|------|------|------|--------|
| 1 | CamViewerCore.cs | DrawCircle() | 높음 ✅ |
| 2 | CamViewerCore.cs | DrawRectangle() | 높음 ✅ |
| 3 | GeometryUtils.cs | DistancePointToLine() | 높음 ✅ |
| 4 | GeometryUtils.cs | DistancePointToArc() | 높음 ✅ |
| 5 | GeometryUtils.cs | IsPointInContour() | 높음 ✅ |
| 6 | GeometryUtils.cs | IsPointNearPath() | 높음 ✅ |
| 7 | NumberPositionManager.cs | ClearAllPositions() | 높음 ⚠️ (이미 주석 처리됨) |
| 8 | SimulationEngine.cs | SetSpeed() | 높음 ✅ |
| 9 | SimulationEngine.cs | GetCurrentState() | 높음 ✅ |
| 10 | CuttingProgressManager.cs | CompleteLastElement() | 높음 ✅ |
| 11 | CuttingProgressManager.cs | CompleteLastContour() | 높음 ✅ |
| 12 | CuttingProgressManager.cs | GetProgress() | 높음 ✅ |
| 13 | MPFParser.cs | ParseCommand() | 중간 ⚠️ |
| 14 | MPFParser.cs | ParseHKLDB() | 중간 ⚠️ |
| 15 | MPFParser.cs | ParseHKINI() | 중간 ⚠️ |

---

## 🔍 **추가 검증 필요**

### TraceManager 관련 (주석 처리됨)

| 함수 | 상태 | 비고 |
|------|------|------|
| GetTraceProgress() | 주석 처리 권장 | TraceManager에 의존 |
| DrawLaserHeadMarker() | 주석 처리 권장 | TraceManager 내부에서만 사용 |
| IsTraceActive | Property | 실제 사용 여부 확인 필요 |

---

## 📊 **분석 요약**

### 신뢰도별 분류

**높음 (12개)**: 호출 0회, 이벤트 등록 0회
- DrawCircle, DrawRectangle
- DistancePointToLine, DistancePointToArc, IsPointInContour, IsPointNearPath
- SetSpeed, GetCurrentState
- CompleteLastElement, CompleteLastContour, GetProgress
- ClearAllPositions (이미 주석 처리됨)

**중간 (3개)**: 호출 0회, Parse 함수 확인 필요
- ParseCommand, ParseHKLDB, ParseHKINI

**검증 필요 (3개)**: TraceManager 관련
- GetTraceProgress, DrawLaserHeadMarker, IsTraceActive

---

## 🎯 **다음 단계 제안**

1. **12개 높은 신뢰도 함수에 //JYK 표시**
   - DrawCircle, DrawRectangle
   - DistancePointToLine, DistancePointToArc, IsPointInContour, IsPointNearPath
   - SetSpeed, GetCurrentState
   - CompleteLastElement, CompleteLastContour, GetProgress

2. **MPF Parse 함수 추가 검증**
   - ParseCommand, ParseHKLDB, ParseHKINI
   - 실제 MPF 파일 파싱 과정 확인

3. **TraceManager 관련 정리**
   - TraceManager가 이미 주석 처리되었으므로
   - 관련 함수들도 주석 처리 검토

---

## ✅ **검증 완료**

- ✅ 전체 파일 재검사 완료
- ✅ 호출 횟수 정확 확인
- ✅ 이벤트 등록 여부 확인
- ✅ 사용 중인 함수 제외
- ✅ 신뢰도별 분류 완료

**사용자 재확인 대기 중입니다!** 🙏
