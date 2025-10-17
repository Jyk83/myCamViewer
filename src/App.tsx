/**
 * HK Laser Cutting Viewer - Main Application
 * HK 레이저 절단 뷰어 메인 애플리케이션
 */

import { useState, useRef, useEffect } from 'react';
import { FileUploader } from './components/FileUploader';
import { ProgramInfo } from './components/ProgramInfo';
import { ViewControls } from './components/ViewControls';
import { PartsList } from './components/PartsList';
import { SimulationControls } from './components/SimulationControls';
import { LaserViewer } from './viewer/LaserViewer';
import { parseMPFFile } from './parser/MPFParser';
import { segmentPathArray } from './utils/pathSegmentation';
import type { MPFProgram, SimulationState, PathPoint } from './types';
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
  const [contourLabelSize, setContourLabelSize] = useState(24); // 컨투어 라벨 폰트 크기 (기본값: 24)
  
  // 디버그/검증용 바운딩 박스
  const [showDebugBoundingBox, setShowDebugBoundingBox] = useState(false);
  const [debugPartNumber, setDebugPartNumber] = useState(1);
  const [debugContourNumber, setDebugContourNumber] = useState(6);

  // 시뮬레이션 상태
  const [simulationState, setSimulationState] = useState<SimulationState>({
    isRunning: false,
    isPaused: false,
    currentPartIndex: 0,
    currentContourIndex: 0,
    currentPointIndex: 0,
    totalPoints: 0,
    speed: 100,
    stepSize: 4, // 기본 4mm 단위
    completedPaths: new Set(),
  });

  // 시뮬레이션 데이터 (1mm 단위로 분할된 전체 경로)
  const [allPathPoints, setAllPathPoints] = useState<PathPoint[]>([]);
  const simulationTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  // 컨투어 번호 표시 토글 핸들러 (디버그 바운딩 박스도 함께 제어)
  const handleToggleContourLabels = () => {
    if (showContourLabels) {
      // 컨투어 번호를 끄면 디버그 바운딩 박스도 함께 끔
      setShowDebugBoundingBox(false);
    }
    setShowContourLabels(!showContourLabels);
  };

  // 시뮬레이션 컨트롤 핸들러
  const handleSimulationPlay = () => {
    if (allPathPoints.length === 0) return;

    setSimulationState(prev => ({
      ...prev,
      isRunning: true,
      isPaused: false,
    }));

    // 타이머 시작
    if (simulationTimerRef.current) {
      clearInterval(simulationTimerRef.current);
    }

    simulationTimerRef.current = setInterval(() => {
      setSimulationState(prev => {
        // 이미 끝까지 진행했으면 중지
        if (prev.currentPointIndex >= allPathPoints.length - 1) {
          if (simulationTimerRef.current) {
            clearInterval(simulationTimerRef.current);
            simulationTimerRef.current = null;
          }
          return {
            ...prev,
            isRunning: false,
            isPaused: false,
          };
        }

        // 다음 포인트로 진행
        const nextIndex = prev.currentPointIndex + 1;
        const nextPoint = allPathPoints[nextIndex];
        
        // 완료된 경로 추가 (createPathId 사용)
        const newCompletedPaths = new Set(prev.completedPaths);
        newCompletedPaths.add(
          `part-${nextPoint.partIndex}-contour-${nextPoint.contourIndex}-point-${nextIndex}`
        );

        return {
          ...prev,
          currentPartIndex: nextPoint.partIndex,
          currentContourIndex: nextPoint.contourIndex,
          currentPointIndex: nextIndex,
          completedPaths: newCompletedPaths,
        };
      });
    }, simulationState.speed);
  };

  const handleSimulationPause = () => {
    if (simulationTimerRef.current) {
      clearInterval(simulationTimerRef.current);
      simulationTimerRef.current = null;
    }
    setSimulationState(prev => ({
      ...prev,
      isRunning: false,
      isPaused: true,
    }));
  };

  const handleSimulationStop = () => {
    if (simulationTimerRef.current) {
      clearInterval(simulationTimerRef.current);
      simulationTimerRef.current = null;
    }
    setSimulationState({
      isRunning: false,
      isPaused: false,
      currentPartIndex: 0,
      currentContourIndex: 0,
      currentPointIndex: 0,
      totalPoints: allPathPoints.length,
      speed: 100,
      completedPaths: new Set(),
    });
  };

  const handleSimulationSpeedChange = (speed: number) => {
    setSimulationState(prev => ({
      ...prev,
      speed,
    }));

    // 실행 중이면 타이머 재시작
    if (simulationState.isRunning && !simulationState.isPaused) {
      handleSimulationPause();
      setTimeout(() => handleSimulationPlay(), 10);
    }
  };

  const handleSimulationStepSizeChange = (stepSize: number) => {
    // 유효한 범위로 제한 (0.5mm ~ 10mm)
    const clampedStepSize = Math.max(0.5, Math.min(10, stepSize));
    
    setSimulationState(prev => ({
      ...prev,
      stepSize: clampedStepSize,
      // 변경 시 처음부터 다시 시작
      currentPartIndex: 0,
      currentContourIndex: 0,
      currentPointIndex: 0,
      completedPaths: new Set(),
    }));

    // 경로 재생성 필요 - 프로그램이 있으면 재로드
    if (program) {
      // 경로 재생성은 useEffect에서 자동으로 처리됨
      console.log(`이동 단위 변경: ${clampedStepSize}mm`);
    }
  };

  // 컴포넌트 언마운트 시 타이머 정리
  useEffect(() => {
    return () => {
      if (simulationTimerRef.current) {
        clearInterval(simulationTimerRef.current);
      }
    };
  }, []);

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
      
      // 시뮬레이션을 위한 전체 경로 세그먼트 분할
      const segmentedPaths: PathPoint[] = [];
      parsedProgram.parts.forEach((part, partIndex) => {
        // 파트 원점 (HKOST)
        const partOrigin = part.origin;
        
        part.contours.forEach((contour, contourIndex) => {
          // Approach path (HKSTR 이동 - 레이저 OFF)
          // 이전 컨투어의 HKSTO 위치에서 현재 컨투어의 HKSTR 위치까지
          const approachPoints = segmentPathArray(
            contour.approachPath,
            partIndex,
            contourIndex,
            'approach',
            false,  // 레이저 OFF
            simulationState.stepSize  // 분할 단위
          );
          // 파트 원점만큼 좌표 이동
          approachPoints.forEach(p => {
            p.position.x += partOrigin.x;
            p.position.y += partOrigin.y;
          });
          segmentedPaths.push(...approachPoints);
          
          // 피어싱 포인트 (HKSTR 위치, HKPIE 실행)
          // 레이저 ON 시작점
          const piercingPoint: PathPoint = {
            position: {
              x: contour.piercingPosition.x + partOrigin.x,
              y: contour.piercingPosition.y + partOrigin.y,
            },
            partIndex,
            contourIndex,
            segmentIndex: -1,  // 피어싱은 세그먼트가 아님
            progress: 0,
            laserOn: true,  // 레이저 ON 시작
            pathType: 'piercing',
          };
          segmentedPaths.push(piercingPoint);
          
          // Lead-in path (optional) - 레이저 ON 진입 절단
          if (contour.leadIn) {
            const leadInPoints = segmentPathArray(
              contour.leadIn.path,
              partIndex,
              contourIndex,
              'leadIn',
              true,  // 레이저 ON (절단)
              simulationState.stepSize  // 분할 단위
            );
            // 파트 원점만큼 좌표 이동
            leadInPoints.forEach(p => {
              p.position.x += partOrigin.x;
              p.position.y += partOrigin.y;
            });
            segmentedPaths.push(...leadInPoints);
          }
          
          // Cutting path
          const cuttingPoints = segmentPathArray(
            contour.cuttingPath,
            partIndex,
            contourIndex,
            'cutting',
            true,  // 레이저 ON
            simulationState.stepSize  // 분할 단위
          );
          // 파트 원점만큼 좌표 이동
          cuttingPoints.forEach(p => {
            p.position.x += partOrigin.x;
            p.position.y += partOrigin.y;
          });
          segmentedPaths.push(...cuttingPoints);
          
          // HKSTO 종료 포인트 - 레이저 OFF
          const endPoint: PathPoint = {
            position: {
              x: contour.endPosition.x + partOrigin.x,
              y: contour.endPosition.y + partOrigin.y,
            },
            partIndex,
            contourIndex,
            segmentIndex: -2,  // HKSTO 마커
            progress: 1,
            laserOn: false,  // 레이저 OFF
            pathType: 'cutting',
          };
          segmentedPaths.push(endPoint);
        });
        
        // HKPED: 파트 종료 포인트 - 레이저 OFF
        // 마지막 컨투어의 종료 위치를 사용
        if (part.contours.length > 0) {
          const lastContour = part.contours[part.contours.length - 1];
          const partEndPoint: PathPoint = {
            position: {
              x: lastContour.endPosition.x + partOrigin.x,
              y: lastContour.endPosition.y + partOrigin.y,
            },
            partIndex,
            contourIndex: part.contours.length - 1,
            segmentIndex: -3,  // HKPED 마커
            progress: 1,
            laserOn: false,  // 레이저 OFF (파트 종료)
            pathType: 'cutting',
          };
          segmentedPaths.push(partEndPoint);
        }
      });
      setAllPathPoints(segmentedPaths);
      setSimulationState(prev => ({
        ...prev,
        totalPoints: segmentedPaths.length,
      }));

      // 디버깅용 전역 저장
      // @ts-ignore
      window.lastProgram = parsedProgram;
      console.log('window.lastProgram에 저장 완료');
      console.log('=== 파싱 완료 ===');
      console.log('전체 경로 포인트 수:', segmentedPaths.length);
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
                showDebugBoundingBox={showDebugBoundingBox}
                debugPartNumber={debugPartNumber}
                debugContourNumber={debugContourNumber}
                onTogglePiercing={() => setShowPiercing(!showPiercing)}
                onToggleLeadIn={() => setShowLeadIn(!showLeadIn)}
                onToggleApproach={() => setShowApproach(!showApproach)}
                onToggleCutting={() => setShowCutting(!showCutting)}
                onTogglePartLabels={() => setShowPartLabels(!showPartLabels)}
                onToggleContourLabels={handleToggleContourLabels}
                onContourLabelSizeChange={setContourLabelSize}
                onToggleDebugBoundingBox={() => setShowDebugBoundingBox(!showDebugBoundingBox)}
                onDebugPartNumberChange={setDebugPartNumber}
                onDebugContourNumberChange={setDebugContourNumber}
              />
            </div>

            {/* 시뮬레이션 컨트롤 */}
            <div style={{ padding: '0 20px' }}>
              <SimulationControls
                simulationState={simulationState}
                onPlay={handleSimulationPlay}
                onPause={handleSimulationPause}
                onStop={handleSimulationStop}
                onSpeedChange={handleSimulationSpeedChange}
                onStepSizeChange={handleSimulationStepSizeChange}
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
            showDebugBoundingBox={showDebugBoundingBox}
            debugPartNumber={debugPartNumber}
            debugContourNumber={debugContourNumber}
            viewMode="2D"
            simulationState={simulationState}
            allPathPoints={allPathPoints}
          />
        )}
      </div>
    </div>
  );
}

export default App;
