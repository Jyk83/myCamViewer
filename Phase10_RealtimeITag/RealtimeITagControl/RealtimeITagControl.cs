using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using Siemens.Runtime;
using Siemens.Runtime.ITag;
using RealtimeITagControl.UI;
using RealtimeITagControl.MPF;
using RealtimeITagControl.Rendering;

namespace RealtimeITagControl
{
    /// <summary>
    /// Realtime ITag Viewer UserControl
    /// Phase8 (Realtime Viewer) + Phase9 (ITag Communication) 통합
    /// WinCC Graphics Designer에 임포트 가능
    /// IITagManager 인터페이스 구현 - ITag 인스턴스를 팝업 등에 전달 가능
    /// </summary>
    public partial class RealtimeITagControl : UserControl, IITagManager, ITagSink
    {
        #region 멤버 변수

        // ITag 인스턴스 (외부에서 접근 가능)
        private ITag m_ITag;
        private long m_RegisterCookie;
        private bool isConnected = false;
        private bool isCyclicReading = false;
        private string lastErrorMessage = null;

        private ProgramInfoPanel programInfoPanel;
        private Panel viewerPanel;

        private string currentMpfPath;      // 현재 로드된 MPF 파일 경로
        private TagData lastTagData;
        
        // MPF 파싱 데이터
        private MPFProgram mpfProgram;  // 파싱된 MPF 프로그램
        
        // 로그 파일 경로
        private static readonly string logFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
            "RealtimeITagControl_Log.txt");
        
        // Trace 상태 관리
        private TraceState currentTraceState = TraceState.Idle;
        private bool isTracing = false;
        
        // 컨투어별 추적 정보 (렌더링용)
        private System.Collections.Generic.Dictionary<string, ContourTraceInfo> contourStatusMap;
        
        // Pan/Zoom 기능
        private float zoom = 1.0f;          // 확대/축소 배율
        private float panX = 0.0f;          // X축 이동
        private float panY = 0.0f;          // Y축 이동
        private Point lastMousePos;         // 마우스 드래그 시작 위치
        private bool isDragging = false;    // 드래그 중 여부

        #endregion
        
        #region Trace 상태 Enum 및 데이터 클래스
        
        /// <summary>
        /// Trace 상태
        /// </summary>
        private enum TraceState
        {
            Idle,           // 대기 (파일 없음 또는 미시작)
            Loaded,         // 파일 로드됨, 대기 중
            Tracing,        // 트레이싱 중 (WorkStatus = 1)
            Paused,         // 일시정지 (WorkStatus = 2,3)
            Completed       // 완료 (WorkStatus = 0)
        }
        
        /// <summary>
        /// 절단 상태 (렌더링용)
        /// </summary>
        private enum CutStatus
        {
            NotStarted,     // 미시작 (회색)
            InProgress,     // 진행 중 (노란색)
            Completed       // 완료 (초록색)
        }
        
        /// <summary>
        /// 컨투어별 추적 정보
        /// </summary>
        private class ContourTraceInfo
        {
            public int PartNumber { get; set; }
            public int ContourNumber { get; set; }
            public CutStatus Status { get; set; }
            public double CompletedDistance { get; set; }  // 완료된 거리
            
            public ContourTraceInfo(int partNum, int contNum)
            {
                PartNumber = partNum;
                ContourNumber = contNum;
                Status = CutStatus.NotStarted;
                CompletedDistance = 0.0;
            }
        }

        #endregion

        #region IITagManager 구현 (속성)

        public ITag ITag => m_ITag;
        public bool IsConnected => isConnected;
        public bool IsCyclicReading => isCyclicReading;
        public string LastErrorMessage => lastErrorMessage;

        public event EventHandler<TagDataEventArgs> DataChanged;
        public event EventHandler<bool> ConnectionChanged;

        #endregion

        #region 생성자

        public RealtimeITagControl()
        {
            InitializeComponent();
        }

        #endregion
        
        #region 로그 메서드
        
        /// <summary>
        /// 파일 로그 기록 (WinCC 디버깅용)
        /// </summary>
        private void LogToFile(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] {message}\n";
                System.IO.File.AppendAllText(logFilePath, logMessage);
            }
            catch
            {
                // 로그 실패 시 무시
            }
        }
        
        #endregion

        #region 초기화

        private void InitializeComponent()
        {
            // UserControl 크기: 1268x630
            this.Size = new Size(1268, 630);
            this.BackColor = Color.White;

            // 좌측 Program Info 패널 (300x630)
            programInfoPanel = new ProgramInfoPanel
            {
                Location = new Point(0, 0),
                Size = new Size(300, 630),
                Dock = DockStyle.None
            };
            programInfoPanel.SimulationClicked += ProgramInfoPanel_SimulationClicked;
            programInfoPanel.ElementSelectClicked += ProgramInfoPanel_ElementSelectClicked;
            programInfoPanel.ITagTestClicked += ProgramInfoPanel_ITagTestClicked;
            this.Controls.Add(programInfoPanel);
            
            // 초기 연결 상태 표시
            programInfoPanel.UpdateConnectionStatus(false, false, null);

            // 우측 Viewer 패널 (968x630)
            viewerPanel = new Panel
            {
                Location = new Point(300, 0),
                Size = new Size(968, 630),
                BackColor = Color.FromArgb(50, 50, 50),
                Dock = DockStyle.None
            };
            viewerPanel.MouseDown += ViewerPanel_MouseDown;
            viewerPanel.MouseMove += ViewerPanel_MouseMove;
            viewerPanel.MouseUp += ViewerPanel_MouseUp;
            viewerPanel.MouseWheel += ViewerPanel_MouseWheel;
            viewerPanel.MouseClick += ViewerPanel_MouseClick;
            viewerPanel.Paint += ViewerPanel_Paint;
            this.Controls.Add(viewerPanel);

            // Load 시 자동 연결
            this.Load += (s, e) =>
            {
                if (!DesignMode)
                {
                    Connect();
                    StartCyclicRead(500);
                }
            };

            // Dispose 시 연결 해제
            this.Disposed += RealtimeITagControl_Disposed;
        }

        /// <summary>
        /// 리소스 정리 (Dispose 이벤트 핸들러)
        /// </summary>
        private void RealtimeITagControl_Disposed(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] === Dispose 시작 ===");

                // ITag 연결 해제
                Disconnect();

                // 이벤트 구독 해제
                if (programInfoPanel != null)
                {
                    programInfoPanel.SimulationClicked -= ProgramInfoPanel_SimulationClicked;
                    programInfoPanel.ElementSelectClicked -= ProgramInfoPanel_ElementSelectClicked;
                    programInfoPanel.ITagTestClicked -= ProgramInfoPanel_ITagTestClicked;
                }

                if (viewerPanel != null)
                {
                    viewerPanel.MouseDown -= ViewerPanel_MouseDown;
                    viewerPanel.MouseMove -= ViewerPanel_MouseMove;
                    viewerPanel.MouseUp -= ViewerPanel_MouseUp;
                    viewerPanel.MouseWheel -= ViewerPanel_MouseWheel;
                    viewerPanel.MouseClick -= ViewerPanel_MouseClick;
                    viewerPanel.Paint -= ViewerPanel_Paint;
                }

                // MPF 데이터 정리
                currentMpfPath = null;
                mpfProgram = null;  // MPF 프로그램 데이터 해제
                lastTagData = default(TagData);
                
                // 컨투어 상태 맵 정리
                if (contourStatusMap != null)
                {
                    contourStatusMap.Clear();
                    contourStatusMap = null;
                }

                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] === Dispose 완료 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] Dispose 오류: {ex.Message}");
            }
        }

        #endregion

        #region IITagManager 구현 (메서드)

        /// <summary>
        /// ITag 서버 연결
        /// </summary>
        public bool Connect()
        {
            // 로그 파일 초기화
            try
            {
                if (System.IO.File.Exists(logFilePath))
                    System.IO.File.Delete(logFilePath);
                LogToFile("========================================");
                LogToFile("RealtimeITagControl 시작");
                LogToFile($"시작 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                LogToFile("========================================\n");
            }
            catch { }
            
            if (isConnected && m_ITag != null)
            {
                LogToFile("이미 ITag에 연결되어 있음");
                return true;
            }

            try
            {
                lastErrorMessage = null;
                LogToFile("=== ITag 연결 시작 ===");

                // 1순위: Site.GetService (WinCC 내부)
                if (this.Site != null)
                {
                    System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] Site.GetService 시도...");
                    try
                    {
                        m_ITag = (ITag)this.Site.GetService(typeof(ITag));
                        if (m_ITag != null)
                        {
                            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ✅ Site.GetService로 획득 성공");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ⚠️ Site.GetService 반환값 null");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ❌ Site.GetService 오류: {ex.Message}");
                        lastErrorMessage = $"Site.GetService 실패: {ex.Message}";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ⚠️ Site가 null입니다.");
                    lastErrorMessage = "IServiceProvider가 null (WinCC 외부 실행?)";
                }

                // 2순위: COM ProgID로 생성 (독립 실행)
                if (m_ITag == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] COM ProgID 생성 시도...");
                    try
                    {
                        Type itagType = Type.GetTypeFromProgID("CCITagControl.ITagControl.1");
                        if (itagType != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] COM Type 획득: {itagType.FullName}");
                            m_ITag = (ITag)Activator.CreateInstance(itagType);
                            if (m_ITag != null)
                            {
                                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ✅ COM ProgID로 생성 성공");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ❌ Activator.CreateInstance 반환값 null");
                                lastErrorMessage = "COM 인스턴스 생성 실패 (WinCC Runtime 미실행?)";
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ❌ COM ProgID를 찾을 수 없습니다.");
                            lastErrorMessage = "COM ProgID 'CCITagControl.ITagControl.1'을 찾을 수 없습니다";
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ❌ COM 생성 오류: {ex.Message}");
                        lastErrorMessage = $"COM 생성 실패: {ex.Message}";
                    }
                }

                if (m_ITag == null)
                {
                    string errorMsg = "ITag 인스턴스 생성 실패 - WinCC Runtime이 실행 중인지 확인하세요";
                    System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ❌ {errorMsg}");
                    if (string.IsNullOrEmpty(lastErrorMessage))
                    {
                        lastErrorMessage = errorMsg;
                    }
                    isConnected = false;
                    ConnectionChanged?.Invoke(this, false);
                    UpdateConnectionStatusUI();
                    return false;
                }

                // ITagSink 콜백 등록 (this = RealtimeITagControl)
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ITagSink 등록 시도...");
                m_RegisterCookie = m_ITag.Register(this);
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ✅ ITagSink 등록 완료 (Cookie: {m_RegisterCookie})");

                isConnected = true;
                ConnectionChanged?.Invoke(this, true);
                LogToFile("✅ ITag 서버 연결 성공");
                LogToFile("=== ITag 연결 완료 ===\n");
                
                UpdateConnectionStatusUI();
                return true;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Connect 예외 발생: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ❌ {errorMsg}");
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] 스택 트레이스: {ex.StackTrace}");
                lastErrorMessage = errorMsg;
                isConnected = false;
                ConnectionChanged?.Invoke(this, false);
                UpdateConnectionStatusUI();
                return false;
            }
        }

        /// <summary>
        /// ITag 서버 연결 해제 (완벽한 정리)
        /// </summary>
        public void Disconnect()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] Disconnect 시작...");

                // 1. 주기적 읽기 중지
                StopCyclicRead();

                // 2. ITagSink 등록 해제
                if (m_ITag != null && isConnected)
                {
                    try
                    {
                        m_ITag.Unregister((int)m_RegisterCookie);
                        System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ✅ ITagSink 등록 해제 완료");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ⚠️ ITagSink Unregister 오류: {ex.Message}");
                    }
                }

                // 3. 상태 초기화
                isConnected = false;
                isCyclicReading = false;
                
                // 4. ITag 인스턴스 null 처리 (메모리 해제)
                m_ITag = null;
                m_RegisterCookie = 0;

                // 5. 이벤트 발생
                ConnectionChanged?.Invoke(this, false);
                
                // 6. UI 업데이트
                UpdateConnectionStatusUI();

                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ✅ Disconnect 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ❌ Disconnect 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 14개 Tag 주기적 읽기 시작
        /// </summary>
        public bool StartCyclicRead(int cycleMs = 500)
        {
            if (!isConnected)
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] StartCyclicRead: 연결 안됨");
                return false;
            }

            if (isCyclicReading)
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] StartCyclicRead: 이미 실행 중");
                return true;
            }

            try
            {
                int tagCount = TagDefinitions.AllTagNames.Length;
                int[] cycles = Enumerable.Repeat(cycleMs, tagCount).ToArray();
                int[] cookies = Enumerable.Range(1, tagCount).ToArray();

                object serverCookie = null;

                m_ITag.ReadTagCyclic(
                    (int)m_RegisterCookie,
                    (object)TagDefinitions.AllTagNames,
                    (object)cycles,
                    (object)cookies,
                    out serverCookie
                );

                isCyclicReading = true;
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ✅ 주기적 읽기 시작 ({tagCount}개 Tag, {cycleMs}ms)");
                
                UpdateConnectionStatusUI();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ❌ StartCyclicRead 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 주기적 읽기 중지
        /// </summary>
        public void StopCyclicRead()
        {
            if (!isConnected || !isCyclicReading)
                return;

            try
            {
                m_ITag.Cancel((int)m_RegisterCookie);
                isCyclicReading = false;
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] 주기적 읽기 중지");
                
                UpdateConnectionStatusUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] StopCyclicRead 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 단일 Tag 읽기
        /// </summary>
        public bool ReadTag(string tagName, out object value)
        {
            value = null;

            if (!isConnected)
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ReadTag: 연결 안됨");
                return false;
            }

            try
            {
                object result = m_ITag.ReadTag((int)m_RegisterCookie, tagName);
                value = result;
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ReadTag '{tagName}' = {value}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ReadTag 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 여러 Tag 읽기
        /// </summary>
        public bool ReadTags(string[] tagNames, out object[] values)
        {
            values = null;

            if (!isConnected)
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ReadTags: 연결 안됨");
                return false;
            }

            try
            {
                object result = m_ITag.ReadTag((int)m_RegisterCookie, (object)tagNames);

                if (result is Array resultArr)
                {
                    values = new object[resultArr.Length];
                    for (int i = 0; i < resultArr.Length; i++)
                    {
                        values[i] = resultArr.GetValue(i);
                    }
                    System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ReadTags: {values.Length}개 읽기 성공");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ReadTags 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 단일 Tag 쓰기
        /// </summary>
        public bool WriteTag(string tagName, object value)
        {
            if (!isConnected)
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] WriteTag: 연결 안됨");
                return false;
            }

            try
            {
                m_ITag.WriteTag((int)m_RegisterCookie, tagName, value);
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] WriteTag '{tagName}' = {value}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] WriteTag 오류: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region ITagSink 구현

        public void OnDataChanged(int RegisterCookie, object TagNames, object Values, 
            object Qualities, object VarStates, object TimeStamps, object Cookies)
        {
            try
            {
                if (!(TagNames is Array tagArr) || !(Values is Array valArr))
                    return;

                // TagData 구조체 생성
                var tagData = new TagData();

                for (int i = 0; i < tagArr.Length; i++)
                {
                    string tagName = tagArr.GetValue(i)?.ToString();
                    object value = valArr.GetValue(i);

                    if (string.IsNullOrEmpty(tagName))
                        continue;

                    // Tag 이름에 따라 값 할당
                    switch (tagName)
                    {
                        case TagDefinitions.X_MCS:
                            tagData.X_MCS = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.Y_MCS:
                            tagData.Y_MCS = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.X_WCS:
                            tagData.X_WCS = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.Y_WCS:
                            tagData.Y_WCS = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.PROGRESS_DISTANCE:
                            tagData.ProgressDistance = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.CURRENT_PART:
                            tagData.CurrentPart = Convert.ToInt32(value);
                            break;
                        case TagDefinitions.CURRENT_CONT:
                            tagData.CurrentContour = Convert.ToInt32(value);
                            break;
                        case TagDefinitions.WORK_DIR:
                            tagData.WorkDir = value?.ToString() ?? "";
                            break;
                        case TagDefinitions.WORK_MPF_NAME:
                            tagData.WorkMpfName = value?.ToString() ?? "";
                            break;
                        case TagDefinitions.WORK_STATUS:
                            tagData.WorkStatus = (TagDefinitions.WorkStatus)Convert.ToInt32(value);
                            break;
                        case TagDefinitions.ACT_LINE_CODE:
                            tagData.ActLineCode = value?.ToString() ?? "";
                            break;
                        case TagDefinitions.ACT_LINE_NUM:
                            tagData.ActLineNum = Convert.ToInt32(value);
                            break;
                        case TagDefinitions.SEARCH_PART:
                            tagData.SearchPart = Convert.ToInt32(value);
                            break;
                        case TagDefinitions.SEARCH_CONT:
                            tagData.SearchContour = Convert.ToInt32(value);
                            break;
                        case TagDefinitions.DIR_TYPE:
                            tagData.DirType = (TagDefinitions.DirectionType)Convert.ToInt32(value);
                            break;
                    }
                }

                // 내부 처리
                lastTagData = tagData;

                // Program Info 패널 업데이트
                if (programInfoPanel != null)
                {
                    programInfoPanel.UpdateTagData(tagData);
                }

                // MPF 파일 변경 감지 및 로드
                string newMpfPath = tagData.FullMpfPath;
                
                // 디버깅 로그 (최초 1회만)
                if (currentMpfPath == null && !string.IsNullOrEmpty(newMpfPath))
                {
                    LogToFile("=== MPF 경로 정보 (최초) ===");
                    LogToFile($"WorkDir: '{tagData.WorkDir}'");
                    LogToFile($"WorkMpfName: '{tagData.WorkMpfName}'");
                    LogToFile($"FullMpfPath: '{newMpfPath}'");
                    LogToFile("===========================\n");
                }
                
                if (!string.IsNullOrEmpty(newMpfPath) && newMpfPath != currentMpfPath)
                {
                    LogToFile($"📂 새 MPF 파일 감지: {newMpfPath}");
                    bool loaded = LoadMpfFile(newMpfPath);
                    if (loaded)
                    {
                        currentTraceState = TraceState.Loaded;
                        LogToFile($"✅ MPF 로드 및 상태 변경: Loaded");
                    }
                    else
                    {
                        LogToFile($"❌ MPF 로드 실패");
                    }
                }

                // Trace 로직 처리 (WorkStatus에 따라)
                ProcessTraceLogic(tagData);

                // Viewer 업데이트
                UpdateViewer(tagData);

                // 이벤트 발생 (외부 구독자에게 전달)
                DataChanged?.Invoke(this, new TagDataEventArgs(tagData));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] OnDataChanged 오류: {ex.Message}");
            }
        }

        public void OnWriteComplete(int RegisterCookie, object TagNames, object Cookies)
        {
            // 필요 시 구현
        }

        public void OnError(int RegisterCookie, object TagNames, object Cookies, object Errors)
        {
            System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] OnError: RegisterCookie={RegisterCookie}");
        }

        public void OnCanceled(int RegisterCookie)
        {
            isCyclicReading = false;
            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] OnCanceled: 주기적 읽기 취소됨");
            UpdateConnectionStatusUI();
        }

        public void OnRemoved(int RegisterCookie, object Cookies)
        {
            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] OnRemoved");
        }

        #endregion

        #region UI 업데이트

        private void UpdateConnectionStatusUI()
        {
            try
            {
                if (programInfoPanel != null && !programInfoPanel.IsDisposed)
                {
                    programInfoPanel.UpdateConnectionStatus(isConnected, isCyclicReading, lastErrorMessage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] UpdateConnectionStatusUI 오류: {ex.Message}");
            }
        }

        #endregion

        #region MPF 파일 로드

        /// <summary>
        /// MPF 파일 로드 및 파싱
        /// </summary>
        /// <param name="mpfPath">MPF 파일 전체 경로</param>
        /// <returns>로드 성공 여부</returns>
        private bool LoadMpfFile(string mpfPath)
        {
            try
            {
                LogToFile("=== LoadMpfFile 시작 ===");
                LogToFile($"요청 경로: {mpfPath}");
                
                // 1. 파일 경로 유효성 검사
                if (string.IsNullOrEmpty(mpfPath))
                {
                    LogToFile("❌ 파일 경로가 비어있음");
                    return false;
                }

                // 2. 파일 존재 확인
                bool fileExists = File.Exists(mpfPath);
                LogToFile($"파일 존재 여부: {fileExists}");
                
                if (!fileExists)
                {
                    LogToFile($"❌ 파일 없음: {mpfPath}");
                    return false;
                }

                // 3. 이미 로드된 파일인지 확인 (재로드 방지)
                if (currentMpfPath == mpfPath && mpfProgram != null)
                {
                    LogToFile($"ℹ️ 이미 로드된 파일 (재로드 안함)");
                    return true;
                }

                LogToFile($"📂 MPF 파일 파싱 시작...");

                // 4. MPF 파일 읽기
                string fileContent = System.IO.File.ReadAllText(mpfPath);
                LogToFile($"   - 파일 읽기 완료: {fileContent.Length} bytes");

                // 5. MPF 파일 파싱
                var parser = new MPFParser();
                mpfProgram = parser.Parse(fileContent);

                if (mpfProgram == null)
                {
                    LogToFile($"❌ MPF 파싱 결과 null");
                    return false;
                }

                // 6. 파싱 결과 요약 로그
                int totalParts = mpfProgram.Parts?.Count ?? 0;
                int totalContours = 0;
                if (mpfProgram.Parts != null)
                {
                    foreach (var part in mpfProgram.Parts)
                    {
                        totalContours += part.Contours?.Count ?? 0;
                    }
                }

                LogToFile($"✅ MPF 파싱 성공!");
                LogToFile($"   - 파일명: {Path.GetFileName(mpfPath)}");
                LogToFile($"   - 파트 수: {totalParts}");
                LogToFile($"   - 컨투어 수: {totalContours}");

                // 7. 현재 로드된 파일 경로 저장
                currentMpfPath = mpfPath;
                
                // 7.5. AutoFit: 초기 Zoom/Pan 설정
                AutoFitView();

                // 8. Viewer 다시 그리기
                if (viewerPanel != null && !viewerPanel.IsDisposed)
                {
                    viewerPanel.Invalidate();
                    LogToFile("Viewer 다시 그리기 요청");
                }

                LogToFile("=== LoadMpfFile 완료 ===\n");
                return true;
            }
            catch (Exception ex)
            {
                LogToFile($"❌ LoadMpfFile 예외 발생!");
                LogToFile($"   Exception: {ex.GetType().Name}");
                LogToFile($"   Message: {ex.Message}");
                LogToFile($"   StackTrace: {ex.StackTrace}");
                LogToFile("=== LoadMpfFile 실패 ===\n");
                return false;
            }
        }

        #endregion

        #region Trace 로직 처리
        
        /// <summary>
        /// Trace 로직 처리 (WorkStatus에 따라)
        /// </summary>
        private void ProcessTraceLogic(TagData tagData)
        {
            try
            {
                // 1. ITag 연결 확인
                if (!isConnected)
                {
                    return;
                }

                // 2. MPF 파일 로드 확인
                if (mpfProgram == null)
                {
                    currentTraceState = TraceState.Idle;
                    return;
                }

                // 3. WorkStatus에 따른 처리
                switch (tagData.WorkStatus)
                {
                    case TagDefinitions.WorkStatus.Start:  // WorkStatus = 1
                        // 무조건 처음부터 새로 그리기 (단순 버전)
                        if (currentTraceState != TraceState.Tracing)
                        {
                            StartTracing();
                            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] 🔄 Trace 시작 (처음부터)");
                        }
                        
                        // 실시간 컨투어 상태 업데이트
                        UpdateContourStatus(tagData);
                        
                        currentTraceState = TraceState.Tracing;
                        isTracing = true;
                        break;

                    case TagDefinitions.WorkStatus.End:  // WorkStatus = 0
                        // 완료
                        if (currentTraceState != TraceState.Completed)
                        {
                            CompleteTracing();
                            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ✅ Trace 완료");
                        }
                        currentTraceState = TraceState.Completed;
                        isTracing = false;
                        break;

                    case TagDefinitions.WorkStatus.Reset:      // WorkStatus = 2
                    case TagDefinitions.WorkStatus.FeedHold:   // WorkStatus = 3
                        // 일시정지
                        if (currentTraceState == TraceState.Tracing)
                        {
                            PauseTracing();
                            System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ⏸️ Trace 일시정지 (WorkStatus={tagData.WorkStatus})");
                        }
                        currentTraceState = TraceState.Paused;
                        isTracing = false;
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ProcessTraceLogic 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 실시간 컨투어 상태 업데이트 (CurrentPart, CurrentContour, ProgressDistance 기반)
        /// </summary>
        private void UpdateContourStatus(TagData tagData)
        {
            try
            {
                if (contourStatusMap == null || mpfProgram?.Parts == null)
                    return;
                
                int currentPart = tagData.CurrentPart;
                int currentContour = tagData.CurrentContour;
                double progressDistance = tagData.ProgressDistance;
                
                // 1. 현재 파트/컨투어 이전 것들은 모두 Completed 처리
                for (int partIdx = 0; partIdx < mpfProgram.Parts.Count; partIdx++)
                {
                    var part = mpfProgram.Parts[partIdx];
                    int partNum = partIdx + 1;  // 1-based 인덱스
                    
                    if (part.Contours == null)
                        continue;
                    
                    for (int contIdx = 0; contIdx < part.Contours.Count; contIdx++)
                    {
                        var contour = part.Contours[contIdx];
                        int contNum = contIdx + 1;  // 1-based 인덱스
                        
                        string key = $"{partNum}_{contNum}";
                        
                        if (!contourStatusMap.ContainsKey(key))
                            continue;
                        
                        // 이전 파트들은 모두 완료
                        if (partNum < currentPart)
                        {
                            contourStatusMap[key].Status = CutStatus.Completed;
                        }
                        // 현재 파트의 이전 컨투어들은 완료
                        else if (partNum == currentPart && contNum < currentContour)
                        {
                            contourStatusMap[key].Status = CutStatus.Completed;
                        }
                        // 현재 파트의 현재 컨투어는 진행 중
                        else if (partNum == currentPart && contNum == currentContour)
                        {
                            contourStatusMap[key].Status = CutStatus.InProgress;
                            contourStatusMap[key].CompletedDistance = progressDistance;
                        }
                        // 그 외는 미시작
                        else
                        {
                            contourStatusMap[key].Status = CutStatus.NotStarted;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] UpdateContourStatus 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Trace 시작 (처음부터)
        /// </summary>
        private void StartTracing()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] StartTracing 호출 - 렌더링 상태 초기화");
                
                // 1. 컨투어 상태 맵 초기화
                if (contourStatusMap == null)
                {
                    contourStatusMap = new System.Collections.Generic.Dictionary<string, ContourTraceInfo>();
                }
                else
                {
                    contourStatusMap.Clear();
                }
                
                // 2. mpfProgram의 모든 컨투어를 NotStarted 상태로 초기화
                if (mpfProgram?.Parts != null)
                {
                    for (int partIdx = 0; partIdx < mpfProgram.Parts.Count; partIdx++)
                    {
                        var part = mpfProgram.Parts[partIdx];
                        int partNum = partIdx + 1;  // 1-based 인덱스
                        
                        if (part.Contours == null)
                            continue;
                            
                        for (int contIdx = 0; contIdx < part.Contours.Count; contIdx++)
                        {
                            int contNum = contIdx + 1;  // 1-based 인덱스
                            string key = $"{partNum}_{contNum}";
                            contourStatusMap[key] = new ContourTraceInfo(partNum, contNum);
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] 렌더링 초기화 완료 - {contourStatusMap?.Count ?? 0}개 컨투어");
                
                // 3. Viewer 다시 그리기
                if (viewerPanel != null && !viewerPanel.IsDisposed)
                {
                    viewerPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] StartTracing 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Trace 완료
        /// </summary>
        private void CompleteTracing()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] CompleteTracing 호출 - 모든 컨투어 완료 처리");
                
                // 모든 컨투어를 Completed 상태로 변경
                if (contourStatusMap != null)
                {
                    foreach (var kvp in contourStatusMap)
                    {
                        kvp.Value.Status = CutStatus.Completed;
                    }
                }
                
                // Viewer 다시 그리기
                if (viewerPanel != null && !viewerPanel.IsDisposed)
                {
                    viewerPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] CompleteTracing 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Trace 일시정지
        /// </summary>
        private void PauseTracing()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] PauseTracing 호출 - 현재 상태 유지");
                
                // 현재 상태 유지 (아무 동작 안함)
                // Viewer는 자동으로 현재 상태 유지
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] PauseTracing 오류: {ex.Message}");
            }
        }

        #endregion

        #region Viewer 업데이트

        private void UpdateViewer(TagData data)
        {
            try
            {
                // TODO: 실시간 렌더링 구현
                
                if (viewerPanel != null && !viewerPanel.IsDisposed)
                {
                    viewerPanel.Invalidate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] UpdateViewer 오류: {ex.Message}");
            }
        }

        #endregion

        #region Viewer 패널 이벤트 핸들러

        private void ViewerPanel_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Graphics g = e.Graphics;
                g.Clear(Color.FromArgb(50, 50, 50));
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // MPF 프로그램이 없으면 안내 메시지 표시
                if (mpfProgram == null || mpfProgram.Parts == null || mpfProgram.Parts.Count == 0)
                {
                    string message = "Realtime ITag Viewer\n\n";
                    
                    if (string.IsNullOrEmpty(lastTagData.WorkDir) && string.IsNullOrEmpty(lastTagData.WorkMpfName))
                    {
                        message += "📂 MPF 파일 대기 중\n\n";
                        message += "다음 ITag를 설정하세요:\n";
                        message += "• HMI_VIEW_WORK_DIR\n";
                        message += "• HMI_VIEW_WORK_MPF_NAME";
                    }
                    else if (string.IsNullOrEmpty(lastTagData.WorkMpfName))
                    {
                        message += "⚠️ MPF 파일명 없음\n\n";
                        message += $"작업 폴더: {lastTagData.WorkDir}\n";
                        message += "HMI_VIEW_WORK_MPF_NAME을 설정하세요";
                    }
                    else if (!string.IsNullOrEmpty(currentMpfPath))
                    {
                        message += "❌ MPF 로드 실패\n\n";
                        message += $"파일: {currentMpfPath}\n";
                        message += "파일 존재 여부를 확인하세요";
                    }
                    else
                    {
                        message += "⏳ MPF 로딩 중...";
                    }
                    
                    g.DrawString(message, 
                        new Font("맑은 고딕", 14, FontStyle.Regular), 
                        Brushes.White, 
                        new PointF(250, 200));
                    return;
                }
                
                // 렌더링: 모든 파트와 컨투어 그리기
                RenderMpfProgram(g);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ViewerPanel_Paint 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// MPF 프로그램 렌더링 (컨투어 상태에 따라 색상 구분)
        /// </summary>
        private void RenderMpfProgram(Graphics g)
        {
            try
            {
                if (mpfProgram?.Parts == null)
                    return;
                
                RenderSettings settings = RenderSettings.Instance;
                
                // 좌표 변환을 위한 스케일 계산 (Pan/Zoom 적용)
                float viewWidth = viewerPanel.Width;
                float viewHeight = viewerPanel.Height;
                float baseScale = CalculateAutoScale();
                float scale = baseScale * zoom;  // Zoom 적용
                
                // Pan 적용 (화면 중심 + Pan offset)
                float offsetX = viewWidth / 2 + panX;
                float offsetY = viewHeight / 2 + panY;
                
                // 1. Workpiece Exterior 배경 (전체 화면)
                using (SolidBrush exteriorBrush = new SolidBrush(settings.WorkpieceExteriorColor))
                {
                    g.FillRectangle(exteriorBrush, 0, 0, viewWidth, viewHeight);
                }
                
                // 2. Workpiece Interior 배경 (Workpiece 영역)
                if (mpfProgram.Workpiece != null)
                {
                    float wpWidth = (float)(mpfProgram.Workpiece.Width * scale);
                    float wpHeight = (float)(mpfProgram.Workpiece.Height * scale);
                    float wpX = offsetX - wpWidth / 2;
                    float wpY = offsetY - wpHeight / 2;
                    
                    using (SolidBrush interiorBrush = new SolidBrush(settings.WorkpieceInteriorColor))
                    {
                        g.FillRectangle(interiorBrush, wpX, wpY, wpWidth, wpHeight);
                    }
                    
                    // 3. Workpiece Boundary (옵션)
                    if (settings.ShowWorkpieceBoundary)
                    {
                        using (Pen boundaryPen = new Pen(settings.WorkpieceBoundaryColor, settings.WorkpieceBoundaryWidth))
                        {
                            g.DrawRectangle(boundaryPen, wpX, wpY, wpWidth, wpHeight);
                        }
                    }
                }
                
                // 4. 모든 파트 렌더링
                for (int partIdx = 0; partIdx < mpfProgram.Parts.Count; partIdx++)
                {
                    var part = mpfProgram.Parts[partIdx];
                    int partNum = partIdx + 1;  // 1-based 인덱스
                    
                    if (part.Contours == null)
                        continue;
                    
                    // Phase8 로직: Part Origin 좌표 적용
                    float partOffsetX = (float)(part.Origin.X * scale) + offsetX;
                    float partOffsetY = offsetY - (float)(part.Origin.Y * scale);  // Y축 반전
                    
                    for (int contIdx = 0; contIdx < part.Contours.Count; contIdx++)
                    {
                        var contour = part.Contours[contIdx];
                        int contNum = contIdx + 1;  // 1-based 인덱스
                        
                        // 컨투어 상태 가져오기
                        string key = $"{partNum}_{contNum}";
                        CutStatus status = CutStatus.NotStarted;
                        double completedDistance = 0.0;
                        
                        if (contourStatusMap != null && contourStatusMap.ContainsKey(key))
                        {
                            status = contourStatusMap[key].Status;
                            completedDistance = contourStatusMap[key].CompletedDistance;
                        }
                        
                        // 컨투어 그리기 (Part Origin 적용)
                        RenderContour(g, contour, status, completedDistance, scale, partOffsetX, partOffsetY);
                    }
                }
                
                // 상태 표시 (우하단)
                DrawStatusLegend(g);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] RenderMpfProgram 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 자동 스케일 계산 (전체 도형이 화면에 맞도록)
        /// </summary>
        private float CalculateAutoScale()
        {
            try
            {
                if (mpfProgram?.Parts == null)
                    return 1.0f;
                
                float minX = float.MaxValue, minY = float.MaxValue;
                float maxX = float.MinValue, maxY = float.MinValue;
                
                // 전체 바운딩 박스 계산
                foreach (var part in mpfProgram.Parts)
                {
                    if (part.Contours == null)
                        continue;
                    
                    foreach (var contour in part.Contours)
                    {
                        if (contour.AllSegments == null)
                            continue;
                        
                        foreach (var segment in contour.AllSegments)
                        {
                            minX = Math.Min(minX, (float)segment.Start.X);
                            minY = Math.Min(minY, (float)segment.Start.Y);
                            maxX = Math.Max(maxX, (float)segment.Start.X);
                            maxY = Math.Max(maxY, (float)segment.Start.Y);
                            
                            minX = Math.Min(minX, (float)segment.End.X);
                            minY = Math.Min(minY, (float)segment.End.Y);
                            maxX = Math.Max(maxX, (float)segment.End.X);
                            maxY = Math.Max(maxY, (float)segment.End.Y);
                        }
                    }
                }
                
                float rangeX = maxX - minX;
                float rangeY = maxY - minY;
                
                if (rangeX <= 0 || rangeY <= 0)
                    return 1.0f;
                
                float viewWidth = viewerPanel.Width * 0.8f;  // 여백 20%
                float viewHeight = viewerPanel.Height * 0.8f;
                
                float scaleX = viewWidth / rangeX;
                float scaleY = viewHeight / rangeY;
                
                return Math.Min(scaleX, scaleY);
            }
            catch
            {
                return 1.0f;
            }
        }
        
        /// <summary>
        /// AutoFit: 전체 프로그램이 화면에 맞도록 Zoom/Pan 설정
        /// </summary>
        private void AutoFitView()
        {
            try
            {
                if (mpfProgram?.Workpiece == null)
                    return;
                
                // Workpiece 크기 기반 초기 Zoom 계산
                float width = (float)mpfProgram.Workpiece.Width;
                float height = (float)mpfProgram.Workpiece.Height;
                
                if (width <= 0 || height <= 0)
                    return;
                
                // RenderSettings에서 InitialZoomMultiplier 사용
                RenderSettings settings = RenderSettings.Instance;
                float multiplier = settings.InitialZoomMultiplier;
                
                float zoomByWidth = (float)viewerPanel.Width / width * multiplier;
                float zoomByHeight = (float)viewerPanel.Height / height * multiplier;
                
                zoom = Math.Min(zoomByWidth, zoomByHeight);
                
                // Pan 초기화 (중심)
                panX = 0.0f;
                panY = 0.0f;
                
                LogToFile($"AutoFit: Zoom={zoom:F3}, Workpiece Size=({width:F1}, {height:F1})");
            }
            catch (Exception ex)
            {
                LogToFile($"AutoFitView 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 컨투어 렌더링 (Phase8 로직: Lead-in, Piercing, Marking 구분)
        /// </summary>
        private void RenderContour(Graphics g, Contour contour, CutStatus status, 
            double completedDistance, float scale, float offsetX, float offsetY)
        {
            try
            {
                if (contour == null)
                    return;
                
                RenderSettings settings = RenderSettings.Instance;
                bool isMarking = (contour.CuttingType == 10);
                
                // 1. Piercing Point 렌더링 (PiercingType != 0)
                if (contour.PiercingType != 0 && contour.PiercingPosition != null)
                {
                    float pierceX = (float)(contour.PiercingPosition.X * scale) + offsetX;
                    float pierceY = offsetY - (float)(contour.PiercingPosition.Y * scale);
                    float pierceSize = settings.PiercingPointSize;
                    Color pierceColor = settings.PiercingPointColor;
                    
                    using (SolidBrush brush = new SolidBrush(pierceColor))
                    {
                        g.FillEllipse(brush, pierceX - pierceSize/2, pierceY - pierceSize/2, pierceSize, pierceSize);
                    }
                }
                
                // 2. AllSegments 렌더링 (Lead-in 포함)
                if (contour.AllSegments != null)
                {
                    int elementIndex = 0;
                    foreach (var segment in contour.AllSegments)
                    {
                        // Lead-in 세그먼트 체크
                        bool isLeadInSegment = (contour.LeadIn != null && contour.LeadIn.Path != null && 
                                                contour.LeadIn.Path.Contains(segment));
                        
                        // 색상 및 굵기 결정
                        Color segmentColor;
                        float lineWidth;
                        
                        if (isLeadInSegment)
                        {
                            // Lead-in: 노란색, 얇은 선
                            segmentColor = settings.LeadInColor;
                            lineWidth = settings.LeadInWidth;
                        }
                        else if (isMarking)
                        {
                            // Marking: 노란색
                            segmentColor = settings.MarkingColor;
                            lineWidth = settings.CuttingPendingWidth;
                        }
                        else
                        {
                            // 일반 절단 경로: 상태에 따라
                            switch (status)
                            {
                                case CutStatus.Completed:
                                    segmentColor = settings.CuttingCompletedColor;
                                    lineWidth = settings.CuttingCompletedWidth;
                                    break;
                                case CutStatus.InProgress:
                                    segmentColor = settings.CuttingInProgressColor;
                                    lineWidth = settings.CuttingInProgressWidth;
                                    break;
                                case CutStatus.NotStarted:
                                default:
                                    segmentColor = settings.CuttingPendingColor;
                                    lineWidth = settings.CuttingPendingWidth;
                                    break;
                            }
                        }
                        
                        // 세그먼트 렌더링
                        using (Pen pen = new Pen(segmentColor, lineWidth))
                        {
                            DrawPathSegment(g, pen, segment, scale, offsetX, offsetY);
                        }
                        
                        elementIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] RenderContour 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// PathSegment 렌더링 (Line 또는 Arc)
        /// </summary>
        private void DrawPathSegment(Graphics g, Pen pen, PathSegment segment, 
            float scale, float offsetX, float offsetY)
        {
            if (segment == null)
                return;
            
            if (segment.Type == PathSegmentType.Line)
            {
                float x1 = (float)(segment.Start.X * scale) + offsetX;
                float y1 = offsetY - (float)(segment.Start.Y * scale);
                float x2 = (float)(segment.End.X * scale) + offsetX;
                float y2 = offsetY - (float)(segment.End.Y * scale);
                g.DrawLine(pen, x1, y1, x2, y2);
            }
            else if (segment.Type == PathSegmentType.Arc)
            {
                DrawArc(g, pen, segment as ArcSegment, scale, offsetX, offsetY);
            }
        }
        
        /// <summary>
        /// 호(Arc) 그리기 - Phase7/8 정확한 호 렌더링 로직
        /// </summary>
        private void DrawArc(Graphics g, Pen pen, ArcSegment arc, 
            float scale, float offsetX, float offsetY)
        {
            try
            {
                if (arc == null)
                    return;
                    
                float cx = (float)(arc.Center.X * scale) + offsetX;
                float cy = offsetY - (float)(arc.Center.Y * scale);
                float radius = (float)(arc.Radius * scale);
                
                // GDI+ DrawArc 파라미터: (x, y, width, height, startAngle, sweepAngle)
                // x, y: 바운딩 박스의 좌상단
                // startAngle: 시작 각도 (도 단위, 3시 방향 = 0도, 시계방향)
                // sweepAngle: 스윕 각도 (도 단위)
                
                float rectX = cx - radius;
                float rectY = cy - radius;
                float rectWidth = radius * 2;
                float rectHeight = radius * 2;
                
                // 각도 계산 (arc.StartAngle, arc.EndAngle는 이미 도 단위)
                float startAngleDeg = (float)arc.StartAngle;
                float endAngleDeg = (float)arc.EndAngle;
                
                // Y축 반전으로 인해 각도도 반전 필요
                startAngleDeg = -startAngleDeg;
                endAngleDeg = -endAngleDeg;
                
                // Sweep angle 계산
                float sweepAngle = endAngleDeg - startAngleDeg;
                
                // Clockwise 방향 고려
                if (arc.Clockwise)
                {
                    // CW: sweep angle이 음수일 수 있음
                    if (sweepAngle > 0)
                        sweepAngle -= 360;
                }
                else
                {
                    // CCW: sweep angle이 양수여야 함
                    if (sweepAngle < 0)
                        sweepAngle += 360;
                }
                
                // GDI+ DrawArc 호출
                g.DrawArc(pen, rectX, rectY, rectWidth, rectHeight, startAngleDeg, sweepAngle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] DrawArc 오류: {ex.Message}");
            }
        }
        

        
        /// <summary>
        /// 상태 범례 표시 (우하단)
        /// </summary>
        private void DrawStatusLegend(Graphics g)
        {
            try
            {
                float x = viewerPanel.Width - 150;
                float y = viewerPanel.Height - 100;
                
                Font font = new Font("Arial", 10, FontStyle.Regular);
                
                // 미시작 (회색)
                g.DrawLine(new Pen(Color.Gray, 2), x, y, x + 30, y);
                g.DrawString("미시작", font, Brushes.White, x + 35, y - 7);
                
                // 진행 중 (노란색)
                g.DrawLine(new Pen(Color.Yellow, 2), x, y + 25, x + 30, y + 25);
                g.DrawString("진행 중", font, Brushes.White, x + 35, y + 18);
                
                // 완료 (초록색)
                g.DrawLine(new Pen(Color.LimeGreen, 2), x, y + 50, x + 30, y + 50);
                g.DrawString("완료", font, Brushes.White, x + 35, y + 43);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] DrawStatusLegend 오류: {ex.Message}");
            }
        }

        private void ViewerPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastMousePos = e.Location;
            }
        }
        
        private void ViewerPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.Button == MouseButtons.Left)
            {
                // Pan: 마우스 이동에 따라 뷰 이동
                float dx = -(e.X - lastMousePos.X) / (float)viewerPanel.Width * 2.0f / zoom;
                float dy = (e.Y - lastMousePos.Y) / (float)viewerPanel.Height * 2.0f / zoom;
                
                panX += dx * 100f;  // GDI+ 좌표계 스케일 조정
                panY += dy * 100f;
                
                lastMousePos = e.Location;
                viewerPanel.Invalidate();  // 다시 그리기
            }
            
            // TODO: 마우스 좌표를 월드 좌표로 변환하여 표시
        }
        
        private void ViewerPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
            }
        }
        
        private void ViewerPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            // Zoom: 마우스 휠로 확대/축소
            float zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            zoom *= zoomFactor;
            
            // Zoom 범위 제한
            if (zoom < 0.1f) zoom = 0.1f;
            if (zoom > 100.0f) zoom = 100.0f;
            
            viewerPanel.Invalidate();  // 다시 그리기
        }

        private void ViewerPanel_MouseClick(object sender, MouseEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] Viewer 클릭: ({e.X}, {e.Y})");
        }

        #endregion

        #region Program Info 패널 이벤트 핸들러

        private void ProgramInfoPanel_SimulationClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] 시뮬레이션 버튼 클릭");
        }

        private void ProgramInfoPanel_ElementSelectClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] 엘리먼트 선택 버튼 클릭");
        }

        private void ProgramInfoPanel_ITagTestClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ITag 테스트 버튼 클릭");
                
                // ITag 테스트 팝업 폼 열기 (this의 ITag 인스턴스 전달)
                using (ITagTestForm testForm = new ITagTestForm(this))
                {
                    testForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ProgramInfoPanel_ITagTestClicked 오류: {ex.Message}");
                MessageBox.Show($"ITag 테스트 폼 열기 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
