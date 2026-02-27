# Phase 2: Simulation Engine - 시뮬레이션 추가

## 🎯 목표

V20 MPF Viewer의 절단 시뮬레이션 기능을 Task 기반으로 현대화하여 구현

---

## ✅ Phase 1 기능 + 추가 기능

### 🆕 새로운 기능

#### 1. SimulationEngine (Task 기반)
- **V20 개선**: `Thread.Abort()` → `CancellationToken` 사용
- **Triple Nested Loop**: Part → Contour → Element
- **상태 관리**: Stopped, Running, Paused, Completed
- **타이밍 제어**: 10-1000ms 조절 가능 (기본 50ms)
- **진행률 추적**: 실시간 퍼센트 계산

#### 2. 시뮬레이션 UI
- **제어 버튼**:
  - 시작 (Start)
  - 일시정지/재개 (Pause/Resume)
  - 중지 (Stop)
- **속도 조절**: TrackBar로 실시간 조정
- **진행률 표시**: ProgressBar + 퍼센트 레이블
- **로그 창**: 1000줄 제한, 자동 스크롤
- **상태 표시**: 실행 중/일시정지/중지 색상 구분

#### 3. 점진적 렌더링
- **완료된 세그먼트만 표시**
- **현재 절단 위치 추적**
- **실시간 업데이트**: 50ms마다 화면 갱신

---

## 📂 파일 구조

```
Phase2_Simulation/
├── WinFormsApp/
│   ├── MPF/                       # Phase 1과 동일
│   ├── Simulation/                # 🆕 시뮬레이션 엔진
│   │   └── SimulationEngine.cs    # Task 기반 시뮬레이션
│   ├── CamViewerControl.cs         # 🔧 시뮬레이션 통합
│   ├── MainForm.cs                 # 🔧 시뮬레이션 UI 추가
│   └── CamViewerPOC.csproj         # 🔧 SimulationEngine 추가
├── NativeRenderer/                 # Phase 1과 동일
├── SampleMPF/
│   └── simple_test.mpf
└── README_PHASE2.md (이 파일)
```

---

## 🆚 V20 MPF Viewer와 비교

| 기능 | V20 (기존) | Phase 2 (현재) |
|------|-----------|---------------|
| **스레딩** | Thread + Abort() ❌ | Task + CancellationToken ✅ |
| **일시정지** | ✅ | ✅ |
| **진행률** | ✅ | ✅ (퍼센트 표시) |
| **속도 조절** | ❌ | ✅ (10-1000ms) |
| **로그** | ✅ ListBox | ✅ 개선된 ListBox (1000줄 제한) |
| **UI 업데이트** | `Invoke()` | ✅ `BeginInvoke()` (더 빠름) |
| **리소스 관리** | Thread.Join() | ✅ `Dispose()` 패턴 |

---

## 🚀 빌드 방법

### Windows:
```batch
REM C++ DLL 빌드 (Phase 1과 동일)
build_native.bat

REM C# 애플리케이션 빌드
build_csharp.bat
```

### Visual Studio:
1. `CamViewerPOC.sln` 열기
2. F5 또는 Ctrl+F5로 실행

---

## 🧪 테스트 방법

### 1. MPF 파일 로드
1. **"Load MPF File"** 버튼 클릭
2. `SampleMPF/simple_test.mpf` 선택
3. 정적 표시 확인

### 2. 시뮬레이션 실행
1. 오른쪽 패널에서 **"시작 (Start)"** 버튼 클릭
2. 절단 경로가 순차적으로 표시되는지 확인
3. 로그 창에서 진행 상황 확인

### 3. 일시정지/재개
1. 시뮬레이션 중 **"일시정지 (Pause)"** 클릭
2. 화면이 멈추는지 확인
3. **"재개 (Resume)"** 클릭
4. 시뮬레이션이 이어지는지 확인

### 4. 속도 조절
1. 시뮬레이션 중 TrackBar 조정
2. 속도 레이블 변경 확인
3. 실제 속도 변화 확인

### 5. 중지
1. **"중지 (Stop)"** 클릭
2. 전체 경로가 다시 표시되는지 확인
3. 진행률이 0%로 리셋되는지 확인

---

## 📊 검증 체크리스트

### Phase 1 기능 (재검증)
- [ ] MPF 파일 로드
- [ ] 정적 렌더링
- [ ] 줌/팬 조작

### Phase 2 기능 (새로운)
- [ ] 시뮬레이션 시작
- [ ] 점진적 렌더링 (세그먼트 하나씩 표시)
- [ ] 일시정지 작동
- [ ] 재개 작동
- [ ] 중지 작동 (전체 표시로 복귀)
- [ ] 속도 조절 (10-1000ms)
- [ ] 진행률 바 업데이트
- [ ] 로그 창 업데이트 (Part/Contour/Element 정보)
- [ ] 상태 레이블 색상 변경 (실행중=녹색, 일시정지=노랑, 중지=빨강)

### 성능 검증
- [ ] 시뮬레이션 중 UI 반응성 (버튼 클릭 가능)
- [ ] 로그 창 성능 (1000줄 제한)
- [ ] 메모리 누수 없음 (Task 정리 확인)

---

## 🐛 알려진 제한사항

1. **파일 탐색 없음**: 직접 파일 선택 필요 (Phase 3에서 추가 예정)
2. **G-code 블록 표시**: 간단한 형식만 표시 (X, Y, I, J)
3. **역방향 시뮬레이션 없음**: V20에는 있었지만 현재 미구현

---

## 🔧 핵심 코드 설명

### SimulationEngine.cs

```csharp
// V20: Thread.Abort() 사용 (위험)
_threadSimulation.Abort();

// Phase 2: CancellationToken 사용 (안전)
cts.Cancel();
```

#### Triple Nested Loop (V20 스타일 유지):
```csharp
for (int pi = 0; pi < program.Parts.Count; pi++)
{
    for (int ci = 0; ci < part.Contours.Count; ci++)
    {
        for (int ei = 0; ei < segments.Count; ei++)
        {
            // 타이밍 제어
            await Task.Delay(responseTime);
            
            // 진행률 이벤트 발생
            RaiseProgressUpdated(...);
        }
    }
}
```

### CamViewerControl.cs

#### 점진적 렌더링:
```csharp
// 완료된 세그먼트만 그리기
if (pi < currentSimPartIndex ||
    (pi == currentSimPartIndex && ci < currentSimContourIndex))
{
    // 전체 컨투어 그리기
}
else if (pi == currentSimPartIndex && ci == currentSimContourIndex)
{
    // 현재 Element까지만 그리기
    maxElements = currentSimElementIndex + 1;
}
```

---

## ➡️ 다음 단계

Phase 2 검증 완료 후 **Phase 3 (파일 탐색 UI)** 진행

---

## 🎓 학습 포인트

### Task vs Thread
- **Thread**: 저수준 제어, Abort() 위험
- **Task**: 고수준 추상화, CancellationToken 안전

### UI 스레드 동기화
- **Invoke()**: 동기 호출 (블로킹)
- **BeginInvoke()**: 비동기 호출 (더 빠름)

### 이벤트 기반 아키텍처
- **ProgressUpdated**: 시뮬레이션 진행 알림
- **SimulationCompleted**: 완료 알림
- **LogMessage**: 로그 메시지 전달

---

**Phase**: 2 of 3  
**Status**: ✅ Ready for Testing  
**Date**: 2025-11-18  
**V20 Improvements**: Thread.Abort() 제거, 속도 조절 추가, 로그 최적화
