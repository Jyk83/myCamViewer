/**
 * Path Segmentation Utilities
 * 경로 세그먼트 분할 유틸리티 (기본 4mm 단위)
 */

import type { PathSegment, PathPoint, Point2D } from '../types';

/**
 * 기본 분할 단위 (mm)
 */
export const DEFAULT_STEP_SIZE = 4;

/**
 * 직선 세그먼트를 지정된 단위로 분할
 * @param stepSize 분할 단위 (mm), 기본값 4mm
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
 * @param stepSize 분할 단위 (mm), 기본값 4mm
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

  // 지정된 단위로 분할
  const numSteps = Math.ceil(arcLength / stepSize);
  const points: PathPoint[] = [];

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
 * @param stepSize 분할 단위 (mm), 기본값 4mm
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
 * @param stepSize 분할 단위 (mm), 기본값 4mm
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

  segments.forEach((segment, index) => {
    const points = segmentPath(segment, index, partIndex, contourIndex, pathType, laserOn, stepSize);
    allPoints.push(...points);
  });

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
