# Simulation Rendering Fix - Completed Contours Display

## Problem Description

When using large stepSize values (10-100mm) in distance-based simulation, the laser head moves so quickly that:

1. **Visual persistence issue**: The rendering couldn't keep up with the simulation speed
2. **Lost context**: Completed contours/parts were not visually distinct from current work
3. **Poor feedback**: Users couldn't see what had already been cut

**User's Requirement** (translated from Korean):
> "현재 진행해야 할 파트+컨투어의 이전 파트+컨투어를 다 빨간색으로 처리해야해. 왜냐하면 이미 절단하고 지난 상태이니깐."
>
> "All previous parts+contours before the current one being processed should be displayed in red because they're already cut and passed."

## Solution Implementation

### Rendering Strategy

Modified `drawSimulationPath()` in `LaserViewer.tsx` to distinguish between:

1. **Completed Contours** (already fully cut):
   - Color: Bright red `#ff3333` (more visible)
   - Line width: 6px (thicker)
   - Z-index: 0.6 (rendered on top)

2. **Current In-Progress Contour**:
   - Color: Normal red `#ff0000` (darker)
   - Line width: 4px (standard)
   - Z-index: 0.5 (rendered below)

### Algorithm

```typescript
// 1. Group path points by (partIndex, contourIndex)
const contourGroups: ContourGroup[] = [];

// 2. Mark all groups except the last one as completed
for (let i = 0; i < contourGroups.length - 1; i++) {
  contourGroups[i].isCompleted = true;
}

// 3. Render each group with appropriate styling
contourGroups.forEach(group => {
  const color = group.isCompleted ? '#ff3333' : '#ff0000';
  const linewidth = group.isCompleted ? 6 : 4;
  const zIndex = group.isCompleted ? 0.6 : 0.5;
  // ... render with these properties
});
```

### Benefits

1. **Visual Continuity**: All completed work remains visible throughout simulation
2. **Progress Tracking**: Clear distinction between completed and in-progress work
3. **High-Speed Compatibility**: Works perfectly with fast simulation (large stepSize)
4. **Depth Perception**: Z-index layering makes completed work stand out

## Testing

To test this feature:

1. Load a file with multiple parts/contours (e.g., REM_SAMPLE.TXT)
2. Set stepSize to 50-100mm for fast simulation
3. Start simulation and observe:
   - ✅ Completed contours remain visible in bright red
   - ✅ Current contour is darker red
   - ✅ Yellow laser head shows exact position
   - ✅ All cut work is persistent and clearly visible

## Technical Details

### Data Structure

```typescript
interface ContourGroup {
  partIndex: number;
  contourIndex: number;
  points: PathPoint[];
  isCompleted: boolean; // Fully completed vs. in-progress
}
```

### Color Scheme

| State | Color | Hex | Line Width | Z-Index | Purpose |
|-------|-------|-----|------------|---------|---------|
| Completed | Bright Red | #ff3333 | 6px | 0.6 | High visibility for cut work |
| In-Progress | Normal Red | #ff0000 | 4px | 0.5 | Current cutting operation |
| Laser Head | Yellow | #ffff00 | - | 1.0 | Exact position marker |

### Rendering Order

1. Completed contours (bright red, thick, on top)
2. Current in-progress contour (darker red, standard)
3. Laser head marker (yellow, highest z-index)

## Related Files

- `/src/viewer/LaserViewer.tsx` - Main rendering logic
- `/src/types/index.ts` - PathPoint, SimulationState definitions
- `/src/App.tsx` - Distance-based simulation timer

## Future Enhancements

Potential improvements:
1. Fade effect for very old completed contours
2. Different colors for different parts (rainbow mode)
3. Completion percentage overlay per part
4. Animation trail effect for the laser head

## Git Commit

```
commit 4dbd526
fix: Display all completed contours in bright red during fast simulation

- Problem: When stepSize is large (10-100mm), simulation moves so fast that 
  completed contours are not visually persistent
- Solution: Distinguish between completed contours and current in-progress contour
- Completed contours: Bright red (#ff3333) + thicker line (6px) + higher z-index
- In-progress contour: Normal red (#ff0000) + standard line (4px)
- Group path points by (partIndex, contourIndex) to identify completion status
- All contours before the last one are marked as completed
- Ensures all cut parts remain visible throughout simulation
```

## References

- Original implementation: Distance-based simulation (commit 57a0241)
- HKSCRC laser state control (REM_SAMPLE.TXT support)
- Coordinate system: 1mm = 1 Three.js unit (COORDINATE_SYSTEM.md)
