# RenderSettings.json 설정 가이드

## 개요
`RenderSettings.json` 파일을 직접 수정하여 CAM Viewer의 렌더링 설정을 변경할 수 있습니다.

## 파일 위치
- 프로젝트 루트 디렉토리: `Phase8_RealtimeTrace/RenderSettings.json`

## 설정 항목

### 1. Sizes (크기 설정)

#### 파트/컨투어 번호 폰트 크기
```json
"Sizes": {
  "PartNumberSize": 4,      // 파트 번호 폰트 크기 (픽셀)
  "ContourNumberSize": 3    // 컨투어 번호 폰트 크기 (픽셀)
}
```

**추천 값:**
- `PartNumberSize`: 3 ~ 6 (기본값: 4)
- `ContourNumberSize`: 2 ~ 5 (기본값: 3)

#### 기타 크기 설정
```json
"Sizes": {
  "WorkpieceBoundaryWidth": 2,    // 워크피스 외곽선 두께
  "PartOriginSize": 8,            // 파트 원점 크기
  "PiercingPointSize": 2,         // 피어싱 포인트 크기
  "LeadInWidth": 1,               // 리드인 경로 두께
  "CuttingCompletedWidth": 1,     // 완료된 절단 경로 두께
  "CuttingInProgressWidth": 1,    // 진행 중 절단 경로 두께
  "CuttingPendingWidth": 1        // 대기 중 절단 경로 두께
}
```

### 2. Colors (색상 설정)

색상은 HEX 코드로 설정합니다 (예: `#FFFFFF`는 흰색).

```json
"Colors": {
  "PartNumberColor": "#FFFFFF",      // 파트 번호 색상 (기본: 흰색)
  "ContourNumberColor": "#C8C8C8",   // 컨투어 번호 색상 (기본: 연한 회색)
  "WorkpieceBoundaryColor": "#646464",
  "WorkpieceInteriorColor": "#004040",
  "WorkpieceExteriorColor": "#000000",
  "PiercingPointColor": "#FF0000",
  "LeadInColor": "#FFFF00",
  "CuttingCompletedColor": "#00FFFF",
  "CuttingInProgressColor": "#FF0000",
  "CuttingPendingColor": "#505050",
  "MarkingColor": "#FFFF00"
}
```

### 3. Visibility (표시 여부)

```json
"Visibility": {
  "ShowPartOrigin": false,         // 파트 원점 표시 (기본: 꺼짐)
  "ShowWorkpieceBoundary": false,  // 워크피스 외곽선 표시 (기본: 꺼짐)
  "ShowPartNumbers": false,        // 파트 번호 표시 (기본: 꺼짐)
  "ShowContourNumbers": false      // 컨투어 번호 표시 (기본: 꺼짐)
}
```

**참고:** 
- 파트 번호와 컨투어 번호는 UI의 "Part #", "Contour #" 버튼으로도 제어 가능
- 버튼 상태는 이 설정과 동기화됩니다

### 4. View (뷰 설정)

```json
"View": {
  "InitialZoomMultiplier": 0.005   // 초기 줌 배율 (0.001 ~ 0.1)
}
```

## 사용 방법

1. **파일 편집**: `RenderSettings.json` 파일을 텍스트 에디터로 엽니다
2. **값 수정**: 원하는 항목의 값을 변경합니다
3. **저장**: 파일을 저장합니다
4. **재시작**: CAM Viewer 애플리케이션을 재시작하여 변경사항을 적용합니다

## 예시: 폰트 크기 조정

```json
{
  "Sizes": {
    "PartNumberSize": 5,      // 파트 번호를 더 크게
    "ContourNumberSize": 4    // 컨투어 번호를 더 크게
  }
}
```

## 주의사항

- JSON 형식을 유지해야 합니다 (쉼표, 따옴표 등)
- 잘못된 형식의 JSON은 기본값으로 복원됩니다
- 색상 코드는 반드시 `#RRGGBB` 형식을 사용하세요

## 백업

중요한 설정을 변경하기 전에 `RenderSettings.json` 파일을 백업하는 것을 권장합니다.
