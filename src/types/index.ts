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
  piercingType: number;
  cuttingType: number;
  x: number;
  y: number;
  toolCompensation: number;
  contourWidth: number;
  contourHeight: number;
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
  gCode: number;
  x: number;
  y: number;
  i: number;
  j: number;
  webOnOff: number;
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

// ========== 색상 정의 ==========

export const Colors = {
  workpiece: '#f0f0f0',
  workpieceBorder: '#333333',
  partOrigin: '#ff0000',
  piercing: '#ff6b6b',
  leadIn: '#ffa500',
  approach: '#4ecdc4',
  cutting: '#2196f3',
  marking: '#9c27b0',
  selected: '#ffeb3b',
  grid: '#e0e0e0',
};
