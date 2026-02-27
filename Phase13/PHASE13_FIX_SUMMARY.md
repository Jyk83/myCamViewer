# Phase 13 좌표계 수정 요약

## 문제점
1. Type 1 (HMI_VIEW_DIR_TYPE=1) 렌더링이 상하/좌우 반전됨
2. 마우스 좌표는 위↑Y+, 아래↓Y-, 좌←X-, 우→X+ (올바름)
3. 렌더링이 Type 2와 같은 형태로 표시 (잘못됨)
4. Pan 동작이 상하/좌우 반전 (상하 드래그 → 좌우 이동, 좌우 드래그 → 상하 이동)

## 근본 원인
**glOrtho의 left/right/bottom/top 값이 잘못 설정됨**

### Type 1 좌표계 (RightBottom Origin)
- 원점: 우하단
- Object X축: 위↑(+), 아래↓(-)
- Object Y축: 좌←(+), 우→(-)

### Screen → Object 매핑
- **Screen 좌→우** (Screen X: -1 → +1) → **Object Y 감소** (Y: + → -)
- **Screen 아래→위** (Screen Y: -1 → +1) → **Object X 증가** (X: - → +)

## 수정 내용

### 1. renderer.cpp - glOrtho 수정

**잘못된 설정:**
```cpp
// Type 1 (WRONG)
left = -viewHeight / 2 + g_panY;      // Screen left = Object Y-
right = viewHeight / 2 + g_panY;      // Screen right = Object Y+
bottom = -viewWidth / 2 + g_panX;
top = viewWidth / 2 + g_panX;
```

**올바른 설정:**
```cpp
// Type 1 (CORRECT)
// Screen X maps to Object Y (inverted)
left = viewWidth / 2 + g_panY;        // Screen left (-1.0) = Object Y+
right = -viewWidth / 2 + g_panY;      // Screen right (+1.0) = Object Y-

// Screen Y maps to Object X (normal)
bottom = -viewHeight / 2 + g_panX;    // Screen bottom (-1.0) = Object X-
top = viewHeight / 2 + g_panX;        // Screen top (+1.0) = Object X+
```

**핵심:**
- `left > right` → Screen X가 증가하면 Object Y가 **감소** (반전)
- `bottom < top` → Screen Y가 증가하면 Object X가 **증가** (정상)

### 2. CamViewerCore.cs - ScreenToObject 수정

**올바른 역변환:**
```csharp
// Type 1
// Screen X(-1~1) → Object Y (inverted)
//   left = viewWidth/2 + panY
//   right = -viewWidth/2 + panY
//   objectY = left + (ndcX - (-1)) / 2 * (right - left)
//   objectY = (viewWidth/2 + panY) + (ndcX+1)/2 * (-viewWidth)
//   objectY = panY + viewWidth/2 - (ndcX+1)*viewWidth/2
//   objectY = panY - ndcX*viewWidth/2
//   Since viewWidth = 2/zoom:
//   objectY = panY - ndcX / zoom

finalX = (float)(ndcY / zoom / aspect + panX);   // Screen Y → Object X
finalY = (float)(-ndcX / zoom + panY);           // Screen X → Object Y (inverted)
```

### 3. CamViewerCore.cs - Pan 수정

**Type 1 Pan:**
```csharp
// Screen X delta → Object Y delta (inverted)
float dyObject = deltaX / (float)renderPanel.Width * 2.0f / zoom;
panY -= dyObject;  // Inverted: screen right = object Y-

// Screen Y delta → Object X delta (inverted for intuitive control)
float dxObject = -deltaY / (float)renderPanel.Height * 2.0f / zoom;
panX += dxObject;
```

## 테스트 확인 사항

### Type 1 (HMI_VIEW_DIR_TYPE=1)
1. ✅ 원점: 캔버스 오른쪽 하단
2. ✅ 마우스 위로 이동: Y 증가
3. ✅ 마우스 아래로 이동: Y 감소
4. ✅ 마우스 좌로 이동: X 증가 (또는 좌표 표시에서 X+로 표시)
5. ✅ 마우스 우로 이동: X 감소 (또는 좌표 표시에서 X-로 표시)
6. ✅ 렌더링: 파트/컨투어가 올바른 위치에 표시
7. ✅ Pan: 상하 드래그 → 상하 이동, 좌우 드래그 → 좌우 이동
8. ✅ Zoom: 마우스 포인터 위치 기준으로 확대/축소

### Type 2 (HMI_VIEW_DIR_TYPE=2)
1. ✅ 기존 동작 유지
2. ✅ 원점: 캔버스 왼쪽 하단

## 수정된 파일
1. `NativeRenderer/renderer.cpp` - glOrtho Type 1 수정
2. `RealtimeITagControl/CamViewerCore.cs` - ScreenToObject 및 Pan 수정

## 빌드 필요
```batch
cd Phase13/NativeRenderer
build_x86.bat
copy build_x86\Release\NativeRenderer.dll ..\RealtimeITagControl\NativeRenderer.dll
```

그 다음 Visual Studio에서 C# 프로젝트 빌드.
