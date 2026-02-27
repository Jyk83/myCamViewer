# Phase 13: Canvas Origin and Mouse Coordinates

## 목표

1. **캔버스 방향 기준점 변경 (HMI_VIEW_DIR_TYPE)**
   - Type 1: 우하단 원점 (RightBottom) - Type 2를 **-90도 회전**
   - Type 2: 좌하단 원점 (LeftBottom, 기본)

2. **마우스 실시간 좌표 반환**
   - Object Space 좌표를 mm 단위로 표시
   - MPF 원점 기준 좌표

## Type 1 (RightBottom) 좌표계

### 원점 및 축 방향
- **원점**: 캔버스 오른쪽 하단
- **X축**: 위↑(+), 아래↓(-) → Type 2의 Y축
- **Y축**: 좌←(+), 우→(-) → Type 2의 -X축
- **회전**: Type 2 기준 **-90도 회전** (반시계방향)

### 화면-객체 매핑
- **Screen X (좌→우)**: Object Y (좌+, 우+) - NORMAL
- **Screen Y (아래→위)**: Object X (아래+, 위-) - INVERTED

### glOrtho 설정 (width/height swap + Y 반전)
```cpp
// Type 1: RightBottom
left = -viewHeight / 2 + g_panY;    // Screen left (-1.0) → Object Y-
right = viewHeight / 2 + g_panY;    // Screen right (+1.0) → Object Y+
bottom = viewWidth / 2 + g_panX;    // Screen bottom (-1.0) → Object X+
top = -viewWidth / 2 + g_panX;      // Screen top (+1.0) → Object X-
```

### ScreenToObject 역변환
```csharp
// Screen X → Object Y (normal)
finalY = ndcX / zoom / aspect + panY;

// Screen Y → Object X (inverted)
finalX = -ndcY / zoom + panX;
```

### Pan 방향
```csharp
// Screen X → Object Y (negative for intuitive control)
panY += -deltaX / width * 2.0f / zoom / aspect;

// Screen Y → Object X (positive for intuitive control)
panX += deltaY / height * 2.0f / zoom;
```

## Type 2 (LeftBottom) 좌표계

### 원점 및 축 방향
- **원점**: 캔버스 왼쪽 하단 (OpenGL 기본)
- **X축**: 우→(+), 좌←(-)
- **Y축**: 위↑(+), 아래↓(-)

### glOrtho 설정
```cpp
// Type 2: LeftBottom (기본)
left = -viewWidth / 2 + g_panX;
right = viewWidth / 2 + g_panX;
bottom = -viewHeight / 2 + g_panY;
top = viewHeight / 2 + g_panY;
```

### ScreenToObject 역변환
```csharp
// Normal OpenGL mapping
finalX = ndcX / zoom + panX;
finalY = ndcY / zoom / aspect + panY;
```

## 구현 상세

### 1. C++ NativeRenderer

**renderer.cpp**
- `g_viewDirection` 변수 추가 (1=RightBottom, 2=LeftBottom)
- `SetViewDirection(int direction)` 함수 추가
- `BeginMPFRenderWithBackground()`에서 ViewDirection에 따라 glOrtho 설정

**renderer.h**
- `SetViewDirection(int direction)` 함수 선언

### 2. C# CamViewerCore

**CamViewerCore.cs**
- `SetViewDirection()` extern 선언
- `RenderMPFScene()`에서 SetViewDirection 호출
- `ScreenToObject()`: ViewDirection에 맞게 좌표 역변환
- `RenderPanel_MouseMove()`: ViewDirection에 맞게 Pan 처리
- `UpdateMouseCoordinates()`: 마우스 좌표 실시간 업데이트

### 3. C# RealtimeITagControl

**RealtimeITagControl.cs**
- `UpdateViewDirection()`: HMI_VIEW_DIR_TYPE 태그 읽기
- `ITagManager_DataChanged`에서 UpdateViewDirection 호출
- `MouseCoordinatesChanged` 이벤트 핸들러

### 4. C# RenderSettings

**RenderSettings.cs**
- `ViewDirectionType` enum 추가
- `ViewDirection` 속성 추가

### 5. C# ProgramInfoPanel

**ProgramInfoPanel.cs**
- 마우스 좌표 표시 Label 추가 (txtCoordinates)
- MouseCoordinatesChanged 이벤트 핸들러

## 테스트 시나리오

### Type 1 (HMI_VIEW_DIR_TYPE=1) 테스트
1. ITTag에서 HMI_VIEW_DIR_TYPE=1 설정
2. MPF 로드
3. 확인 사항:
   - 원점이 우하단에 위치
   - 마우스 위로 이동 → X 좌표 증가
   - 마우스 아래로 이동 → X 좌표 감소
   - 마우스 좌로 이동 → Y 좌표 증가
   - 마우스 우로 이동 → Y 좌표 감소
   - 파트/컨투어가 정상적으로 그려짐
   - Pan: 마우스 방향과 캔버스 이동 방향 일치
   - Zoom: 마우스 포인터 위치 고정

### Type 2 (HMI_VIEW_DIR_TYPE=2) 테스트
1. ITTag에서 HMI_VIEW_DIR_TYPE=2 설정
2. MPF 로드
3. 확인 사항:
   - 원점이 좌하단에 위치 (기존과 동일)
   - 마우스 우로 이동 → X 좌표 증가
   - 마우스 위로 이동 → Y 좌표 증가
   - 모든 기능 정상 작동

## 주요 수정 사항

1. **glOrtho 좌표계 변환**
   - Type 1: left/right 반전으로 X축 반전 효과
   - Type 2: 기존 OpenGL 좌표계 유지

2. **ScreenToObject 정확한 역변환**
   - glOrtho 설정에 정확히 매칭되는 역변환 공식

3. **Pan 방향 수정**
   - Type 1: Screen X→Object Y, Screen Y→Object X
   - aspect ratio 고려

4. **마우스 좌표 0,0 초기화 문제 해결**
   - Throttling 제거
   - 항상 최신 좌표 유지

## 빌드 방법

### 1. C++ DLL 빌드
```batch
cd Phase13/NativeRenderer
build_x86.bat
copy build_x86\Release\NativeRenderer.dll ..\RealtimeITagControl\NativeRenderer.dll
```

### 2. C# 프로젝트 빌드
Visual Studio에서 RealtimeITagControl.sln 빌드 (F6)
