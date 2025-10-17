/**
 * Program Information Panel
 * MPF 프로그램 정보 패널
 */

import { useState, useEffect } from 'react';
import type { MPFProgram } from '../types';
import { MaterialTypes, AssistGasTypes, PiercingTypes, CuttingTypes } from '../types';

interface ProgramInfoProps {
  program: MPFProgram;
  filename: string;
}

export function ProgramInfo({ program, filename }: ProgramInfoProps) {
  const [contour6Info, setContour6Info] = useState<{
    width: number;
    height: number;
    centerX: number;
    topY: number;
  } | null>(null);

  // 컨투어 6번 정보를 주기적으로 확인
  useEffect(() => {
    const interval = setInterval(() => {
      // @ts-ignore
      if (window.contour6Info) {
        // @ts-ignore
        setContour6Info(window.contour6Info);
      }
    }, 500);

    return () => clearInterval(interval);
  }, []);

  return (
    <div
      style={{
        padding: '20px',
        backgroundColor: '#f5f5f5',
        borderRadius: '8px',
        marginBottom: '20px',
      }}
    >
      <h3 style={{ marginTop: 0, marginBottom: '15px', color: '#333' }}>
        📄 프로그램 정보
      </h3>

      <div style={{ fontSize: '14px', lineHeight: '1.8' }}>
        <InfoRow label="파일명" value={filename} />
        <InfoRow label="버전" value={program.version} />
        <InfoRow
          label="재질"
          value={MaterialTypes[program.hkldb.material] || `Unknown (${program.hkldb.material})`}
        />
        <InfoRow label="절단 DB" value={program.hkldb.dbName} />
        <InfoRow
          label="보조 가스"
          value={AssistGasTypes[program.hkldb.assistGas] || `Unknown (${program.hkldb.assistGas})`}
        />
        <InfoRow
          label="워크피스 크기"
          value={`${program.workpiece.width.toFixed(2)} × ${program.workpiece.height.toFixed(2)} mm`}
        />
        <InfoRow label="총 파트 수" value={`${program.parts.length}개`} />
        <InfoRow
          label="총 컨투어 수"
          value={`${program.parts.reduce((sum, part) => sum + part.contours.length, 0)}개`}
        />
      </div>

      {/* 컨투어 6번 정보 표시 */}
      {contour6Info && (
        <div style={{ 
          marginTop: '15px', 
          paddingTop: '15px', 
          borderTop: '1px solid #ddd' 
        }}>
          <h4 style={{ 
            marginTop: 0, 
            marginBottom: '10px', 
            color: '#333',
            fontSize: '14px',
            fontWeight: 'bold'
          }}>
            🔍 컨투어 #6 상세
          </h4>
          <div style={{ fontSize: '13px', lineHeight: '1.8' }}>
            <InfoRow 
              label="계산된 너비" 
              value={`${contour6Info.width.toFixed(2)} mm`} 
            />
            <InfoRow 
              label="계산된 높이" 
              value={`${contour6Info.height.toFixed(2)} mm`} 
            />
            <InfoRow 
              label="중심 X" 
              value={`${contour6Info.centerX.toFixed(2)} mm`} 
            />
            <InfoRow 
              label="상단 Y" 
              value={`${contour6Info.topY.toFixed(2)} mm`} 
            />
          </div>
        </div>
      )}
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div style={{ display: 'flex', marginBottom: '8px' }}>
      <span style={{ fontWeight: 'bold', minWidth: '140px', color: '#555' }}>
        {label}:
      </span>
      <span style={{ color: '#333' }}>{value}</span>
    </div>
  );
}
