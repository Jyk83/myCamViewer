/**
 * HK Laser Cutting Viewer - Main Application
 * HK 레이저 절단 뷰어 메인 애플리케이션
 */

import { useState } from 'react';
import { FileUploader } from './components/FileUploader';
import { ProgramInfo } from './components/ProgramInfo';
import { ViewControls } from './components/ViewControls';
import { PartsList } from './components/PartsList';
import { LaserViewer } from './viewer/LaserViewer';
import { parseMPFFile } from './parser/MPFParser';
import type { MPFProgram } from './types';
import './App.css';

function App() {
  const [program, setProgram] = useState<MPFProgram | null>(null);
  const [filename, setFilename] = useState<string>('');
  const [selectedPartId, setSelectedPartId] = useState<string | undefined>();
  const [error, setError] = useState<string | null>(null);

  // 뷰어 옵션
  const [showPiercing, setShowPiercing] = useState(true);
  const [showLeadIn, setShowLeadIn] = useState(true);
  const [showApproach, setShowApproach] = useState(true);
  const [showCutting, setShowCutting] = useState(true);

  const handleFileLoad = (content: string, name: string) => {
    try {
      setError(null);
      const parsedProgram = parseMPFFile(content);
      setProgram(parsedProgram);
      setFilename(name);
      console.log('Parsed program:', parsedProgram);
    } catch (err) {
      console.error('Parse error:', err);
      setError(err instanceof Error ? err.message : '파일 파싱 중 오류가 발생했습니다.');
      setProgram(null);
    }
  };

  return (
    <div style={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
      {/* 좌측 사이드바 */}
      <div
        style={{
          width: '400px',
          backgroundColor: '#fafafa',
          borderRight: '1px solid #ddd',
          overflowY: 'auto',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {/* 헤더 */}
        <div
          style={{
            padding: '20px',
            backgroundColor: '#2196f3',
            color: 'white',
            borderBottom: '1px solid #1976d2',
          }}
        >
          <h1 style={{ margin: 0, fontSize: '24px', fontWeight: 'bold' }}>
            ⚡ HK Laser Viewer
          </h1>
          <p style={{ margin: '8px 0 0 0', fontSize: '14px', opacity: 0.9 }}>
            레이저 절단 경로 시각화
          </p>
        </div>

        {/* 파일 업로더 */}
        <FileUploader onFileLoad={handleFileLoad} />

        {/* 에러 메시지 */}
        {error && (
          <div
            style={{
              margin: '0 20px 20px 20px',
              padding: '15px',
              backgroundColor: '#ffebee',
              border: '1px solid #ef5350',
              borderRadius: '4px',
              color: '#c62828',
              fontSize: '14px',
            }}
          >
            <strong>⚠️ 오류:</strong> {error}
          </div>
        )}

        {/* 프로그램 정보 */}
        {program && (
          <>
            <div style={{ padding: '0 20px' }}>
              <ProgramInfo program={program} filename={filename} />
            </div>

            {/* 뷰 컨트롤 */}
            <div style={{ padding: '0 20px' }}>
              <ViewControls
                showPiercing={showPiercing}
                showLeadIn={showLeadIn}
                showApproach={showApproach}
                showCutting={showCutting}
                onTogglePiercing={() => setShowPiercing(!showPiercing)}
                onToggleLeadIn={() => setShowLeadIn(!showLeadIn)}
                onToggleApproach={() => setShowApproach(!showApproach)}
                onToggleCutting={() => setShowCutting(!showCutting)}
              />
            </div>

            {/* 파트 목록 */}
            <div style={{ padding: '0 20px 20px 20px' }}>
              <PartsList
                program={program}
                selectedPartId={selectedPartId}
                onSelectPart={setSelectedPartId}
              />
            </div>
          </>
        )}
      </div>

      {/* 메인 뷰어 영역 */}
      <div style={{ flex: 1, position: 'relative', backgroundColor: '#ffffff' }}>
        {!program ? (
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              height: '100%',
              color: '#999',
              fontSize: '18px',
              flexDirection: 'column',
              gap: '20px',
            }}
          >
            <div style={{ fontSize: '64px' }}>📂</div>
            <div>MPF 파일을 선택하여 시작하세요</div>
            <div style={{ fontSize: '14px', color: '#ccc' }}>
              HK 레이저 절단 프로그램 파일 (.txt, .mpf, .nc)
            </div>
          </div>
        ) : (
          <LaserViewer
            program={program}
            selectedPartId={selectedPartId}
            showPiercing={showPiercing}
            showLeadIn={showLeadIn}
            showApproach={showApproach}
            showCutting={showCutting}
            viewMode="2D"
          />
        )}
      </div>
    </div>
  );
}

export default App;
