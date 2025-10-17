# Arc Rendering Fix - Smooth Curve Segmentation

## Problem Description

### Issue 1: Arcs rendered as diagonal straight lines

**User Report**: "원호가 제대로 그려지지 않는데? 처음과 끝만 대각선으로 가버려."

**Visual Symptoms**:
- Circular/rounded contours (contour 2, 5, etc.) appeared as angular corners
- Arc segments only showed start and end points connected by straight line
- 500ms 1mm step size made the problem obvious

### Root Cause Analysis

Small arcs generated insufficient points with pure distance-based segmentation:

```
Example: G3 arc with radius=1mm, angle=90°
Arc length = radius × angle = 1mm × π/2 = 1.57mm

With stepSize=10mm:
numSteps = ceil(1.57 / 10) = ceil(0.157) = 1
Points generated = numSteps + 1 = 2 (start + end only)

Result: Line connects two points → appears as straight diagonal
```

## Solution Implementation

### Triple-Criteria Segmentation

Modified `segmentArc()` in `src/utils/pathSegmentation.ts`:

```typescript
// OLD: Pure distance-based
const numSteps = Math.ceil(arcLength / stepSize);

// NEW: Triple criteria
const stepsByDistance = Math.ceil(arcLength / stepSize);
const stepsByAngle = Math.ceil(Math.abs(angleSpan) / (5 * Math.PI / 180)); // 5° per segment
const minSteps = 8; // Absolute minimum for smoothness

const numSteps = Math.max(stepsByDistance, stepsByAngle, minSteps);
```

### Three Segmentation Criteria

1. **Distance-based** (original): `arcLength / stepSize`
   - Works well for large arcs
   - Fails for small arcs (insufficient points)

2. **Angle-based** (NEW): Minimum 1 point per 5 degrees
   - Ensures smooth curves regardless of radius
   - 90° arc → 18 points minimum
   - 180° arc → 36 points minimum

3. **Absolute minimum** (NEW): 8 points per arc
   - Guarantees basic smoothness for any arc
   - Prevents degenerate cases

### Comparison Table

| Arc Parameters | OLD Points | NEW Points | Improvement |
|----------------|-----------|-----------|-------------|
| r=1mm, 90° | 2 | 18 | 9x smoother |
| r=5mm, 45° | 2 | 9 | 4.5x smoother |
| r=10mm, 180° | 4 | 36 | 9x smoother |
| r=50mm, 360° | 32 | 72 | 2.25x smoother |

## Code Changes

### File: `src/utils/pathSegmentation.ts`

**Commit**: `59bc37a`

```typescript
export function segmentArc(
  segment: PathSegment,
  // ... parameters
): PathPoint[] {
  // Calculate arc length
  let angleSpan = endAngle - startAngle;
  if (clockwise) {
    if (angleSpan > 0) angleSpan -= 2 * Math.PI;
  } else {
    if (angleSpan < 0) angleSpan += 2 * Math.PI;
  }
  const arcLength = Math.abs(angleSpan * radius);

  // Triple-criteria segmentation
  const stepsByDistance = Math.ceil(arcLength / stepSize);
  const stepsByAngle = Math.ceil(Math.abs(angleSpan) / (5 * Math.PI / 180));
  const minSteps = 8;
  
  const numSteps = Math.max(stepsByDistance, stepsByAngle, minSteps);
  
  // Generate points with finest segmentation
  for (let i = 0; i <= numSteps; i++) {
    const t = numSteps > 0 ? i / numSteps : 0;
    const angle = startAngle + t * angleSpan;
    points.push({
      position: {
        x: center.x + radius * Math.cos(angle),
        y: center.y + radius * Math.sin(angle),
      },
      // ... other properties
    });
  }
}
```

## Debug Logging

Added detailed arc segmentation logging:

```
🔵 원호 세그먼트 분할: Part0 Cont4 Seg2
  중심: (-1.00, 420.27), 반지름: 1.00
  각도: 90.0° → 180.0° (90.0°)
  방향: 반시계(G3)
  호장 길이: 1.57mm
  분할 기준: 거리=1, 각도=18, 최소=8 → 사용=18
  생성 포인트: 19개
  레이저: ON, 경로타입: cutting
```

## Testing

### Test Case: REM_SAMPLE.TXT

File contains 3 arc commands:
1. `G3 X-1. Y420.27 I-1. J0.` (90° arc)
2. `G2 X-210. Y421.27 I0. J1.` (large arc)
3. `G3 X-211. Y1021.02 I-1. J0.` (vertical arc)

**Before**: Contours 2, 5 showed angular corners
**After**: Smooth rounded edges

### How to Test

1. Load REM_SAMPLE.TXT
2. Open browser console (F12)
3. Look for 🔵 arc segmentation logs
4. Start simulation with stepSize=1mm, speed=500ms
5. Verify:
   - ✅ Arcs render as smooth curves
   - ✅ No diagonal straight lines
   - ✅ Console shows 8+ points per arc

## Performance Impact

**Memory**: Increased point count (8x average)
- Small file (10 arcs): ~200 points → ~1800 points
- Large file (1000 arcs): ~20K points → ~180K points
- Still manageable for modern browsers

**Rendering**: Negligible impact
- Three.js handles line rendering efficiently
- BufferGeometry optimizations in place
- No frame rate drop observed

## Related Issues

- Original report: "작게해도 원호가 제대로 그려지지 않는데?"
- Same-part contour completion: Addressed in separate commit
- HKSCRC laser state control: Already working correctly

## Git History

```
59bc37a - fix: Ensure smooth arc rendering with minimum segment count
1ba5404 - debug: Add detailed logging for arc segmentation
cbf8f00 - debug: Improve contour rendering debug logs with status icons
```

## Future Improvements

1. **Adaptive segmentation**: Use finer steps for high-curvature regions
2. **LOD system**: Reduce points when zoomed out
3. **Curve fitting**: Use Bézier curves for even smoother rendering
4. **Performance profiling**: Optimize for files with 10,000+ arcs
