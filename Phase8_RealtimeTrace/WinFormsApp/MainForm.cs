using System;
using System.Drawing;
using System.Windows.Forms;
using CamViewerPOC.FileExplorer;
using CamViewerPOC.Rendering;
using CamViewerPOC.Selection;
using CamViewerPOC.Trace;

namespace CamViewerPOC
{
    /// <summary>
    /// Main demonstration form
    /// </summary>
    public partial class MainForm : Form
    {
        private CamViewerControl viewerControl;
        private Panel simulationPanel;
        private FileExplorerControl fileExplorerControl;
        private Button btnTraceTest;  // Phase 7: Real-time trace test
        
        // Phase 5: Selection controls
        private CheckBox chkMultiSelect;
        
        // MPF info panel
        private Panel mpfInfoPanel;
        private Label lblMPFInfo;
        
        // Simulation controls
        private Button btnSimStart;
        private Button btnSimPause;
        private Button btnSimStop;
        private TrackBar trackSimSpeed;
        private ProgressBar progressBarSim;
        private ListBox listBoxLog;
        private Label lblSimStatus;
        private Label lblProgress;
        private Label lblSpeedLabel;

        public MainForm()
        {
            InitializeComponent();
            InitializeConfiguration();
            SetupUI();
            SetupSimulationPanel();
            SetupEventHandlers();
        }
        
        /// <summary>
        /// Initialize application configuration
        /// </summary>
        private void InitializeConfiguration()
        {
            // AppConfig.Instance automatically loads from file (or creates default)
            // Load render settings from file (or use defaults)
            try
            {
                System.Diagnostics.Debug.WriteLine("[Config] Initializing AppConfig...");
                var config = AppConfig.Instance;
                System.Diagnostics.Debug.WriteLine("[Config] LastFolderPath: " + config.LastFolderPath);
                System.Diagnostics.Debug.WriteLine("[Config] RenderSettingsPath: " + config.RenderSettingsPath);
                
                string renderSettingsPath = config.RenderSettingsPath;
                if (System.IO.File.Exists(renderSettingsPath))
                {
                    System.Diagnostics.Debug.WriteLine("[Config] Loading render settings from: " + renderSettingsPath);
                    RenderSettings.Instance.LoadFromFile(renderSettingsPath);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Config] Render settings file not found, using defaults");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Config] Failed to load render settings: " + ex.Message);
                MessageBox.Show("설정 초기화 오류:\n" + ex.Message, "경고", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // MainForm
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.Name = "MainForm";
            this.Text = "CAM Viewer POC - Phase 4: Graphics (Enhanced Rendering + Custom Colors/Sizes)";
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Add form closing event to save configuration
            this.FormClosing += MainForm_FormClosing;
            
            this.ResumeLayout(false);
        }
        
        /// <summary>
        /// Save configuration before closing
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[Config] Saving configuration on exit...");
                
                // Save application config (last folder path)
                AppConfig.Instance.Save();
                System.Diagnostics.Debug.WriteLine("[Config] AppConfig saved");
                
                // Save render settings
                string renderSettingsPath = AppConfig.Instance.RenderSettingsPath;
                RenderSettings.Instance.SaveToFile(renderSettingsPath);
                System.Diagnostics.Debug.WriteLine("[Config] RenderSettings saved to: " + renderSettingsPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Config] Failed to save configuration: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("[Config] Stack trace: " + ex.StackTrace);
            }
        }

        private void SetupUI()
        {
            // MPF Info Panel (top)
            mpfInfoPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(45, 45, 48),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            lblMPFInfo = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.LightGray,
                Text = "MPF 파일 없음",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 0, 0)
            };
            
            mpfInfoPanel.Controls.Add(lblMPFInfo);

            // File Explorer control
            fileExplorerControl = new FileExplorerControl
            {
                Dock = DockStyle.Left,
                Width = 400
            };
            fileExplorerControl.FileDoubleClicked += FileExplorerControl_FileDoubleClicked;
            fileExplorerControl.FileSelected += FileExplorerControl_FileSelected;
            fileExplorerControl.FolderChanged += FileExplorerControl_FolderChanged;
            
            // Initialize TreeView with last folder path (or default if not available)
            string lastPath = AppConfig.Instance.LastFolderPath;
            if (!string.IsNullOrEmpty(lastPath) && System.IO.Directory.Exists(lastPath))
            {
                System.Diagnostics.Debug.WriteLine("[MainForm] Initializing FileExplorer with last path: " + lastPath);
                fileExplorerControl.InitializeTreeView(lastPath);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MainForm] Initializing FileExplorer with default path");
                fileExplorerControl.InitializeTreeView();
            }

            // Viewer control
            viewerControl = new CamViewerControl
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(viewerControl);
            this.Controls.Add(fileExplorerControl);
            this.Controls.Add(mpfInfoPanel);
        }

        // Diagnostics button removed as per Phase 5 requirements
        // Render settings button removed - use JSON file for configuration

        /// <summary>
        /// Phase 8.2: Open Real-time Trace Test dialog (Updated to use TraceManager)
        /// </summary>
        private void BtnTraceTest_Click(object sender, EventArgs e)
        {
            var program = viewerControl.GetCurrentProgram();
            if (program == null)
            {
                MessageBox.Show("먼저 MPF 파일을 로드하세요.", "트레이스 테스트", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Phase 8.2: TraceTestForm is not yet updated for Phase 8.2
            // TODO: Update TraceTestForm to use TraceManager
            // For now, this button does nothing (silently ignored)
            
            /* Phase 7 code - to be updated for Phase 8.2
            var traceManager = viewerControl.GetTraceManager();
            if (traceManager == null)
            {
                MessageBox.Show("TraceManager가 초기화되지 않았습니다.", "트레이스 테스트",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            using (TraceTestForm traceForm = new TraceTestForm(traceManager, program, viewerControl))
            {
                traceForm.ShowDialog();
            }
            */
        }

        // ========================================
        // Simulation Panel Setup
        // ========================================

        private void SetupSimulationPanel()
        {
            simulationPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 350,
                BackColor = Color.FromArgb(37, 37, 38),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Simulation controls group
            Label lblSimTitle = new Label
            {
                Text = "시뮬레이션 제어",
                Location = new Point(10, 10),
                Size = new Size(330, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };

            btnSimStart = CreateSimButton("시작 (Start)", 10, 45, Color.FromArgb(0, 204, 102));
            btnSimStart.Click += BtnSimStart_Click;

            btnSimPause = CreateSimButton("일시정지 (Pause)", 120, 45, Color.FromArgb(255, 193, 7));
            btnSimPause.Click += BtnSimPause_Click;
            btnSimPause.Enabled = false;

            btnSimStop = CreateSimButton("중지 (Stop)", 230, 45, Color.FromArgb(244, 67, 54));
            btnSimStop.Click += BtnSimStop_Click;
            btnSimStop.Enabled = false;

            // Speed control
            lblSpeedLabel = new Label
            {
                Text = "속도: 50ms/element",
                Location = new Point(10, 90),
                Size = new Size(330, 20),
                ForeColor = Color.White
            };

            trackSimSpeed = new TrackBar
            {
                Location = new Point(10, 115),
                Size = new Size(330, 45),
                Minimum = 10,
                Maximum = 500,
                Value = 50,
                TickFrequency = 50
            };
            trackSimSpeed.ValueChanged += TrackSimSpeed_ValueChanged;

            // Progress bar
            lblProgress = new Label
            {
                Text = "진행률: 0%",
                Location = new Point(10, 170),
                Size = new Size(330, 20),
                ForeColor = Color.White
            };

            progressBarSim = new ProgressBar
            {
                Location = new Point(10, 195),
                Size = new Size(330, 23),
                Style = ProgressBarStyle.Continuous
            };

            // Status label
            lblSimStatus = new Label
            {
                Text = "상태: 대기 중",
                Location = new Point(10, 228),
                Size = new Size(330, 20),
                ForeColor = Color.Yellow
            };

            // Log listbox
            Label lblLog = new Label
            {
                Text = "시뮬레이션 로그:",
                Location = new Point(10, 258),
                Size = new Size(330, 20),
                ForeColor = Color.White
            };

            listBoxLog = new ListBox
            {
                Location = new Point(10, 283),
                Size = new Size(330, 390),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 8),
                HorizontalScrollbar = true
            };

            // Display toggle buttons
            Button btnTogglePartNumbers = new Button
            {
                Text = "파트 번호 (Part #)",
                Location = new Point(10, 683),
                Size = new Size(160, 35),
                BackColor = Color.FromArgb(63, 81, 181), // Indigo
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnTogglePartNumbers.Click += (s, e) => {
                RenderSettings.Instance.ShowPartNumbers = !RenderSettings.Instance.ShowPartNumbers;
                btnTogglePartNumbers.BackColor = RenderSettings.Instance.ShowPartNumbers ? 
                    Color.FromArgb(255, 64, 129) : Color.FromArgb(63, 81, 181);
                viewerControl.Invalidate();
            };

            Button btnToggleContourNumbers = new Button
            {
                Text = "컨투어 번호 (Contour #)",
                Location = new Point(180, 683),
                Size = new Size(160, 35),
                BackColor = Color.FromArgb(0, 150, 136), // Teal
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnToggleContourNumbers.Click += (s, e) => {
                RenderSettings.Instance.ShowContourNumbers = !RenderSettings.Instance.ShowContourNumbers;
                btnToggleContourNumbers.BackColor = RenderSettings.Instance.ShowContourNumbers ? 
                    Color.FromArgb(255, 64, 129) : Color.FromArgb(0, 150, 136);
                viewerControl.Invalidate();
            };

            // Phase 5: Show Selection Areas debug checkbox
            CheckBox chkShowSelectionAreas = new CheckBox
            {
                Text = "컨투어 선택 영역 표시 (Show Selection Areas)",
                Location = new Point(10, 723),
                Size = new Size(330, 25),
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Checked = false
            };
            chkShowSelectionAreas.CheckedChanged += (s, e) =>
            {
                viewerControl.SetShowSelectionAreas(chkShowSelectionAreas.Checked);
                viewerControl.Invalidate();
            };

            // Phase 5: Selection mode controls
            Label lblSelectionTitle = new Label
            {
                Text = "선택 모드 (Selection Mode)",
                Location = new Point(10, 753),
                Size = new Size(330, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            RadioButton rbSelectionNone = new RadioButton
            {
                Text = "없음 (None)",
                Location = new Point(10, 783),
                Size = new Size(100, 25),
                ForeColor = Color.White,
                Checked = true,
                Tag = "None"
            };
            rbSelectionNone.CheckedChanged += RbSelection_CheckedChanged;

            RadioButton rbSelectionContour = new RadioButton
            {
                Text = "컨투어 (Contour)",
                Location = new Point(120, 783),
                Size = new Size(110, 25),
                ForeColor = Color.White,
                Tag = "Contour"
            };
            rbSelectionContour.CheckedChanged += RbSelection_CheckedChanged;

            RadioButton rbSelectionElement = new RadioButton
            {
                Text = "엘리먼트 (Element)",
                Location = new Point(240, 783),
                Size = new Size(100, 25),
                ForeColor = Color.White,
                Tag = "Element"
            };
            rbSelectionElement.CheckedChanged += RbSelection_CheckedChanged;

            // Phase 5 Fix: Element detail selection button
            Button btnElementDetail = new Button
            {
                Text = "상세 선택...",
                Location = new Point(240, 813),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("맑은 고딕", 8, FontStyle.Bold)
            };
            btnElementDetail.Click += BtnElementDetail_Click;

            chkMultiSelect = new CheckBox
            {
                Text = "다중 선택 (Multi-select)",
                Location = new Point(10, 813),
                Size = new Size(200, 25),
                ForeColor = Color.White,
                Checked = false,
                Enabled = false  // Phase 5: 초기에는 비활성화 (엘리먼트 선택 시에만 활성화)
            };
            chkMultiSelect.CheckedChanged += (s, e) =>
            {
                viewerControl.SetMultiSelectEnabled(chkMultiSelect.Checked);
            };

            // Phase 6: Contour selection method controls
            Label lblSelectionMethod = new Label
            {
                Text = "컨투어 선택 방식 (Selection Method)",
                Location = new Point(360, 700),
                Size = new Size(280, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            RadioButton rbBoundingBox = new RadioButton
            {
                Text = "바운딩 박스 (Bounding Box)",
                Location = new Point(360, 730),
                Size = new Size(250, 25),
                ForeColor = Color.White,
                Checked = true
            };
            rbBoundingBox.CheckedChanged += (s, e) =>
            {
                if (rbBoundingBox.Checked)
                {
                    viewerControl.SetContourSelectionMethod(Selection.SelectionManager.ContourSelectionMethod.BoundingBox);
                    AddLog("[Selection Method] Changed to: Bounding Box");
                }
            };

            RadioButton rbConvexHull = new RadioButton
            {
                Text = "볼록 껍질 (Convex Hull)",
                Location = new Point(360, 758),
                Size = new Size(250, 25),
                ForeColor = Color.White
            };
            rbConvexHull.CheckedChanged += (s, e) =>
            {
                if (rbConvexHull.Checked)
                {
                    viewerControl.SetContourSelectionMethod(Selection.SelectionManager.ContourSelectionMethod.ConvexHull);
                    AddLog("[Selection Method] Changed to: Convex Hull");
                }
            };

            RadioButton rbPolygon = new RadioButton
            {
                Text = "정밀 폴리곤 (Precise Polygon)",
                Location = new Point(360, 786),
                Size = new Size(250, 25),
                ForeColor = Color.White
            };
            rbPolygon.CheckedChanged += (s, e) =>
            {
                if (rbPolygon.Checked)
                {
                    viewerControl.SetContourSelectionMethod(Selection.SelectionManager.ContourSelectionMethod.Polygon);
                    AddLog("[Selection Method] Changed to: Precise Polygon (Ray-casting)");
                }
            };


            // Phase 7: Real-time Trace Test button
            btnTraceTest = new Button
            {
                Text = "실시간 트레이스 테스트 (Trace Test)",
                Location = new Point(10, 850),
                Size = new Size(330, 40),
                BackColor = Color.FromArgb(255, 69, 0), // Orange-Red for emphasis
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnTraceTest.Click += BtnTraceTest_Click;

            simulationPanel.Controls.AddRange(new Control[]
            {
                lblSimTitle, btnSimStart, btnSimPause, btnSimStop,
                lblSpeedLabel, trackSimSpeed,
                lblProgress, progressBarSim,
                lblSimStatus, lblLog, listBoxLog,
                btnTogglePartNumbers, btnToggleContourNumbers,
                chkShowSelectionAreas,
                lblSelectionTitle, rbSelectionNone, rbSelectionContour, rbSelectionElement, btnElementDetail, chkMultiSelect,
                lblSelectionMethod, rbBoundingBox, rbConvexHull, rbPolygon,
                btnTraceTest  // Phase 7
            });

            this.Controls.Add(simulationPanel);
        }

        private Button CreateSimButton(string text, int x, int y, Color backColor)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(100, 35),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
        }

        // ========================================
        // Event Handlers Setup
        // ========================================

        private void SetupEventHandlers()
        {
            viewerControl.SimulationProgress += ViewerControl_SimulationProgress;
            viewerControl.SimulationLog += ViewerControl_SimulationLog;
            viewerControl.MPFLoaded += ViewerControl_MPFLoaded;
            viewerControl.LogMessage += ViewerControl_LogMessage;
        }
        
        private void ViewerControl_MPFLoaded(object sender, CamViewerControl.MPFLoadedEventArgs e)
        {
            lblMPFInfo.Text = string.Format("버전: {0} | 워크피스: {1}mm x {2}mm | 파트: {3}개 | 컨투어: {4}개",
                e.Version, e.WorkpieceWidth, e.WorkpieceHeight, e.PartCount, e.ContourCount);
            UpdateSimulationControls();
        }

        private void ViewerControl_SimulationProgress(object sender, CamViewerPOC.Simulation.SimulationProgressEventArgs e)
        {
            lblProgress.Text = string.Format("진행률: {0:F1}%", e.ProgressPercentage);
            progressBarSim.Value = Math.Min(100, (int)e.ProgressPercentage);

            string logEntry = string.Format("[{0:HH:mm:ss}] Part:{1}/{2} Contour:{3}/{4} Element:{5}/{6} - {7}",
                DateTime.Now,
                e.PartIndex + 1, e.TotalParts,
                e.ContourIndex + 1, e.TotalContours,
                e.ElementIndex + 1, e.TotalElements,
                e.GCodeBlock);

            AddLog(logEntry);
        }

        private void ViewerControl_SimulationLog(object sender, string message)
        {
            AddLog("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message);
        }
        
        private void ViewerControl_LogMessage(object sender, string message)
        {
            // Add timestamp and display in log window
            AddLog("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message);
        }

        private void AddLog(string message)
        {
            if (listBoxLog.InvokeRequired)
            {
                listBoxLog.Invoke(new Action(() => AddLog(message)));
                return;
            }

            listBoxLog.Items.Add(message);
            listBoxLog.TopIndex = listBoxLog.Items.Count - 1;

            // Limit log size
            while (listBoxLog.Items.Count > 1000)
            {
                listBoxLog.Items.RemoveAt(0);
            }
        }

        // ========================================
        // Simulation Button Handlers
        // ========================================

        private void BtnSimStart_Click(object sender, EventArgs e)
        {
            viewerControl.StartSimulation();
            UpdateSimulationControls();
            lblSimStatus.Text = "상태: 실행 중";
            lblSimStatus.ForeColor = Color.LimeGreen;
        }

        private void BtnSimPause_Click(object sender, EventArgs e)
        {
            if (viewerControl.GetSimulationState() == CamViewerPOC.Simulation.SimulationState.Running)
            {
                viewerControl.PauseSimulation();
                btnSimPause.Text = "재개 (Resume)";
                lblSimStatus.Text = "상태: 일시정지";
                lblSimStatus.ForeColor = Color.Yellow;
            }
            else if (viewerControl.GetSimulationState() == CamViewerPOC.Simulation.SimulationState.Paused)
            {
                viewerControl.ResumeSimulation();
                btnSimPause.Text = "일시정지 (Pause)";
                lblSimStatus.Text = "상태: 실행 중";
                lblSimStatus.ForeColor = Color.LimeGreen;
            }
        }

        private void BtnSimStop_Click(object sender, EventArgs e)
        {
            viewerControl.StopSimulation();
            UpdateSimulationControls();
            lblSimStatus.Text = "상태: 중지됨";
            lblSimStatus.ForeColor = Color.Red;
            progressBarSim.Value = 0;
            lblProgress.Text = "진행률: 0%";
        }

        private void TrackSimSpeed_ValueChanged(object sender, EventArgs e)
        {
            int speed = trackSimSpeed.Value;
            viewerControl.SetSimulationSpeed(speed);
            lblSpeedLabel.Text = string.Format("속도: {0}ms/element", speed);
        }

        // Phase 5: Selection mode change handler
        private void RbSelection_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb != null && rb.Checked)
            {
                string mode = rb.Tag as string;
                switch (mode)
                {
                    case "None":
                        viewerControl.SetSelectionMode(Selection.SelectionManager.SelectionMode.None);
                        // Phase 5: 다중 선택 비활성화
                        if (chkMultiSelect != null)
                        {
                            chkMultiSelect.Enabled = false;
                            chkMultiSelect.Checked = false;
                        }
                        AddLog("[Selection] Mode: None");
                        break;
                    case "Contour":
                        viewerControl.SetSelectionMode(Selection.SelectionManager.SelectionMode.Contour);
                        // Phase 5: 다중 선택 비활성화 (컨투어는 단일 선택만)
                        if (chkMultiSelect != null)
                        {
                            chkMultiSelect.Enabled = false;
                            chkMultiSelect.Checked = false;
                        }
                        AddLog("[Selection] Mode: Contour");
                        break;
                    case "Element":
                        viewerControl.SetSelectionMode(Selection.SelectionManager.SelectionMode.Element);
                        // Phase 5: 다중 선택 활성화 (엘리먼트만 다중 선택 가능)
                        if (chkMultiSelect != null)
                        {
                            chkMultiSelect.Enabled = true;
                        }
                        AddLog("[Selection] Mode: Element");
                        break;
                }
            }
        }

        // Phase 5.4: Number positioning handlers

        private void UpdateSimulationControls()
        {
            CamViewerPOC.Simulation.SimulationState state = viewerControl.GetSimulationState();

            btnSimStart.Enabled = (state == CamViewerPOC.Simulation.SimulationState.Idle ||
                                   state == CamViewerPOC.Simulation.SimulationState.Stopped || 
                                   state == CamViewerPOC.Simulation.SimulationState.Completed);
            btnSimPause.Enabled = (state == CamViewerPOC.Simulation.SimulationState.Running || 
                                   state == CamViewerPOC.Simulation.SimulationState.Paused);
            btnSimStop.Enabled = (state == CamViewerPOC.Simulation.SimulationState.Running || 
                                  state == CamViewerPOC.Simulation.SimulationState.Paused);

            if (state == CamViewerPOC.Simulation.SimulationState.Stopped || 
                state == CamViewerPOC.Simulation.SimulationState.Completed)
            {
                lblSimStatus.Text = "상태: 대기 중";
                lblSimStatus.ForeColor = Color.Yellow;
            }
        }

        // ========================================
        // File Explorer Event Handlers
        // ========================================

        private void FileExplorerControl_FileSelected(object sender, string filePath)
        {
            // 파일 선택 시 로그에 표시
            AddLog("[File Selected] " + System.IO.Path.GetFileName(filePath));
        }

        private void FileExplorerControl_FileDoubleClicked(object sender, string filePath)
        {
            // 파일 더블클릭 시 자동 로드
            AddLog("[Loading] " + filePath);
            viewerControl.LoadMPFFile(filePath);
            UpdateSimulationControls();
        }
        
        private void FileExplorerControl_FolderChanged(object sender, string folderPath)
        {
            // 폴더 변경 시 AppConfig에 저장
            AppConfig.Instance.LastFolderPath = folderPath;
        }

        // ========================================
        // Element Detail Selection
        // ========================================

        private void BtnElementDetail_Click(object sender, EventArgs e)
        {
            // Open element selection dialog
            var program = viewerControl.GetCurrentProgram();
            if (program == null)
            {
                MessageBox.Show("MPF 파일을 먼저 로드해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectionManager = viewerControl.GetSelectionManager();
            using (ElementSelectionForm form = new ElementSelectionForm(program, selectionManager))
            {
                // Wire up preview event
                form.ElementPreviewChanged += (s, args) => viewerControl.Invalidate();

                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Log selection result (SelectedXxx are 0-based, display as 1-based)
                    AddLog($"[Element Selected] Part {form.SelectedPartIndex + 1}, " +
                           $"Contour {form.SelectedContourIndex + 1}, " +
                           $"Element {form.SelectedElementIndex + 1}");
                    AddLog($"  G-Code: {form.SelectedGCode}");

                    // Force redraw
                    viewerControl.Invalidate();
                }
            }
        }
    }
}
