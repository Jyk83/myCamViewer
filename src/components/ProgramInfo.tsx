/**
 * Program Information Panel
 * MPF 프로그램 정보 패널
 */

import type { MPFProgram } from '../types';
import { MaterialTypes, AssistGasTypes, PiercingTypes, CuttingTypes } from '../types';

interface ProgramInfoProps {
  program: MPFProgram;
  filename: string;
}

export function ProgramInfo({ program, filename }: ProgramInfoProps) {
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
