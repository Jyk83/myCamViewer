using System;
using System.Drawing;
using System.Windows.Forms;

namespace CamViewerPOC.Rendering
{
    /// <summary>
    /// Form for editing render settings
    /// </summary>
    public partial class RenderSettingsForm : Form
    {
        private RenderSettings settings;
        private Button btnPiercing, btnLeadIn, btnCuttingCompleted;
        private Button btnCuttingInProgress, btnCuttingPending;
        private Button btnPartNumber, btnContourNumber, btnMarking;
        private NumericUpDown numPiercingSize, numLeadInWidth;
        private NumericUpDown numCuttingCompletedWidth, numCuttingInProgressWidth, numCuttingPendingWidth;
        private NumericUpDown numPartNumberSize, numContourNumberSize;
        private CheckBox chkShowPartNumbers;
        private CheckBox chkShowContourNumbers;
        private NumericUpDown numInitialZoom;
        private Button btnOK, btnCancel, btnReset;

        // Phase 5.3: Part boundary controls
        private CheckBox chkShowPartBoundaries;
        private Button btnPartBoundary;
        private NumericUpDown numPartBoundaryWidth;

        // Phase 5.5: Canvas orientation control
        private ComboBox cmbOrientation;

        public RenderSettingsForm()
        {
            settings = RenderSettings.Instance;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Render Settings (렌더링 설정)";
            this.Size = new Size(500, 800); // Taller to fit all controls
            this.FormBorderStyle = FormBorderStyle.Sizable; // Allow resizing
            this.MaximizeBox = true;  // Allow maximize
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AutoScroll = true; // Enable scrolling if needed
            this.MinimumSize = new Size(500, 600); // Minimum size

            int yPos = 20; // Current Y position for controls

            // Color settings section
            Label lblColorSection = CreateLabel("색상 설정 (Colors)", 10, yPos, true);
            this.Controls.Add(lblColorSection);
            yPos += 30;


            // Piercing point
            this.Controls.Add(CreateLabel("피어싱 포인트:", 20, yPos));
            btnPiercing = CreateColorButton(200, yPos);
            this.Controls.Add(btnPiercing);
            yPos += 35;

            // Lead-in (now unused, but keep for compatibility)
            this.Controls.Add(CreateLabel("리드인 경로:", 20, yPos));
            btnLeadIn = CreateColorButton(200, yPos);
            this.Controls.Add(btnLeadIn);
            yPos += 35;

            // Cutting completed
            this.Controls.Add(CreateLabel("절단 완료:", 20, yPos));
            btnCuttingCompleted = CreateColorButton(200, yPos);
            this.Controls.Add(btnCuttingCompleted);
            yPos += 35;

            // Cutting in progress
            this.Controls.Add(CreateLabel("절단 진행 중:", 20, yPos));
            btnCuttingInProgress = CreateColorButton(200, yPos);
            this.Controls.Add(btnCuttingInProgress);
            yPos += 35;

            // Cutting pending (uncut)
            this.Controls.Add(CreateLabel("절단 대기(미절단):", 20, yPos));
            btnCuttingPending = CreateColorButton(200, yPos);
            this.Controls.Add(btnCuttingPending);
            yPos += 35;

            // Part number
            this.Controls.Add(CreateLabel("파트 번호:", 20, yPos));
            btnPartNumber = CreateColorButton(200, yPos);
            this.Controls.Add(btnPartNumber);
            yPos += 35;

            // Contour number
            this.Controls.Add(CreateLabel("컨투어 번호:", 20, yPos));
            btnContourNumber = CreateColorButton(200, yPos);
            this.Controls.Add(btnContourNumber);
            yPos += 35;

            // Marking path
            this.Controls.Add(CreateLabel("마킹 경로:", 20, yPos));
            btnMarking = CreateColorButton(200, yPos);
            this.Controls.Add(btnMarking);
            yPos += 45;

            // Size settings section
            Label lblSizeSection = CreateLabel("크기 설정 (Sizes in pixels)", 10, yPos, true);
            this.Controls.Add(lblSizeSection);
            yPos += 30;


            // Piercing size
            this.Controls.Add(CreateLabel("피어싱 포인트 크기:", 20, yPos));
            numPiercingSize = CreateNumericUpDown(200, yPos, 1m, 20m, 1m);
            this.Controls.Add(numPiercingSize);
            yPos += 35;

            // Lead-in width
            this.Controls.Add(CreateLabel("리드인 두께:", 20, yPos));
            numLeadInWidth = CreateNumericUpDown(200, yPos, 0.5m, 10m, 0.5m);
            this.Controls.Add(numLeadInWidth);
            yPos += 35;

            // Cutting completed width
            this.Controls.Add(CreateLabel("절단 완료 두께:", 20, yPos));
            numCuttingCompletedWidth = CreateNumericUpDown(200, yPos, 0.5m, 10m, 0.5m);
            this.Controls.Add(numCuttingCompletedWidth);
            yPos += 35;

            // Cutting in progress width
            this.Controls.Add(CreateLabel("절단 진행 중 두께:", 20, yPos));
            numCuttingInProgressWidth = CreateNumericUpDown(200, yPos, 0.5m, 10m, 0.5m);
            this.Controls.Add(numCuttingInProgressWidth);
            yPos += 35;

            // Cutting pending width
            this.Controls.Add(CreateLabel("절단 대기 두께:", 20, yPos));
            numCuttingPendingWidth = CreateNumericUpDown(200, yPos, 0.5m, 10m, 0.5m);
            this.Controls.Add(numCuttingPendingWidth);
            yPos += 35;

            // Part number size
            this.Controls.Add(CreateLabel("파트 번호 크기:", 20, yPos));
            numPartNumberSize = CreateNumericUpDown(200, yPos, 6m, 48m, 1m);
            this.Controls.Add(numPartNumberSize);
            yPos += 35;

            // Contour number size
            this.Controls.Add(CreateLabel("컨투어 번호 크기:", 20, yPos));
            numContourNumberSize = CreateNumericUpDown(200, yPos, 6m, 48m, 1m);
            this.Controls.Add(numContourNumberSize);
            yPos += 45;

            // Visibility section
            Label lblVisibilitySection = CreateLabel("표시 설정 (Visibility)", 10, yPos, true);
            this.Controls.Add(lblVisibilitySection);
            yPos += 30;



            chkShowPartNumbers = new CheckBox
            {
                Text = "파트 번호 표시 (Show Part Numbers)",
                Location = new Point(20, yPos),
                Size = new Size(300, 20),
                ForeColor = Color.Black
            };
            this.Controls.Add(chkShowPartNumbers);
            yPos += 28;

            chkShowContourNumbers = new CheckBox
            {
                Text = "컨투어 번호 표시 (Show Contour Numbers)",
                Location = new Point(20, yPos),
                Size = new Size(350, 20),
                ForeColor = Color.Black
            };
            this.Controls.Add(chkShowContourNumbers);
            yPos += 30;

            // Phase 5.3: Part boundary visibility
            chkShowPartBoundaries = new CheckBox
            {
                Text = "파트 외곽선 표시 (Show Part Boundaries)",
                Location = new Point(20, yPos),
                Size = new Size(350, 20),
                ForeColor = Color.Black
            };
            this.Controls.Add(chkShowPartBoundaries);
            yPos += 45;

            // Phase 5.3: Part boundary settings
            Label lblPartBoundarySection = CreateLabel("파트 외곽선 설정 (Part Boundary)", 10, yPos, true);
            this.Controls.Add(lblPartBoundarySection);
            yPos += 30;

            // Part boundary color
            this.Controls.Add(CreateLabel("외곽선 색상 (Color):", 20, yPos));
            btnPartBoundary = CreateColorButton(200, yPos, Color.LightBlue);
            this.Controls.Add(btnPartBoundary);
            yPos += 35;

            // Part boundary width
            this.Controls.Add(CreateLabel("외곽선 두께 (Width):", 20, yPos));
            numPartBoundaryWidth = CreateNumericUpDown(200, yPos, 0.5m, 10.0m, 0.5m);
            this.Controls.Add(numPartBoundaryWidth);
            yPos += 35;


            // View settings section
            Label lblViewSection = CreateLabel("뷰 설정 (View Settings)", 10, yPos, true);
            this.Controls.Add(lblViewSection);
            yPos += 30;

            // Initial zoom multiplier
            this.Controls.Add(CreateLabel("초기 줌 배율 (Initial Zoom):", 20, yPos));
            numInitialZoom = CreateNumericUpDown(200, yPos, 0.0001m, 0.1m, 0.0001m);
            numInitialZoom.DecimalPlaces = 4; // 4 decimal places for fine-grained precision
            numInitialZoom.Value = 0.005m; // Will be overwritten by LoadSettings
            this.Controls.Add(numInitialZoom);
            
            // Add help label
            Label lblZoomHelp = new Label
            {
                Text = "작은값=작게, 큰값=크게 (0.0001~0.1)",
                Location = new Point(290, yPos + 2),
                Size = new Size(200, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8, FontStyle.Italic)
            };
            this.Controls.Add(lblZoomHelp);
            yPos += 45;

            // Phase 5.5: Canvas orientation section
            Label lblOrientationSection = CreateLabel("캔버스 방향 (Canvas Orientation)", 10, yPos, true);
            this.Controls.Add(lblOrientationSection);
            yPos += 30;

            this.Controls.Add(CreateLabel("회전 (Rotation):", 20, yPos));
            cmbOrientation = new ComboBox
            {
                Location = new Point(200, yPos),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbOrientation.Items.AddRange(new object[] {
                "0° (기본)",
                "90° CW (시계방향)",
                "180°",
                "270° CW (반시계방향)"
            });
            this.Controls.Add(cmbOrientation);
            yPos += 45;

            // Action buttons
            btnOK = new Button
            {
                Text = "OK",
                Location = new Point(180, yPos),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(270, yPos),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnCancel);

            btnReset = new Button
            {
                Text = "Reset",
                Location = new Point(360, yPos),
                Size = new Size(80, 30)
            };
            btnReset.Click += BtnReset_Click;
            this.Controls.Add(btnReset);
        }

        private Label CreateLabel(string text, int x, int y, bool bold = false)
        {
            Label lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(170, 20),
                ForeColor = Color.Black
            };
            if (bold)
            {
                lbl.Font = new Font(lbl.Font, FontStyle.Bold);
                lbl.Size = new Size(300, 20);
            }
            return lbl;
        }

        private Button CreateColorButton(int x, int y)
        {
            Button btn = new Button
            {
                Location = new Point(x, y - 2),
                Size = new Size(100, 25),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.Click += ColorButton_Click;
            return btn;
        }

        private Button CreateColorButton(int x, int y, Color initialColor)
        {
            Button btn = CreateColorButton(x, y);
            btn.BackColor = initialColor;
            return btn;
        }

        private NumericUpDown CreateNumericUpDown(int x, int y, decimal min, decimal max, decimal increment)
        {
            return new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(80, 25),
                Minimum = min,
                Maximum = max,
                DecimalPlaces = 1,
                Increment = increment
            };
        }

        private void LoadSettings()
        {
            // Load colors
            btnPiercing.BackColor = settings.PiercingPointColor;
            btnLeadIn.BackColor = settings.LeadInColor;
            btnCuttingCompleted.BackColor = settings.CuttingCompletedColor;
            btnCuttingInProgress.BackColor = settings.CuttingInProgressColor;
            btnCuttingPending.BackColor = settings.CuttingPendingColor;
            btnPartNumber.BackColor = settings.PartNumberColor;
            btnContourNumber.BackColor = settings.ContourNumberColor;
            btnMarking.BackColor = settings.MarkingColor;

            // Load sizes
            numPiercingSize.Value = (decimal)settings.PiercingPointSize;
            numLeadInWidth.Value = (decimal)settings.LeadInWidth;
            numCuttingCompletedWidth.Value = (decimal)settings.CuttingCompletedWidth;
            numCuttingInProgressWidth.Value = (decimal)settings.CuttingInProgressWidth;
            numCuttingPendingWidth.Value = (decimal)settings.CuttingPendingWidth;
            numPartNumberSize.Value = (decimal)settings.PartNumberSize;
            numContourNumberSize.Value = (decimal)settings.ContourNumberSize;

            // Load visibility
            chkShowPartNumbers.Checked = settings.ShowPartNumbers;
            chkShowContourNumbers.Checked = settings.ShowContourNumbers;

            // Phase 5.3: Load part boundary settings
            chkShowPartBoundaries.Checked = settings.ShowPartBoundaries;
            btnPartBoundary.BackColor = settings.PartBoundaryColor;
            numPartBoundaryWidth.Value = (decimal)settings.PartBoundaryWidth;

            // Phase 5.5: Load canvas orientation
            cmbOrientation.SelectedIndex = (int)settings.Orientation;

            // Load view settings
            numInitialZoom.Value = (decimal)settings.InitialZoomMultiplier;
        }

        private void SaveSettings()
        {
            // Save colors
            settings.PiercingPointColor = btnPiercing.BackColor;
            settings.LeadInColor = btnLeadIn.BackColor;
            settings.CuttingCompletedColor = btnCuttingCompleted.BackColor;
            settings.CuttingInProgressColor = btnCuttingInProgress.BackColor;
            settings.CuttingPendingColor = btnCuttingPending.BackColor;
            settings.PartNumberColor = btnPartNumber.BackColor;
            settings.ContourNumberColor = btnContourNumber.BackColor;
            settings.MarkingColor = btnMarking.BackColor;

            // Save sizes
            settings.PiercingPointSize = (float)numPiercingSize.Value;
            settings.LeadInWidth = (float)numLeadInWidth.Value;
            settings.CuttingCompletedWidth = (float)numCuttingCompletedWidth.Value;
            settings.CuttingInProgressWidth = (float)numCuttingInProgressWidth.Value;
            settings.CuttingPendingWidth = (float)numCuttingPendingWidth.Value;
            settings.PartNumberSize = (float)numPartNumberSize.Value;
            settings.ContourNumberSize = (float)numContourNumberSize.Value;

            // Save visibility
            settings.ShowPartNumbers = chkShowPartNumbers.Checked;
            settings.ShowContourNumbers = chkShowContourNumbers.Checked;

            // Phase 5.3: Save part boundary settings
            settings.ShowPartBoundaries = chkShowPartBoundaries.Checked;
            settings.PartBoundaryColor = btnPartBoundary.BackColor;
            settings.PartBoundaryWidth = (float)numPartBoundaryWidth.Value;

            // Phase 5.5: Save canvas orientation
            settings.Orientation = (CanvasOrientation)cmbOrientation.SelectedIndex;

            // Save view settings
            settings.InitialZoomMultiplier = (float)numInitialZoom.Value;
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.Color = btn.BackColor;
                colorDialog.FullOpen = true;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    btn.BackColor = colorDialog.Color;
                }
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            SaveSettings();
            
            // Save to file
            try
            {
                string filePath = AppConfig.Instance.RenderSettingsPath;
                settings.SaveToFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("설정 파일 저장 오류:\n" + ex.Message, "오류", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("모든 설정을 기본값으로 되돌리시겠습니까?", "확인", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                settings.ResetToDefaults();
                LoadSettings();
            }
        }
    }
}
