# TraceViewer 좌표계 및 스케일 시스템

## 좌표계 개요

TraceViewer는 **1:1 스케일**을 사용합니다: **1mm = 1 Three.js unit**

### 기본 원칙

```
실제 물리적 거리 (mm) = Three.js 좌표 단위 (unit)
```

예시:
- G-code: `G1 X100 Y200` → Three.js: `Vector3(100, 200, z)`
- 100mm 직선 → `new THREE.Vector3(0,0,0)` to `new THREE.Vector3(100,0,0)`

---

## Three.js 카메라 설정

### OrthographicCamera (2D 뷰)

```typescript
const frustumSize = 500; // 초기 시야 크기 (500 Three.js units = 500mm)

const camera = new THREE.OrthographicCamera(
  (frustumSize * aspect) / -2,  // left
  (frustumSize * aspect) / 2,   // right
  frustumSize / 2,               // top
  frustumSize / -2,              // bottom
  0.1,                           // near
  1000                           // far
);
```

**의미**:
- 초기 화면에 약 500mm 영역이 보임
- 화면 높이 = 500mm
- 화면 폭 = 500mm * aspect ratio

### 워크피스 자동 맞춤 (fitCameraToWorkpiece)

```typescript
const frustumHeight = Math.max(
  height * 1.2,           // 워크피스 높이의 120%
  width * 1.2 / aspect    // 워크피스 폭을 고려한 높이
);

camera.top = frustumHeight / 2;
camera.bottom = -frustumHeight / 2;
```

**예시**:
- 워크피스: 640mm x 1021mm
- frustumHeight = max(1021 * 1.2, 640 * 1.2 / aspect)
- 결과: 전체 워크피스가 화면에 여유 있게 표시됨

---

## 픽셀과 mm 간의 관계

### 계산 방법

```
픽셀 밀도 = 화면 높이 (픽셀) / frustumHeight (mm)
```

**예시 1**: 화면 높이 800px, frustumHeight = 1000mm
```
픽셀 밀도 = 800 / 1000 = 0.8 pixel/mm
1mm = 0.8 픽셀
```

**예시 2**: 화면 높이 1200px, frustumHeight = 600mm (줌인 상태)
```
픽셀 밀도 = 1200 / 600 = 2 pixel/mm
1mm = 2 픽셀
```

### 줌 레벨에 따른 변화

```typescript
// 줌 아웃 (delta > 1): frustumSize 증가 → pixel/mm 감소
camera.top = camera.top * 1.1;  // 10% 줌 아웃

// 줌 인 (delta < 1): frustumSize 감소 → pixel/mm 증가
camera.top = camera.top * 0.9;  // 10% 줌 인
```

**최소/최대 줌**:
```typescript
const maxZoom = 2000;  // 최대 frustumSize = 2000mm (0.4px/mm @ 800px)
const minZoom = 10;    // 최소 frustumSize = 10mm (80px/mm @ 800px)
```

---

## 경로 좌표 시스템

### G-code → Three.js 직접 매핑

**G-code**:
```gcode
G1 X100.5 Y200.3
```

**PathPoint**:
```typescript
{
  position: { x: 100.5, y: 200.3 },  // mm 단위 그대로
  ...
}
```

**Three.js Rendering**:
```typescript
new THREE.Vector3(
  point.position.x,  // 100.5
  point.position.y,  // 200.3
  0.5                // Z 레이어
)
```

### 파트 원점 오프셋 (HKOST)

```typescript
// HKOST(10, 20, ...) → 파트 원점
const partOrigin = { x: 10, y: 20 };

// 컨투어 좌표에 원점 적용
point.position.x += partOrigin.x;  // 상대 좌표 → 절대 좌표
point.position.y += partOrigin.y;
```

---

## 거리 계산 (시뮬레이션)

### PathPoint 간 거리

```typescript
const dx = p2.position.x - p1.position.x;  // mm
const dy = p2.position.y - p1.position.y;  // mm
const distance = Math.sqrt(dx * dx + dy * dy);  // mm
```

### 전체 경로 거리

```typescript
let totalDistance = 0;
for (let i = 0; i < pathPoints.length - 1; i++) {
  const dx = pathPoints[i+1].position.x - pathPoints[i].position.x;
  const dy = pathPoints[i+1].position.y - pathPoints[i].position.y;
  totalDistance += Math.sqrt(dx * dx + dy * dy);
}
// totalDistance는 mm 단위
```

### 시뮬레이션 진행

```typescript
// stepSize = 10mm (타이머당 이동 거리)
// speed = 100ms (타이머 간격)
// 결과: 100ms마다 10mm 이동 = 100mm/s 속도
```

---

## 렌더링 요소 크기

### 레이저 헤드 마커

```typescript
const headGeometry = new THREE.CircleGeometry(3, 16);
// 반지름 = 3mm
// 지름 = 6mm
```

### 파트 원점 마커

```typescript
const originGeometry = new THREE.CircleGeometry(2, 16);
// 반지름 = 2mm
```

### 라벨 폰트 크기

```typescript
// 컨투어 라벨 크기 (조정 가능)
contourLabelSize: 24  // 픽셀 단위 (화면 고정)
// 주의: 폰트는 픽셀 단위이므로 줌과 무관
```

---

## 실제 장비 적용 시 고려사항

### 1. 좌표계 일치 확인

```typescript
// 장비 좌표 → Three.js 좌표
const deviceX = 123.45;  // mm
const deviceY = 678.90;  // mm

// 직접 사용 가능 (1:1 매핑)
new THREE.Vector3(deviceX, deviceY, 0);
```

### 2. 화면 크기에 따른 자동 조정

```typescript
// 워크피스 크기에 맞춰 자동 줌
fitCameraToWorkpiece(workpieceWidth, workpieceHeight);
```

### 3. 실시간 위치 업데이트

```typescript
// 장비에서 받은 현재 위치 (mm)
const currentPosition = { x: 123.45, y: 678.90 };

// PathPoint 배열에서 가장 가까운 포인트 찾기
let minDistance = Infinity;
let closestIndex = 0;

for (let i = 0; i < pathPoints.length; i++) {
  const dx = pathPoints[i].position.x - currentPosition.x;
  const dy = pathPoints[i].position.y - currentPosition.y;
  const dist = Math.sqrt(dx * dx + dy * dy);
  
  if (dist < minDistance) {
    minDistance = dist;
    closestIndex = i;
  }
}

// closestIndex로 레이저 헤드 표시
```

### 4. 누적 거리 계산

```typescript
// 시작점부터 현재 위치까지의 거리
let accumulatedDistance = 0;
for (let i = 0; i < closestIndex; i++) {
  const dx = pathPoints[i+1].position.x - pathPoints[i].position.x;
  const dy = pathPoints[i+1].position.y - pathPoints[i].position.y;
  accumulatedDistance += Math.sqrt(dx * dx + dy * dy);
}
// accumulatedDistance = 실시간 진행 거리 (mm)
```

---

## 요약

| 항목 | 값 | 비고 |
|------|-----|------|
| 좌표 스케일 | 1mm = 1 unit | G-code mm → Three.js unit 직접 매핑 |
| 초기 frustumSize | 500mm | 화면에 500mm 영역 표시 |
| 줌 범위 | 10mm ~ 2000mm | 최대 80px/mm ~ 최소 0.4px/mm |
| 거리 계산 | 피타고라스 정리 | √(dx² + dy²) mm |
| 시뮬레이션 | 거리 기반 | stepSize(mm) / speed(ms) |
| 실시간 트레이스 | 1:1 매핑 | 장비 mm → Three.js unit |

---

## 검증 방법

### 1. 좌표 확인

```javascript
// 브라우저 콘솔에서
console.log(window.lastProgram.parts[0].contours[0].cuttingPath[0]);
// 출력: { type: 'line', start: {x: 46.55, y: 36.55}, end: {x: 46.55, y: 23.55} }
// G-code의 좌표와 일치해야 함
```

### 2. 거리 측정

```javascript
// 10mm 직선 세그먼트 확인
const segment = { start: {x: 0, y: 0}, end: {x: 10, y: 0} };
const distance = Math.sqrt(
  Math.pow(segment.end.x - segment.start.x, 2) +
  Math.pow(segment.end.y - segment.start.y, 2)
);
console.log(distance); // 출력: 10 (mm)
```

### 3. 픽셀 밀도 확인

```javascript
// 현재 카메라 설정
const camera = cameraRef.current;
const frustumHeight = camera.top - camera.bottom;
const screenHeight = renderer.domElement.height;
const pixelPerMM = screenHeight / frustumHeight;
console.log(`1mm = ${pixelPerMM.toFixed(2)} pixels`);
```
