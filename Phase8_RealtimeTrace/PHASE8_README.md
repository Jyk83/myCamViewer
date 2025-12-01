# Phase 8: 실시간 트레이스 및 통신 구현

## 📋 프로젝트 개요

Phase 8은 HK 레이저 절단기의 **실시간 절단 진행 상황 모니터링** 및 **장비 통신** 기능을 구현하는 단계입니다.
Phase 7의 고급 렌더링 기능을 기반으로, 실제 장비와의 통신을 통해 실시간으로 절단 상태를 시각화합니다.

## 🎯 개발 목표

### 1. 파트/컨투어 번호 표시
- **참조**: HKCamInterface 프로젝트의 OpenGLCAMViewWnd.cpp/h
- 파트 번호 표시 (Part Number Display)
- 컨투어 번호 표시 (Contour Number Display)
- 줌 레벨에 따른 동적 크기 조정
- 사용자 설정 가능한 폰트 크기

### 2. 실시간 트레이스 구현
- **참조**: HKCAMInterfaceDLL.cpp의 `CVStartCutting()`, `CVUpdateCutting()` 함수
- 실시간 절단 진행 상황 표시
- 레이저 헤드 현재 위치 추적
- 완료된 경로와 진행 중인 경로 구분 렌더링
- 절단 거리 및 진행률 계산

### 3. 장비 통신 구현
- **Itag 통신**: Siemens NCU와의 변수 통신
- **OPC UA 통신**: 표준 산업용 통신 프로토콜
- 통신 방법 선택 가능 (Itag / OPC UA)
- 실시간 위치 데이터 수신 (X, Y, Z 좌표)
- 절단 상태 모니터링 (레이저 ON/OFF, 파트/컨투어 번호)

## 📂 프로젝트 구조

```
Phase8_RealtimeTrace/
├── WinFormsApp/           # C# WinForms 메인 애플리케이션
│   ├── MainForm.cs        # 메인 UI
│   ├── ViewerControl.cs   # 뷰어 컨트롤
│   └── Communication/     # 통신 모듈 (NEW)
│       ├── ItagClient.cs      # Itag 통신 클라이언트
│       ├── OpcUaClient.cs     # OPC UA 통신 클라이언트
│       └── CommunicationManager.cs  # 통신 관리자
├── NativeRenderer/        # C++ OpenGL 렌더링 엔진
│   ├── MPFParser/         # MPF 파일 파서
│   └── OpenGLRenderer/    # OpenGL 렌더링
└── SampleMPF/             # 테스트용 MPF 파일
```

## 🔧 주요 구현 기능

### 1. 파트/컨투어 번호 표시 시스템

**HKCamInterface 참조 코드:**
```cpp
// OpenGLCAMViewWnd.h
void DisplayPartNo(bool bDisplay);
void DisplayContourNo(bool bDisplay);
void SetPartNoHeight(int nHeight);
void SetContourNoHeight(int nHeight);
void LimitPartContourNumbers(int nMaxCount);

// 옵션 코드
#define iOPT_PART_NUMBER        102  // 파트 번호 표시 on/off
#define iOPT_PARTNO_HEIGHT      104  // 파트 번호 폰트 높이 (픽셀)
#define iOPT_CONTOUR_NUMBER     105  // 컨투어 번호 표시 on/off
#define iOPT_CONTOURNO_HEIGHT   106  // 컨투어 번호 폰트 높이 (픽셀)
#define iOPT_LIMIT_PART_CONT_NO 107  // 표시 제한 개수
```

**구현 계획:**
- OpenGL 텍스트 렌더링을 통한 번호 표시
- 바운딩 박스 기반 위치 계산
- 동적 크기 조정 (줌 레벨에 따라)
- 성능 최적화 (표시 개수 제한)

### 2. 실시간 트레이스 시스템

**HKCAMInterfaceDLL 참조 함수:**
```cpp
// 절단 시작
DLLSPEC int CVStartCutting(HWND hWndCV, int nPartFrom, int nContourFrom, int nIsReverse);

// 절단 진행 업데이트 (방법 1: G-code 블록 기반)
DLLSPEC int CVUpdateCutting(HWND hWndCV, int part, int contour, 
                             const char* currentBlock, double progress, 
                             double xwcs, double ywcs, int isGcodeBlock);

// 절단 진행 업데이트 (방법 2: MPF 라인 번호 기반)
DLLSPEC int CVUpdateCutting2(HWND hWndCV, int part, int contour, 
                              int mpfLineNo, double progress, 
                              double xwcs, double ywcs);

// 컨투어 길이 조회
DLLSPEC int CVGetContourLength(HWND hWndCV, int part, int contour, 
                                double& length, bool isReset);
```

**구현 계획:**
- 절단 진행 상태 추적 (part, contour, element, progress)
- 완료된 경로 색상 변경 (빨간색)
- 현재 절단 위치 하이라이트 (발광 효과)
- 레이저 헤드 마커 표시
- 진행률 계산 및 표시

### 3. 통신 시스템 설계

#### 3.1 Itag 통신 (Siemens 전용)

**참조 문서**: `109760182_ActiveXWCCPenUS.pdf`, `109760182_ControlDevelopment_samples.7z`

**Itag 변수 목록:**
```
// 실시간 위치
$AA_IW[X] - X축 실제 위치 (mm)
$AA_IW[Y] - Y축 실제 위치 (mm)
$AA_IW[Z] - Z축 실제 위치 (mm)

// 절단 상태
$AC_MARKER[0] - 레이저 상태 (0=OFF, 1=ON)
$AC_MARKER[1] - 현재 파트 번호
$AC_MARKER[2] - 현재 컨투어 번호

// 진행 상황
$AC_MARKER[10] - 절단 진행률 (0-100%)
```

**구현 방법:**
```csharp
// Siemens.Runtime.ControlDev.dll 사용
using Siemens.Runtime.ControlDev;

public class ItagClient
{
    private ITagService _tagService;
    
    public void Connect(string ipAddress)
    {
        _tagService = new TagService();
        _tagService.Connect(ipAddress);
    }
    
    public double ReadAxisPosition(string axis)
    {
        return _tagService.ReadTag($"$AA_IW[{axis}]");
    }
    
    public void SubscribeRealtimeData(Action<RealtimeData> callback)
    {
        // 주기적으로 변수 읽기 (100ms 간격)
        _timer = new Timer(100);
        _timer.Elapsed += (s, e) => {
            var data = new RealtimeData {
                X = ReadAxisPosition("X"),
                Y = ReadAxisPosition("Y"),
                Z = ReadAxisPosition("Z"),
                LaserOn = ReadMarker(0) == 1,
                PartNumber = (int)ReadMarker(1),
                ContourNumber = (int)ReadMarker(2)
            };
            callback(data);
        };
    }
}
```

#### 3.2 OPC UA 통신 (표준 프로토콜)

**장점:**
- 산업 표준 프로토콜
- 다양한 장비 지원
- 보안 기능 내장

**구현 방법:**
```csharp
// OPC UA .NET 라이브러리 사용
using Opc.Ua;
using Opc.Ua.Client;

public class OpcUaClient
{
    private Session _session;
    
    public async Task ConnectAsync(string endpointUrl)
    {
        var config = new ApplicationConfiguration();
        var endpoint = new ConfiguredEndpoint(null, 
            new EndpointDescription(endpointUrl));
        
        _session = await Session.Create(config, endpoint, 
            false, "CAM Viewer", 60000, null, null);
    }
    
    public double ReadNodeValue(string nodeId)
    {
        var node = new NodeId(nodeId);
        var value = _session.ReadValue(node);
        return Convert.ToDouble(value.Value);
    }
    
    public void SubscribeRealtimeData(Action<RealtimeData> callback)
    {
        var subscription = new Subscription(_session.DefaultSubscription) {
            PublishingInterval = 100,
            PublishingEnabled = true
        };
        
        // 구독할 노드 추가
        var items = new List<MonitoredItem> {
            new MonitoredItem { NodeId = "ns=2;s=Axis.X.Position" },
            new MonitoredItem { NodeId = "ns=2;s=Axis.Y.Position" },
            new MonitoredItem { NodeId = "ns=2;s=Laser.State" }
        };
        
        subscription.AddItems(items);
        _session.AddSubscription(subscription);
        subscription.Create();
    }
}
```

#### 3.3 통신 관리자

```csharp
public enum CommunicationType
{
    None,
    Itag,
    OpcUa
}

public class CommunicationManager
{
    private ItagClient _itagClient;
    private OpcUaClient _opcUaClient;
    private CommunicationType _activeType;
    
    public event Action<RealtimeData> OnDataReceived;
    
    public void SelectCommunication(CommunicationType type)
    {
        DisconnectAll();
        _activeType = type;
    }
    
    public async Task ConnectAsync(string address)
    {
        switch (_activeType)
        {
            case CommunicationType.Itag:
                _itagClient.Connect(address);
                _itagClient.SubscribeRealtimeData(data => OnDataReceived?.Invoke(data));
                break;
                
            case CommunicationType.OpcUa:
                await _opcUaClient.ConnectAsync(address);
                _opcUaClient.SubscribeRealtimeData(data => OnDataReceived?.Invoke(data));
                break;
        }
    }
}
```

## 🔄 실시간 데이터 흐름

```
┌──────────────────┐
│   NCU1760 장비    │
│  (Siemens PLC)   │
└────────┬─────────┘
         │
         ├─ Itag 변수 ($AA_IW, $AC_MARKER)
         │
         └─ OPC UA 노드 (Axis.X.Position 등)
         │
         ↓
┌────────────────────────┐
│  통신 클라이언트        │
│  (ItagClient/OpcUa)    │
└───────────┬────────────┘
            │ RealtimeData
            ↓
┌────────────────────────┐
│  CommunicationManager  │
│  - 데이터 수집         │
│  - 이벤트 발행         │
└───────────┬────────────┘
            │ OnDataReceived
            ↓
┌────────────────────────┐
│  MainForm              │
│  - 데이터 처리         │
│  - UI 업데이트         │
└───────────┬────────────┘
            │ UpdateCutting()
            ↓
┌────────────────────────┐
│  Native Renderer       │
│  - 경로 업데이트       │
│  - 레이저 헤드 표시    │
│  - OpenGL 렌더링       │
└────────────────────────┘
```

## 📊 데이터 구조

```csharp
public class RealtimeData
{
    // 위치 정보
    public double X { get; set; }      // X축 위치 (mm)
    public double Y { get; set; }      // Y축 위치 (mm)
    public double Z { get; set; }      // Z축 위치 (mm)
    
    // 절단 상태
    public bool LaserOn { get; set; }  // 레이저 ON/OFF
    public int PartNumber { get; set; }     // 현재 파트 번호
    public int ContourNumber { get; set; }  // 현재 컨투어 번호
    
    // 진행 정보
    public double Progress { get; set; }    // 진행률 (0-100%)
    public double CutDistance { get; set; } // 절단 거리 (mm)
    
    // 타임스탬프
    public DateTime Timestamp { get; set; }
}

public class CuttingProgress
{
    public int StartPart { get; set; }
    public int StartContour { get; set; }
    public int CurrentPart { get; set; }
    public int CurrentContour { get; set; }
    public int CurrentElement { get; set; }
    public double ElementProgress { get; set; }  // 0.0 ~ 1.0
    
    // 렌더링 정보
    public List<CompletedPath> CompletedPaths { get; set; }
    public Point2D CurrentPosition { get; set; }
    public double TotalCutDistance { get; set; }
}
```

## 🎨 렌더링 전략

### 1. 완료된 경로 표시
- **색상**: 빨간색 (#FF0000)
- **두께**: 일반 경로보다 약간 굵게
- **투명도**: 완전 불투명

### 2. 현재 절단 위치
- **레이저 헤드 마커**: 밝은 파란색 원형 마커
- **발광 효과**: Glow shader 적용
- **크기**: 줌 레벨에 따라 동적 조정

### 3. 진행 중인 세그먼트
- **색상**: 주황색 (#FFA500)
- **애니메이션**: 점진적 색상 변화
- **진행률 표시**: 세그먼트 일부만 색상 적용

## 🔨 구현 단계

### Phase 8.1: 파트/컨투어 번호 표시
**기간**: 1주
- [ ] OpenGL 텍스트 렌더링 시스템 구현
- [ ] 바운딩 박스 기반 라벨 위치 계산
- [ ] 동적 크기 조정 로직
- [ ] UI 옵션 추가 (표시 on/off, 폰트 크기)

### Phase 8.2: 실시간 트레이스 구현
**기간**: 2주
- [ ] 절단 진행 상태 추적 시스템
- [ ] 완료된 경로 렌더링
- [ ] 레이저 헤드 마커 표시
- [ ] 진행률 계산 및 표시
- [ ] 테스트 및 디버깅

### Phase 8.3: Itag 통신 구현
**기간**: 1주
- [ ] Siemens.Runtime.ControlDev.dll 통합
- [ ] Itag 클라이언트 구현
- [ ] 변수 읽기/쓰기 기능
- [ ] 실시간 데이터 구독
- [ ] 에러 처리

### Phase 8.4: OPC UA 통신 구현
**기간**: 1주
- [ ] OPC UA .NET 라이브러리 통합
- [ ] OPC UA 클라이언트 구현
- [ ] 노드 탐색 기능
- [ ] 실시간 데이터 구독
- [ ] 보안 설정

### Phase 8.5: 통합 및 테스트
**기간**: 1주
- [ ] 통신 관리자 구현
- [ ] 통신 방법 선택 UI
- [ ] 전체 시스템 통합 테스트
- [ ] 성능 최적화
- [ ] 문서화

## 📚 참조 문서

### HKCamInterface 프로젝트
- `HKCAMInterfaceDLL.h` - DLL 인터페이스 정의
- `HKCAMInterfaceDLL.cpp` - 실시간 트레이스 함수 구현
- `OpenGLCAMViewWnd.h` - 뷰어 클래스 정의
- `OpenGLCAMViewWnd.cpp` - 렌더링 및 번호 표시 구현
- `CAMViewData.h` - 데이터 구조 정의

### Siemens 문서
- `109760182_ActiveXWCCPenUS.pdf` - ActiveX WCC 매뉴얼
- `109760182_ControlDevelopment_samples.7z` - 샘플 코드
- `Siemens.Runtime.ControlDev.dll` - .NET 인터페이스 DLL

### 기술 문서
- OPC UA .NET Standard: https://github.com/OPCFoundation/UA-.NETStandard
- Siemens Sinumerik ONE NCU1760 프로그래밍 매뉴얼
- HK 레이저 절단기 통신 사양서

## 🚀 빠른 시작

### 1. 프로젝트 빌드
```bash
cd Phase8_RealtimeTrace
build_all.bat
```

### 2. 테스트 실행
```bash
# 시뮬레이션 모드 (통신 없음)
WinFormsApp\bin\Debug\CamViewerPOC.exe

# Itag 통신 모드
WinFormsApp\bin\Debug\CamViewerPOC.exe /comm:itag /ip:192.168.1.100

# OPC UA 통신 모드
WinFormsApp\bin\Debug\CamViewerPOC.exe /comm:opcua /url:opc.tcp://localhost:4840
```

## 🎯 성공 기준

- [ ] 파트 번호가 정확한 위치에 표시됨
- [ ] 컨투어 번호가 정확한 위치에 표시됨
- [ ] 줌 레벨에 따라 번호 크기가 적절히 조정됨
- [ ] 실시간으로 절단 진행 상황이 업데이트됨
- [ ] 완료된 경로가 빨간색으로 표시됨
- [ ] 레이저 헤드 현재 위치가 표시됨
- [ ] Itag 통신으로 실시간 데이터 수신
- [ ] OPC UA 통신으로 실시간 데이터 수신
- [ ] 통신 방법을 사용자가 선택 가능
- [ ] 통신 에러 발생 시 적절한 처리
- [ ] 초당 10회 이상 데이터 업데이트 (100ms 이하)
- [ ] CPU 사용률 10% 이하
- [ ] 메모리 사용량 200MB 이하

## 📝 작업 일지

### 2025-11-27
- Phase 8 프로젝트 생성 (Phase 7 복사)
- Phase 8 README 작성
- 개발 계획 수립
- HKCamInterface 참조 코드 분석
- 통신 시스템 설계

---

**다음 단계**: Phase 8.1 파트/컨투어 번호 표시 구현 시작
