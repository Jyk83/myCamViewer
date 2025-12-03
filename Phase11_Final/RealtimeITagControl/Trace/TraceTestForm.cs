using System;
using System.Drawing;
using System.Windows.Forms;
using RealtimeITagControl.MPF;

namespace RealtimeITagControl.Trace
{
    /// <summary>
    /// Test interface for real-time trace data input
    /// Simulates NC controller data without actual communication
    /// </summary>
    public partial class TraceTestForm : Form
    {
        private CuttingProgressManager progressManager;
        private MPFProgram currentProgram;
        private CamViewerControl viewerControl;

        // UI Controls
        private Label lblTitle;
        private Label lblMPFFile;
        private GroupBox grpPosition;
        private Label lblPart;
        private NumericUpDown numPart;
        private Label lblContour;
        private NumericUpDown numContour;
        private Label lblElement;
        private NumericUpDown numElement;
        private Label lblProgress;
        private TrackBar trackProgress;
        private Label lblProgressValue;
        
        private GroupBox grpCoordinates;
        private Label lblWcsX;
        private NumericUpDown numWcsX;
        private Label lblWcsY;
        private NumericUpDown numWcsY;

        private GroupBox grpGCode;
        private Label lblGCodeBlock;
        private TextBox txtGCodeBlock;
        private Label lblMPFLineNo;
        private NumericUpDown numMPFLineNo;

        private Button btnStart;
        private Button btnUpdate;
        private Button btnStop;
        private Button btnReset;
        private Button btnCompleteElement;
        private Button btnCompleteContour;

        private Label lblStatus;
        private TextBox txtLog;

        public TraceTestForm(CuttingProgressManager manager, MPFProgram program, CamViewerControl viewer)
        {
            progressManager = manager;
            currentProgram = program;
            viewerControl = viewer;
            
            InitializeComponent();
            InitializeUI();
            UpdateMPFInfo();
            
            // Subscribe to progress events
            progressManager.ProgressUpdated += ProgressManager_ProgressUpdated;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "실시간 트레이스 테스트 (Real-time Trace Test)";
            this.Size = new Size(600, 670);  // Reduced height after removing WCS coordinates
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            
            this.ResumeLayout(false);
        }

        private void InitializeUI()
        {
            int yPos = 20;
            int leftMargin = 20;
            int controlWidth = 540;

            // Title
            lblTitle = new Label
            {
                Text = "실시간 트레이스 테스트 (Real-time Trace Test)",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 30),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White
            };
            this.Controls.Add(lblTitle);
            yPos += 40;

            // MPF File Info
            lblMPFFile = new Label
            {
                Text = "MPF 파일: [로드된 파일 없음]",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray
            };
            this.Controls.Add(lblMPFFile);
            yPos += 35;

            // Position Group
            grpPosition = new GroupBox
            {
                Text = "위치 정보 (Position)",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 160),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(grpPosition);

            int grpY = 25;
            
            // Part Number
            lblPart = new Label
            {
                Text = "파트 번호 (Part):",
                Location = new Point(15, grpY),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9)
            };
            grpPosition.Controls.Add(lblPart);

            numPart = new NumericUpDown
            {
                Location = new Point(170, grpY),
                Size = new Size(100, 25),
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            numPart.ValueChanged += NumPart_ValueChanged;  // Phase 7: Update contour/element ranges
            grpPosition.Controls.Add(numPart);
            grpY += 30;

            // Contour Number
            lblContour = new Label
            {
                Text = "컨투어 번호 (Contour):",
                Location = new Point(15, grpY),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9)
            };
            grpPosition.Controls.Add(lblContour);

            numContour = new NumericUpDown
            {
                Location = new Point(170, grpY),
                Size = new Size(100, 25),
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            numContour.ValueChanged += NumContour_ValueChanged;  // Phase 7: Update element range
            grpPosition.Controls.Add(numContour);
            grpY += 30;

            // Element Number
            lblElement = new Label
            {
                Text = "엘리먼트 번호 (Element):",
                Location = new Point(15, grpY),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9)
            };
            grpPosition.Controls.Add(lblElement);

            numElement = new NumericUpDown
            {
                Location = new Point(170, grpY),
                Size = new Size(100, 25),
                Minimum = 0,
                Maximum = 10000,
                Value = 0,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            grpPosition.Controls.Add(numElement);
            grpY += 30;

            // Progress
            lblProgress = new Label
            {
                Text = "진행률 (Progress): 0%",
                Location = new Point(15, grpY),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9)
            };
            grpPosition.Controls.Add(lblProgress);
            grpY += 25;

            trackProgress = new TrackBar
            {
                Location = new Point(15, grpY),
                Size = new Size(400, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                TickFrequency = 10
            };
            trackProgress.ValueChanged += TrackProgress_ValueChanged;
            grpPosition.Controls.Add(trackProgress);

            lblProgressValue = new Label
            {
                Text = "0.00",
                Location = new Point(420, grpY + 5),
                Size = new Size(50, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Cyan
            };
            grpPosition.Controls.Add(lblProgressValue);

            yPos += 170;

            // WCS 좌표 그룹 삭제 (Phase 7 요구사항)

            // G-Code Group
            grpGCode = new GroupBox
            {
                Text = "G코드 정보 (G-Code Info)",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 100),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(grpGCode);

            grpY = 25;
            
            lblGCodeBlock = new Label
            {
                Text = "G코드 블록:",
                Location = new Point(15, grpY),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9)
            };
            grpGCode.Controls.Add(lblGCodeBlock);

            txtGCodeBlock = new TextBox
            {
                Location = new Point(120, grpY),
                Size = new Size(400, 25),
                Text = "G1 X100 Y50 F3000",
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            grpGCode.Controls.Add(txtGCodeBlock);
            grpY += 35;

            lblMPFLineNo = new Label
            {
                Text = "MPF 라인 번호:",
                Location = new Point(15, grpY),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9)
            };
            grpGCode.Controls.Add(lblMPFLineNo);

            numMPFLineNo = new NumericUpDown
            {
                Location = new Point(120, grpY),
                Size = new Size(120, 25),
                Minimum = 1,
                Maximum = 100000,
                Value = 1,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            numMPFLineNo.ValueChanged += NumMPFLineNo_ValueChanged;  // Phase 7: Update G-code block
            grpGCode.Controls.Add(numMPFLineNo);

            yPos += 110;

            // Control Buttons
            int btnY = yPos;
            int btnWidth = 130;
            int btnHeight = 35;
            int btnSpacing = 10;
            int btnX = leftMargin;

            btnStart = CreateButton("시작 (Start)", btnX, btnY, btnWidth, btnHeight, Color.FromArgb(0, 122, 204));
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);
            btnX += btnWidth + btnSpacing;

            btnUpdate = CreateButton("업데이트 (Update)", btnX, btnY, btnWidth, btnHeight, Color.FromArgb(76, 175, 80));
            btnUpdate.Click += BtnUpdate_Click;
            this.Controls.Add(btnUpdate);
            btnX += btnWidth + btnSpacing;

            btnStop = CreateButton("중지 (Stop)", btnX, btnY, btnWidth, btnHeight, Color.FromArgb(244, 67, 54));
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);
            btnX += btnWidth + btnSpacing;

            btnReset = CreateButton("리셋 (Reset)", btnX, btnY, btnWidth, btnHeight, Color.FromArgb(255, 152, 0));
            btnReset.Click += BtnReset_Click;
            this.Controls.Add(btnReset);

            btnY += btnHeight + btnSpacing;
            btnX = leftMargin;

            btnCompleteElement = CreateButton("엘리먼트 완료", btnX, btnY, btnWidth + 70, btnHeight, Color.FromArgb(103, 58, 183));
            btnCompleteElement.Click += BtnCompleteElement_Click;
            this.Controls.Add(btnCompleteElement);
            btnX += btnWidth + 70 + btnSpacing;

            btnCompleteContour = CreateButton("컨투어 완료", btnX, btnY, btnWidth + 70, btnHeight, Color.FromArgb(156, 39, 176));
            btnCompleteContour.Click += BtnCompleteContour_Click;
            this.Controls.Add(btnCompleteContour);

            yPos = btnY + btnHeight + 15;

            // Status Label
            lblStatus = new Label
            {
                Text = "상태: 대기 중 (Idle)",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Yellow
            };
            this.Controls.Add(lblStatus);
            yPos += 30;

            // Log TextBox
            txtLog = new TextBox
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 8)
            };
            this.Controls.Add(txtLog);

            UpdateButtonStates();
        }

        private Button CreateButton(string text, int x, int y, int width, int height, Color backColor)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }

        private void TrackProgress_ValueChanged(object sender, EventArgs e)
        {
            double progress = trackProgress.Value / 100.0;
            lblProgress.Text = $"진행률 (Progress): {trackProgress.Value}%";
            lblProgressValue.Text = progress.ToString("F2");
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            int partNo = (int)numPart.Value;
            int contourNo = (int)numContour.Value;

            bool success = progressManager.StartCuttingProgress(partNo, contourNo, false);
            
            if (success)
            {
                AddLog($"✓ 절단 시작: Part {partNo}, Contour {contourNo}");
                LogHelper.Log("TraceTestForm", $"Cutting Start: Part {partNo}, Contour {contourNo}");
                lblStatus.Text = "상태: 진행 중 (In Progress)";
                lblStatus.ForeColor = Color.LimeGreen;
            }
            else
            {
                AddLog($"✗ 절단 시작 실패: Part {partNo}, Contour {contourNo}");
                lblStatus.Text = "상태: 오류 (Error)";
                lblStatus.ForeColor = Color.Red;
            }

            UpdateButtonStates();
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            int partNo = (int)numPart.Value;
            int contourNo = (int)numContour.Value;
            int elementIdx = (int)numElement.Value;
            double progress = trackProgress.Value / 100.0;

            bool success = progressManager.UpdateProgress(partNo, contourNo, elementIdx, progress);
            
            if (success)
            {
                AddLog($"✓ 업데이트: P{partNo} C{contourNo} E{elementIdx} Prog={progress:F2}");
                LogHelper.Log("TraceTestForm", $"Progress Updated: Part {partNo}, Contour {contourNo}, Element {elementIdx}, Progress {progress:F2}");
                
                // 뷰어 강제 리프레시 (색상 업데이트)
                viewerControl?.Invalidate();
            }
            else
            {
                AddLog($"✗ 업데이트 실패");
                LogHelper.Log("TraceTestForm", $"Update Failed: Part {partNo}, Contour {contourNo}");
            }

            UpdateButtonStates();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            progressManager.StopCuttingProgress();
            AddLog("■ 절단 중지");
            LogHelper.Log("TraceTestForm", "Cutting Stopped");
            lblStatus.Text = "상태: 중지됨 (Stopped)";
            lblStatus.ForeColor = Color.Orange;
            UpdateButtonStates();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            progressManager.ResetCuttingProgress();
            LogHelper.Log("TraceTestForm", "Cutting Progress Reset");
            AddLog("⟲ 진행 상태 리셋");
            lblStatus.Text = "상태: 대기 중 (Idle)";
            lblStatus.ForeColor = Color.Yellow;
            trackProgress.Value = 0;
            UpdateButtonStates();
        }

        private void BtnCompleteElement_Click(object sender, EventArgs e)
        {
            progressManager.CompleteLastElement();
            AddLog($"✓ 엘리먼트 완료");
            trackProgress.Value = 100;
        }

        private void BtnCompleteContour_Click(object sender, EventArgs e)
        {
            progressManager.CompleteLastContour();
            AddLog($"✓ 컨투어 완료");
            trackProgress.Value = 100;
        }

        private void ProgressManager_ProgressUpdated(object sender, CuttingProgressEventArgs e)
        {
            // Update UI on progress change
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ProgressManager_ProgressUpdated(sender, e)));
                return;
            }

            AddLog($"[Event] P{e.PartIndex + 1} C{e.ContourIndex + 1} E{e.ElementIndex} = {e.Progress:F2}");
            
            // Trigger viewer repaint
            viewerControl?.Invalidate();
        }

        /// <summary>
        /// Phase 7: Part number changed - update contour range
        /// </summary>
        private void NumPart_ValueChanged(object sender, EventArgs e)
        {
            UpdateContourRange();
            UpdateElementRange();
        }

        /// <summary>
        /// Phase 7: Contour number changed - update element range
        /// </summary>
        private void NumContour_ValueChanged(object sender, EventArgs e)
        {
            UpdateElementRange();
        }

        /// <summary>
        /// Phase 7: MPF line number changed - update G-code block
        /// </summary>
        private void NumMPFLineNo_ValueChanged(object sender, EventArgs e)
        {
            UpdateGCodeBlock();
        }

        /// <summary>
        /// Phase 7: Update contour range based on selected part
        /// </summary>
        private void UpdateContourRange()
        {
            if (currentProgram == null || currentProgram.Parts == null)
                return;

            int partIndex = (int)numPart.Value - 1;
            if (partIndex < 0 || partIndex >= currentProgram.Parts.Count)
                return;

            Part part = currentProgram.Parts[partIndex];
            if (part == null || part.Contours == null)
                return;

            numContour.Maximum = part.Contours.Count;
            if (numContour.Value > numContour.Maximum)
                numContour.Value = numContour.Maximum;
        }

        /// <summary>
        /// Phase 7: Update element range based on selected part/contour
        /// </summary>
        private void UpdateElementRange()
        {
            if (currentProgram == null || currentProgram.Parts == null)
            {
                numElement.Maximum = 0;
                return;
            }

            int partIndex = (int)numPart.Value - 1;
            int contourIndex = (int)numContour.Value - 1;

            if (partIndex < 0 || partIndex >= currentProgram.Parts.Count)
            {
                numElement.Maximum = 0;
                return;
            }

            Part part = currentProgram.Parts[partIndex];
            if (part == null || part.Contours == null || contourIndex < 0 || contourIndex >= part.Contours.Count)
            {
                numElement.Maximum = 0;
                return;
            }

            Contour contour = part.Contours[contourIndex];
            if (contour == null || contour.AllSegments == null)
            {
                numElement.Maximum = 0;
                return;
            }

            // Element index is 0-based, so maximum is count - 1
            int maxElement = Math.Max(0, contour.AllSegments.Count - 1);
            numElement.Maximum = maxElement;
            
            if (numElement.Value > numElement.Maximum)
                numElement.Value = 0;

            AddLog($"📊 Element 범위 업데이트: 0 ~ {maxElement} (총 {contour.AllSegments.Count}개)");
        }

        /// <summary>
        /// Phase 7: Update G-code block based on MPF line number
        /// </summary>
        private void UpdateGCodeBlock()
        {
            if (currentProgram == null || string.IsNullOrEmpty(currentProgram.FilePath))
            {
                txtGCodeBlock.Text = "G1 X100 Y50 F3000";
                return;
            }

            try
            {
                int lineNo = (int)numMPFLineNo.Value;
                string[] lines = System.IO.File.ReadAllLines(currentProgram.FilePath);

                if (lineNo > 0 && lineNo <= lines.Length)
                {
                    string gcodeLine = lines[lineNo - 1].Trim();
                    
                    // Remove comments
                    int commentIndex = gcodeLine.IndexOf(';');
                    if (commentIndex >= 0)
                        gcodeLine = gcodeLine.Substring(0, commentIndex).Trim();

                    if (!string.IsNullOrEmpty(gcodeLine))
                    {
                        txtGCodeBlock.Text = gcodeLine;
                        AddLog($"📝 G코드 블록 업데이트: 라인 {lineNo} → {gcodeLine}");
                    }
                    else
                    {
                        txtGCodeBlock.Text = "(빈 라인 또는 주석)";
                    }
                }
                else
                {
                    txtGCodeBlock.Text = "(라인 번호 범위 초과)";
                }
            }
            catch (Exception ex)
            {
                txtGCodeBlock.Text = $"(오류: {ex.Message})";
                AddLog($"✗ G코드 읽기 오류: {ex.Message}");
            }
        }

        private void UpdateMPFInfo()
        {
            if (currentProgram != null && !string.IsNullOrEmpty(currentProgram.FilePath))
            {
                string fileName = System.IO.Path.GetFileName(currentProgram.FilePath);
                lblMPFFile.Text = $"MPF 파일: {fileName} (Parts: {currentProgram.Parts?.Count ?? 0})";
                
                // Phase 7: Initialize ranges based on current program
                UpdateContourRange();
                UpdateElementRange();
            }
            else
            {
                lblMPFFile.Text = "MPF 파일: [로드된 파일 없음]";
            }
        }

        private void UpdateButtonStates()
        {
            bool hasProgress = progressManager.HasCuttingProgress;
            bool inProgress = progressManager.IsUnderCuttingProgress;

            btnStart.Enabled = !inProgress;
            btnUpdate.Enabled = inProgress;
            btnStop.Enabled = inProgress;
            btnReset.Enabled = hasProgress || inProgress;
            btnCompleteElement.Enabled = hasProgress;
            btnCompleteContour.Enabled = hasProgress;
        }

        private void AddLog(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AddLog(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            txtLog.AppendText($"[{timestamp}] {message}\r\n");
            
            // Auto-scroll
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();

            // Limit log size
            if (txtLog.Lines.Length > 500)
            {
                string[] lines = txtLog.Lines;
                txtLog.Lines = new string[lines.Length - 100].CopyFrom(lines, 100);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Unsubscribe from events
            if (progressManager != null)
            {
                progressManager.ProgressUpdated -= ProgressManager_ProgressUpdated;
            }
            base.OnFormClosing(e);
        }
    }

    // Extension method for array copy
    internal static class ArrayExtensions
    {
        public static T[] CopyFrom<T>(this T[] destination, T[] source, int startIndex)
        {
            Array.Copy(source, startIndex, destination, 0, destination.Length);
            return destination;
        }
    }
}
