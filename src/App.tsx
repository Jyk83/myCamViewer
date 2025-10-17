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

// @ts-ignore - 디버깅용 전역 변수
window.lastProgram = null;

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
  const [showPartLabels, setShowPartLabels] = useState(true);
  const [showContourLabels, setShowContourLabels] = useState(true);
  const [contourLabelSize, setContourLabelSize] = useState(12); // 컨투어 라벨 폰트 크기

  const handleFileLoad = (content: string, name: string) => {
    try {
      setError(null);
      console.log('=== MPF 파일 파싱 시작 ===');
      console.log('파일명:', name);
      console.log('파일 크기:', content.length, 'bytes');
      
      const parsedProgram = parseMPFFile(content);
      
      console.log('=== 파싱 결과 ===');
      console.log('버전:', parsedProgram.version);
      console.log('재질:', parsedProgram.hkldb);
      console.log('워크피스:', parsedProgram.workpiece);
      console.log('네스팅 정보:', parsedProgram.nesting);
      console.log('파트 수:', parsedProgram.parts.length);
      console.log('파트 상세:', parsedProgram.parts);
      
      if (parsedProgram.parts.length > 0) {
        console.log('첫 번째 파트:', parsedProgram.parts[0]);
        if (parsedProgram.parts[0].contours.length > 0) {
          console.log('첫 번째 컨투어:', parsedProgram.parts[0].contours[0]);
        }
      }
      
      setProgram(parsedProgram);
      setFilename(name);
      
      // 디버깅용 전역 저장
      // @ts-ignore
      window.lastProgram = parsedProgram;
      console.log('window.lastProgram에 저장 완료');
      console.log('=== 파싱 완료 ===');
    } catch (err) {
      console.error('=== 파싱 에러 ===');
      console.error('에러:', err);
      console.error('스택:', err instanceof Error ? err.stack : 'No stack trace');
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
                showPartLabels={showPartLabels}
                showContourLabels={showContourLabels}
                contourLabelSize={contourLabelSize}
                onTogglePiercing={() => setShowPiercing(!showPiercing)}
                onToggleLeadIn={() => setShowLeadIn(!showLeadIn)}
                onToggleApproach={() => setShowApproach(!showApproach)}
                onToggleCutting={() => setShowCutting(!showCutting)}
                onTogglePartLabels={() => setShowPartLabels(!showPartLabels)}
                onToggleContourLabels={() => setShowContourLabels(!showContourLabels)}
                onContourLabelSizeChange={setContourLabelSize}
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
            showPartLabels={showPartLabels}
            showContourLabels={showContourLabels}
            contourLabelSize={contourLabelSize}
            viewMode="2D"
          />
        )}
      </div>
    </div>
  );
}

export default App;
