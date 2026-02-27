# V20 MPF Viewer - C# 프로젝트 분석 리포트

## 📋 프로젝트 개요

**프로젝트명**: V20 MPF Viewer  
**출력명**: HK MPF Viewer  
**언어**: C# (.NET Framework 4.5)  
**플랫폼**: Windows Forms Application  
**개발 도구**: Visual Studio 2015  
**프로젝트 타입**: WinExe (Windows 실행 파일)

---

## 🎯 프로젝트 목적

V20 MPF Viewer는 **CNC 공작 기계용 MPF(Machine Program File) 파일을 시각화**하고 시뮬레이션하는 윈도우즈 데스크톱 애플리케이션입니다. Siemens Sinumerik CNC 컨트롤러에서 생성된 MPF 파일을 읽어 2D 그래픽으로 표시하고, 절단 경로를 시뮬레이션할 수 있습니다.

### 주요 용도:
- **CNC 프로그램 검증**: 실제 가공 전 절단 경로 확인
- **레이저/플라즈마 절단 시각화**: Part, Contour, Element 단위로 절단 경로 표시
- **가공 시뮬레이션**: 실시간 절단 진행 상황 시뮬레이션
- **파일 브라우징**: MPF 파일 탐색 및 관리

---

## 📂 프로젝트 구조

### 주요 파일 구성

```
V20 MPF Viewer/
├── V20 MPF Viewer.sln              # 솔루션 파일
├── V20 MPF Viewer.v12.suo          # Visual Studio 사용자 설정
├── .vs/                            # Visual Studio 캐시
└── V20 MPF Viewer/                 # 메인 프로젝트 폴더
    ├── Program.cs                  # 애플리케이션 진입점
    ├── Form1.cs                    # 메인 폼 (핵심 로직)
    ├── Form1.Designer.cs           # 메인 폼 UI 디자이너
    ├── Form1.resx                  # 메인 폼 리소스
    ├── CodeViewer.cs               # 코드 뷰어 폼
    ├── CodeViewer.Designer.cs      # 코드 뷰어 UI
    ├── CodeViewer.resx             # 코드 뷰어 리소스
    ├── DoubleBufferPanel.cs        # 더블 버퍼링 패널 (성능 최적화)
    ├── HKUtils.cs                  # 유틸리티 클래스
    ├── V20 MPF Viewer.csproj       # 프로젝트 파일
    ├── App.config                  # 애플리케이션 설정
    ├── hk_icon2.ico                # 애플리케이션 아이콘
    ├── Dlls/                       # 네이티브 DLL 및 인터페이스
    │   ├── HKCamInterface.cs       # CAM 뷰어 DLL 인터페이스
    │   └── HKCAMInterface.dll      # 네이티브 C++ DLL (추정)
    ├── bin/                        # 빌드 출력
    ├── obj/                        # 중간 빌드 파일
    ├── Properties/                 # 어셈블리 정보
    │   ├── AssemblyInfo.cs
    │   ├── Resources.resx
    │   ├── Resources.Designer.cs
    │   ├── Settings.settings
    │   └── Settings.Designer.cs
    └── Resources/                  # 이미지 리소스
        ├── RealSize.png
        ├── zoom_in.png
        ├── zoom_out.png
        ├── widthFit.png
        └── laser.png
```

---

## 🔧 핵심 기능 분석

### 1. **파일 탐색 기능** (TreeView + ListView)

#### 구현 위치: `Form1.cs` - Line 49~299

**주요 메서드**:
- `Form1_Load()`: 드라이브 정보 로드 및 TreeView 초기화
- `Fill(TreeNode dirNode)`: 디렉토리 하위 폴더 추가
- `treeView1_NodeMouseClick()`: 폴더 클릭 시 MPF 파일 목록 표시

**특징**:
- 기본 경로: `C:\ProgramData\Siemens\MotionControl\User\Sinumerik\Data\Prog`
- MPF 파일 필터링 (`.mpf`, `.MPF` 확장자만 표시)
- ListView에 파일명, 수정일, 타입, 크기, 전체 경로 표시
- FolderBrowserDialog를 통한 사용자 정의 경로 설정

```csharp
// 코드 예시 (Line 250-266)
if (fileinfo.Extension == ".mpf" || fileinfo.Extension == ".MPF")
{
    listView1.Items.Add(fileinfo.Name);
    listView1.Items[Count].SubItems.Add(fileinfo.LastWriteTime.ToString());
    listView1.Items[Count].SubItems.Add(fileinfo.Attributes.ToString());
    listView1.Items[Count].SubItems.Add(fileinfo.Length.ToString());
    listView1.Items[Count].SubItems.Add(fileinfo.FullName.ToString());
    Count++;
}
```

---

### 2. **MPF 파일 뷰어** (HK CAM Viewer Integration)

#### 구현 위치: `Form1.cs` - Line 301~405

**주요 메서드**:
- `CreateHKCamViewer()`: HK CAM Viewer 윈도우 생성
- `LoadMpf(string mpfPath, string mpfName)`: MPF 파일 로드
- `Display()`: 프로그램 정보 및 그래픽 표시
- `TraceMouseCoords(double x, double y)`: 마우스 위치의 실제 좌표 추적

**네이티브 DLL 연동**:
- `HKCAMInterface.dll`을 P/Invoke로 호출
- `CVCreateWindow()`: CAM 뷰어 윈도우 생성
- `CVLoadCAMFile()`: MPF 파일 로드
- `CVGetWholeSize()`: 전체 시트 크기 조회

**시각화 옵션**:
- Part Number 표시 (`chxShowPartNo`)
- Contour Number 표시 (`chxShowContourNo`)
- 줌 인/아웃 기능
- 실제 크기 보기
- 창에 맞춤

```csharp
// 코드 예시 (Line 310-326)
CVHandle = HKCamInterface.CVCreateWindow(
    this.panelCV.Handle, 
    HKCamInterface.CVID_REAL_GRAPHIC, 
    0, 0, 
    this.panelCV.Size.Width, 
    this.panelCV.Size.Height, 
    _traceMouseCoordsDelegate
);

HKCamInterface.CVSetOption(CVHandle, (int)HKCamInterface.CVOption.PART_NUMBER, 
    chxShowPartNo.Checked ? (uint)1 : (uint)0);
```

---

### 3. **절단 시뮬레이션** (Cutting Simulation)

#### 구현 위치: `Form1.cs` - Line 510~788

**주요 메서드**:
- `OnSimulation()`: 실제 시뮬레이션 루프
- `OnStart(bool isInverse)`: 시뮬레이션 시작
- `GetAllBlocks()`: 모든 Block 코드 조회
- `DisplayCuttingDoneArea()`: 절단 완료 영역 표시

**시뮬레이션 로직**:
1. **3단계 계층 구조**: Part → Contour → Element
2. **비동기 처리**: 별도 Thread에서 시뮬레이션 실행
3. **타이밍 제어**: 50ms 간격으로 Element 업데이트 (`_responseTime`)
4. **실시간 피드백**: G-code 블록을 ListBox에 표시

**시뮬레이션 흐름**:
```
Part 1 → Contour 1 → Element 0, 1, 2, ...
      → Contour 2 → Element 0, 1, 2, ...
Part 2 → Contour 1 → ...
```

```csharp
// 코드 예시 (Line 634-656)
for (ei = _element; ei < elementCount;)
{
    System.Threading.Thread.Sleep(20);
    
    if (_tc.IsProgressComplete(_responseTime))
    {
        var random = new Random();
        var pathn = (Convert.ToDouble(random.Next(1, 100000)) / Convert.ToDouble(100000));
        block = GetBlockCode(pi, ci, ei);
        
        if (!string.IsNullOrEmpty(block))
        {
            this.Invoke((Action)(() =>
            {
                HKCamInterface.CVUpdateCutting(CVHandle, pi, ci, block, pathn, 0, 0, 1);
                listboxAdd(string.Format("Part:{0}, Contour:{1}, Element:{2}, G code: {3}", 
                    pi, ci, ei, block));
            }));
        }
        ei++;
        _tc.StartTimer();
    }
}
```

**중요 특징**:
- **Thread Safety**: `Invoke()`를 사용한 UI 스레드 동기화
- **일시정지/재개**: `_isRun` 플래그로 제어
- **진행률 추적**: Part, Contour, Element 위치 저장

---

### 4. **줌 및 뷰 제어**

#### 구현 위치: `Form1.cs` - Line 408~427

**제공 기능**:
- **Zoom In** (`btn_ZoomIn_Click`)
- **Zoom Out** (`btn_Zoomout_Click`)
- **Real Size** (`btn_RealSize_Click`): 1:1 비율 표시
- **Fit to Window** (`btn_FitToWnd_Click`): 창에 맞춤

```csharp
private void btn_ZoomIn_Click(object sender, EventArgs e)
{
    HKCamInterface.CVZoomContens(CVHandle, 1);
}

private void btn_FitToWnd_Click(object sender, EventArgs e)
{
    HKCamInterface.CVZoomFitToWindow(CVHandle);
}
```

---

## 🔌 외부 의존성 분석

### 1. **HKCAMInterface.dll** (네이티브 C++ DLL)

#### P/Invoke 선언 위치: `HKCamInterface.cs`

**주요 함수**:
```csharp
[DllImport("DLLs\\HKCAMInterface.dll", CallingConvention = CallingConvention.Cdecl)]
internal static extern IntPtr CVCreateWindow(
    IntPtr hWndParent, uint ID, int x, int y, int cx, int cy, 
    TraceMouseCoordsCallback callback
);

[DllImport("DLLs\\HKCAMInterface.dll")]
internal static extern int CVLoadCAMFile(
    IntPtr hWndCV, byte[] pszFilePath, 
    StringBuilder error, int length
);

[DllImport("DLLs\\HKCAMInterface.dll")]
internal static extern int CVUpdateCutting(
    IntPtr hWndCV, int part, int contour, 
    string block, double progress, int x, int y, int z
);
```

**DLL 역할**:
- MPF 파일 파싱 및 렌더링 엔진
- 2D CAD 그래픽 렌더링
- Part/Contour/Element 계층 구조 관리
- 절단 경로 시뮬레이션 지원

**에러 코드**:
- `CV_NOERROR = 0`: 성공
- `CVERR_FILELOAD = 201`: 파일 로드 실패
- `CVERR_CAMDATA = 301`: MPF 데이터 손상
- `CVERR_INVALID_PART_NO = 304`: 잘못된 Part 번호

---

### 2. **.NET Framework 참조**

#### 프로젝트 파일: `V20 MPF Viewer.csproj` - Line 40~52

```xml
<Reference Include="System" />
<Reference Include="System.Core" />
<Reference Include="System.Drawing" />
<Reference Include="System.Windows.Forms" />
<Reference Include="System.Data" />
<Reference Include="System.Xml" />
<Reference Include="System.Net.Http" />
```

---

## 💡 디자인 패턴 및 아키텍처

### 1. **MVP 패턴 (Model-View-Presenter 변형)**

- **View**: `Form1.cs` (UI 이벤트 처리)
- **Model**: HKCAMInterface.dll (데이터 및 비즈니스 로직)
- **Presenter**: `Form1.cs`의 로직 메서드들

### 2. **UI 스레드와 백그라운드 스레드 분리**

```csharp
// Line 717-724
private Thread _threadSimulation = null;
private void InitSimulation()
{
    if (_threadSimulation == null)
    {
        _threadSimulation = new Thread(OnThreadSimulation);
    }
}
```

**장점**:
- UI Blocking 방지
- 시뮬레이션 중단/재개 가능

**단점**:
- `Thread.Abort()` 사용 (권장되지 않는 방법, .NET Core에서 제거됨)

### 3. **더블 버퍼링** (DoubleBufferPanel)

#### 구현: `DoubleBufferPanel.cs`

```csharp
public class DoubleBufferPanel : Panel
{
    public DoubleBufferPanel()
    {
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint, true);
        this.UpdateStyles();
    }
}
```

**목적**: 그래픽 렌더링 시 깜빡임(flickering) 방지

---

## ⚠️ 발견된 문제점 및 개선 사항

### 1. **스레드 관리 문제**

**현재 코드 (Line 670, 736)**:
```csharp
_threadSimulation.Abort();  // ⚠️ 위험: Thread.Abort()는 권장되지 않음
```

**문제점**:
- `Thread.Abort()`는 불안정하며 리소스 누수 가능
- .NET Core/.NET 5+에서는 제거됨

**개선 방안**:
```csharp
// CancellationToken 사용 권장
private CancellationTokenSource _cts;

private void OnStart(bool isInverse)
{
    _cts = new CancellationTokenSource();
    Task.Run(() => OnSimulation(_cts.Token), _cts.Token);
}

private void OnStop()
{
    _cts?.Cancel();
}

private void OnSimulation(CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        // 시뮬레이션 로직
    }
}
```

---

### 2. **예외 처리 누락**

**현재 코드 (Line 194-198)**:
```csharp
catch (Exception ex)
{
    //MessageBox.Show("Error : " + ex.Message);  // 주석 처리됨
}
```

**문제점**:
- 예외가 무시되어 디버깅 어려움
- 사용자에게 에러 피드백 없음

**개선 방안**:
```csharp
catch (UnauthorizedAccessException ex)
{
    listboxAdd($"접근 권한 없음: {ex.Message}");
}
catch (DirectoryNotFoundException ex)
{
    listboxAdd($"디렉토리를 찾을 수 없음: {ex.Message}");
}
catch (Exception ex)
{
    listboxAdd($"오류 발생: {ex.Message}");
    // 로그 기록
}
```

---

### 3. **하드코딩된 경로**

**현재 코드 (Line 21)**:
```csharp
public string defaultDir = @"C:\ProgramData\Siemens\MotionControl\User\Sinumerik\Data\Prog";
```

**문제점**:
- 다른 CNC 시스템과 호환 불가
- 경로가 존재하지 않을 경우 오류

**개선 방안**:
```csharp
// App.config 또는 설정 파일 사용
public string defaultDir = ConfigurationManager.AppSettings["DefaultMpfPath"] 
    ?? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
```

---

### 4. **UI 업데이트 성능 문제**

**현재 코드 (Line 648-652)**:
```csharp
this.Invoke((Action)(() =>
{
    HKCamInterface.CVUpdateCutting(CVHandle, pi, ci, block, pathn, 0, 0, 1);
    listboxAdd(string.Format("Part:{0}, Contour:{1}, Element:{2}, G code: {3}", pi, ci, ei, block));
}));
```

**문제점**:
- 매 Element마다 Invoke 호출 → 성능 저하
- ListBox에 무제한 추가 → 메모리 증가

**개선 방안**:
```csharp
// 배치 업데이트 또는 버퍼링
private Queue<string> _logQueue = new Queue<string>();

private void FlushLogs()
{
    this.Invoke((Action)(() =>
    {
        while (_logQueue.Count > 0)
        {
            listboxAdd(_logQueue.Dequeue());
        }
        
        // ListBox 항목 수 제한
        while (listBox1.Items.Count > 1000)
        {
            listBox1.Items.RemoveAt(0);
        }
    }));
}
```

---

### 5. **리소스 해제 누락**

**현재 코드 (Line 817-820)**:
```csharp
private void Form1_FormClosing(object sender, FormClosingEventArgs e)
{
    DeleteCamViewer();
}
```

**문제점**:
- Thread 종료 처리 없음
- DLL 핸들 해제만 수행

**개선 방안**:
```csharp
private void Form1_FormClosing(object sender, FormClosingEventArgs e)
{
    _isRun = false;
    _cts?.Cancel();
    
    if (_threadSimulation?.IsAlive == true)
    {
        if (!_threadSimulation.Join(1000))  // 1초 대기
        {
            // 강제 종료 필요 시에만
        }
    }
    
    DeleteCamViewer();
}
```

---

## 🚀 확장 가능성 및 추천 개선 사항

### 1. **현대화 (.NET Core/.NET 6+ 마이그레이션)**

**이유**:
- .NET Framework 4.5는 2016년 릴리스 (구형)
- 성능 향상 및 크로스 플랫폼 지원

**마이그레이션 체크리스트**:
- [ ] WinForms → WPF 또는 .NET 6 WinForms
- [ ] `Thread` → `Task` 및 `async/await`
- [ ] P/Invoke 호환성 확인 (HKCAMInterface.dll)

---

### 2. **3D 뷰어 추가**

**현재**: 2D 평면도만 제공  
**제안**: 3D 절단 경로 시각화 (Z축 포함)

**구현 옵션**:
- WPF 3D (Viewport3D)
- OpenGL (SharpGL 라이브러리)
- DirectX (SharpDX)

---

### 3. **파일 형식 확장**

**현재**: MPF 파일만 지원  
**제안**: 다른 CNC 포맷 지원
- G-code (ISO 6983)
- DXF (AutoCAD)
- NC/TAP 파일

---

### 4. **실시간 CNC 연동**

**제안**: 실제 CNC 기계와 통신하여 실시간 진행 상황 모니터링

**구현 방법**:
- OPC UA 프로토콜
- TCP/IP 소켓 통신
- Serial 통신 (RS-232)

---

### 5. **클라우드 기반 협업**

**제안**:
- MPF 파일 클라우드 저장
- 다중 사용자 동시 접근
- 버전 관리 (Git 통합)

---

## 📊 코드 품질 평가

| 항목 | 평가 | 점수 | 비고 |
|------|------|------|------|
| **가독성** | 🟢 양호 | 7/10 | 한글 주석, 명확한 네이밍 |
| **유지보수성** | 🟡 보통 | 6/10 | 메서드 분리 필요, 일부 중복 코드 |
| **성능** | 🟢 양호 | 7/10 | 더블 버퍼링 적용, 비동기 처리 |
| **안정성** | 🟡 보통 | 5/10 | Thread.Abort 사용, 예외 처리 부족 |
| **확장성** | 🟡 보통 | 6/10 | 하드코딩된 값 많음, DLL 의존성 높음 |
| **테스트 가능성** | 🔴 낮음 | 3/10 | 단위 테스트 없음, UI 로직 혼재 |

**종합 평점**: **34/60** (57%)

---

## 🛠️ 개발 환경 설정 가이드

### 필요 소프트웨어

1. **Visual Studio 2015 이상**
   - Workload: .NET Desktop Development
   - 언어: C#

2. **필수 구성 요소**:
   - .NET Framework 4.5 SDK
   - Windows Forms 디자이너

### 빌드 방법

```bash
# 명령줄 빌드
msbuild "V20 MPF Viewer.sln" /p:Configuration=Release /p:Platform="Any CPU"

# 또는 Visual Studio에서
1. 솔루션 열기: V20 MPF Viewer.sln
2. 빌드 → 솔루션 빌드 (Ctrl+Shift+B)
3. 출력: bin\Release\HK MPF Viewer.exe
```

### 배포 체크리스트

- [ ] HKCAMInterface.dll 포함
- [ ] .NET Framework 4.5 설치 확인
- [ ] DLLs 폴더 포함
- [ ] hk_icon2.ico 리소스 임베딩
- [ ] 기본 경로 존재 여부 확인

---

## 📝 코드 메트릭스

### 파일별 코드 라인 수

| 파일명 | 코드 라인 | 주석 라인 | 공백 라인 | 총 라인 |
|--------|-----------|----------|----------|---------|
| Form1.cs | 750 | 80 | 58 | 888 |
| HKCamInterface.cs | ~500 (추정) | 100 | 50 | ~650 |
| Form1.Designer.cs | 600 | 20 | 30 | 650 |
| CodeViewer.Designer.cs | 70 | 5 | 5 | 80 |
| DoubleBufferPanel.cs | 15 | 3 | 2 | 20 |
| HKUtils.cs | 25 | 8 | 5 | 38 |
| Program.cs | 18 | 4 | 0 | 22 |

**총계**: ~1,978 라인 (코드) + ~220 라인 (주석)

---

## 🔍 주요 알고리즘 분석

### 1. **시뮬레이션 타이밍 제어**

#### TimeCheck 클래스 (Line 841-887)

```csharp
public class TimeCheck
{
    private Stopwatch _sw = new Stopwatch();
    
    public bool IsProgressComplete(long nMilliseconds)
    {
        if (_sw.ElapsedMilliseconds >= nMilliseconds) return true;
        return false;
    }
}
```

**목적**: 50ms 간격으로 Element 업데이트 제어

---

### 2. **계층 구조 순회 (Triple Nested Loop)**

```csharp
for (pi = _part; pi <= partCount; pi++)              // Part 레벨
{
    for (ci = _contour; ci <= contourCount; ci++)    // Contour 레벨
    {
        for (ei = _element; ei < elementCount; ei++) // Element 레벨
        {
            // 시뮬레이션 로직
        }
    }
}
```

**시간 복잡도**: O(P × C × E)  
- P: Part 수
- C: Contour 수 (평균)
- E: Element 수 (평균)

---

## 🎓 학습 포인트

### C# 개발자를 위한 교육 가치

1. **P/Invoke (Platform Invocation Services)**
   - 네이티브 DLL 호출 방법
   - 메모리 관리 및 마샬링

2. **멀티스레딩**
   - UI 스레드와 백그라운드 스레드 분리
   - `Invoke()` 패턴

3. **WinForms 컨트롤**
   - TreeView, ListView, Panel 활용
   - 커스텀 컨트롤 (DoubleBufferPanel)

4. **파일 I/O 및 디렉토리 탐색**
   - DriveInfo, DirectoryInfo, FileInfo

5. **이벤트 드리븐 프로그래밍**
   - 델리게이트 및 콜백

---

## 📚 참고 자료

### 관련 기술 문서

- [Siemens Sinumerik CNC Documentation](https://docs.siemens.com/)
- [MPF File Format Specification](https://support.industry.siemens.com/)
- [P/Invoke Tutorial](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)

### 유사 프로젝트

- **CNC Simulator**: 오픈소스 G-code 시뮬레이터
- **CAMotics**: 크로스 플랫폼 CAM 뷰어

---

## ✅ 결론 및 최종 평가

### 강점

✅ **명확한 목적**: CNC MPF 파일 시각화에 특화  
✅ **안정적인 DLL 통합**: HKCAMInterface.dll 활용  
✅ **직관적인 UI**: 파일 탐색 + 뷰어 + 시뮬레이션  
✅ **실시간 피드백**: 마우스 좌표, 시뮬레이션 로그  

### 약점

⚠️ **레거시 기술 스택**: .NET Framework 4.5  
⚠️ **스레드 관리 문제**: Thread.Abort() 사용  
⚠️ **제한적 확장성**: 하드코딩된 경로, 단일 파일 형식  
⚠️ **테스트 부족**: 단위 테스트 없음  

### 추천 우선순위

1. **긴급**: Thread.Abort() → CancellationToken 변경
2. **중요**: 예외 처리 강화
3. **중기**: .NET 6+ 마이그레이션
4. **장기**: 3D 뷰어, 다중 파일 형식 지원

---

## 📞 문의 및 지원

**개발자**: (정보 없음)  
**프로젝트 유형**: 산업용 CNC 소프트웨어  
**라이선스**: (명시되지 않음)

---

**리포트 생성 일시**: 2025-11-18  
**분석 도구**: 코드 리뷰 + 정적 분석  
**리포트 버전**: 1.0
