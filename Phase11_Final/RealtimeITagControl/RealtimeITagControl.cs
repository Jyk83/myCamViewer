using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Siemens.Runtime;
using Siemens.Runtime.ITag;
using RealtimeITagControl.UI;
using RealtimeITagControl.MPF;
using RealtimeITagControl.Rendering;

namespace RealtimeITagControl
{
    // NativeRenderer는 CamViewerCore.cs에 정의되어 있음 (중복 방지)
    
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
        private CamViewerControl camViewerControl;  // Phase8 OpenGL 렌더러 (핵심!)

        private string currentMpfPath;      // 현재 로드된 MPF 파일 경로
        private MPFProgram mpfProgram;      // MPF 프로그램 데이터 (파싱 결과)
        private TagData lastTagData;
        
        // 로그 파일 경로
        private static readonly string logFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
            "RealtimeITagControl_Log.txt");
        
        // Trace 상태 관리
        private TraceState currentTraceState = TraceState.Idle;
        private bool isTracing = false;
        private System.Collections.Generic.Dictionary<string, ContourTraceInfo> contourStatusMap;
        
        // OpenGL 렌더링, Pan/Zoom은 CamViewerControl에서 처리

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
            programInfoPanel.ShowPartNumberChanged += ProgramInfoPanel_ShowPartNumberChanged;
            programInfoPanel.ShowContourNumberChanged += ProgramInfoPanel_ShowContourNumberChanged;
            this.Controls.Add(programInfoPanel);
            
            // 초기 연결 상태 표시
            programInfoPanel.UpdateConnectionStatus(false, false, null);

            // 우측 CamViewerControl (Phase8 OpenGL 렌더러) (968x630)
            camViewerControl = new CamViewerControl
            {
                Location = new Point(300, 0),
                Size = new Size(968, 630),
                Dock = DockStyle.None
            };
            this.Controls.Add(camViewerControl);

            // Load 시 ITag 연결
            this.Load += RealtimeITagControl_Load;

            // Dispose 시 연결 해제
            this.Disposed += RealtimeITagControl_Disposed;
        }

        /// <summary>
        /// UserControl Load 이벤트 핸들러
        /// </summary>
        private void RealtimeITagControl_Load(object sender, EventArgs e)
        {
            if (!DesignMode)
            {
                Connect();
                StartCyclicRead(500);
            }
        }

        /// <summary>
        /// 리소스 정리 (Dispose 이벤트 핸들러)
        /// </summary>
        private void RealtimeITagControl_Disposed(object sender, EventArgs e)
        {
            CleanupResources();
        }

        /// <summary>
        /// Dispose 오버라이드 (WinCC에서 확실하게 호출되도록)
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupResources();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 리소스 정리 공통 메서드
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] === CleanupResources 시작 ===");
                LogToFile("=== CleanupResources 시작 ===");

                // ITag 연결 해제
                Disconnect();

                // 이벤트 구독 해제
                if (programInfoPanel != null)
                {
                    programInfoPanel.SimulationClicked -= ProgramInfoPanel_SimulationClicked;
                    programInfoPanel.ElementSelectClicked -= ProgramInfoPanel_ElementSelectClicked;
                    programInfoPanel.ShowPartNumberChanged -= ProgramInfoPanel_ShowPartNumberChanged;
                    programInfoPanel.ShowContourNumberChanged -= ProgramInfoPanel_ShowContourNumberChanged;
                }

                // CamViewerControl은 자체 Dispose 처리

                // MPF 데이터 정리
                currentMpfPath = null;
                mpfProgram = null;  // MPF 프로그램 데이터 해제
                lastTagData = default(TagData);
                
                // 컨투어 상태는 CamViewerControl에서 관리
                
                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] === CleanupResources 완료 ===");
                LogToFile("=== CleanupResources 완료 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] CleanupResources 오류: {ex.Message}");
                LogToFile($"CleanupResources 오류: {ex.Message}");
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
                
                // 4. COM 객체 명시적 해제 (Deadlock 방지)
                if (m_ITag != null)
                {
                    try
                    {
                        // COM 객체 참조 카운트 감소
                        if (Marshal.IsComObject(m_ITag))
                        {
                            int refCount = Marshal.ReleaseComObject(m_ITag);
                            System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] COM ReleaseComObject: refCount={refCount}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ⚠️ ReleaseComObject 오류: {ex.Message}");
                    }
                    finally
                    {
                        m_ITag = null;
                    }
                }
                
                m_RegisterCookie = 0;

                // 5. 이벤트 발생
                ConnectionChanged?.Invoke(this, false);
                
                // 6. UI 업데이트
                UpdateConnectionStatusUI();

                // 7. Garbage Collection 강제 실행 (COM 리소스 정리)
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

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
        
        #endregion
        
        // OpenGL 초기화 및 정리는 CamViewerControl에서 처리

        #region ITag 기본 읽기/쓰기
        
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
                
                // 8. CamViewerControl에 MPF 파일 로드 (Phase8 방식)
                if (camViewerControl != null && !camViewerControl.IsDisposed)
                {
                    camViewerControl.LoadMPFFile(mpfPath);
                    LogToFile("CamViewerControl에 MPF 파일 로드 완료");
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
                        // 완료 - 마지막 파트/컨투어인지 확인
                        if (currentTraceState != TraceState.Completed)
                        {
                            // 마지막 파트/컨투어 확인
                            bool isLastPartContour = IsLastPartAndContour(tagData.CurrentPart, tagData.CurrentContour);
                            
                            if (isLastPartContour)
                            {
                                // 마지막까지 완료된 경우 모든 컨투어를 Completed 처리
                                CompleteTracing();
                                System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] ✅ Trace 완료 - 마지막 파트/컨투어 도달");
                            }
                            else
                            {
                                // 중간에 멈춘 경우 현재까지만 업데이트
                                UpdateContourStatus(tagData);
                                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] ⏹️ Trace 중지 - Part {tagData.CurrentPart}, Contour {tagData.CurrentContour}");
                            }
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
        /// 마지막 파트와 컨투어인지 확인
        /// </summary>
        private bool IsLastPartAndContour(int currentPart, int currentContour)
        {
            if (mpfProgram == null || mpfProgram.Parts == null || mpfProgram.Parts.Count == 0)
                return false;

            // 마지막 파트 확인
            int lastPartNumber = mpfProgram.Parts.Count;
            if (currentPart != lastPartNumber)
                return false;

            // 마지막 파트의 마지막 컨투어 확인
            var lastPart = mpfProgram.Parts[mpfProgram.Parts.Count - 1];
            if (lastPart == null || lastPart.Contours == null || lastPart.Contours.Count == 0)
                return false;

            int lastContourNumber = lastPart.Contours.Count;
            return currentContour == lastContourNumber;
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
                
                // 3. CamViewerControl Trace 시작
                if (camViewerControl != null && !camViewerControl.IsDisposed)
                {
                    camViewerControl.Invalidate();
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
                
                // CamViewerControl 다시 그리기
                if (camViewerControl != null && !camViewerControl.IsDisposed)
                {
                    camViewerControl.Invalidate();
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
                // Trace 중일 때만 Invalidate (불필요한 다시 그리기 방지)
                if (isTracing && camViewerControl != null && !camViewerControl.IsDisposed)
                {
                    // UpdateContourStatus에서 실제로 상태가 변경된 경우에만 다시 그림
                    // 현재는 간단히 Tracing 상태일 때만 업데이트
                    camViewerControl.Invalidate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] UpdateViewer 오류: {ex.Message}");
            }
        }

        #endregion

        #region Program Info 패널 이벤트 핸들러

        private void ProgramInfoPanel_SimulationClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] 시뮬레이션 버튼 클릭");
            
            // CamViewerControl의 Simulation 기능 호출
            if (camViewerControl != null)
            {
                camViewerControl.StartSimulation();
            }
        }

        private void ProgramInfoPanel_ElementSelectClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] 엘리먼트 선택 버튼 클릭");
            
            // Toggle Contour Selection Mode
            if (camViewerControl != null)
            {
                var selectionManager = camViewerControl.GetSelectionManager();
                if (selectionManager != null)
                {
                    // Toggle between None and Contour selection mode
                    if (selectionManager.CurrentMode == RealtimeITagControl.Selection.SelectionManager.SelectionMode.Contour)
                    {
                        // Turn off selection mode
                        camViewerControl.SetSelectionMode(RealtimeITagControl.Selection.SelectionManager.SelectionMode.None);
                        System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] Contour Selection OFF");
                    }
                    else
                    {
                        // Turn on contour selection mode
                        camViewerControl.SetSelectionMode(RealtimeITagControl.Selection.SelectionManager.SelectionMode.Contour);
                        System.Diagnostics.Debug.WriteLine("[RealtimeITagControl] Contour Selection ON");
                    }
                }
            }
        }

        /// <summary>
        /// 파트 번호 표시 체크박스 변경 이벤트
        /// </summary>
        private void ProgramInfoPanel_ShowPartNumberChanged(object sender, bool isChecked)
        {
            System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] 파트 번호 표시: {isChecked}");
            
            if (camViewerControl != null)
            {
                // RenderSettings 업데이트
                Rendering.RenderSettings.Instance.ShowPartNumbers = isChecked;
                
                // 파트 외곽선도 함께 표시/숨김
                Rendering.RenderSettings.Instance.ShowPartBoundaries = isChecked;
                
                // 화면 갱신
                camViewerControl.Invalidate();
            }
        }

        /// <summary>
        /// 컨투어 번호 표시 체크박스 변경 이벤트
        /// </summary>
        private void ProgramInfoPanel_ShowContourNumberChanged(object sender, bool isChecked)
        {
            System.Diagnostics.Debug.WriteLine($"[RealtimeITagControl] 컨투어 번호 표시: {isChecked}");
            
            if (camViewerControl != null)
            {
                // RenderSettings 업데이트
                Rendering.RenderSettings.Instance.ShowContourNumbers = isChecked;
                
                // 화면 갱신
                camViewerControl.Invalidate();
            }
        }

        #endregion
    }
}
