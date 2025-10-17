/**
 * View Controls Component
 * 뷰어 제어 컴포넌트
 */

interface ViewControlsProps {
  showPiercing: boolean;
  showLeadIn: boolean;
  showApproach: boolean;
  showCutting: boolean;
  showPartLabels: boolean;
  showContourLabels: boolean;
  contourLabelSize: number;
  onTogglePiercing: () => void;
  onToggleLeadIn: () => void;
  onToggleApproach: () => void;
  onToggleCutting: () => void;
  onTogglePartLabels: () => void;
  onToggleContourLabels: () => void;
  onContourLabelSizeChange: (size: number) => void;
}

export function ViewControls({
  showPiercing,
  showLeadIn,
  showApproach,
  showCutting,
  showPartLabels,
  showContourLabels,
  contourLabelSize,
  onTogglePiercing,
  onToggleLeadIn,
  onToggleApproach,
  onToggleCutting,
  onTogglePartLabels,
  onToggleContourLabels,
  onContourLabelSizeChange,
}: ViewControlsProps) {
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
        🎨 표시 옵션
      </h3>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        <ToggleButton
          label="피어싱 위치"
          color="#ff6b6b"
          checked={showPiercing}
          onChange={onTogglePiercing}
        />
        <ToggleButton
          label="리드인 경로"
          color="#ffa500"
          checked={showLeadIn}
          onChange={onToggleLeadIn}
        />
        <ToggleButton
          label="어프로치 경로"
          color="#4ecdc4"
          checked={showApproach}
          onChange={onToggleApproach}
        />
        <ToggleButton
          label="절단 경로"
          color="#2196f3"
          checked={showCutting}
          onChange={onToggleCutting}
        />
      </div>

      <h3 style={{ marginTop: '20px', marginBottom: '15px', color: '#333', fontSize: '16px' }}>
        🏷️ 레이블 옵션
      </h3>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
        <ToggleButton
          label="파트 번호"
          color="#ffeb3b"
          checked={showPartLabels}
          onChange={onTogglePartLabels}
        />
        <ToggleButton
          label="컨투어 번호"
          color="#ffffff"
          checked={showContourLabels}
          onChange={onToggleContourLabels}
        />
        
        {/* 컨투어 라벨 크기 조절 */}
        {showContourLabels && (
          <div style={{ 
            padding: '10px', 
            backgroundColor: 'white', 
            borderRadius: '4px',
            marginTop: '5px'
          }}>
            <label style={{ 
              display: 'flex', 
              flexDirection: 'column', 
              gap: '8px',
              fontSize: '14px',
              color: '#333'
            }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span>컨투어 번호 크기</span>
                <span style={{ 
                  fontSize: '13px', 
                  fontWeight: 'bold',
                  color: '#2196f3',
                  minWidth: '40px',
                  textAlign: 'right'
                }}>
                  {contourLabelSize}
                </span>
              </div>
              <input
                type="range"
                min="6"
                max="48"
                step="2"
                value={contourLabelSize}
                onChange={(e) => onContourLabelSizeChange(Number(e.target.value))}
                style={{ width: '100%', cursor: 'pointer' }}
              />
            </label>
          </div>
        )}
      </div>

      <div style={{ marginTop: '20px', padding: '15px', backgroundColor: 'white', borderRadius: '4px' }}>
        <h4 style={{ margin: '0 0 10px 0', fontSize: '14px', color: '#555' }}>
          💡 조작법
        </h4>
        <ul style={{ margin: 0, paddingLeft: '20px', fontSize: '13px', color: '#666', lineHeight: '1.8' }}>
          <li><strong>마우스 왼쪽</strong>: 이동 (팬)</li>
          <li><strong>마우스 휠</strong>: 확대/축소</li>
          <li><strong>마우스 오른쪽</strong>: 회전 (3D 모드)</li>
        </ul>
      </div>
    </div>
  );
}

interface ToggleButtonProps {
  label: string;
  color: string;
  checked: boolean;
  onChange: () => void;
}

function ToggleButton({ label, color, checked, onChange }: ToggleButtonProps) {
  return (
    <label
      style={{
        display: 'flex',
        alignItems: 'center',
        padding: '10px',
        backgroundColor: 'white',
        borderRadius: '4px',
        cursor: 'pointer',
        userSelect: 'none',
      }}
    >
      <input
        type="checkbox"
        checked={checked}
        onChange={onChange}
        style={{ marginRight: '10px', cursor: 'pointer' }}
      />
      <div
        style={{
          width: '20px',
          height: '20px',
          backgroundColor: color,
          borderRadius: '50%',
          marginRight: '10px',
        }}
      />
      <span style={{ fontSize: '14px', color: '#333' }}>{label}</span>
    </label>
  );
}
