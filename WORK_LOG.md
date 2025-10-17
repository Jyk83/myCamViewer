# HK Laser Cutting Viewer - Work Log

## 프로젝트 개요
Three.js 기반 HK 레이저 절단 경로 시각화 뷰어

**저장소**: https://github.com/Jyk83/myCamViewer.git  
**브랜치**: master

---

## 완료된 작업 내역

### 1. 라벨 표시 개선 (Label Display Improvements)
**날짜**: 2025-10-17

#### 1.1 컨투어 라벨 NaN 문제 수정
- **문제**: 컨투어 라벨에 NaN 표시
- **원인**: ArcSegment 타입에 i, j 필드 누락
- **해결**: 
  - `src/types/index.ts`: ArcSegment 인터페이스에 i, j 필드 추가
  - `src/parser/MPFParser.ts`: 파싱 시 i, j 값 포함
- **커밋**: 타입 정의 및 파서 수정

#### 1.2 원호 바운딩 박스 계산 수정
- **문제**: 라벨이 원 내부에 표시됨
- **원인**: 원호의 시작/끝점만 사용하여 바운딩 박스 계산
- **해결**: 원의 중심점 ± 반지름으로 정확한 바운딩 박스 계산
- **위치**: `src/viewer/LaserViewer.tsx` - calculateActualBoundingBox 함수
```typescript
if (segment.type === 'arc') {
  const centerX = segment.center.x;
  const centerY = segment.center.y;
  const radius = segment.radius;
  
  addPoint(centerX - radius, centerY - radius);
  addPoint(centerX + radius, centerY - radius);
  addPoint(centerX - radius, centerY + radius);
  addPoint(centerX + radius, centerY + radius);
}
```

#### 1.3 라벨 크기 및 가시성 개선
- **파트 라벨**: fontSize 24, scale 12x6
- **컨투어 라벨**: 
  - 위치: topY + 2 (상단 중앙 위 2mm 마진)
  - 크기: 사용자 조정 가능 (6-48, 기본값 24)
  - 스케일: fontSize / 3 비례
- **텍스트 외곽선**: 검정색 6px stroke로 가독성 향상

#### 1.4 사용자 조정 가능한 컨투어 라벨 크기
- **UI**: 슬라이더 추가 (범위: 6-48, 스텝: 2)
- **상태 관리**: App.tsx에서 contourLabelSize state 관리
- **동적 적용**: 라벨 크기 실시간 반영

---

### 2. 줌 기능 개선 (Zoom Functionality)
**날짜**: 2025-10-17

#### 2.1 마우스 포인터 중심 줌 구현
- **기능**: 마우스 휠로 마우스 위치 기준 확대/축소
- **구현**: 
  - OrbitControls 기본 줌 비활성화
  - 커스텀 wheel 이벤트 핸들러 추가
  - 마우스 월드 좌표 계산 및 카메라 조정

#### 2.2 2D 직교 투영 줌 수정 (1차)
- **문제**: 3D 스타일 줌 (vector.unproject 사용)
- **해결**: 2D 직교 투영 방식으로 변경
  - 카메라 frustum과 target 기반 월드 좌표 계산
  - 3D 깊이 계산 제거
- **커밋**: e957dac

#### 2.3 2D 줌 비틀림 현상 수정 (2차)
- **문제**: 마우스가 중앙에서 벗어나면 줌이 비틀림
- **원인**: controls.target 기준으로 좌표 계산
- **해결**: camera.position 기준으로 변경
  - 카메라와 타겟을 함께 이동
  - 순수 2D 평면 계산
```typescript
const mouseWorldX = camera.position.x + (mouseX * oldWidth) / 2;
const mouseWorldY = camera.position.y + (mouseY * oldHeight) / 2;

// 줌 후 위치 조정
camera.position.x = newCameraX;
camera.position.y = newCameraY;
controls.target.x = newCameraX;
controls.target.y = newCameraY;
```
- **커밋**: 5f1f3f7
- **평가**: ✅ 완벽함. 퍼펙트.

---

### 3. 디버그 바운딩 박스 기능 (Debug Bounding Box)
**날짜**: 2025-10-17

#### 3.1 하드코딩 제거 및 UI 추가
- **이전**: 컨투어 6번만 하드코딩으로 바운딩 박스 표시
- **개선**: 사용자 선택 가능
  - 체크박스: 바운딩 박스 표시 on/off
  - 입력 필드: 파트 번호 (기본값: 1)
  - 입력 필드: 컨투어 번호 (기본값: 6)
- **디버그 정보**: window.debugContourInfo에 저장
- **커밋**: c359c24

#### 3.2 표시 제어 개선
**파트 바운딩 박스 연동**
- 파트 번호 체크박스와 파트 바운딩 박스(노란색 점선) 연동
- 체크 시: 박스 + 텍스트 표시
- 체크 해제: 둘 다 숨김

**디버그 박스 연동**
- 컨투어 번호 체크박스와 디버그 옵션 연동
- 컨투어 번호 체크 해제 → 디버그 섹션 숨김 + 자동 해제
- 다시 켰을 때: 디버그 박스는 체크 해제 상태 유지
- **핸들러**: handleToggleContourLabels in App.tsx
- **커밋**: c2194bb

---

### 4. 워크피스 시각화 개선 (Workpiece Visualization)
**날짜**: 2025-10-17

#### 4.1 배경 및 자재 영역 구분
- **이전**: 
  - 전체 화면 청록색 배경
  - 노란색 점선으로 워크피스 테두리
- **개선**:
  - 씬 배경: 검정색 (0x000000) - 자재 외부
  - 워크피스: PlaneGeometry로 채워진 사각형 (Colors.workpiece) - 실제 자재
  - 점선 테두리 제거
```typescript
scene.background = new THREE.Color(0x000000);

const geometry = new THREE.PlaneGeometry(width, height);
const material = new THREE.MeshBasicMaterial({ 
  color: new THREE.Color(Colors.workpiece),
  side: THREE.DoubleSide 
});
```
- **효과**: 자재 영역과 외부 공간이 명확하게 구분
- **커밋**: 596b6ec

---

### 5. 파트 번호 위치 조정 (Part Label Position)
**날짜**: 2025-10-17

#### 5.1 오프셋 적용
- **이전**: 바운딩 박스 정중앙 (partWidth/2, partHeight/2)
- **개선**: 오프셋 적용 (partWidth/2 - 5, partHeight/2 + 5)
  - X축: -5 (왼쪽으로)
  - Y축: +5 (위쪽으로)
- **효과**: 가독성 개선, 다른 요소와 겹침 방지
- **커밋**: 2a08586

---

## 기술 스택

### Frontend
- **React** + TypeScript
- **Three.js** (3D/2D 시각화)
  - OrthographicCamera (2D 뷰)
  - OrbitControls (카메라 제어)
  - Canvas 기반 텍스트 스프라이트
- **Vite** (빌드 도구)

### 핵심 구조
```
src/
├── types/index.ts          # 타입 정의 (MPFProgram, Part, Contour, Segment 등)
├── parser/MPFParser.ts     # G-code 파싱 (G1/G2/G3 명령어)
├── viewer/LaserViewer.tsx  # Three.js 시각화 컴포넌트
├── components/
│   ├── ViewControls.tsx    # 뷰어 옵션 UI
│   ├── ProgramInfo.tsx     # 프로그램 정보 표시
│   ├── PartsList.tsx       # 파트 목록
│   └── FileUploader.tsx    # 파일 업로드
└── App.tsx                 # 메인 애플리케이션
```

---

## 현재 기능

### 파일 파싱
- ✅ MPF 파일 로드 및 파싱
- ✅ G-code 명령어 해석 (G0/G1/G2/G3)
- ✅ 원호 중심점 계산 (I/J 오프셋)
- ✅ 바운딩 박스 계산

### 시각화
- ✅ 2D 직교 투영 뷰
- ✅ 워크피스 표시 (채워진 사각형)
- ✅ 그리드 표시
- ✅ 파트 및 컨투어 경로 표시
  - 피어싱 위치 (빨간 점)
  - 리드인 경로 (주황색)
  - 어프로치 경로 (청록색)
  - 절단 경로 (파란색/흰색)
- ✅ 파트 번호 라벨 (노란색, 오프셋 적용)
- ✅ 컨투어 번호 라벨 (흰색, 크기 조정 가능)
- ✅ 파트 바운딩 박스 (노란색 점선)

### 카메라 제어
- ✅ 마우스 왼쪽 버튼: 팬 (이동)
- ✅ 마우스 휠: 마우스 중심 줌
- ✅ 마우스 오른쪽 버튼: 회전 (3D 모드)
- ✅ 2D 직교 투영 줌 (비틀림 없음)

### UI 컨트롤
- ✅ 경로 타입별 표시 on/off
  - 피어싱, 리드인, 어프로치, 절단
- ✅ 라벨 표시 on/off
  - 파트 번호 (+ 바운딩 박스)
  - 컨투어 번호
- ✅ 컨투어 라벨 크기 슬라이더 (6-48)
- ✅ 디버그 바운딩 박스 (파트/컨투어 선택)

---

## 다음 작업 계획

### Phase 1: 시뮬레이션 기능 (Simulation Mode)
**목표**: 레이저 절단 가공 상태를 실시간 반영하여 뷰어에 표시

#### 1.1 시뮬레이션 UI 추가
- [ ] 시뮬레이션 컨트롤 패널 추가
  - [ ] 재생/일시정지 버튼
  - [ ] 정지/리셋 버튼
  - [ ] 속도 조절 슬라이더 (시간 간격 선택: 50ms, 100ms, 200ms, 500ms)
  - [ ] 진행 상태 표시 (현재 파트/컨투어, 진행률)

#### 1.2 경로 트레이스 구현
- [ ] 경로 세그먼트 분할
  - [ ] 직선(G1): 1mm 단위로 분할
  - [ ] 원호(G2/G3): 1mm 호장 단위로 분할
  - [ ] 각 세그먼트의 시작/끝 좌표 및 방향 계산
  
- [ ] 절단 순서 관리
  - [ ] 파트 1 → 파트 N 순서
  - [ ] 각 파트 내: 컨투어 1 → 컨투어 N 순서
  - [ ] 각 컨투어 내: 피어싱 → 리드인 → 어프로치 → 절단 순서

#### 1.3 시각적 트레이스 표현
- [ ] 진행 구간 하이라이트
  - [ ] 완료된 구간: 빨간색으로 표시
  - [ ] 현재 절단 위치: 밝은 빨간색/발광 효과
  - [ ] 미진행 구간: 기존 색상 유지
  
- [ ] 레이저 헤드 표시
  - [ ] 현재 위치에 레이저 헤드 마커 표시
  - [ ] 이동 방향 화살표 표시 (선택적)

#### 1.4 시뮬레이션 상태 관리
```typescript
interface SimulationState {
  isRunning: boolean;
  isPaused: boolean;
  currentPartIndex: number;
  currentContourIndex: number;
  currentSegmentIndex: number;
  currentProgress: number; // 0.0 ~ 1.0
  speed: number; // ms per step (100, 200, 500...)
  completedPaths: Set<string>; // "part-1-contour-3-segment-5"
}
```

#### 1.5 시뮬레이션 로직
- [ ] 타이머 기반 진행 (setInterval/requestAnimationFrame)
- [ ] 1mm 단위 이동 계산
- [ ] 경로 완료 시 다음 세그먼트로 이동
- [ ] 컨투어 완료 시 다음 컨투어로 이동
- [ ] 파트 완료 시 다음 파트로 이동
- [ ] 전체 완료 시 시뮬레이션 종료

#### 1.6 Three.js 렌더링 최적화
- [ ] 동적 색상 변경 (완료 구간만 재렌더링)
- [ ] BufferGeometry 업데이트 최적화
- [ ] 필요 시 인스턴싱 활용

---

### Phase 2: 실시간 데이터 연동 (Real-time Data Integration)
**목표**: 실제 레이저 절단기 데이터와 연동하여 실시간 트레이스

#### 2.1 데이터 통신 인터페이스 설계
- [ ] 통신 프로토콜 정의
  - [ ] WebSocket / REST API / Serial 통신 선택
  - [ ] 데이터 포맷 정의 (JSON, Binary 등)
  
- [ ] 데이터 구조 정의
```typescript
interface RealTimeData {
  timestamp: number;
  currentPosition: { x: number; y: number; z: number };
  currentPart: number;
  currentContour: number;
  laserState: 'off' | 'piercing' | 'cutting' | 'marking';
  feedRate: number; // mm/min
  powerLevel: number; // %
}
```

#### 2.2 데이터 수신 및 파싱
- [ ] WebSocket 클라이언트 구현
- [ ] 데이터 버퍼링 및 동기화
- [ ] 에러 처리 및 재연결 로직

#### 2.3 실시간 뷰어 업데이트
- [ ] 수신 데이터 기반 렌더링
- [ ] 지연 시간(latency) 처리
- [ ] 프레임 드롭 방지

#### 2.4 실시간 정보 표시
- [ ] 현재 위치 좌표 표시
- [ ] 레이저 상태 표시
- [ ] 가공 속도 표시
- [ ] 예상 완료 시간 표시

---

## 기술적 고려사항

### 시뮬레이션 성능
- **경로 세그먼트 수**: 수천~수만 개 예상
- **렌더링 최적화**: 
  - 변경된 세그먼트만 업데이트
  - BufferGeometry의 attributes.color 동적 수정
  - 필요 시 LOD(Level of Detail) 적용

### 1mm 단위 계산
- **직선**: 
  ```typescript
  const length = Math.sqrt(dx*dx + dy*dy);
  const numSteps = Math.ceil(length);
  for (let i = 0; i <= numSteps; i++) {
    const t = i / numSteps;
    const point = { x: startX + t * dx, y: startY + t * dy };
  }
  ```
  
- **원호**: 
  ```typescript
  const arcLength = Math.abs(endAngle - startAngle) * radius;
  const numSteps = Math.ceil(arcLength);
  for (let i = 0; i <= numSteps; i++) {
    const t = i / numSteps;
    const angle = startAngle + t * (endAngle - startAngle);
    const point = { 
      x: centerX + radius * Math.cos(angle),
      y: centerY + radius * Math.sin(angle)
    };
  }
  ```

### 색상 변경 전략
**옵션 1**: 세그먼트별 Line 객체 생성
- 장점: 간단한 구현
- 단점: 객체 수 증가로 성능 저하 가능

**옵션 2**: BufferGeometry attributes.color 동적 수정
- 장점: 성능 최적화
- 단점: 구현 복잡도 증가
```typescript
const colors = new Float32Array(positions.length);
geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
material.vertexColors = true;

// 업데이트
geometry.attributes.color.needsUpdate = true;
```

**권장**: Phase 1에서는 옵션 1, 성능 이슈 발생 시 옵션 2로 마이그레이션

---

## Git 워크플로우

### 커밋 컨벤션
```
feat: 새로운 기능 추가
fix: 버그 수정
refactor: 코드 리팩토링
style: 코드 포맷팅, 세미콜론 등
docs: 문서 수정
test: 테스트 코드
chore: 빌드, 설정 파일 수정
```

### 최근 커밋 히스토리
- `2a08586` - feat: add offset to part number label position
- `596b6ec` - feat: improve workpiece visualization with solid background
- `c2194bb` - feat: improve part and debug bounding box visibility control
- `c359c24` - feat: add configurable debug bounding box visualization
- `5f1f3f7` - fix: correct 2D orthographic zoom to use camera position
- `e957dac` - fix: implement proper 2D orthographic zoom behavior

---

## 참고 자료

### Three.js 문서
- [OrthographicCamera](https://threejs.org/docs/#api/en/cameras/OrthographicCamera)
- [OrbitControls](https://threejs.org/docs/#examples/en/controls/OrbitControls)
- [BufferGeometry](https://threejs.org/docs/#api/en/core/BufferGeometry)
- [Line](https://threejs.org/docs/#api/en/objects/Line)
- [Sprite](https://threejs.org/docs/#api/en/objects/Sprite)

### G-code 참고
- G0: 급속 이동
- G1: 직선 보간
- G2: 시계방향 원호 보간
- G3: 반시계방향 원호 보간
- I/J: 원호 중심점 오프셋 (상대 좌표)

---

## 개발 환경

### 실행 명령어
```bash
# 개발 서버 시작
cd /home/user/webapp/hk-laser-viewer && npm run dev

# 빌드
npm run build

# 프리뷰
npm run preview
```

### 포트
- 개발 서버: 기본 5173 (Vite)

---

## 문제 해결 기록

### 1. 컨투어 라벨 NaN 표시
- **원인**: ArcSegment 타입에 i, j 필드 누락
- **해결**: 타입 정의 추가 및 파서 수정

### 2. 원 내부에 라벨 표시
- **원인**: 원호의 시작/끝점만 사용한 바운딩 박스 계산
- **해결**: 중심점 ± 반지름으로 정확한 계산

### 3. 3D 스타일 줌 동작
- **원인**: vector.unproject() 사용 (3D 깊이 계산)
- **해결**: camera.position 기반 2D 좌표 계산

### 4. 마우스 위치에서 줌 비틀림
- **원인**: controls.target 기준 좌표 계산
- **해결**: camera.position 기준으로 변경, 카메라와 타겟 동시 이동

---

## 메모

### 전역 디버그 변수
- `window.lastProgram`: 파싱된 MPF 프로그램 데이터
- `window.debugContourInfo`: 디버그 선택된 컨투어 정보
  - partNumber, contourNumber
  - width, height, bbox
  - centerX, topY

### 색상 정의 (Colors)
```typescript
export const Colors = {
  workpiece: 0x1a3d3d,        // 워크피스 (어두운 청록색)
  workpieceBorder: 0xffeb3b,  // 워크피스 테두리 (노란색) - 현재 미사용
  grid: 0x555555,             // 그리드 (회색)
  partOrigin: 0xff0000,       // 파트 원점 (빨간색) - 현재 미사용
  partLabel: 0xffeb3b,        // 파트 라벨 (노란색)
  contourLabel: 0xffffff,     // 컨투어 라벨 (흰색)
  piercing: 0xff6b6b,         // 피어싱 (밝은 빨강)
  leadIn: 0xffa500,           // 리드인 (주황색)
  approach: 0x4ecdc4,         // 어프로치 (청록색)
  cutting: 0x2196f3,          // 절단 (파란색)
  marking: 0xffffff,          // 마킹 (흰색)
};
```

---

## 작업 완료 체크리스트

### ✅ 완료
- [x] MPF 파일 파싱
- [x] 2D 시각화 기본 구현
- [x] 라벨 표시 (파트/컨투어)
- [x] 마우스 중심 줌
- [x] 2D 직교 투영 줌 수정
- [x] 디버그 바운딩 박스
- [x] 워크피스 시각화 개선
- [x] 파트 번호 위치 조정

### 🚧 진행 예정
- [ ] 시뮬레이션 UI
- [ ] 경로 트레이스 구현
- [ ] 실시간 데이터 연동

---

**최종 업데이트**: 2025-10-17  
**현재 커밋**: 2a08586  
**다음 작업**: 시뮬레이션 기능 구현
