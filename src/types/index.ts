/**
 * HK Laser Cutting Viewer - Type Definitions
 * HK 레이저 절단 뷰어 타입 정의
 */

// ========== HK 함수 타입 ==========

export interface HKLDBCommand {
  type: 'HKLDB';
  material: number;
  dbName: string;
  assistGas: number;
}

export interface HKINICommand {
  type: 'HKINI';
  totalParts: number;
  width: number;
  height: number;
}

export interface HKOSTCommand {
  type: 'HKOST';
  x: number;
  y: number;
  rotation: number;
  partNumber: number;
  contourCount: number;
}

export interface HKSTRCommand {
  type: 'HKSTR';
  piercingType: number;       // 0: 피어싱 없음 (내부 컨투어 등), 1: 일반 피어싱
  cuttingType: number;        // 1: 일반 절단, 2: 펄스 절단, 10: 마킹(각인), 11: 반복 절단
  x: number;                  // 컨투어 시작 X좌표 (피어싱 위치)
  y: number;                  // 컨투어 시작 Y좌표 (피어싱 위치)
  toolCompensation: number;   // 공구 보정 (0: 없음, 7: 왼쪽, 8: 오른쪽)
  contourWidth: number;       // 컨투어 바운딩 박스 폭
  contourHeight: number;      // 컨투어 바운딩 박스 높이
}

export interface HKPIECommand {
  type: 'HKPIE';
}

export interface HKLEACommand {
  type: 'HKLEA';
  gCode: number;
  x: number;
  y: number;
  i: number;
  j: number;
}

export interface HKCUTCommand {
  type: 'HKCUT';
}

export interface HKSTOCommand {
  type: 'HKSTO';
  gCode: number;      // 0: 추가 구획 없음, 1: G1 직선, 2: G2 시계방향 원호, 3: G3 반시계방향 원호
  x: number;          // 리드인 종점의 X좌표
  y: number;          // 리드인 종점의 Y좌표
  i: number;          // 리드인 종점의 I좌표 (원호 중심의 X 오프셋)
  j: number;          // 리드인 종점의 J좌표 (원호 중심의 Y 오프셋)
  webOnOff: number;   // 0: CAM에서 마이크로 웹을 사용하지 않음, 1: CAM에서 마이크로 웹을 사용한 경우
}

export interface HKPEDCommand {
  type: 'HKPED';
}

export interface HKENDCommand {
  type: 'HKEND';
}

export interface HKSCRCCommand {
  type: 'HKSCRC';
  cuttingType?: number;
  cuttingKind?: number;
  x?: number;
  y?: number;
  params?: number;
}

export type HKCommand =
  | HKLDBCommand
  | HKINICommand
  | HKOSTCommand
  | HKSTRCommand
  | HKPIECommand
  | HKLEACommand
  | HKCUTCommand
  | HKSTOCommand
  | HKPEDCommand
  | HKENDCommand
  | HKSCRCCommand;

// ========== G-code 타입 ==========

export interface GCodeCommand {
  type: 'GCODE';
  command: string; // G0, G1, G2, G3, M30 등
  x?: number;
  y?: number;
  z?: number;
  i?: number;
  j?: number;
  f?: number; // Feed rate
}

export interface NBlockCommand {
  type: 'NBLOCK';
  blockNumber: number;
}

export interface CommentCommand {
  type: 'COMMENT';
  text: string;
}

export type Command = HKCommand | GCodeCommand | NBlockCommand | CommentCommand;

// ========== 좌표 및 경로 타입 ==========

export interface Point2D {
  x: number;
  y: number;
}

export interface Point3D extends Point2D {
  z: number;
}

export interface LineSegment {
  type: 'line';
  start: Point2D;
  end: Point2D;
}

export interface ArcSegment {
  type: 'arc';
  start: Point2D;
  end: Point2D;
  center: Point2D;
  radius: number;
  clockwise: boolean; // G2: true (CW), G3: false (CCW)
  startAngle: number;
  endAngle: number;
  i: number; // X축 오프셋 (중심점까지의 X 거리)
  j: number; // Y축 오프셋 (중심점까지의 Y 거리)
}

export type PathSegment = LineSegment | ArcSegment;

// ========== 컨투어 타입 ==========

export interface Contour {
  id: string;
  blockNumber: number;
  
  // HKSTR 정보
  piercingType: number;
  cuttingType: number;
  piercingPosition: Point2D;
  toolCompensation: number;
  boundingBox: {
    width: number;
    height: number;
  };
  
  // Lead-in 정보
  leadIn?: {
    gCode: number;
    path: PathSegment[];
  };
  
  // Approach & Cutting path
  approachPath: PathSegment[];
  cuttingPath: PathSegment[];
  
  // HKSTO 정보
  endGCode: number;
  endPosition: Point2D;
  
  // 전체 경로 (시각화용)
  allSegments: PathSegment[];
}

// ========== 파트 타입 ==========

export interface Part {
  id: string;
  blockNumber: number;
  origin: Point2D;
  rotation: number;
  contours: Contour[];
  boundingBox?: {
    min: Point2D;
    max: Point2D;
  };
}

// ========== 네스팅 정보 ==========

export interface NestingInfo {
  partOriginBlockNumber: number;
  origin: Point2D;
  rotation: number;
  partCodeBlockNumber: number;
  contourCount: number;
}

// ========== MPF 프로그램 전체 구조 ==========

export interface MPFProgram {
  version: string;
  
  // 초기화 정보
  hkldb: HKLDBCommand;
  hkini: HKINICommand;
  
  // 네스팅 정보
  nesting: NestingInfo[];
  
  // 파트 정보
  parts: Part[];
  
  // 워크피스 정보
  workpiece: {
    width: number;
    height: number;
  };
  
  // 원본 커맨드 (디버깅용)
  rawCommands?: Command[];
}

// ========== 재질 및 가스 정보 ==========

export const MaterialTypes: Record<number, string> = {
  1: 'MS (Mild Steel)',
  2: 'STS (Stainless Steel)',
  3: 'AL (Aluminum)',
  4: 'BRASS',
  5: 'COPPER',
  9: 'USER',
};

export const AssistGasTypes: Record<number, string> = {
  1: 'O2 (Oxygen)',
  2: 'N2 (Nitrogen)',
  3: 'AIR',
};

export const PiercingTypes: Record<number, string> = {
  0: 'No Piercing',
  1: 'Normal Piercing',
  4: 'User Defined',
  10: 'Shot Marking',
  11: 'Repeat Piercing',
};

export const CuttingTypes: Record<number, string> = {
  0: 'None',
  1: 'Normal Cutting',
  2: 'Pulse Cutting',
  10: 'Marking (Engraving)',
  11: 'Repeat Cutting',
};

// ========== 시뮬레이션 타입 ==========

/**
 * 분할된 미세 경로 포인트 (기본 10mm 단위)
 */
export interface PathPoint {
  position: Point2D;
  partIndex: number;          // 파트 인덱스 (0부터 시작)
  contourIndex: number;       // 컨투어 인덱스 (0부터 시작)
  segmentIndex: number;       // 원본 세그먼트 인덱스
  progress: number;           // 세그먼트 내 진행률 (0.0 ~ 1.0)
  laserOn: boolean;           // 레이저 상태 (G0: false, G1/G2/G3: true)
  pathType: 'piercing' | 'leadIn' | 'approach' | 'cutting';
}

/**
 * 시뮬레이션 상태
 */
export interface SimulationState {
  isRunning: boolean;         // 실행 중
  isPaused: boolean;          // 일시정지
  currentPartIndex: number;   // 현재 파트 (0부터 시작)
  currentContourIndex: number;// 현재 컨투어 (0부터 시작)
  currentPointIndex: number;  // 현재 포인트 (0부터 시작)
  currentDistance: number;    // 현재 진행 거리 (mm)
  totalDistance: number;      // 전체 경로 거리 (mm)
  speed: number;              // ms per step (100, 200, 500...)
  stepSize: number;           // 타이머당 이동 거리 (mm, 기본 10mm, 최대 100mm)
  completedPaths: Set<string>; // "part-0-contour-1-point-50"
}

/**
 * 경로 ID 생성 헬퍼
 */
export function createPathId(partIndex: number, contourIndex: number, pointIndex: number): string {
  return `part-${partIndex}-contour-${contourIndex}-point-${pointIndex}`;
}

// ========== 색상 정의 ==========

export const Colors = {
  workpiece: '#1a3a3a',        // 어두운 청록색 배경
  workpieceBorder: '#66d9d9',  // 밝은 청록색 테두리 (점선)
  partOrigin: '#ff3333',       // 빨간색 원점
  piercing: '#ff3333',         // 빨간색 피어싱 포인트
  leadIn: '#3366ff',           // 파란색 Lead-in (HKLEA 경로)
  approach: '#3366ff',         // 파란색 Approach
  cutting: '#ffffff',          // 흰색 절단 경로 (HKCUT 이후)
  marking: '#ffff00',          // 노란색 마킹 (cuttingType=10)
  selected: '#ffff00',         // 노란색 선택
  grid: '#2a4a4a',             // 어두운 그리드
  partLabel: '#ffff00',        // 노란색 파트 레이블
  contourLabel: '#ffffff',     // 흰색 컨투어 레이블
  simulated: '#ff0000',        // 빨간색 시뮬레이션 완료 경로
  laserHead: '#ffff00',        // 노란색 레이저 헤드
};
