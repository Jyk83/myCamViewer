/**
 * Simulation Controls Component
 * 시뮬레이션 제어 컴포넌트
 */

import { SimulationState } from '../types';

interface SimulationControlsProps {
  simulationState: SimulationState;
  onPlay: () => void;
  onPause: () => void;
  onStop: () => void;
  onSpeedChange: (speed: number) => void;
}

export function SimulationControls({
  simulationState,
  onPlay,
  onPause,
  onStop,
  onSpeedChange,
}: SimulationControlsProps) {
  const { isRunning, isPaused, currentPartIndex, currentContourIndex, currentPointIndex, totalPoints, speed } = simulationState;

  // 진행률 계산
  const progress = totalPoints > 0 ? (currentPointIndex / totalPoints) * 100 : 0;

  // 속도 옵션 (ms per step)
  const speedOptions = [
    { label: '매우 빠름 (50ms)', value: 50 },
    { label: '빠름 (100ms)', value: 100 },
    { label: '보통 (200ms)', value: 200 },
    { label: '느림 (500ms)', value: 500 },
    { label: '매우 느림 (1000ms)', value: 1000 },
  ];

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
        🎬 시뮬레이션 컨트롤
      </h3>

      {/* 재생 컨트롤 버튼 */}
      <div style={{ display: 'flex', gap: '10px', marginBottom: '15px' }}>
        {!isRunning || isPaused ? (
          <button
            onClick={onPlay}
            style={{
              flex: 1,
              padding: '12px',
              backgroundColor: '#4CAF50',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontSize: '14px',
              fontWeight: 'bold',
            }}
          >
            ▶️ {isPaused ? '계속' : '재생'}
          </button>
        ) : (
          <button
            onClick={onPause}
            style={{
              flex: 1,
              padding: '12px',
              backgroundColor: '#FF9800',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontSize: '14px',
              fontWeight: 'bold',
            }}
          >
            ⏸️ 일시정지
          </button>
        )}

        <button
          onClick={onStop}
          disabled={!isRunning && !isPaused}
          style={{
            flex: 1,
            padding: '12px',
            backgroundColor: !isRunning && !isPaused ? '#ccc' : '#f44336',
            color: 'white',
            border: 'none',
            borderRadius: '4px',
            cursor: !isRunning && !isPaused ? 'not-allowed' : 'pointer',
            fontSize: '14px',
            fontWeight: 'bold',
          }}
        >
          ⏹️ 정지
        </button>
      </div>

      {/* 진행 상태 */}
      <div style={{ marginBottom: '15px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '5px' }}>
          <span style={{ fontSize: '13px', color: '#666' }}>
            파트 {currentPartIndex + 1}, 컨투어 {currentContourIndex + 1}
          </span>
          <span style={{ fontSize: '13px', color: '#666', fontWeight: 'bold' }}>
            {currentPointIndex} / {totalPoints} ({progress.toFixed(1)}%)
          </span>
        </div>

        {/* 진행률 바 */}
        <div
          style={{
            width: '100%',
            height: '8px',
            backgroundColor: '#e0e0e0',
            borderRadius: '4px',
            overflow: 'hidden',
          }}
        >
          <div
            style={{
              width: `${progress}%`,
              height: '100%',
              backgroundColor: isRunning && !isPaused ? '#4CAF50' : '#FF9800',
              transition: 'width 0.3s ease',
            }}
          />
        </div>
      </div>

      {/* 속도 조절 */}
      <div style={{ backgroundColor: 'white', padding: '15px', borderRadius: '4px' }}>
        <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontSize: '14px', color: '#333', fontWeight: 'bold' }}>
              ⚡ 시뮬레이션 속도
            </span>
            <span style={{ fontSize: '13px', color: '#2196f3', fontWeight: 'bold' }}>
              {speed}ms
            </span>
          </div>

          <select
            value={speed}
            onChange={(e) => onSpeedChange(Number(e.target.value))}
            style={{
              padding: '8px',
              fontSize: '14px',
              border: '1px solid #ddd',
              borderRadius: '4px',
              cursor: 'pointer',
            }}
          >
            {speedOptions.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {/* 상태 정보 */}
      <div
        style={{
          marginTop: '15px',
          padding: '10px',
          backgroundColor: isRunning && !isPaused ? '#e8f5e9' : isPaused ? '#fff3e0' : '#f5f5f5',
          borderRadius: '4px',
          fontSize: '12px',
          color: '#666',
          textAlign: 'center',
        }}
      >
        {isRunning && !isPaused && '🟢 시뮬레이션 실행 중...'}
        {isPaused && '🟡 일시정지됨'}
        {!isRunning && !isPaused && '⚪ 대기 중'}
      </div>
    </div>
  );
}
