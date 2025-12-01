# Phase 8 개발 로드맵

## 📅 전체 일정 (6주)

```
Week 1: Phase 8.1 - 파트/컨투어 번호 표시
Week 2-3: Phase 8.2 - 실시간 트레이스 구현
Week 4: Phase 8.3 - Itag 통신 구현
Week 5: Phase 8.4 - OPC UA 통신 구현
Week 6: Phase 8.5 - 통합 및 테스트
```

---

## 📋 Week 1: Phase 8.1 - 파트/컨투어 번호 표시

### Day 1-2: OpenGL 텍스트 렌더링 시스템
**목표**: HKCamInterface의 OpenGLNumber 클래스를 참조하여 텍스트 렌더링 시스템 구현

**참조 파일**:
- `HKCamInterface_Reference/OpenGLNumber.h`
- `HKCamInterface_Reference/OpenGLNumber.cpp`

**작업 내용**:
```csharp
// NativeRenderer/TextRenderer.h
class TextRenderer
{
public:
    void Initialize();
    void DrawText(const char* text, float x, float y, float height, COLORREF color);
    void SetFont(int height);
    
private:
    GLuint m_displayListBase;
    HFONT m_hFont;
};
```

**체크리스트**:
- [ ] OpenGL display list 기반 폰트 생성
- [ ] 텍스트 렌더링 함수 구현
- [ ] 폰트 크기 동적 변경
- [ ] 색상 적용
- [ ] 테스트 프로그램 작성

### Day 3-4: 라벨 위치 계산
**목표**: 파트와 컨투어의 바운딩 박스를 기반으로 라벨 표시 위치 계산

**참조 코드**:
```cpp
// HKCamInterface_Reference/OpenGLCAMViewWnd.cpp
void CCAMViewWnd::CreatePartBoundList(UINT idList)
{
    // 파트 바운딩 박스 계산
    // 중앙 위치에 파트 번호 표시
}
```

**작업 내용**:
```csharp
// NativeRenderer/LabelPositionCalculator.cs
public class LabelPositionCalculator
{
    public Point2D CalculatePartLabelPosition(BoundingBox bbox)
    {
        // 파트 바운딩 박스 중앙 계산
        return new Point2D(
            bbox.CenterX - 5,  // 약간 왼쪽으로 오프셋
            bbox.CenterY + 5   // 약간 위쪽으로 오프셋
        );
    }
    
    public Point2D CalculateContourLabelPosition(BoundingBox bbox)
    {
        // 컨투어 상단 중앙 + 마진
        return new Point2D(
            bbox.CenterX,
            bbox.TopY + 2.0    // 2mm 위쪽
        );
    }
}
```

**체크리스트**:
- [ ] 바운딩 박스 계산 로직
- [ ] 파트 라벨 위치 계산 (중앙 + 오프셋)
- [ ] 컨투어 라벨 위치 계산 (상단 중앙 + 마진)
- [ ] 줌 레벨에 따른 동적 조정
- [ ] 겹침 방지 로직 (선택사항)

### Day 5: UI 옵션 추가
**목표**: 라벨 표시 on/off 및 폰트 크기 조절 UI 추가

**HKCamInterface 옵션 코드**:
```cpp
#define iOPT_PART_NUMBER        102  // nValue==0 => 표시 안함
#define iOPT_PARTNO_HEIGHT      104  // nValue=폰트 높이 (픽셀)
#define iOPT_CONTOUR_NUMBER     105  // nValue==0 => 표시 안함
#define iOPT_CONTOURNO_HEIGHT   106  // nValue=폰트 높이 (픽셀)
#define iOPT_LIMIT_PART_CONT_NO 107  // 표시 제한 개수
```

**UI 구성**:
```
┌─────────────────────────────────┐
│ 라벨 표시 옵션                   │
├─────────────────────────────────┤
│ ☑ 파트 번호 표시                 │
│   폰트 크기: [24] ▲▼            │
│                                 │
│ ☑ 컨투어 번호 표시               │
│   폰트 크기: [18] ▲▼            │
│                                 │
│ 표시 제한: [100] 개             │
└─────────────────────────────────┘
```

**체크리스트**:
- [ ] 체크박스 UI 추가 (파트/컨투어)
- [ ] 폰트 크기 조절 NumericUpDown
- [ ] 표시 개수 제한 옵션
- [ ] 옵션 저장/로드
- [ ] 실시간 업데이트

---

## 📋 Week 2-3: Phase 8.2 - 실시간 트레이스 구현

### Day 6-7: 절단 진행 상태 추적 시스템
**목표**: CVStartCutting, CVUpdateCutting 함수 참조하여 진행 상태 추적

**참조 함수**:
```cpp
// HKCAMInterfaceDLL.cpp
DLLSPEC int CVStartCutting(HWND hWndCV, int nPartFrom, int nContourFrom, int nIsReverse)
{
    return s_ViewerFactory.StartCutting(hWndCV, nPartFrom, nContourFrom, (0 != nIsReverse));
}

DLLSPEC int CVUpdateCutting(HWND hWndCV, int part, int contour, 
                             const char* currentBlock, double progress, 
                             double xwcs, double ywcs, int isGcodeBlock)
{
    return s_ViewerFactory.UpdateCutting(hWndCV, part, contour, currentBlock, 
                                         progress, xwcs, ywcs, (isGcodeBlock != 0));
}
```

**데이터 구조**:
```csharp
public class CuttingState
{
    public int StartPart { get; set; }
    public int StartContour { get; set; }
    public int CurrentPart { get; set; }
    public int CurrentContour { get; set; }
    public int CurrentElement { get; set; }
    public double ElementProgress { get; set; }  // 0.0 ~ 1.0
    public bool IsReverse { get; set; }
    
    // 위치 정보
    public double CurrentX { get; set; }
    public double CurrentY { get; set; }
    
    // 통계
    public double TotalCutDistance { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
}
```

**체크리스트**:
- [ ] CuttingState 클래스 구현
- [ ] StartCutting() 함수 구현
- [ ] UpdateCutting() 함수 구현
- [ ] 진행 상태 검증 로직
- [ ] 에러 처리

### Day 8-10: 완료된 경로 렌더링
**목표**: 완료된 파트/컨투어를 빨간색으로 표시

**렌더링 전략**:
```cpp
// OpenGLCAMViewWnd.cpp 참조
void CCAMViewWnd::DrawProgress(camContour& cLast, int& iLastElement, double& progress)
{
    // 완료된 엘리먼트: 빨간색
    glColor3f(1.0f, 0.0f, 0.0f);
    
    // 진행 중인 엘리먼트: 부분적으로 색상 적용
    // 미진행 엘리먼트: 원래 색상 유지
}
```

**구현 방법**:
```csharp
public class PathRenderer
{
    // 경로 상태별 색상
    private Color CompletedColor = Color.FromArgb(255, 0, 0);    // 빨간색
    private Color InProgressColor = Color.FromArgb(255, 165, 0); // 주황색
    private Color PendingColor = Color.FromArgb(33, 150, 243);   // 파란색
    
    public void RenderPath(PathSegment segment, PathState state, double progress)
    {
        switch (state)
        {
            case PathState.Completed:
                RenderCompletedSegment(segment);
                break;
            case PathState.InProgress:
                RenderPartialSegment(segment, progress);
                break;
            case PathState.Pending:
                RenderPendingSegment(segment);
                break;
        }
    }
    
    private void RenderPartialSegment(PathSegment segment, double progress)
    {
        // 완료된 부분: 빨간색
        RenderSegmentPortion(segment, 0, progress, CompletedColor);
        
        // 미완료 부분: 원래 색상
        RenderSegmentPortion(segment, progress, 1.0, PendingColor);
    }
}
```

**체크리스트**:
- [ ] 경로 상태 분류 (완료/진행중/대기)
- [ ] 완료된 경로 빨간색 렌더링
- [ ] 진행 중인 세그먼트 부분 렌더링
- [ ] 색상 전환 애니메이션
- [ ] 성능 최적화 (버퍼 재사용)

### Day 11-12: 레이저 헤드 마커 표시
**목표**: 현재 절단 위치에 레이저 헤드 마커 표시 및 발광 효과

**마커 디자인**:
```
    ╱│╲
   ╱ │ ╲   ← 삼각형 모양
  ╱  ●  ╲  ← 중앙에 점
 ╱_______╲
```

**구현**:
```csharp
public class LaserHeadMarker
{
    private float _markerSize = 5.0f;  // mm
    private Color _markerColor = Color.FromArgb(0, 255, 255);  // 시안
    
    public void Render(double x, double y, float zoomScale)
    {
        // 크기는 줌 레벨에 따라 조정
        float size = _markerSize / zoomScale;
        
        // 외곽 발광 효과
        RenderGlow(x, y, size * 2);
        
        // 삼각형 마커
        RenderTriangle(x, y, size);
        
        // 중앙 점
        RenderCenter(x, y, size * 0.2f);
    }
    
    private void RenderGlow(double x, double y, float radius)
    {
        // Radial gradient shader
        // 중앙: 밝음, 가장자리: 투명
    }
}
```

**체크리스트**:
- [ ] 레이저 헤드 마커 모양 디자인
- [ ] 발광 효과 셰이더 구현
- [ ] 줌 레벨 대응 크기 조정
- [ ] 애니메이션 효과 (맥박 효과)
- [ ] 회전 방향 표시 (선택사항)

### Day 13: 진행률 계산 및 표시
**목표**: 절단 거리 및 진행률 계산

**참조 함수**:
```cpp
DLLSPEC int CVGetContourLength(HWND hWndCV, int part, int contour, 
                                double& length, bool isReset);
```

**구현**:
```csharp
public class ProgressCalculator
{
    private Dictionary<(int, int), double> _contourLengths 
        = new Dictionary<(int, int), double>();
    
    public double CalculateTotalProgress(CuttingState state, MPFProgram program)
    {
        double completedDistance = 0;
        double totalDistance = 0;
        
        // 전체 파트/컨투어 길이 합산
        foreach (var part in program.Parts)
        {
            foreach (var contour in part.Contours)
            {
                double length = CalculateContourLength(contour);
                totalDistance += length;
                
                if (IsContourCompleted(part.Number, contour.Number, state))
                {
                    completedDistance += length;
                }
            }
        }
        
        // 현재 진행 중인 컨투어의 부분 길이 추가
        completedDistance += CalculateCurrentProgress(state);
        
        return (completedDistance / totalDistance) * 100.0;
    }
}
```

**UI 표시**:
```
┌─────────────────────────────────┐
│ 절단 진행 상황                   │
├─────────────────────────────────┤
│ 파트: 3/10                      │
│ 컨투어: 5/8                     │
│ 거리: 1,234.5 / 5,678.9 mm      │
│ 진행률: [████████░░] 75.2%      │
│ 경과 시간: 00:15:32             │
│ 예상 완료: 00:05:18             │
└─────────────────────────────────┘
```

**체크리스트**:
- [ ] 컨투어 길이 계산 함수
- [ ] 전체 진행률 계산
- [ ] 경과 시간 추적
- [ ] 예상 완료 시간 계산
- [ ] UI 업데이트 (프로그레스 바)

---

## 📋 Week 4: Phase 8.3 - Itag 통신 구현

### Day 14-15: Siemens DLL 통합
**목표**: Siemens.Runtime.ControlDev.dll 통합 및 기본 연결

**참조 문서**:
- `109760182_ActiveXWCCPenUS.pdf`
- `109760182_ControlDevelopment_samples.7z`

**구현**:
```csharp
using Siemens.Runtime.ControlDev;

public class ItagClient : IDisposable
{
    private ITagService _tagService;
    private Timer _pollTimer;
    private bool _isConnected;
    
    public event Action<RealtimeData> OnDataReceived;
    public event Action<string> OnError;
    
    public async Task ConnectAsync(string ipAddress)
    {
        try
        {
            _tagService = new TagService();
            await _tagService.ConnectAsync(ipAddress);
            _isConnected = true;
            
            StartPolling();
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"연결 실패: {ex.Message}");
            throw;
        }
    }
    
    public void Disconnect()
    {
        StopPolling();
        _tagService?.Disconnect();
        _isConnected = false;
    }
}
```

**체크리스트**:
- [ ] DLL 참조 추가
- [ ] TagService 클래스 사용법 학습
- [ ] 연결/해제 기능 구현
- [ ] 에러 처리
- [ ] 연결 상태 모니터링

### Day 16-17: 변수 읽기/쓰기
**목표**: NCU 변수 읽기 및 쓰기 기능 구현

**Itag 변수 맵**:
```csharp
public static class ItagVariables
{
    // 축 위치
    public const string AXIS_X = "$AA_IW[X]";
    public const string AXIS_Y = "$AA_IW[Y]";
    public const string AXIS_Z = "$AA_IW[Z]";
    
    // 마커 변수
    public const string LASER_STATE = "$AC_MARKER[0]";
    public const string PART_NUMBER = "$AC_MARKER[1]";
    public const string CONTOUR_NUMBER = "$AC_MARKER[2]";
    public const string PROGRESS = "$AC_MARKER[10]";
}

public class ItagVariableReader
{
    public double ReadAxisPosition(string axis)
    {
        string varName = $"$AA_IW[{axis}]";
        return _tagService.ReadDouble(varName);
    }
    
    public RealtimeData ReadAllData()
    {
        return new RealtimeData
        {
            X = ReadAxisPosition("X"),
            Y = ReadAxisPosition("Y"),
            Z = ReadAxisPosition("Z"),
            LaserOn = ReadMarker(0) == 1,
            PartNumber = (int)ReadMarker(1),
            ContourNumber = (int)ReadMarker(2),
            Progress = ReadMarker(10),
            Timestamp = DateTime.Now
        };
    }
    
    private double ReadMarker(int index)
    {
        return _tagService.ReadDouble($"$AC_MARKER[{index}]");
    }
}
```

**체크리스트**:
- [ ] 변수 이름 매핑
- [ ] ReadDouble/ReadInt 함수 구현
- [ ] WriteDouble/WriteInt 함수 구현
- [ ] 배치 읽기 최적화
- [ ] 에러 처리 및 재시도

### Day 18: 실시간 데이터 구독
**목표**: 주기적으로 변수를 읽어 이벤트 발행

**폴링 방식**:
```csharp
private void StartPolling()
{
    _pollTimer = new Timer(100);  // 100ms = 10Hz
    _pollTimer.Elapsed += OnPollTimer;
    _pollTimer.Start();
}

private void OnPollTimer(object sender, ElapsedEventArgs e)
{
    if (!_isConnected) return;
    
    try
    {
        var data = ReadAllData();
        OnDataReceived?.Invoke(data);
    }
    catch (Exception ex)
    {
        OnError?.Invoke($"데이터 읽기 실패: {ex.Message}");
    }
}
```

**체크리스트**:
- [ ] 타이머 기반 폴링 구현
- [ ] 업데이트 주기 설정 (50ms ~ 200ms)
- [ ] 데이터 변경 감지 (delta)
- [ ] 이벤트 발행
- [ ] 성능 모니터링

---

## 📋 Week 5: Phase 8.4 - OPC UA 통신 구현

### Day 19-20: OPC UA 라이브러리 통합
**목표**: OPC UA .NET Standard 라이브러리 통합

**NuGet 패키지**:
```
Install-Package OPCFoundation.NetStandard.Opc.Ua
```

**기본 연결**:
```csharp
using Opc.Ua;
using Opc.Ua.Client;

public class OpcUaClient : IDisposable
{
    private Session _session;
    private Subscription _subscription;
    
    public async Task ConnectAsync(string endpointUrl)
    {
        var config = new ApplicationConfiguration
        {
            ApplicationName = "HK CAM Viewer",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                AutoAcceptUntrustedCertificates = true
            }
        };
        
        var endpoint = new ConfiguredEndpoint(null, 
            new EndpointDescription(endpointUrl));
        
        _session = await Session.Create(config, endpoint, 
            false, "CAM Viewer", 60000, null, null);
    }
}
```

**체크리스트**:
- [ ] NuGet 패키지 설치
- [ ] ApplicationConfiguration 설정
- [ ] 세션 생성 및 연결
- [ ] 보안 인증서 처리
- [ ] 연결 상태 모니터링

### Day 21-22: 노드 탐색 및 데이터 읽기
**목표**: OPC UA 노드 탐색 및 데이터 읽기

**노드 구조** (예시):
```
Root
├── Axis
│   ├── X
│   │   └── Position (ns=2;s=Axis.X.Position)
│   ├── Y
│   │   └── Position (ns=2;s=Axis.Y.Position)
│   └── Z
│       └── Position (ns=2;s=Axis.Z.Position)
└── Laser
    ├── State (ns=2;s=Laser.State)
    └── Power (ns=2;s=Laser.Power)
```

**구현**:
```csharp
public class OpcUaNodeReader
{
    public double ReadNodeValue(string nodeId)
    {
        var node = new NodeId(nodeId);
        var value = _session.ReadValue(node);
        return Convert.ToDouble(value.Value);
    }
    
    public RealtimeData ReadAllData()
    {
        return new RealtimeData
        {
            X = ReadNodeValue("ns=2;s=Axis.X.Position"),
            Y = ReadNodeValue("ns=2;s=Axis.Y.Position"),
            Z = ReadNodeValue("ns=2;s=Axis.Z.Position"),
            LaserOn = ReadNodeValue("ns=2;s=Laser.State") == 1,
            Timestamp = DateTime.Now
        };
    }
}
```

**체크리스트**:
- [ ] 노드 브라우징 기능
- [ ] NodeId 매핑
- [ ] ReadValue 함수 구현
- [ ] WriteValue 함수 구현
- [ ] 배치 읽기/쓰기

### Day 23: 실시간 구독
**목표**: OPC UA 구독 메커니즘 구현

**구독 설정**:
```csharp
private void CreateSubscription()
{
    _subscription = new Subscription(_session.DefaultSubscription)
    {
        PublishingInterval = 100,  // 100ms
        PublishingEnabled = true,
        LifetimeCount = 100,
        MaxNotificationsPerPublish = 1000
    };
    
    // 모니터링 항목 추가
    AddMonitoredItem("ns=2;s=Axis.X.Position");
    AddMonitoredItem("ns=2;s=Axis.Y.Position");
    AddMonitoredItem("ns=2;s=Laser.State");
    
    _session.AddSubscription(_subscription);
    _subscription.Create();
}

private void AddMonitoredItem(string nodeId)
{
    var item = new MonitoredItem
    {
        StartNodeId = new NodeId(nodeId),
        SamplingInterval = 100,
        QueueSize = 10
    };
    
    item.Notification += OnDataChanged;
    _subscription.AddItem(item);
}

private void OnDataChanged(MonitoredItem item, MonitoredItemNotificationEventArgs e)
{
    var value = ((MonitoredItemNotification)e.NotificationValue).Value;
    // 데이터 처리 및 이벤트 발행
}
```

**체크리스트**:
- [ ] Subscription 생성
- [ ] MonitoredItem 추가
- [ ] 데이터 변경 이벤트 처리
- [ ] 구독 관리 (추가/삭제)
- [ ] 재연결 처리

---

## 📋 Week 6: Phase 8.5 - 통합 및 테스트

### Day 24-25: 통신 관리자 구현
**목표**: Itag와 OPC UA를 통합 관리하는 매니저 클래스

```csharp
public class CommunicationManager : IDisposable
{
    private ItagClient _itagClient;
    private OpcUaClient _opcUaClient;
    private CommunicationType _activeType;
    
    public event Action<RealtimeData> OnDataReceived;
    public event Action<string> OnConnectionStateChanged;
    public event Action<string> OnError;
    
    public void SelectCommunication(CommunicationType type)
    {
        DisconnectAll();
        _activeType = type;
    }
    
    public async Task ConnectAsync(ConnectionSettings settings)
    {
        switch (_activeType)
        {
            case CommunicationType.Itag:
                await ConnectItagAsync(settings);
                break;
            case CommunicationType.OpcUa:
                await ConnectOpcUaAsync(settings);
                break;
        }
    }
    
    private void OnDataReceivedInternal(RealtimeData data)
    {
        // 데이터 검증
        if (ValidateData(data))
        {
            OnDataReceived?.Invoke(data);
        }
    }
}
```

**체크리스트**:
- [ ] 통신 타입 선택 로직
- [ ] 연결/해제 통합
- [ ] 이벤트 라우팅
- [ ] 데이터 검증
- [ ] 에러 처리 통합

### Day 26-27: UI 통합
**목표**: 통신 설정 및 모니터링 UI

**UI 디자인**:
```
┌─────────────────────────────────────────┐
│ 통신 설정                                │
├─────────────────────────────────────────┤
│ 통신 방법: ( ) Itag  (●) OPC UA         │
│                                          │
│ [Itag 설정]                              │
│ IP 주소: [192.168.1.100]                 │
│ 포트: [49152]                            │
│                                          │
│ [OPC UA 설정]                            │
│ Endpoint: [opc.tcp://localhost:4840]    │
│ 보안: [None ▼]                          │
│                                          │
│ [연결] [해제] [테스트]                   │
│                                          │
│ 상태: ● 연결됨 (10 Hz)                   │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ 실시간 데이터                            │
├─────────────────────────────────────────┤
│ X: 123.456 mm                            │
│ Y: 234.567 mm                            │
│ Z: 2.500 mm                              │
│                                          │
│ 레이저: ● ON                             │
│ 파트: 3                                  │
│ 컨투어: 5                                │
│                                          │
│ 업데이트: 0.1초 전                        │
└─────────────────────────────────────────┘
```

**체크리스트**:
- [ ] 통신 설정 폼
- [ ] 연결 상태 표시
- [ ] 실시간 데이터 표시
- [ ] 에러 메시지 표시
- [ ] 설정 저장/로드

### Day 28-29: 전체 시스템 테스트
**목표**: 통합 테스트 및 버그 수정

**테스트 시나리오**:
1. **시뮬레이션 모드** (통신 없음)
   - MPF 파일 로드
   - 시뮬레이션 재생
   - 라벨 표시 확인

2. **Itag 통신 모드**
   - NCU 연결
   - 실시간 데이터 수신
   - 트레이스 표시

3. **OPC UA 통신 모드**
   - OPC UA 서버 연결
   - 노드 탐색
   - 실시간 구독

4. **성능 테스트**
   - CPU 사용률
   - 메모리 사용량
   - 업데이트 주기

**체크리스트**:
- [ ] 기능 테스트
- [ ] 성능 테스트
- [ ] 에러 시나리오 테스트
- [ ] 장시간 안정성 테스트
- [ ] 사용성 테스트

### Day 30: 문서화 및 배포
**목표**: 최종 문서화 및 배포 준비

**문서 목록**:
- [ ] 사용자 매뉴얼
- [ ] 설치 가이드
- [ ] 통신 설정 가이드
- [ ] 트러블슈팅 가이드
- [ ] API 문서

**배포 체크리스트**:
- [ ] 빌드 스크립트 작성
- [ ] 설치 프로그램 생성
- [ ] DLL 및 의존성 패키징
- [ ] 릴리스 노트 작성
- [ ] 버전 태깅

---

## 🎯 마일스톤

### Milestone 1: 라벨 표시 완료 (Week 1 종료)
- ✅ 파트 번호 표시
- ✅ 컨투어 번호 표시
- ✅ 동적 크기 조정
- ✅ UI 옵션

### Milestone 2: 트레이스 구현 완료 (Week 3 종료)
- ✅ 절단 진행 추적
- ✅ 완료 경로 렌더링
- ✅ 레이저 헤드 마커
- ✅ 진행률 계산

### Milestone 3: Itag 통신 완료 (Week 4 종료)
- ✅ Siemens DLL 통합
- ✅ 변수 읽기/쓰기
- ✅ 실시간 구독

### Milestone 4: OPC UA 통신 완료 (Week 5 종료)
- ✅ OPC UA 라이브러리 통합
- ✅ 노드 탐색 및 읽기
- ✅ 실시간 구독

### Milestone 5: Phase 8 완료 (Week 6 종료)
- ✅ 통합 및 테스트
- ✅ UI 완성
- ✅ 문서화
- ✅ 배포 준비

---

## 📊 진행 상황 추적

| 작업 | 예상 시간 | 진행률 | 상태 |
|------|----------|--------|------|
| 8.1 라벨 표시 | 5일 | 0% | 대기 |
| 8.2 트레이스 구현 | 8일 | 0% | 대기 |
| 8.3 Itag 통신 | 5일 | 0% | 대기 |
| 8.4 OPC UA 통신 | 5일 | 0% | 대기 |
| 8.5 통합 테스트 | 7일 | 0% | 대기 |

**전체 진행률**: 0% (0/30일)

---

**다음 작업**: Phase 8.1 시작 - OpenGL 텍스트 렌더링 시스템 구현
