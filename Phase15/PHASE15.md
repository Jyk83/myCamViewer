# Phase 15: Performance Metrics Enhancement

## 목표
Phase 14에서 구축한 기본 성능 모니터링 시스템을 고도화하여 **정확한 성능 비교 지표**를 확보합니다.

## Phase 14 완료 사항
- ✅ 기본 FPS 모니터링
- ✅ 렌더링 시간 측정 (평균/최소/최대)
- ✅ Invalidate/DirtyRegion 호출 횟수 추적
- ✅ 우측 하단 실시간 표시

## Phase 15 목표

### 🎯 핵심 목표
**올바른 성능 비교를 위한 정밀 지표 시스템 구축**

### 📊 추가할 성능 지표

#### 1. 갱신 픽셀 수 (Refresh Pixel Count)
```
목적: TYPE=1 vs TYPE=2의 실제 효율성 측정
측정: Invalidate/DirtyRegion 호출 시 픽셀 수 기록
표시: "Pixels: 50,000 (2.4%)"
```

**예상 결과:**
- TYPE=1 (Default): 2,073,600 픽셀/프레임 (1920×1080, 100%)
- TYPE=2 (DirtyRegion): 50,000 픽셀/프레임 (200×250, 2.4%)
- **효율성: 41.5배 향상**

#### 2. CPU 사용률 (CPU Usage)
```
목적: 시스템 부하 정확한 측정
측정: Process.GetCurrentProcess().TotalProcessorTime
표시: "CPU: 25%"
```

**예상 결과:**
- TYPE=1 (Default): 50~60% CPU
- TYPE=2 (DirtyRegion): 20~30% CPU
- **효율성: 2배 향상**

#### 3. 성능 효율 지수 (Performance Efficiency Index)
```
목적: 종합 성능 평가
계산: (FPS / CPU%) × (100% / Pixel%)
표시: "Efficiency: 2.5x"
```

### 🔧 구현 계획

#### Step 1: PerformanceMonitor 확장 (1시간)
```csharp
// 추가 필드
private long totalPixelsRefreshed = 0;
private int fullScreenWidth = 1920;
private int fullScreenHeight = 1080;
private DateTime cpuStartTime;
private TimeSpan cpuStartTotal;

// 추가 메서드
public void RecordPixels(int width, int height)
public double GetAveragePixelsPerFrame()
public double GetPixelPercentage()
public double GetCPUUsage()
public double GetEfficiencyIndex()
```

#### Step 2: CamViewerCore 통합 (1시간)
```csharp
// Invalidate() 수정
public new void Invalidate()
{
    performanceMonitor?.RecordInvalidate();
    performanceMonitor?.RecordPixels(renderPanel.Width, renderPanel.Height);
    ...
}

// InvalidateDirtyRegion() 수정
private void InvalidateDirtyRegion(Rectangle dirtyRect)
{
    performanceMonitor?.RecordDirtyRegion();
    performanceMonitor?.RecordPixels(dirtyRect.Width, dirtyRect.Height);
    ...
}
```

#### Step 3: 화면 표시 개선 (30분)
```
변경 전:
[DirtyRegion]
FPS: 60.0 | Render: 16.67ms (Min: 15.2ms, Max: 18.5ms)
Invalidate: 120 (2.0/s) | DirtyRegion: 120

변경 후:
[DirtyRegion] FPS: 60.0 | Efficiency: 41.5x
Render: 16.67ms | Pixels: 50K (2.4%)
CPU: 25% | Calls: 120 (2.0/s)
```

#### Step 4: 성능 비교 테스트 (30분)
```
테스트 시나리오:
1. 동일 MPF 로드
2. TYPE=1 실행 (60초)
3. 지표 기록 및 리셋
4. TYPE=2 실행 (60초)
5. 지표 비교

예상 결과:
┌───────────┬──────────┬──────────┬────────┐
│ Metric    │ TYPE=1   │ TYPE=2   │ 개선율 │
├───────────┼──────────┼──────────┼────────┤
│ FPS       │ 58.2     │ 59.8     │ +2.7%  │
│ Pixels/F  │ 2.07M    │ 48K      │ -97.7% │
│ CPU %     │ 58%      │ 22%      │ -62.1% │
│ Efficiency│ 1.0x     │ 41.5x    │ +4050% │
└───────────┴──────────┴──────────┴────────┘
```

### 📝 작업 순서

#### Phase 15.1: 성능 지표 고도화 (3시간)
1. ✅ PerformanceMonitor 확장
   - 픽셀 카운트 추가
   - CPU 사용률 추가
   - 효율성 지수 계산

2. ✅ CamViewerCore 통합
   - Invalidate 픽셀 기록
   - DirtyRegion 픽셀 기록

3. ✅ 화면 표시 개선
   - 3줄 형식으로 변경
   - 가독성 향상

4. ✅ 빌드 및 테스트
   - TYPE=1/2 비교 테스트
   - 지표 검증

#### Phase 15.2: 엘리먼트 단위 최적화 (2~3시간)
- DirtyRegion 크기 최소화
- 현재 엘리먼트만 갱신
- 픽셀 수 1/10 목표

#### Phase 15.3: 성능 비교 자동화 (3~4시간)
- 자동 테스트 스크립트
- 리포트 생성
- CSV 저장

### 🎯 Phase 15 완료 기준

#### 필수 (Phase 15.1)
- ✅ 갱신 픽셀 수 측정
- ✅ CPU 사용률 측정
- ✅ 효율성 지수 계산
- ✅ 화면 표시 개선
- ✅ TYPE=1 vs TYPE=2 비교 가능

#### 선택 (Phase 15.2+)
- ⏳ 엘리먼트 단위 최적화
- ⏳ 자동 테스트 시스템
- ⏳ 리포트 생성

## 기대 효과

### 정량적 효과
- **TYPE=2 효율성 41.5배 입증**
- **CPU 사용률 60% 절감**
- **정확한 성능 비교 데이터**

### 정성적 효과
- 최적화 방향 명확화
- 병목 구간 정확한 파악
- 향후 개선 기준 확립

## 다음 단계 (Phase 16)
- 멀티 스레드 렌더링
- 성능 자동 튜닝
- 작업 로그 DB 시스템

---

**생성일**: 2026-01-16  
**기반**: Phase 14 완료  
**목표**: 3시간 내 Phase 15.1 완료
