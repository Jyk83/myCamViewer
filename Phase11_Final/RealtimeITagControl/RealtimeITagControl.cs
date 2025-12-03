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
    /// ITagManager 싱글톤 패턴 사용 (중복 인스턴스 방지)
    /// </summary>
    public partial class RealtimeITagControl : UserControl, IITagManager
    {
        #region 멤버 변수

        // ITagManager 싱글톤 사용 (중복 방지)
        private ITagManager tagManager => ITagManager.Instance;

        private ProgramInfoPanel programInfoPanel;
        private CamViewerControl camViewerControl;  // Phase8 OpenGL 렌더러 (핵심!)

        private string currentMpfPath;      // 현재 로드된 MPF 파일 경로
        private MPFProgram mpfProgram;      // MPF 프로그램 데이터 (파싱 결과)
        private TagData? lastTagData = null;  // Nullable struct
        
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
            programInfoPanel.TraceTestClicked += ProgramInfoPanel_TraceTestClicked;
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
            camViewerControl.ContourSelected += CamViewerControl_ContourSelected;
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
            // 중복 호출 방지
            lock (disposeLock)
            {
                if (isDisposed)
                {
                    LogHelper.Log("RealtimeITagControl", "CleanupResources already called - skipping");
                    return;
                }
                isDisposed = true;
            }
            
            try
            {
                LogHelper.Log("RealtimeITagControl", "=== CleanupResources 시작 ===");

                // 0. TraceTestForm 종료 처리
                try
                {
                    if (traceTestForm != null && !traceTestForm.IsDisposed)
                    {
                        traceTestForm.Close();
                        traceTestForm.Dispose();
                        traceTestForm = null;
                        LogHelper.Log("RealtimeITagControl", "TraceTestForm closed and disposed");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log("RealtimeITagControl", $"TraceTestForm cleanup error: {ex.Message}");
                }

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
                        programInfoPanel.TraceTestClicked -= ProgramInfoPanel_TraceTestClicked;
                        programInfoPanel.ShowPartNumberChanged -= ProgramInfoPanel_ShowPartNumberChanged;
                        programInfoPanel.ShowContourNumberChanged -= ProgramInfoPanel_ShowContourNumberChanged;
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
        /// 14개 Tag 주기적 읽기 시작
        /// </summary>
        public bool StartCyclicRead(int cycleMs = 500)
        {
            // ITagManager 싱글톤에 위임
            bool result = tagManager.StartCyclicRead(cycleMs);
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
                            }
                            else
                            {
                                // 중간에 멈춘 경우 현재까지만 업데이트
                                UpdateContourStatus(tagData);
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
                            
                            // Phase 11: progressManager에도 업데이트 전달
                            if (camViewerControl?.progressManager != null)
                            {
                                // Contour의 총 길이 계산 (AllSegments의 Length 합산)
                                double totalDistance = 0.0;
                                if (contour.AllSegments != null)
                                {
                                    foreach (var segment in contour.AllSegments)
                                    {
                                        if (segment != null)
                                        {
                                            totalDistance += segment.Length;
                                        }
                                    }
                                }
                                
                                // progressDistance를 progress 비율로 변환 (0.0~1.0)
                                double progressRatio = totalDistance > 0 ? progressDistance / totalDistance : 0.0;
                                progressRatio = Math.Max(0.0, Math.Min(1.0, progressRatio));  // Clamp to [0, 1]
                                
                                // Element index는 현재로서는 0으로 설정 (향후 확장 가능)
                                camViewerControl.progressManager.UpdateProgress(partIdx, contIdx, 0, progressRatio);
                            }
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

        // TraceTestForm 싱글톤 인스턴스
        private Trace.TraceTestForm traceTestForm = null;

        /// <summary>
        /// TraceTestForm 호출 이벤트
        /// </summary>
        private void ProgramInfoPanel_TraceTestClicked(object sender, EventArgs e)
        {
            if (mpfProgram == null)
            {
                MessageBox.Show("MPF 파일을 먼저 로드하세요.", "Trace Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 이미 열려있는 폼이 있으면 활성화만
                if (traceTestForm != null && !traceTestForm.IsDisposed)
                {
                    traceTestForm.BringToFront();
                    traceTestForm.Focus();
                    LogHelper.Log("RealtimeITagControl", "TraceTestForm already opened - brought to front");
                    return;
                }

                // CuttingProgressManager는 camViewerControl의 것을 사용
                if (camViewerControl.progressManager == null)
                {
                    MessageBox.Show("CuttingProgressManager가 초기화되지 않았습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                traceTestForm = new Trace.TraceTestForm(camViewerControl.progressManager, mpfProgram, camViewerControl);
                traceTestForm.TopMost = true;  // 항상 최상위 표시
                traceTestForm.FormClosed += (s, args) => { traceTestForm = null; };  // 종료 시 참조 해제
                traceTestForm.Show();
                LogHelper.Log("RealtimeITagControl", "TraceTestForm opened");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"TraceTestForm 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogHelper.Log("RealtimeITagControl", $"TraceTestForm error: {ex.Message}");
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

        #endregion
    }
}
