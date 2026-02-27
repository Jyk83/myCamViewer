# Phase 13 빌드 안내

## 수정된 파일

### 1. C++ NativeRenderer
- `NativeRenderer/renderer.cpp` 
  - ViewDirection 변수 추가 (`g_viewDirection`)
  - `SetViewDirection()` 함수 추가
  - `BeginMPFRenderWithBackground()`의 glOrtho 수정 (ViewDirection에 따라 좌표계 변경)

- `NativeRenderer/renderer.h`
  - `SetViewDirection()` 함수 선언 추가

### 2. C# CamViewerCore
- `RealtimeITagControl/CamViewerCore.cs`
  - `SetViewDirection()` extern 선언 추가
  - `RenderMPFScene()`에서 SetViewDirection 호출 추가
  - `ScreenToObject()` 함수 수정 (glOrtho에 맞게 좌표 역변환)
  - DrawContour의 Transformed 함수들 제거하고 원래 NativeRenderer 함수 사용
  - TransformRenderCoordinates 관련 3개 함수 제거

### 3. C# RealtimeITagControl
- `RealtimeITagControl/RealtimeITagControl.cs`
  - `UpdateViewDirection()` 메서드에서 HMI_VIEW_DIR_TYPE 태그 읽기
  - `ITagManager_DataChanged`에서 UpdateViewDirection 호출

### 4. C# RenderSettings
- `RealtimeITagControl/Rendering/RenderSettings.cs`
  - `ViewDirectionType` enum 추가 (RightBottom=1, LeftBottom=2)
  - `ViewDirection` 속성 추가

### 5. C# ProgramInfoPanel
- `RealtimeITagControl/UI/ProgramInfoPanel.cs`
  - 마우스 좌표 표시 Label 추가

## 빌드 순서

### 1단계: C++ DLL 빌드 (Windows에서)

```batch
cd Phase13/NativeRenderer
build_x86.bat
```

빌드 결과:
- `NativeRenderer/build_x86/Release/NativeRenderer.dll` 생성

생성된 DLL을 다음 위치로 복사:
```batch
copy build_x86\Release\NativeRenderer.dll ..\RealtimeITagControl\NativeRenderer.dll
```

### 2단계: C# 프로젝트 빌드

Visual Studio에서 `RealtimeITagControl.sln` 열기 → 빌드 (F6)

또는 커맨드라인:
```batch
cd Phase13
msbuild RealtimeITagControl.sln /p:Configuration=Release
```

## 테스트 방법

### Type 1 테스트 (HMI_VIEW_DIR_TYPE=1, 우하단 원점)
1. ITTag 서버에서 `HMI_VIEW_DIR_TYPE` = 1 설정
2. MPF 파일 로드
3. 확인 사항:
   - 원점이 캔버스 오른쪽 하단에 위치
   - X축: 위로 가면 +, 아래로 가면 -
   - Y축: 좌측으로 가면 +, 우측으로 가면 -
   - 파트/컨투어 번호 위치 정상
   - Arc 컨투어 방향 정상
   - 마우스 좌표가 실시간으로 표시되고 멈춰도 0,0으로 초기화 안 됨

### Type 2 테스트 (HMI_VIEW_DIR_TYPE=2, 좌하단 원점, 기본)
1. ITTag 서버에서 `HMI_VIEW_DIR_TYPE` = 2 설정
2. MPF 파일 로드
3. 확인 사항:
   - 원점이 캔버스 왼쪽 하단에 위치 (기존 구현)
   - X축: 우측으로 가면 +, 좌측으로 가면 -
   - Y축: 위로 가면 +, 아래로 가면 -
   - 모든 기능 정상 작동

## 주요 변경 사항 요약

1. **ViewDirection 처리 방식 변경**
   - 이전: C#에서 모든 렌더링 좌표 변환 (DrawLineTransformed 등)
   - 현재: C++ glOrtho에서 좌표계 변환 (깔끔하고 효율적)

2. **glOrtho 좌표계 변환**
   - Type 1: width와 height를 swap하고 panX/panY도 swap
   - Type 2: 기존 OpenGL 좌표계 유지

3. **ScreenToObject 좌표 역변환**
   - glOrtho 설정에 맞게 역변환 공식 적용
   - Type 1: aspect ratio를 X 좌표에 적용, panX/panY swap
   - Type 2: 기존 공식 유지

4. **마우스 좌표 0,0 초기화 문제 해결**
   - Throttling 로직 제거
   - UpdateMouseCoordinates에서 항상 좌표 업데이트

## 문제 해결

### DLL 빌드 오류
- Visual Studio 2019 이상 필요
- Windows SDK 설치 확인
- OpenGL 라이브러리 (opengl32.lib) 링크 확인

### 좌표가 이상하게 나오는 경우
- NativeRenderer.dll이 최신 버전인지 확인
- Phase13/RealtimeITagControl/NativeRenderer.dll이 새로 빌드한 DLL인지 확인
- DLL을 다시 복사하고 프로젝트 재빌드

### 마우스 좌표가 0,0으로 초기화되는 경우
- 최신 코드인지 확인 (throttling 제거됨)
- MouseMove 이벤트가 정상 발생하는지 확인 (로그 확인)

