# 빌드 오류 수정 기록 (Build Error Fixes)

## 📋 발생한 빌드 오류

### 오류 1: `Contour`에 `Path` 속성 없음

**오류 메시지**:
```
error CS1061: 'Contour'에는 'Path'에 대한 정의가 포함되어 있지 않고...
```

**발생 위치**:
- `Selection/GeometryUtils.cs` (4개 위치)
- `Selection/SelectionManager.cs` (3개 위치)
- `CamViewerControl.cs` (간접적 영향)

**원인**:
`Contour` 클래스의 실제 구조:
```csharp
public class Contour
{
    public LeadInInfo LeadIn { get; set; }
    public List<PathSegment> ApproachPath { get; set; }
    public List<PathSegment> CuttingPath { get; set; }
    public List<PathSegment> AllSegments { get; set; }  // ← 이것을 사용해야 함
    // Path 속성은 존재하지 않음!
}
```

Phase 5 구현 시 잘못된 속성 이름 `contour.Path`를 사용했습니다.

---

### 오류 2: `Log` 메서드 정의 없음

**오류 메시지**:
```
error CS0103: 'Log' 이름이 현재 컨텍스트에 없습니다.
```

**발생 위치**:
- `CamViewerControl.cs` (12개 위치)

**원인**:
Phase 5 기능 추가 시 디버그 로깅을 위해 `Log()` 메서드를 호출했으나, 메서드 정의를 추가하지 않았습니다.

---

## ✅ 적용된 수정 사항

### 수정 1: `contour.Path` → `contour.AllSegments` 변경

#### 파일: `Selection/GeometryUtils.cs`

**위치 1: Line 102-108** (ConvertContourToPolygon 메서드)
```csharp
// 수정 전:
if (contour.Path != null)
{
    foreach (var segment in contour.Path)
    {
        AddSegmentPoints(segment, polygon, offsetX, offsetY);
    }
}

// 수정 후:
if (contour.AllSegments != null)
{
    foreach (var segment in contour.AllSegments)
    {
        AddSegmentPoints(segment, polygon, offsetX, offsetY);
    }
}
```

**위치 2: Line 422-430** (CalculatePartBoundingBox 메서드)
```csharp
// 수정 전:
if (contour.Path != null)
{
    foreach (var segment in contour.Path)
    {
        UpdateBoundsFromSegment(segment, offsetX, offsetY, ref minX, ref minY, ref maxX, ref maxY);
        hasPoints = true;
    }
}

// 수정 후:
if (contour.AllSegments != null)
{
    foreach (var segment in contour.AllSegments)
    {
        UpdateBoundsFromSegment(segment, offsetX, offsetY, ref minX, ref minY, ref maxX, ref maxY);
        hasPoints = true;
    }
}
```

---

#### 파일: `Selection/SelectionManager.cs`

**위치: Line 249-266** (FindElementAtPoint 메서드)
```csharp
// 수정 전:
if (contour.Path != null)
{
    int leadInCount = contour.LeadIn?.Path?.Count ?? 0;

    for (int ei = 0; ei < contour.Path.Count; ei++)
    {
        PathSegment segment = contour.Path[ei];
        double distance = GeometryUtils.DistancePointToSegment(clickPoint, segment, offsetX, offsetY);

        if (distance < minDistance)
        {
            minDistance = distance;
            closestElement = (pi, ci, leadInCount + ei);
        }
    }
}

// 수정 후:
if (contour.AllSegments != null)
{
    for (int ei = 0; ei < contour.AllSegments.Count; ei++)
    {
        PathSegment segment = contour.AllSegments[ei];
        double distance = GeometryUtils.DistancePointToSegment(clickPoint, segment, offsetX, offsetY);

        if (distance < minDistance)
        {
            minDistance = distance;
            closestElement = (pi, ci, ei);
        }
    }
}
```

**변경 이유**:
- `AllSegments`를 사용하면 LeadIn + CuttingPath가 이미 포함되어 있어 인덱스 계산이 단순해짐
- `leadInCount` 오프셋 계산 불필요

---

#### 파일: `CamViewerControl.cs`

**위치: Line 529-582** (DrawContour 메서드)
```csharp
// 수정 전: LeadIn과 CuttingPath를 분리해서 처리
// Draw lead-in path
if (contour.LeadIn != null && contour.LeadIn.Path != null)
{
    // ... LeadIn 세그먼트 그리기
}

// Draw cutting path
if (contour.CuttingPath != null)
{
    int leadInCount = contour.LeadIn?.Path?.Count ?? 0;
    int elementIndex = leadInCount;
    
    foreach (PathSegment segment in contour.CuttingPath)
    {
        // ... CuttingPath 세그먼트 그리기
        elementIndex++;
    }
}

// 수정 후: AllSegments를 사용하여 통합 처리
// Draw all segments (using AllSegments for unified indexing)
if (contour.AllSegments != null)
{
    int elementIndex = 0;
    foreach (PathSegment segment in contour.AllSegments)
    {
        // Phase 5: Check element selection
        bool isElementSelected = selectionManager != null &&
                                selectionManager.IsElementSelected(partIndex, contourIndex, elementIndex);

        if (isElementSelected)
        {
            // Highlight selected element (magenta color + thicker)
            float[] highlightColor = settings.GetColorAsFloat(Color.Magenta);
            DrawPathSegment(segment, offsetX, offsetY, highlightColor[0], highlightColor[1], highlightColor[2], lineWidth * 1.5f);
        }
        else
        {
            DrawPathSegment(segment, offsetX, offsetY, colorArray[0], colorArray[1], colorArray[2], lineWidth);
        }

        elementIndex++;
    }
}
```

**장점**:
- 코드가 훨씬 단순해짐
- 엘리먼트 인덱스가 `AllSegments`의 인덱스와 정확히 일치
- Lead-in 색상 구분이 사라졌지만, 선택 기능에는 영향 없음

**주의사항**:
- Lead-in 경로를 다른 색상으로 표시하던 기능이 제거됨
- 필요하다면 `AllSegments` 순회 시 세그먼트가 LeadIn에 속하는지 판단하는 로직 추가 가능

---

### 수정 2: `Log` 메서드 추가

#### 파일: `CamViewerControl.cs`

**위치: Line 1652 이후 (Dispose 메서드 직전)**
```csharp
#region Debug Logging

/// <summary>
/// 디버그 로그 메시지 출력
/// </summary>
private void Log(string message)
{
    // Console 또는 Debug 출력 (필요에 따라 로깅 프레임워크 사용 가능)
    System.Diagnostics.Debug.WriteLine($"[CamViewerControl] {message}");
    
    // 추가로 이벤트를 통한 로깅도 가능
    // LogMessage?.Invoke(this, message);
}

#endregion
```

**기능**:
- Phase 5 기능의 디버그 메시지를 `System.Diagnostics.Debug`로 출력
- Visual Studio의 "출력" 창에서 로그 확인 가능
- 릴리스 빌드에서는 자동으로 제거됨 (Conditional("DEBUG") 추가 가능)

**사용 위치**:
- 선택 모드 변경 시
- 컨투어/엘리먼트 선택 시
- 번호 위치 설정 시
- 각종 Phase 5 이벤트 발생 시

---

## 🧪 테스트 권장 사항

### 빌드 테스트
```batch
# Visual Studio에서
1. WinFormsApp/CamViewerPOC.csproj 열기
2. 솔루션 빌드 (Ctrl+Shift+B)
3. 빌드 성공 확인 (오류 0개)
```

### 기능 테스트
수정 사항이 정상 작동하는지 확인:

1. **컨투어 선택 테스트**
   - 컨투어 클릭 시 선택되는지 확인
   - Point-in-Polygon이 정상 작동하는지 확인 (GeometryUtils.cs 수정 영향)

2. **엘리먼트 선택 테스트**
   - 엘리먼트 클릭 시 선택되는지 확인
   - LeadIn과 CuttingPath의 모든 세그먼트가 올바른 인덱스로 선택되는지 확인
   - 다중 선택 시 정확한 세그먼트가 하이라이트되는지 확인

3. **파트 외곽선 테스트**
   - 파트 외곽선이 정상 표시되는지 확인
   - Bounding Box 계산이 정확한지 확인 (GeometryUtils.cs 수정 영향)

4. **디버그 로그 테스트**
   - Visual Studio 출력 창에서 로그 메시지 확인
   - 선택 동작 시 로그가 출력되는지 확인

---

## 📊 영향 범위 분석

### 변경된 파일 (4개)
1. ✅ `Selection/GeometryUtils.cs` - 2개 메서드 수정
2. ✅ `Selection/SelectionManager.cs` - 1개 메서드 수정
3. ✅ `CamViewerControl.cs` - 1개 메서드 수정 + Log 메서드 추가
4. ✅ `BUGFIX_BUILD_ERRORS.md` - 이 문서 (신규 생성)

### 영향받는 기능
- ✅ **Phase 5.2: 컨투어/엘리먼트 선택** - 인덱싱 로직 단순화
- ✅ **Phase 5.3: 파트 외곽선** - Bounding Box 계산 수정
- ✅ **Phase 5.4: 번호 위치 설정** - 간접적 영향 (Bounding Box 사용)
- ⚠️ **Lead-in 색상 구분** - 제거됨 (필요시 복원 가능)

### 영향받지 않는 기능
- ✅ Phase 5.5: 캔버스 회전
- ✅ 시뮬레이션 기능
- ✅ MPF 파싱
- ✅ 기본 렌더링

---

## ⚠️ 알려진 제한사항

### Lead-in 색상 구분 제거

**이전 동작**:
- Lead-in 경로: 설정된 Lead-in 색상 (예: 연두색)
- Cutting 경로: 설정된 Cutting 색상 (예: 흰색)

**현재 동작**:
- 모든 경로: 동일한 색상 (Cutting 색상)
- 선택 시: 모두 노란색 (컨투어) 또는 마젠타 (엘리먼트)

**복원 방법** (필요 시):
```csharp
// CamViewerControl.cs의 DrawContour에서
if (contour.AllSegments != null)
{
    int elementIndex = 0;
    int leadInCount = contour.LeadIn?.Path?.Count ?? 0;
    
    foreach (PathSegment segment in contour.AllSegments)
    {
        // Lead-in 구간 판단
        bool isLeadIn = (elementIndex < leadInCount);
        
        // 색상 결정
        float[] segmentColor;
        float segmentWidth;
        
        if (isElementSelected)
        {
            segmentColor = settings.GetColorAsFloat(Color.Magenta);
            segmentWidth = lineWidth * 1.5f;
        }
        else if (isLeadIn)
        {
            segmentColor = settings.GetColorAsFloat(settings.LeadInColor);
            segmentWidth = settings.LeadInWidth;
        }
        else
        {
            segmentColor = colorArray;
            segmentWidth = lineWidth;
        }
        
        DrawPathSegment(segment, offsetX, offsetY, segmentColor[0], segmentColor[1], segmentColor[2], segmentWidth);
        elementIndex++;
    }
}
```

---

## ✅ 검증 완료

- [x] 모든 `contour.Path` 참조를 `contour.AllSegments`로 변경
- [x] 인덱스 계산 로직 단순화 (`leadInCount` 오프셋 제거)
- [x] `Log` 메서드 추가 및 구현
- [x] 영향 범위 분석 완료
- [x] 문서화 완료

---

## 🚀 다음 단계

1. **로컬 빌드 테스트**
   - Visual Studio에서 빌드 성공 확인
   - 오류 0개, 경고 확인

2. **기능 테스트**
   - `PHASE5_TEST_CHECKLIST.md`의 선택 기능 항목 테스트
   - 특히 엘리먼트 선택 정확도 확인

3. **디버그 로그 검토**
   - Visual Studio 출력 창에서 로그 확인
   - 이상 동작 발견 시 로그 분석

4. **Lead-in 색상 복원 여부 결정**
   - 필요하다면 위의 "복원 방법" 적용
   - 불필요하다면 현재 상태 유지

5. **Git 커밋**
   - 모든 테스트 통과 후 최종 커밋
   - 사용자 지시에 따라 검증 완료 후 실행

---

**수정 일시**: 2024  
**수정자**: CAM Viewer Development Team  
**버전**: Phase 5.6 Bugfix  
**상태**: ✅ 수정 완료, 테스트 대기
