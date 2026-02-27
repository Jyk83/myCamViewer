# Phase 14.1: 제거 대상 상세 분석 리포트

## 🔍 TraceManager 및 관련 함수 호출 관계 분석

### 호출 체인 구조

```
레벨 1 (Public API):
    CamViewerCore.StartCuttingTrace()      → 호출 없음 ❌
    CamViewerCore.UpdateCuttingProgress()   → 호출 없음 ❌
    CamViewerCore.StopCuttingTrace()        → 호출 없음 ❌
         ↓
레벨 2 (Wrapper):
    TraceManager.StartTrace()               → 호출 없음 ❌
    TraceManager.UpdateProgress()           → 호출 없음 ❌
    TraceManager.StopTrace()                → 호출 없음 ❌
         ↓
레벨 3 (Native API):
    NativeRenderer.StartCuttingTrace()      → 호출 없음 ❌
    NativeRenderer.UpdateCuttingTrace()     → 호출 없음 ❌
    NativeRenderer.StopCuttingTrace()       → 호출 없음 ❌
```

### ✅ 결론: 전체 체인이 미사용

모든 함수가 연쇄적으로 사용되지 않습니다.

---

## 📊 제거 대상 목록

### A. 완전 삭제 가능 (파일 단위)

#### 1. TraceStatusPanel.cs ✅ 삭제 확정
- **파일**: `Trace/TraceStatusPanel.cs`
- **크기**: 8,649 bytes
- **근거**: 
  - 인스턴스 생성 없음
  - UI에 표시 안 됨
  - 이벤트 연결 없음
  - 완전 미사용
- **영향**: 없음
- **조치**: 파일 삭제

---

### B. 주석 처리 대상 (//JYK)

#### 1. TraceManager.cs 전체 ⚠️ 주석 처리
- **파일**: `Trace/TraceManager.cs`
- **크기**: 6,917 bytes
- **근거**:
  - 인스턴스만 생성, 메서드 호출 없음
  - CuttingProgressManager로 대체됨
- **조치**: 전체 클래스 주석 처리

#### 2. CamViewerCore.cs 관련 함수들 ⚠️ 주석 처리

##### 2.1 GetTraceManager()
```csharp
// Line ~439
//JYK: TraceManager 미사용으로 제거
public TraceManager GetTraceManager()
{
    return traceManager;
}
```
- **호출 횟수**: 0회
- **조치**: 주석 처리

##### 2.2 StartCuttingTrace()
```csharp
// Line ~2554
//JYK: TraceManager 미사용으로 제거
public bool StartCuttingTrace(int startPart, int startContour, bool isReverse = false)
{
    // TraceManager.StartTrace() 호출
    // → NativeRenderer.StartCuttingTrace() 호출
}
```
- **호출 횟수**: 0회
- **조치**: 주석 처리

##### 2.3 UpdateCuttingProgress()
```csharp
// Line ~2587
//JYK: TraceManager 미사용으로 제거
public bool UpdateCuttingProgress(int part, int contour, double progress, 
                                   double posX, double posY, string currentBlock = "")
{
    // TraceManager.UpdateProgress() 호출
    // → NativeRenderer.UpdateCuttingTrace() 호출
}
```
- **호출 횟수**: 0회
- **조치**: 주석 처리

##### 2.4 StopCuttingTrace()
```csharp
// Line ~2598
//JYK: TraceManager 미사용으로 제거
public void StopCuttingTrace()
{
    // TraceManager.StopTrace() 호출
    // → NativeRenderer.StopCuttingTrace() 호출
}
```
- **호출 횟수**: 0회
- **조치**: 주석 처리

##### 2.5 traceManager 인스턴스
```csharp
// Line ~82
//JYK: TraceManager 미사용으로 제거
private TraceManager traceManager = null;

// Line ~404 (LoadMPFFile)
//JYK: TraceManager 미사용으로 제거
traceManager = new TraceManager(currentProgram);
```
- **조치**: 주석 처리

---

### C. NativeRenderer 함수 유지 ⚠️

#### NativeRenderer.StartCuttingTrace()
#### NativeRenderer.UpdateCuttingTrace()  
#### NativeRenderer.StopCuttingTrace()

**조치**: **유지**

**이유**:
1. Native DLL 외부 함수 선언 (extern)
2. 나중에 직접 사용 가능성
3. DLL API 일부
4. 주석 처리하면 컴파일 에러 가능

---

## 📝 제거 작업 상세 계획

### Phase 14.1.1: 완전 삭제 (5분)

#### Step 1: TraceStatusPanel.cs 삭제
```bash
# 1. 파일 삭제
rm Trace/TraceStatusPanel.cs

# 2. csproj에서 제거
# <Compile Include="Trace\TraceStatusPanel.cs"> 라인 삭제

# 3. 컴파일 확인
```

**영향 범위**: 없음 (완전 미사용)

---

### Phase 14.1.2: 주석 처리 (15분)

#### Step 1: TraceManager.cs 전체 주석
```csharp
//JYK: Phase 14.1 - TraceManager 미사용으로 전체 주석 처리
//      CuttingProgressManager로 대체됨
//      필요 시 복원 가능
/*
using System;
using RealtimeITagControl.MPF;

namespace RealtimeITagControl.Trace
{
    public class TraceManager
    {
        // ... 전체 내용
    }
}
*/
```

#### Step 2: CamViewerCore.cs 관련 부분 주석

##### 2.1 필드 선언
```csharp
// Line ~82
//JYK: Phase 14.1 - TraceManager 미사용으로 주석 처리
//private TraceManager traceManager = null;
```

##### 2.2 LoadMPFFile()에서 초기화
```csharp
// Line ~404
//JYK: Phase 14.1 - TraceManager 초기화 주석 처리
//traceManager = new TraceManager(currentProgram);
```

##### 2.3 GetTraceManager()
```csharp
// Line ~439
//JYK: Phase 14.1 - TraceManager 미사용으로 주석 처리
/*
/// <summary>
/// Phase 8.2: Get TraceManager instance
/// </summary>
public TraceManager GetTraceManager()
{
    return traceManager;
}
*/
```

##### 2.4 StartCuttingTrace()
```csharp
// Line ~2554
//JYK: Phase 14.1 - TraceManager 미사용으로 주석 처리
/*
public bool StartCuttingTrace(int startPart, int startContour, bool isReverse = false)
{
    // 유효성 검사
    if (currentProgram == null)
    {
        return false;
    }

    // ... 전체 내용
}
*/
```

##### 2.5 UpdateCuttingProgress()
```csharp
// Line ~2587
//JYK: Phase 14.1 - TraceManager 미사용으로 주석 처리
/*
public bool UpdateCuttingProgress(int part, int contour, double progress,
                                   double posX, double posY, string currentBlock = "")
{
    if (!traceManager.IsActive) return false;
    return traceManager.UpdateProgress(part, contour, progress, posX, posY, currentBlock);
}
*/
```

##### 2.6 StopCuttingTrace()
```csharp
// Line ~2643
//JYK: Phase 14.1 - TraceManager 미사용으로 주석 처리
/*
public void StopCuttingTrace()
{
    if (!traceManager.IsActive)
    {
        return;
    }

    traceManager.StopTrace();
    Invalidate();  // 화면 갱신
}
*/
```

##### 2.7 Dispose()에서 정리
```csharp
// Dispose() 내부에서 traceManager 정리 부분 주석
//JYK: Phase 14.1 - TraceManager 미사용으로 주석 처리
//traceManager = null;
```

---

## ✅ 체크리스트

### 작업 전 확인
- [ ] Git 커밋 완료 (현재 상태 저장)
- [ ] 백업 완료

### 삭제 작업
- [ ] TraceStatusPanel.cs 파일 삭제
- [ ] csproj에서 TraceStatusPanel.cs 제거
- [ ] 컴파일 확인

### 주석 처리
- [ ] TraceManager.cs 전체 주석 (//JYK)
- [ ] CamViewerCore.cs - traceManager 필드 주석
- [ ] CamViewerCore.cs - LoadMPFFile() 초기화 주석
- [ ] CamViewerCore.cs - GetTraceManager() 주석
- [ ] CamViewerCore.cs - StartCuttingTrace() 주석
- [ ] CamViewerCore.cs - UpdateCuttingProgress() 주석
- [ ] CamViewerCore.cs - StopCuttingTrace() 주석
- [ ] CamViewerCore.cs - Dispose() 정리 주석
- [ ] 컴파일 확인

### 최종 확인
- [ ] 빌드 성공
- [ ] 실행 테스트
- [ ] Git 커밋

---

## 📊 예상 효과

| 항목 | Before | After | 개선 |
|------|--------|-------|------|
| **파일 수** | 27개 | 26개 | -1개 |
| **코드 라인** | ~495 줄 | ~50 줄 주석 | -445 줄 |
| **파일 크기** | 15,566 bytes | 0 bytes | -15,566 bytes |
| **유지보수** | 혼란 | 명확 | ✅ |

---

## 🎯 다음 단계

1. **즉시 실행**: TraceStatusPanel.cs 삭제
2. **주의 실행**: TraceManager 관련 주석 처리
3. **컴파일 확인**
4. **Git 커밋**

---

**Date**: 2026-01-15  
**Phase**: 14.1 (제거 작업)  
**Status**: 승인 대기
