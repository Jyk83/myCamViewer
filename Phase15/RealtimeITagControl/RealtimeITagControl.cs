using System;
using System.Collections.Generic;
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
    /// ITagManager 싱글톤 패턴 사용 (중복 인스턴스 방지)
    /// </summary>
    public partial class RealtimeITagControl : UserControl, IITagManager
    {
        #region 멤버 변수

        // ITagManager 싱글톤 사용 (중복 방지)
        private ITagManager tagManager => ITagManager.Instance;

        private ProgramInfoPanel programInfoPanel;
        private CamViewerControl camViewerControl;  // Phase8 OpenGL 렌더러 (핵심!)
        private ContourColorLegendForm contourLegendForm;  // Phase 12: 컨투어 색상 범례 폼

        private string currentMpfPath;      // 현재 로드된 MPF 파일 경로
        private MPFProgram mpfProgram;      // MPF 프로그램 데이터 (파싱 결과)
        private TagData? lastTagData = null;  // Nullable struct
        private int cycleMs = 25;  // Phase 13: ITag cycle time (auto-adjusted based on MPF complexity)

        // 로그 파일 경로

        // Trace 상태 관리
        private TraceState currentTraceState = TraceState.Idle;
        private bool isTracing = false;
        private System.Collections.Generic.Dictionary<string, ContourTraceInfo> contourStatusMap;

        // Dispose 중복 호출 방지
        private bool isDisposed = false;
        private readonly object disposeLock = new object();

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

        #region IITagManager 구현 (속성) - ITagManager 싱글톤 위임

        public ITag ITag => tagManager.ITagInstance;
        public bool IsConnected => tagManager.IsConnected;
        public bool IsCyclicReading => tagManager.IsCyclicReading;
        public string LastErrorMessage => tagManager.LastErrorMessage;

        // 이벤트는 ITagManager의 이벤트를 그대로 전달
        public event EventHandler<TagDataEventArgs> DataChanged
        {
            add { tagManager.DataChanged += value; }
            remove { tagManager.DataChanged -= value; }
        }

        public event EventHandler<bool> ConnectionChanged
        {
            add { tagManager.ConnectionChanged += value; }
            remove { tagManager.ConnectionChanged -= value; }
        }

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
            programInfoPanel.StopSimulationClicked += ProgramInfoPanel_StopSimulationClicked;
            programInfoPanel.ElementSelectClicked += ProgramInfoPanel_ElementSelectClicked;
            programInfoPanel.ShowPartNumberChanged += ProgramInfoPanel_ShowPartNumberChanged;
            programInfoPanel.ShowContourNumberChanged += ProgramInfoPanel_ShowContourNumberChanged;
            programInfoPanel.EnableContourSelectionChanged += ProgramInfoPanel_EnableContourSelectionChanged;  // Phase 12
            programInfoPanel.ShowContourSelectionBoxesClicked += ProgramInfoPanel_ShowContourSelectionBoxesClicked;  // Phase 12
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
            camViewerControl.ContourSelected += CamViewerControl_ContourSelected;
            camViewerControl.MouseCoordinatesChanged += CamViewerControl_MouseCoordinatesChanged; // Phase 13
            camViewerControl.MPFLoaded += CamViewerControl_MPFLoaded; // Phase 13
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
                // ITagManager 싱글톤 초기화 (Site 전달)
                ITagManager.Initialize(this.Site);

                // ITagManager 이벤트 구독
                tagManager.DataChanged += ITagManager_DataChanged;
                tagManager.ConnectionChanged += ITagManager_ConnectionChanged;

                // 연결 및 주기적 읽기 시작
                Connect();
                StartCyclicRead(this.cycleMs);
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
            // 중복 호출 방지
            lock (disposeLock)
            {
                if (isDisposed)
                {
                    return;
                }
                isDisposed = true;
            }

            try
            {
                LogHelper.Log("RealtimeITagControl", "=== CleanupResources 시작 ===");

                // 1. ITag 연결 해제 (Cyclic Read 먼저 중단)
                try
                {
                    if (tagManager != null && tagManager.IsCyclicReading)
                    {
                        tagManager.StopCyclicRead();
                        LogHelper.Log("RealtimeITagControl", "Cyclic Read stopped");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log("RealtimeITagControl", $"Cyclic Read stop error: {ex.Message}");
                }

                // 2. ITagManager 이벤트 구독 해제
                try
                {
                    if (tagManager != null)
                    {
                        tagManager.DataChanged -= ITagManager_DataChanged;
                        tagManager.ConnectionChanged -= ITagManager_ConnectionChanged;
                        LogHelper.Log("RealtimeITagControl", "ITagManager events unsubscribed");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log("RealtimeITagControl", $"ITagManager event unsubscribe error: {ex.Message}");
                }

                // 3. ITag 연결 해제
                try
                {
                    Disconnect();
                    LogHelper.Log("RealtimeITagControl", "ITag Disconnected");
                }
                catch (Exception ex)
                {
                    LogHelper.Log("RealtimeITagControl", $"ITag Disconnect error: {ex.Message}");
                }

                // 4. UI 이벤트 구독 해제
                try
                {
                    if (programInfoPanel != null)
                    {
                        programInfoPanel.SimulationClicked -= ProgramInfoPanel_SimulationClicked;
                        programInfoPanel.StopSimulationClicked -= ProgramInfoPanel_StopSimulationClicked;
                        programInfoPanel.ElementSelectClicked -= ProgramInfoPanel_ElementSelectClicked;
                        programInfoPanel.ShowPartNumberChanged -= ProgramInfoPanel_ShowPartNumberChanged;
                        programInfoPanel.ShowContourNumberChanged -= ProgramInfoPanel_ShowContourNumberChanged;
                        programInfoPanel.EnableContourSelectionChanged -= ProgramInfoPanel_EnableContourSelectionChanged;  // Phase 12
                        LogHelper.Log("RealtimeITagControl", "ProgramInfoPanel events unsubscribed");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log("RealtimeITagControl", $"ProgramInfoPanel event unsubscribe error: {ex.Message}");
                }

                // 5. CamViewerControl 이벤트 구독 해제 및 Dispose
                try
                {
                    if (camViewerControl != null && !camViewerControl.IsDisposed)
                    {
                        try
                        {
                            camViewerControl.ContourSelected -= CamViewerControl_ContourSelected;
                            camViewerControl.MouseCoordinatesChanged -= CamViewerControl_MouseCoordinatesChanged; // Phase 13
                            LogHelper.Log("RealtimeITagControl", "CamViewerControl events unsubscribed");
                        }
                        catch (Exception unsubEx)
                        {
                            LogHelper.Log("RealtimeITagControl", $"CamViewerControl event unsubscribe error: {unsubEx.Message}");
                        }

                        // WinCC 환경에서 Controls.Clear() 시도 시 null 참조 발생
                        // OpenGL 및 ITag 리소스는 이미 정리되었으므로 생략

                        try
                        {
                            // CamViewerControl 자체 Dispose 호출 (OpenGL 리소스 정리)
                            camViewerControl.Dispose();
                            LogHelper.Log("RealtimeITagControl", "CamViewerControl disposed (OpenGL cleanup)");
                        }
                        catch (Exception disposeEx)
                        {
                            // WinCC FwDotNetContainer 오류는 무시 (정상 동작)
                            if (disposeEx.Message.Contains("GetService") || disposeEx.Message.Contains("FwDotNetContainer"))
                            {
                                LogHelper.Log("RealtimeITagControl", "CamViewerControl dispose completed (WinCC Container error ignored)");
                            }
                            else
                            {
                                LogHelper.Log("RealtimeITagControl", $"CamViewerControl dispose error: {disposeEx.Message}\n{disposeEx.StackTrace}");
                            }
                        }

                        camViewerControl = null;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log("RealtimeITagControl", $"CamViewerControl cleanup error: {ex.Message}\n{ex.StackTrace}");
                }

                // 6. ProgramInfoPanel Dispose
                try
                {
                    if (programInfoPanel != null && !programInfoPanel.IsDisposed)
                    {
                        // WinCC 환경에서 ProgramInfoPanel Dispose 시 오류 발생
                        // 이벤트 구독 해제만으로 충분하므로 Dispose 생략
                        LogHelper.Log("RealtimeITagControl", "ProgramInfoPanel cleanup skipped (WinCC managed)");

                        programInfoPanel = null;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log("RealtimeITagControl", $"ProgramInfoPanel cleanup error: {ex.Message}");
                }

                // 7. MPF 데이터 정리
                try
                {
                    currentMpfPath = null;
                    mpfProgram = null;
                    lastTagData = default(TagData);
                    contourStatusMap?.Clear();
                    contourStatusMap = null;
                    LogHelper.Log("RealtimeITagControl", "MPF data cleared");
                }
                catch (Exception ex)
                {
                    LogHelper.Log("RealtimeITagControl", $"MPF data clear error: {ex.Message}");
                }

                LogHelper.Log("RealtimeITagControl", "=== CleanupResources 완료 ===");
            }
            catch (Exception ex)
            {
                LogHelper.Log("RealtimeITagControl", $"CleanupResources critical error: {ex.Message}\n{ex.StackTrace}");
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
            }
            catch { }

            // ITagManager 싱글톤에 위임
            bool result = tagManager.Connect();

            if (result)
            {
                UpdateConnectionStatusUI();
            }
            else
            {
                UpdateConnectionStatusUI();
            }

            return result;
        }

        /// <summary>
        /// ITag 서버 연결 해제 (완벽한 정리)
        /// </summary>
        public void Disconnect()
        {
            // ITagManager 싱글톤에 위임
            tagManager.Disconnect();
            UpdateConnectionStatusUI();
        }

        /// <summary>
        /// <summary>
        /// 14개 Tag 주기적 읽기 시작
        /// </summary>
        public bool StartCyclicRead(int CycleMs)
        {
            if (CycleMs <= 0)
            {
                CycleMs = 25;
            }

            // ITagManager 싱글톤에 위임
            bool result = tagManager.StartCyclicRead(CycleMs);
            UpdateConnectionStatusUI();
            return result;
        }

        /// <summary>
        /// 주기적 읽기 중지
        /// </summary>
        public void StopCyclicRead()
        {
            // ITagManager 싱글톤에 위임
            tagManager.StopCyclicRead();
            UpdateConnectionStatusUI();
        }
        #endregion

        // OpenGL 초기화 및 정리는 CamViewerControl에서 처리

        #region ITag 기본 읽기/쓰기

        /// <summary>
        /// 단일 Tag 읽기
        /// </summary>
        public bool ReadTag(string tagName, out object value)
        {
            // ITagManager 싱글톤에 위임
            return tagManager.ReadTag(tagName, out value);
        }

        /// <summary>
        /// 여러 Tag 읽기
        /// </summary>
        public bool ReadTags(string[] tagNames, out object[] values)
        {
            // ITagManager 싱글톤에 위임
            return tagManager.ReadTags(tagNames, out values);
        }

        /// <summary>
        /// 단일 Tag 쓰기
        /// </summary>
        public bool WriteTag(string tagName, object value)
        {
            // ITagManager 싱글톤에 위임
            return tagManager.WriteTag(tagName, value);
        }

        #endregion

        #region ITagManager 이벤트 핸들러

        /// <summary>
        /// ITagManager DataChanged 이벤트 핸들러 (ITagSink 대체)
        /// </summary>
        private void ITagManager_DataChanged(object sender, TagDataEventArgs e)
        {
            try
            {
                TagData tagData = e.Data;

                // 내부 처리
                lastTagData = tagData;

                // Phase 13: Update ViewDirection based on HMI_VIEW_DIR_TYPE tag
                UpdateViewDirection(tagData);

                // Phase 14.2: Update OpenGL rendering mode based on HMI_OPENGL_TYPE tag
                UpdateOpenGLMode(tagData);

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
                }

                if (!string.IsNullOrEmpty(newMpfPath) && newMpfPath != currentMpfPath)
                {
                    bool loaded = LoadMpfFile(newMpfPath);
                    if (loaded)
                    {
                        currentTraceState = TraceState.Loaded;
                    }
                    else
                    {
                    }
                }

                // Trace 로직 처리 (WorkStatus에 따라)
                ProcessTraceLogic(tagData);

                // Viewer 업데이트
                UpdateViewer(tagData);
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// ITagManager ConnectionChanged 이벤트 핸들러
        /// </summary>
        private void ITagManager_ConnectionChanged(object sender, bool isConnected)
        {
            UpdateConnectionStatusUI();
        }

        #endregion

        #region UI 업데이트

        private void UpdateConnectionStatusUI()
        {
            try
            {
                if (programInfoPanel != null && !programInfoPanel.IsDisposed)
                {
                    programInfoPanel.UpdateConnectionStatus(tagManager.IsConnected, tagManager.IsCyclicReading, tagManager.LastErrorMessage);
                }
            }
            catch (Exception ex)
            {
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

                // 1. 파일 경로 유효성 검사
                if (string.IsNullOrEmpty(mpfPath))
                {
                    return false;
                }

                // 2. 파일 존재 확인
                bool fileExists = File.Exists(mpfPath);

                if (!fileExists)
                {
                    return false;
                }

                // 3. 이미 로드된 파일인지 확인 (재로드 방지)
                if (currentMpfPath == mpfPath && mpfProgram != null)
                {
                    return true;
                }


                // 4. MPF 파일 읽기
                string fileContent = System.IO.File.ReadAllText(mpfPath);

                // 5. MPF 파일 파싱
                var parser = new MPFParser();
                mpfProgram = parser.Parse(fileContent);

                if (mpfProgram == null)
                {
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


                // 7. 현재 로드된 파일 경로 저장
                currentMpfPath = mpfPath;

                // 8. CamViewerControl에 MPF 파일 로드 (Phase8 방식)
                if (camViewerControl != null && !camViewerControl.IsDisposed)
                {
                    camViewerControl.LoadMPFFile(mpfPath);
                }

                return true;
            }
            catch (Exception ex)
            {
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
                if (!tagManager.IsConnected)
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
                        }

                        // 실시간 컨투어 상태 업데이트
                        UpdateContourStatus(tagData);

                        // Phase 13: Invalidate는 ProgressUpdated 이벤트에서 처리
                        // (UpdateContourStatus → UpdateElementProgress → UpdateProgress → ProgressUpdated)


                        currentTraceState = TraceState.Tracing;
                        isTracing = true;
                        
                        // Phase 15.3: Start performance recording automatically
                        camViewerControl?.StartPerformanceRecording();
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
                            }
                            else
                            {
                                // 중간에 멈춘 경우 현재까지만 업데이트
                                UpdateContourStatus(tagData);
                            }
                        }
                        currentTraceState = TraceState.Completed;
                        isTracing = false;
                        
                        // Phase 15.3: Stop and export performance
                        camViewerControl?.StopPerformanceRecording();
                        string csvPath = camViewerControl?.ExportPerformanceCSV();
                        if (!string.IsNullOrEmpty(csvPath))
                        {
                            LogHelper.Log("RealtimeITagControl", $"Performance CSV saved: {csvPath}");
                        }
                        break;

                    case TagDefinitions.WorkStatus.Reset:      // WorkStatus = 2
                    case TagDefinitions.WorkStatus.FeedHold:   // WorkStatus = 3
                        // 일시정지
                        if (currentTraceState == TraceState.Tracing)
                        {
                            PauseTracing();
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
        /// Phase 13: 실시간 엘리먼트 단위 트레이스 (ActLineCode + ProgressDistance 기반)
        /// </summary>
        private void UpdateContourStatus(TagData tagData)
        {
            try
            {
                if (contourStatusMap == null || mpfProgram?.Parts == null)
                    return;

                int currentPart = tagData.CurrentPart;
                int currentContour = tagData.CurrentContour;
                double progressDistance = tagData.ProgressDistance;  // 실제 거리 (mm)
                string actLineCode = tagData.ActLineCode;  // 현재 실행 중인 G-code

                // Phase 13 디버깅: 입력 데이터 확인
                LogHelper.Log("RealtimeITagControl", 
                    $"[Phase13] UpdateContourStatus: Part={currentPart}, Contour={currentContour}, " +
                    $"ProgressDistance={progressDistance:F2}mm, ActLineCode=\"{actLineCode}\"");

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
                        // 현재 파트의 현재 컨투어는 진행 중 → Phase 13: 엘리먼트 단위 트레이스
                        else if (partNum == currentPart && contNum == currentContour)
                        {
                            contourStatusMap[key].Status = CutStatus.InProgress;
                            contourStatusMap[key].CompletedDistance = progressDistance;

                            // Phase 13: 엘리먼트 단위 진행률 계산 (ActLineCode + ProgressDistance)
                            UpdateElementProgress(partIdx, contIdx, contour, tagData.ActLineCode, progressDistance);
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
            }
        }

        /// <summary>
        /// Trace 시작 (처음부터)
        /// </summary>
        private void StartTracing()
        {
            try
            {

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


                // 3. progressManager 시작 (최초 Part/Contour는 ITag 데이터에서 받음)
                if (camViewerControl?.progressManager != null && lastTagData.HasValue)
                {
                    int partIdx = lastTagData.Value.CurrentPart - 1;  // 0-based
                    int contIdx = lastTagData.Value.CurrentContour - 1;  // 0-based
                    if (partIdx >= 0 && contIdx >= 0)
                    {
                        camViewerControl.progressManager.StartCuttingProgress(lastTagData.Value.CurrentPart, lastTagData.Value.CurrentContour, false);
                        LogHelper.Log("RealtimeITagControl", $"CuttingProgress Started: Part {lastTagData.Value.CurrentPart}, Contour {lastTagData.Value.CurrentContour}");
                    }
                }

                // 4. CamViewerControl Trace 시작
                if (camViewerControl != null && !camViewerControl.IsDisposed)
                {
                    camViewerControl.Invalidate();
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Trace 완료
        /// </summary>
        private void CompleteTracing()
        {
            try
            {

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
            }
        }

        /// <summary>
        /// Trace 일시정지
        /// </summary>
        private void PauseTracing()
        {
            try
            {

                // 현재 상태 유지 (아무 동작 안함)
                // Viewer는 자동으로 현재 상태 유지
            }
            catch (Exception ex)
            {
            }
        }

        #endregion

        #region Phase 13: View Direction Update

        /// <summary>
        /// Phase 13: Update ViewDirection based on HMI_VIEW_DIR_TYPE tag
        /// </summary>
        private void UpdateViewDirection(TagData tagData)
        {
            try
            {
                // HMI_VIEW_DIR_TYPE 값에 따라 RenderSettings.ViewDirection 업데이트
                // Type 1 = RightBottom (우하단 원점)
                // Type 2 = LeftBottom (좌하단 원점, OpenGL 기본)

                int dirTypeValue = (int)tagData.DirType;

                Rendering.ViewDirectionType newDirection;
                if (dirTypeValue == 1)
                {
                    newDirection = Rendering.ViewDirectionType.RightBottom;
                }
                else if (dirTypeValue == 2)
                {
                    newDirection = Rendering.ViewDirectionType.LeftBottom;
                }
                else
                {
                    // 기본값: Type 1 (RightBottom)
                    newDirection = Rendering.ViewDirectionType.RightBottom;
                }

                // RenderSettings 업데이트
                if (Rendering.RenderSettings.Instance.ViewDirection != newDirection)
                {
                    Rendering.RenderSettings.Instance.ViewDirection = newDirection;

                    LogHelper.Log("RealtimeITagControl", $"[Phase13] ViewDirection changed to: {newDirection} (HMI_VIEW_DIR_TYPE={dirTypeValue})");

                    // ViewDirection 변경 시 화면 다시 그리기
                    if (camViewerControl != null && !camViewerControl.IsDisposed)
                    {
                        camViewerControl.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log("RealtimeITagControl", $"[Phase13] UpdateViewDirection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Phase 14.2: Update OpenGL rendering mode based on HMI_OPENGL_TYPE tag
        /// </summary>
        private void UpdateOpenGLMode(TagData tagData)
        {
            try
            {
                // HMI_OPENGL_TYPE 값에 따라 OpenGL 렌더링 모드 업데이트
                // Type 1 = Default (전체 화면 갱신)
                // Type 2 = DirtyRegion (영역만 갱신)

                int openglType = tagData.OpenGLType;

                // 현재 모드와 다를 때만 변경
                if ((int)Rendering.OpenGLSettings.CurrentMode != openglType)
                {
                    Rendering.OpenGLSettings.SetModeFromITag(openglType);

                    LogHelper.Log("RealtimeITagControl", 
                        $"[Phase14.2] OpenGL Mode changed: {Rendering.OpenGLSettings.CurrentMode} " +
                        $"(HMI_OPENGL_TYPE={openglType})");

                    // 모드 변경 시 화면 다시 그리기
                    if (camViewerControl != null && !camViewerControl.IsDisposed)
                    {
                        camViewerControl.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log("RealtimeITagControl", $"[Phase14.2] UpdateOpenGLMode error: {ex.Message}");
            }
        }

        #endregion

        #region Viewer 업데이트

        private void UpdateViewer(TagData data)
        {
            try
            {
                // Phase 14.2: 실시간 트레이스 전용 Invalidate (TYPE에 따라 처리)
                // 조건: isTracing (Tag 변화 + 트레이스 중)만 확인
                // 시뮬레이션과 실시간 트레이스는 동시 동작 불가
                
                if (isTracing && camViewerControl != null && !camViewerControl.IsDisposed)
                {
                    // Phase 15.2: OpenGL 렌더링 모드에 따른 Invalidate
                    if (Rendering.OpenGLSettings.CurrentMode == Rendering.OpenGLRenderMode.DirtyRegion)
                    {
                        // TYPE=2: Dirty Region - 엘리먼트 단위 최소 영역 갱신
                        if (camViewerControl.progressManager != null)
                        {
                            int elementIndex = camViewerControl.progressManager.CurrentElementIndex;
                            double progress = camViewerControl.progressManager.ElementProgress;
                            
                            // 엘리먼트 단위 갱신 (픽셀 수 1/10 감소 목표)
                            camViewerControl.InvalidateElementRegion(
                                data.CurrentPart - 1,  // 0-based
                                data.CurrentContour - 1,  // 0-based
                                elementIndex,
                                progress
                            );
                        }
                        else
                        {
                            // fallback: 컨투어 단위
                            camViewerControl.InvalidateProgressRegion(data.CurrentPart, data.CurrentContour);
                        }
                    }
                    else
                    {
                        // TYPE=1: Default - 전체 화면 갱신
                        camViewerControl.Invalidate();
                    }

                    // Phase 15.3: Record performance snapshot if recording
                    if (camViewerControl.IsPerformanceRecording())
                    {
                        camViewerControl.RecordPerformanceSnapshot((int)data.ProgressDistance);
                    }

                    // Phase 15.3: Update performance info in ProgramInfoPanel
                    if (programInfoPanel != null)
                    {
                        string perfText = $"[{Rendering.OpenGLSettings.GetModeDescription(Rendering.OpenGLSettings.CurrentMode)}]\n{camViewerControl.GetPerformanceSummary()}";
                        programInfoPanel.UpdatePerformanceInfo(perfText);
                    }
                }
            }
            catch (Exception ex)
            {
                // 예외 로깅만 유지
                LogHelper.Log("RealtimeITagControl", $"UpdateViewer error: {ex.Message}");
            }
        }

        #endregion

        #region Program Info 패널 이벤트 핸들러

        private void ProgramInfoPanel_SimulationClicked(object sender, EventArgs e)
        {

            // 시뮬레이션 상태에 따라 토글 (시작/일시정지/재개)
            if (camViewerControl != null)
            {
                var state = camViewerControl.GetSimulationState();

                switch (state)
                {
                    case Simulation.SimulationState.Idle:
                    case Simulation.SimulationState.Stopped:
                    case Simulation.SimulationState.Completed:
                        // 시뮬레이션 시작
                        camViewerControl.StartSimulation();
                        UpdateSimulationButtonUI();
                        break;

                    case Simulation.SimulationState.Running:
                        // 일시정지
                        camViewerControl.PauseSimulation();
                        UpdateSimulationButtonUI();
                        break;

                    case Simulation.SimulationState.Paused:
                        // 재개
                        camViewerControl.ResumeSimulation();
                        UpdateSimulationButtonUI();
                        break;
                }
            }
        }

        private void ProgramInfoPanel_StopSimulationClicked(object sender, EventArgs e)
        {

            // 시뮬레이션 중단
            if (camViewerControl != null)
            {
                camViewerControl.StopSimulation();
                UpdateSimulationButtonUI();
            }
        }

        /// <summary>
        /// 시뮬레이션 버튼 UI 상태 업데이트
        /// </summary>
        private void UpdateSimulationButtonUI()
        {
            if (camViewerControl == null || programInfoPanel == null)
                return;

            var state = camViewerControl.GetSimulationState();
            bool isRunning = (state == Simulation.SimulationState.Running);
            bool isPaused = (state == Simulation.SimulationState.Paused);

            programInfoPanel.UpdateSimulationButtonState(isRunning, isPaused);
        }

        private void ProgramInfoPanel_ElementSelectClicked(object sender, EventArgs e)
        {
            // Element 선택 팝업 호출
            if (mpfProgram == null)
            {
                MessageBox.Show("MPF 파일을 먼저 로드하세요.", "Element 선택", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Element 선택 팝업 폼 생성
                var elementSelectForm = new System.Windows.Forms.Form
                {
                    Text = "Element 선택",
                    Size = new System.Drawing.Size(400, 250),
                    StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var lblPart = new Label { Text = "Part 번호:", Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(80, 20) };
                var numPart = new NumericUpDown { Location = new System.Drawing.Point(110, 20), Size = new System.Drawing.Size(100, 20), Minimum = 1, Maximum = mpfProgram.Parts.Count, Value = 1 };

                var lblContour = new Label { Text = "Contour 번호:", Location = new System.Drawing.Point(20, 50), Size = new System.Drawing.Size(80, 20) };
                var numContour = new NumericUpDown { Location = new System.Drawing.Point(110, 50), Size = new System.Drawing.Size(100, 20), Minimum = 1, Maximum = 1, Value = 1 };

                var lblElement = new Label { Text = "Element 번호:", Location = new System.Drawing.Point(20, 80), Size = new System.Drawing.Size(80, 20) };
                var numElement = new NumericUpDown { Location = new System.Drawing.Point(110, 80), Size = new System.Drawing.Size(100, 20), Minimum = 0, Maximum = 0, Value = 0 };

                // Part 변경 시 Contour 최댓값 업데이트
                numPart.ValueChanged += (s, args) =>
                {
                    int partIdx = (int)numPart.Value - 1;
                    if (partIdx >= 0 && partIdx < mpfProgram.Parts.Count)
                    {
                        numContour.Maximum = mpfProgram.Parts[partIdx].Contours.Count;
                        numContour.Value = 1;
                    }
                };

                // Contour 변경 시 Element 최댓값 업데이트
                numContour.ValueChanged += (s, args) =>
                {
                    int partIdx = (int)numPart.Value - 1;
                    int contourIdx = (int)numContour.Value - 1;
                    if (partIdx >= 0 && partIdx < mpfProgram.Parts.Count &&
                        contourIdx >= 0 && contourIdx < mpfProgram.Parts[partIdx].Contours.Count)
                    {
                        var contour = mpfProgram.Parts[partIdx].Contours[contourIdx];
                        numElement.Maximum = contour.AllSegments.Count - 1;
                        numElement.Value = 0;
                    }
                };

                // 초기값 설정
                numPart.Value = 1;

                var btnOk = new Button { Text = "선택", Location = new System.Drawing.Point(120, 150), Size = new System.Drawing.Size(80, 30), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "취소", Location = new System.Drawing.Point(210, 150), Size = new System.Drawing.Size(80, 30), DialogResult = DialogResult.Cancel };

                elementSelectForm.Controls.AddRange(new Control[] { lblPart, numPart, lblContour, numContour, lblElement, numElement, btnOk, btnCancel });
                elementSelectForm.AcceptButton = btnOk;
                elementSelectForm.CancelButton = btnCancel;

                if (elementSelectForm.ShowDialog() == DialogResult.OK)
                {
                    int partIdx = (int)numPart.Value - 1;
                    int contourIdx = (int)numContour.Value - 1;
                    int elementIdx = (int)numElement.Value;

                    // Element 선택 호출
                    if (camViewerControl != null)
                    {
                        var selectionManager = camViewerControl.GetSelectionManager();
                        if (selectionManager != null)
                        {
                            selectionManager.SelectElement(partIdx, contourIdx, elementIdx, false);
                            camViewerControl.Invalidate();
                            LogHelper.Log("RealtimeITagControl", $"Element Selected: Part {partIdx + 1}, Contour {contourIdx + 1}, Element {elementIdx}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Element 선택 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 파트 번호 표시 체크박스 변경 이벤트
        /// </summary>
        private void ProgramInfoPanel_ShowPartNumberChanged(object sender, bool isChecked)
        {

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

            if (camViewerControl != null)
            {
                // RenderSettings 업데이트
                Rendering.RenderSettings.Instance.ShowContourNumbers = isChecked;

                // 화면 갱신
                camViewerControl.Invalidate();
            }
        }

        /// <summary>
        /// 컨투어 선택 박스 표시 버튼 클릭 이벤트
        /// </summary>
        private void ProgramInfoPanel_ShowContourSelectionBoxesClicked(object sender, EventArgs e)
        {
            if (camViewerControl != null && camViewerControl.CurrentProgram != null)
            {
                // 이미 열려있으면 닫기
                if (contourLegendForm != null && !contourLegendForm.IsDisposed)
                {
                    contourLegendForm.Close();
                    contourLegendForm = null;
                    camViewerControl.VisibleContours = null;
                    camViewerControl.Invalidate();
                    return;
                }

                int totalContours = 0;
                foreach (var part in camViewerControl.CurrentProgram.Parts)
                {
                    if (part != null && part.Contours != null)
                    {
                        totalContours += part.Contours.Count;
                    }
                }

                if (totalContours > 0)
                {
                    contourLegendForm = new ContourColorLegendForm(totalContours);

                    // CamViewerControl에 VisibleContours 참조 전달
                    camViewerControl.VisibleContours = contourLegendForm.VisibleContours;

                    // 컨투어 표시 변경 이벤트 처리
                    contourLegendForm.ContourVisibilityChanged += (s, args) =>
                    {
                        // 화면 갱신
                        camViewerControl.Invalidate();
                    };

                    // 폼이 닫힐 때 참조 제거
                    contourLegendForm.FormClosed += (s, args) =>
                    {
                        contourLegendForm = null;
                        camViewerControl.VisibleContours = null;
                        camViewerControl.Invalidate();
                    };

                    // 모달리스로 표시
                    contourLegendForm.Show(this);
                }
            }
        }

        /// <summary>
        /// Phase 12: 컨투어 선택 활성화 체크박스 변경 이벤트
        /// </summary>
        private void ProgramInfoPanel_EnableContourSelectionChanged(object sender, bool isChecked)
        {
            if (camViewerControl != null)
            {
                camViewerControl.SetEnableContourSelection(isChecked);
                LogHelper.Log("RealtimeITagControl", $"Contour selection {(isChecked ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// 컨투어 선택 이벤트 핸들러 (ITag Write 수행)
        /// </summary>
        private void CamViewerControl_ContourSelected(object sender, CamViewerControl.ContourSelectedEventArgs e)
        {
            // ITag가 연결되어 있을 때만 Write 수행
            if (!tagManager.IsConnected)
            {
                return;
            }

            try
            {
                // HMI_VIEW_SEARCH_PART에 Part 번호 Write (1-based)
                tagManager.WriteTag(TagDefinitions.SEARCH_PART, e.PartNumber);

                // HMI_VIEW_SEARCH_CONT에 Contour 번호 Write (1-based)
                tagManager.WriteTag(TagDefinitions.SEARCH_CONT, e.ContourNumber);

            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Phase 13: CamViewerControl MouseCoordinatesChanged 이벤트 핸들러
        /// </summary>
        private void CamViewerControl_MouseCoordinatesChanged(object sender, MouseCoordinatesEventArgs e)
        {
            try
            {
                // ProgramInfoPanel에 마우스 좌표 업데이트
                if (programInfoPanel != null && !programInfoPanel.IsDisposed)
                {
                    programInfoPanel.UpdateMouseCoordinates(e.X, e.Y);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log("RealtimeITagControl", $"[Phase13] MouseCoordinatesChanged handler error: {ex.Message}");
            }
        }


        /// <summary>
        /// Phase 13: MPF loaded - Store contour count for ITag cycle calculation
        /// <summary>
        /// Phase 13: MPF loaded - Calculate and set global cycleMs based on contour count
        /// </summary>
        private void CamViewerControl_MPFLoaded(object sender, CamViewerControl.MPFLoadedEventArgs e)
        {
            try
            {
                // 전체 컨투어 수 저장
                this.cycleMs = CalculateITagCycleMs(e.ContourCount);
                StartCyclicRead(this.cycleMs);
                LogHelper.Log("RealtimeITagControl", $"[Phase13] MPF loaded with {e.ContourCount} contours, calculated cycleMs: {cycleMs}ms");
            }
            catch (Exception ex)
            {
                LogHelper.Log("RealtimeITagControl", $"[Phase13] MPFLoaded handler error: {ex.Message}");
            }
        }

        /// <summary>
        /// Phase 13: 전체 컨투어 수에 따라 ITag 주기 계산
        /// </summary>
        private int CalculateITagCycleMs(int contourCount)
        {
            if (contourCount > 800) return 100;  // 10 Hz (안정성 우선)
            if (contourCount > 400) return 50;   // 20 Hz (균형)
            return 25;  // 40 Hz (빠른 응답)
        }
        #endregion
        #region Phase 13: Element-level Realtime Tracing

        /// <summary>
        /// Phase 13: 엘리먼트 단위 진행률 업데이트
        /// ActLineCode와 ProgressDistance를 이용해 현재 엘리먼트를 찾고 진행률 계산
        /// </summary>
        private void UpdateElementProgress(int partIdx, int contIdx, MPF.Contour contour, string actLineCode, double progressDistance)
        {
            try
            {
                if (camViewerControl?.progressManager == null || contour?.AllSegments == null)
                    return;

                // 1. ActLineCode로 현재 엘리먼트 찾기
                int currentElementIndex = FindElementIndexByGCode(contour, actLineCode);

                // 2. 찾지 못한 경우: 전체 진행률만 표시 (기존 방식)
                if (currentElementIndex < 0)
                {
                    UpdateContourTotalProgress(partIdx, contIdx, contour, progressDistance);
                    return;
                }

                // 3. Phase 14.2: ProgressDistance는 현재 엘리먼트 내 진행 거리 (ActLineCode 기반)
                double cumulativeDistance = CalculateCumulativeDistance(contour.AllSegments, currentElementIndex);
                double currentElementLength = contour.AllSegments[currentElementIndex].GetLength();
                
                // ProgressDistance는 현재 엘리먼트의 진행 거리 (절대 거리 아님)
                double elementProgressDistance = progressDistance;
                
                // 4. 현재 엘리먼트의 진행률 계산
                double elementProgress = currentElementLength > 0 ? elementProgressDistance / currentElementLength : 0.0;
                elementProgress = Math.Max(0.0, Math.Min(1.0, elementProgress));  // Clamp [0, 1]

                // 5. ProgressManager에 업데이트
                camViewerControl.progressManager.UpdateProgress(partIdx, contIdx, currentElementIndex, elementProgress);
            }
            catch (Exception ex)
            {
                LogHelper.Log("RealtimeITagControl", $"[Phase13] UpdateElementProgress error: {ex.Message}");
                // 에러 발생 시 전체 진행률로 대체
                UpdateContourTotalProgress(partIdx, contIdx, contour, progressDistance);
            }
        }

        /// <summary>
        /// Phase 13: ActLineCode로 엘리먼트 인덱스 찾기 (OriginalGCode 매칭)
        /// </summary>
        private int FindElementIndexByGCode(MPF.Contour contour, string actLineCode)
        {
            if (contour?.AllSegments == null || string.IsNullOrEmpty(actLineCode))
            {
                return -1;
            }

            // G-code 정규화 (공백 제거, 대소문자 통일)
            string normalizedActCode = NormalizeGCode(actLineCode);

            for (int i = 0; i < contour.AllSegments.Count; i++)
            {
                var segment = contour.AllSegments[i];
                if (segment == null || string.IsNullOrEmpty(segment.OriginalGCode))
                    continue;

                string normalizedSegmentCode = NormalizeGCode(segment.OriginalGCode);

                // 정규화된 G-code 비교
                if (normalizedSegmentCode == normalizedActCode)
                {
                    return i;  // 엘리먼트 인덱스 반환
                }
            }

            // HKSTO 서브루틴 처리 (GC11, GC12, GC13 → HKSTO)
            // "G1 X=68.171 Y=8.25" → "HKSTO(...)" 매칭은 향후 확장
            // 현재는 직접 매칭만 지원

            return -1;  // 찾지 못함
        }

        /// <summary>
        /// Phase 13: G-code 정규화 (공백 제거, 대소문자 통일)
        /// </summary>
        private string NormalizeGCode(string gcode)
        {
            if (string.IsNullOrEmpty(gcode))
                return string.Empty;

            // 공백 제거, 대문자 변환
            return gcode.Replace(" ", "").Replace("\t", "").ToUpperInvariant();
        }

        /// <summary>
        /// Phase 13: 이전 엘리먼트들의 누적 거리 계산
        /// </summary>
        private double CalculateCumulativeDistance(List<MPF.PathSegment> segments, int currentIndex)
        {
            double cumulative = 0.0;

            for (int i = 0; i < currentIndex && i < segments.Count; i++)
            {
                if (segments[i] != null)
                {
                    cumulative += segments[i].GetLength();
                }
            }

            return cumulative;
        }

        /// <summary>
        /// Phase 13: 컨투어 전체 진행률 업데이트 (엘리먼트 구분 없음, 기존 방식)
        /// </summary>
        private void UpdateContourTotalProgress(int partIdx, int contIdx, MPF.Contour contour, double progressDistance)
        {
            try
            {
                if (camViewerControl?.progressManager == null)
                    return;

                // Contour의 총 길이 계산
                double totalDistance = 0.0;
                if (contour.AllSegments != null)
                {
                    foreach (var segment in contour.AllSegments)
                    {
                        if (segment != null)
                        {
                            totalDistance += segment.GetLength();
                        }
                    }
                }

                // 전체 진행률 계산
                double progressRatio = totalDistance > 0 ? progressDistance / totalDistance : 0.0;
                progressRatio = Math.Max(0.0, Math.Min(1.0, progressRatio));

                // Element index = 0 (전체 진행률)
                camViewerControl.progressManager.UpdateProgress(partIdx, contIdx, 0, progressRatio);
            }
            catch (Exception ex)
            {
                LogHelper.Log("RealtimeITagControl", $"[Phase13] UpdateContourTotalProgress error: {ex.Message}");
            }
        }

        #endregion

        #region Phase 15.3: Performance Testing Methods

        /// <summary>
        /// Phase 15.3: Start automated performance comparison test
        /// </summary>
        public void StartPerformanceTest()
        {
            camViewerControl?.StartPerformanceRecording();
            LogHelper.Log("RealtimeITagControl", "📊 Performance test started");
        }

        /// <summary>
        /// Phase 15.3: Stop automated performance comparison test
        /// </summary>
        public void StopPerformanceTest()
        {
            camViewerControl?.StopPerformanceRecording();
            LogHelper.Log("RealtimeITagControl", "📊 Performance test stopped");
        }

        /// <summary>
        /// Phase 15.3: Export performance comparison CSV
        /// </summary>
        public string ExportPerformanceCSV()
        {
            string csvPath = camViewerControl?.ExportPerformanceCSV();
            if (!string.IsNullOrEmpty(csvPath))
            {
                LogHelper.Log("RealtimeITagControl", $"✅ CSV exported: {csvPath}");
            }
            return csvPath;
        }

        /// <summary>
        /// Phase 15.3: Get performance statistics report
        /// </summary>
        public string GetPerformanceStatistics()
        {
            return camViewerControl?.GetPerformanceStatistics() ?? "Not available";
        }

        /// <summary>
        /// Phase 15.3: Get snapshot count
        /// </summary>
        public int GetSnapshotCount()
        {
            return camViewerControl?.GetSnapshotCount() ?? 0;
        }

        /// <summary>
        /// Phase 15.3: Reset performance monitor
        /// </summary>
        public void ResetPerformanceStats()
        {
            camViewerControl?.ResetPerformanceMonitor();
            LogHelper.Log("RealtimeITagControl", "🔄 Performance stats reset");
        }

        #endregion
    }
}