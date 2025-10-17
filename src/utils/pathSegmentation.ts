/**
 * Path Segmentation Utilities
 * 경로 세그먼트 분할 유틸리티 (기본 10mm 단위)
 */

import type { PathSegment, PathPoint, Point2D } from '../types';

/**
 * 기본 분할 단위 (mm)
 */
export const DEFAULT_STEP_SIZE = 10;

/**
 * 직선 세그먼트를 지정된 단위로 분할
 * @param stepSize 분할 단위 (mm), 기본값 10mm
 */
export function segmentLine(
  segment: PathSegment,
  segmentIndex: number,
  partIndex: number,
  contourIndex: number,
  pathType: 'piercing' | 'leadIn' | 'approach' | 'cutting',
  laserOn: boolean = true,
  stepSize: number = DEFAULT_STEP_SIZE
): PathPoint[] {
  if (segment.type !== 'line') return [];

  const { start, end } = segment;
  const dx = end.x - start.x;
  const dy = end.y - start.y;
  const length = Math.sqrt(dx * dx + dy * dy);

  // 지정된 단위로 분할
  const numSteps = Math.ceil(length / stepSize);
  const points: PathPoint[] = [];

  for (let i = 0; i <= numSteps; i++) {
    const t = numSteps > 0 ? i / numSteps : 0;
    points.push({
      position: {
        x: start.x + t * dx,
        y: start.y + t * dy,
      },
      partIndex,
      contourIndex,
      segmentIndex,
      progress: t,
      laserOn,
      pathType,
    });
  }

  return points;
}

/**
 * 원호 세그먼트를 지정된 단위로 분할
 * @param stepSize 분할 단위 (mm), 기본값 10mm
 */
export function segmentArc(
  segment: PathSegment,
  segmentIndex: number,
  partIndex: number,
  contourIndex: number,
  pathType: 'piercing' | 'leadIn' | 'approach' | 'cutting',
  laserOn: boolean = true,
  stepSize: number = DEFAULT_STEP_SIZE
): PathPoint[] {
  if (segment.type !== 'arc') return [];

  const { center, radius, startAngle, endAngle, clockwise } = segment;

  // 호장 길이 계산
  let angleSpan = endAngle - startAngle;
  if (clockwise) {
    if (angleSpan > 0) angleSpan -= 2 * Math.PI;
  } else {
    if (angleSpan < 0) angleSpan += 2 * Math.PI;
  }
  const arcLength = Math.abs(angleSpan * radius);

  // 원호는 두 가지 기준으로 분할:
  // 1. 거리 기준: arcLength / stepSize
  // 2. 각도 기준: 최소 5도마다 1개 포인트 (매끄러운 곡선)
  const stepsByDistance = Math.ceil(arcLength / stepSize);
  const stepsByAngle = Math.ceil(Math.abs(angleSpan) / (5 * Math.PI / 180)); // 5도마다
  const minSteps = 8; // 원호는 최소 8개 포인트 (매끄러운 곡선 보장)
  
  // 세 가지 중 가장 큰 값 사용 (가장 세밀한 분할)
  const numSteps = Math.max(stepsByDistance, stepsByAngle, minSteps);
  const points: PathPoint[] = [];

  // 디버그 로그 (처음 3개 원호만)
  if (segmentIndex < 3) {
    console.log(`🔵 원호 세그먼트 분할: Part${partIndex} Cont${contourIndex} Seg${segmentIndex}`);
    console.log(`  중심: (${center.x.toFixed(2)}, ${center.y.toFixed(2)}), 반지름: ${radius.toFixed(2)}`);
    console.log(`  각도: ${(startAngle * 180 / Math.PI).toFixed(1)}° → ${(endAngle * 180 / Math.PI).toFixed(1)}° (${(Math.abs(angleSpan) * 180 / Math.PI).toFixed(1)}°)`);
    console.log(`  방향: ${clockwise ? '시계(G2)' : '반시계(G3)'}`);
    console.log(`  호장 길이: ${arcLength.toFixed(2)}mm`);
    console.log(`  분할 기준: 거리=${stepsByDistance}, 각도=${stepsByAngle}, 최소=${minSteps} → 사용=${numSteps}`);
    console.log(`  생성 포인트: ${numSteps+1}개`);
    console.log(`  레이저: ${laserOn ? 'ON' : 'OFF'}, 경로타입: ${pathType}`);
  }

  for (let i = 0; i <= numSteps; i++) {
    const t = numSteps > 0 ? i / numSteps : 0;
    const angle = startAngle + t * angleSpan;
    points.push({
      position: {
        x: center.x + radius * Math.cos(angle),
        y: center.y + radius * Math.sin(angle),
      },
      partIndex,
      contourIndex,
      segmentIndex,
      progress: t,
      laserOn,
      pathType,
    });
  }

  return points;
}

/**
 * 세그먼트를 지정된 단위로 분할 (자동 타입 감지)
 * @param stepSize 분할 단위 (mm), 기본값 10mm
 */
export function segmentPath(
  segment: PathSegment,
  segmentIndex: number,
  partIndex: number,
  contourIndex: number,
  pathType: 'piercing' | 'leadIn' | 'approach' | 'cutting',
  laserOn: boolean = true,
  stepSize: number = DEFAULT_STEP_SIZE
): PathPoint[] {
  if (segment.type === 'line') {
    return segmentLine(segment, segmentIndex, partIndex, contourIndex, pathType, laserOn, stepSize);
  } else if (segment.type === 'arc') {
    return segmentArc(segment, segmentIndex, partIndex, contourIndex, pathType, laserOn, stepSize);
  }
  return [];
}

/**
 * 여러 세그먼트를 순차적으로 지정된 단위로 분할
 * pathType, laserOn, stepSize를 인자로 받는 버전
 * @param stepSize 분할 단위 (mm), 기본값 10mm
 */
export function segmentPathArray(
  segments: PathSegment[],
  partIndex: number,
  contourIndex: number,
  pathType: 'piercing' | 'leadIn' | 'approach' | 'cutting' = 'cutting',
  laserOn: boolean = true,
  stepSize: number = DEFAULT_STEP_SIZE
): PathPoint[] {
  const allPoints: PathPoint[] = [];

  // 디버그: 원호 세그먼트 카운트
  const arcCount = segments.filter(s => s.type === 'arc').length;
  if (arcCount > 0 && partIndex === 0 && contourIndex < 5) {
    console.log(`📊 Part${partIndex} Cont${contourIndex} ${pathType}: ${segments.length}개 세그먼트 (원호 ${arcCount}개)`);
  }

  segments.forEach((segment, index) => {
    const points = segmentPath(segment, index, partIndex, contourIndex, pathType, laserOn, stepSize);
    allPoints.push(...points);
    
    // 디버그: 원호 세그먼트가 생성한 포인트 개수
    if (segment.type === 'arc' && partIndex === 0 && contourIndex < 5) {
      console.log(`  🔹 Seg${index} (arc): ${points.length}개 포인트 생성`);
    }
  });

  if (arcCount > 0 && partIndex === 0 && contourIndex < 5) {
    console.log(`  ✅ 총 ${allPoints.length}개 포인트 생성됨`);
  }

  return allPoints;
}

/**
 * 두 점 사이의 거리 계산
 */
export function distance(p1: Point2D, p2: Point2D): number {
  const dx = p2.x - p1.x;
  const dy = p2.y - p1.y;
  return Math.sqrt(dx * dx + dy * dy);
}

/**
 * 경로 전체 길이 계산
 */
export function calculatePathLength(segments: PathSegment[]): number {
  let totalLength = 0;

  for (const segment of segments) {
    if (segment.type === 'line') {
      totalLength += distance(segment.start, segment.end);
    } else if (segment.type === 'arc') {
      let angleSpan = segment.endAngle - segment.startAngle;
      if (segment.clockwise) {
        if (angleSpan > 0) angleSpan -= 2 * Math.PI;
      } else {
        if (angleSpan < 0) angleSpan += 2 * Math.PI;
      }
      totalLength += Math.abs(angleSpan * segment.radius);
    }
  }

  return totalLength;
}
