/**
 * Parts List Component
 * 파트 목록 컴포넌트
 */

import type { MPFProgram } from '../types';
import { CuttingTypes } from '../types';

interface PartsListProps {
  program: MPFProgram;
  selectedPartId?: string;
  onSelectPart: (partId: string) => void;
}

export function PartsList({ program, selectedPartId, onSelectPart }: PartsListProps) {
  return (
    <div
      style={{
        padding: '20px',
        backgroundColor: '#f5f5f5',
        borderRadius: '8px',
      }}
    >
      <h3 style={{ marginTop: 0, marginBottom: '15px', color: '#333' }}>
        📦 파트 목록
      </h3>

      <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
        {program.parts.map((part, index) => {
          const isSelected = part.id === selectedPartId;
          const totalContours = part.contours.length;
          const cuttingContours = part.contours.filter(c => c.cuttingType === 1 || c.cuttingType === 2).length;
          const markingContours = part.contours.filter(c => c.cuttingType === 10).length;

          return (
            <div
              key={part.id}
              onClick={() => onSelectPart(part.id)}
              style={{
                padding: '15px',
                marginBottom: '10px',
                backgroundColor: isSelected ? '#e3f2fd' : 'white',
                border: isSelected ? '2px solid #2196f3' : '1px solid #ddd',
                borderRadius: '6px',
                cursor: 'pointer',
                transition: 'all 0.2s',
              }}
            >
              <div style={{ fontWeight: 'bold', marginBottom: '8px', color: '#333' }}>
                파트 {index + 1}
              </div>
              <div style={{ fontSize: '13px', color: '#666', lineHeight: '1.6' }}>
                <div>원점: ({part.origin.x.toFixed(2)}, {part.origin.y.toFixed(2)})</div>
                <div>회전: {part.rotation.toFixed(1)}°</div>
                <div>컨투어: {totalContours}개</div>
                {cuttingContours > 0 && <div>  - 절단: {cuttingContours}개</div>}
                {markingContours > 0 && <div>  - 마킹: {markingContours}개</div>}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
