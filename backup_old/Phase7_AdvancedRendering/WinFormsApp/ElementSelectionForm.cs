using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CamViewerPOC.MPF;
using CamViewerPOC.Selection;

namespace CamViewerPOC
{
    /// <summary>
    /// Element selection dialog form
    /// Allows precise element selection by part/contour number and list selection
    /// </summary>
    public partial class ElementSelectionForm : Form
    {
        private NumericUpDown numPartIndex;
        private NumericUpDown numContourIndex;
        private Button btnSearch;
        private ListBox listElements;
        private Label lblInfo;
        private Button btnSelect;
        private Button btnCancel;

        private MPFProgram currentProgram;
        private SelectionManager selectionManager;
        
        // Selected element result
        public int SelectedPartIndex { get; private set; } = -1;
        public int SelectedContourIndex { get; private set; } = -1;
        public int SelectedElementIndex { get; private set; } = -1;
        public string SelectedGCode { get; private set; } = "";

        public ElementSelectionForm(MPFProgram program, SelectionManager manager)
        {
            currentProgram = program;
            selectionManager = manager;
            
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeComponent()
        {
            this.Text = "엘리먼트 선택 (Element Selection)";
            this.Size = new Size(600, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void InitializeUI()
        {
            int yPos = 20;
            int leftMargin = 20;
            int labelWidth = 120;
            int controlWidth = 100;

            // Part Index
            Label lblPart = new Label
            {
                Text = "파트 번호 (Part #):",
                Location = new Point(leftMargin, yPos),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblPart);

            numPartIndex = new NumericUpDown
            {
                Location = new Point(leftMargin + labelWidth, yPos),
                Size = new Size(controlWidth, 25),
                Minimum = 1,  // 1-based for UI
                Maximum = 99,
                Value = 1
            };
            this.Controls.Add(numPartIndex);
            yPos += 35;

            // Contour Index
            Label lblContour = new Label
            {
                Text = "컨투어 번호 (Contour #):",
                Location = new Point(leftMargin, yPos),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblContour);

            numContourIndex = new NumericUpDown
            {
                Location = new Point(leftMargin + labelWidth, yPos),
                Size = new Size(controlWidth, 25),
                Minimum = 1,  // 1-based for UI
                Maximum = 999,
                Value = 1
            };
            this.Controls.Add(numContourIndex);
            yPos += 35;

            // Search Button
            btnSearch = new Button
            {
                Text = "엘리먼트 검색",
                Location = new Point(leftMargin + labelWidth, yPos),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9, FontStyle.Bold)
            };
            btnSearch.Click += BtnSearch_Click;
            this.Controls.Add(btnSearch);
            yPos += 50;

            // Info Label
            lblInfo = new Label
            {
                Text = "파트와 컨투어 번호를 입력하고 '엘리먼트 검색'을 클릭하세요.",
                Location = new Point(leftMargin, yPos),
                Size = new Size(550, 25),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblInfo);
            yPos += 30;

            // Elements ListBox
            listElements = new ListBox
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(550, 250),
                Font = new Font("Consolas", 9),
                SelectionMode = SelectionMode.One
            };
            listElements.SelectedIndexChanged += ListElements_SelectedIndexChanged;
            this.Controls.Add(listElements);
            yPos += 260;

            // Select Button
            btnSelect = new Button
            {
                Text = "선택",
                Location = new Point(350, yPos),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9, FontStyle.Bold),
                Enabled = false
            };
            btnSelect.Click += BtnSelect_Click;
            this.Controls.Add(btnSelect);

            // Cancel Button
            btnCancel = new Button
            {
                Text = "취소",
                Location = new Point(470, yPos),
                Size = new Size(100, 35),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9, FontStyle.Bold)
            };
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            // Convert 1-based UI index to 0-based internal index
            int partIndex = (int)numPartIndex.Value - 1;
            int contourIndex = (int)numContourIndex.Value - 1;

            listElements.Items.Clear();
            btnSelect.Enabled = false;

            // Validate indices
            if (currentProgram == null || currentProgram.Parts == null)
            {
                lblInfo.Text = "프로그램이 로드되지 않았습니다.";
                lblInfo.ForeColor = Color.Red;
                return;
            }

            if (partIndex < 0 || partIndex >= currentProgram.Parts.Count)
            {
                lblInfo.Text = $"파트 {partIndex + 1}를 찾을 수 없습니다. (최대: {currentProgram.Parts.Count})";
                lblInfo.ForeColor = Color.Red;
                return;
            }

            Part part = currentProgram.Parts[partIndex];
            if (part.Contours == null || contourIndex < 0 || contourIndex >= part.Contours.Count)
            {
                lblInfo.Text = $"컨투어 {contourIndex + 1}를 찾을 수 없습니다. (최대: {part.Contours.Count})";
                lblInfo.ForeColor = Color.Red;
                return;
            }

            Contour contour = part.Contours[contourIndex];
            if (contour.AllSegments == null || contour.AllSegments.Count == 0)
            {
                lblInfo.Text = "이 컨투어에 엘리먼트가 없습니다.";
                lblInfo.ForeColor = Color.Orange;
                return;
            }

            // Populate list with elements
            for (int i = 0; i < contour.AllSegments.Count; i++)
            {
                PathSegment segment = contour.AllSegments[i];
                string description = GetElementDescription(i, segment);
                listElements.Items.Add(description);
            }

            lblInfo.Text = $"{contour.AllSegments.Count}개의 엘리먼트를 찾았습니다. 리스트에서 선택하세요.";
            lblInfo.ForeColor = Color.Green;
        }

        private string GetElementDescription(int index, PathSegment segment)
        {
            string desc = "";
            string gcode = GetSegmentGCode(segment); // Generate or get original G-Code
            
            // Display 1-based element number for UI consistency
            int displayIndex = index + 1;
            
            if (segment is LineSegment line)
            {
                desc = $"Element {displayIndex}: LINE | G-Code: {gcode}";
            }
            else if (segment is ArcSegment arc)
            {
                string direction = arc.Clockwise ? "CW" : "CCW";
                desc = $"Element {displayIndex}: ARC {direction} | G-Code: {gcode}";
            }
            else
            {
                desc = $"Element {displayIndex}: UNKNOWN | G-Code: {gcode}";
            }
            
            return desc;
        }

        private void ListElements_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listElements.SelectedIndex >= 0)
            {
                btnSelect.Enabled = true;
                
                // Preview selection on viewer (if selectionManager is available)
                // Convert 1-based UI values to 0-based for internal use
                int partIndex = (int)numPartIndex.Value - 1;
                int contourIndex = (int)numContourIndex.Value - 1;
                int elementIndex = listElements.SelectedIndex;  // Already 0-based

                if (selectionManager != null)
                {
                    // Temporarily select this element to show preview
                    selectionManager.SelectElement(partIndex, contourIndex, elementIndex, false);
                    
                    // Trigger redraw event (if available)
                    ElementPreviewChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                btnSelect.Enabled = false;
            }
        }

        private void BtnSelect_Click(object sender, EventArgs e)
        {
            if (listElements.SelectedIndex < 0)
                return;

            // Store 0-based indices for internal use
            SelectedPartIndex = (int)numPartIndex.Value - 1;
            SelectedContourIndex = (int)numContourIndex.Value - 1;
            SelectedElementIndex = listElements.SelectedIndex;  // Already 0-based

            // Get G-Code for this element using 0-based indices
            Part part = currentProgram.Parts[SelectedPartIndex];
            Contour contour = part.Contours[SelectedContourIndex];
            PathSegment segment = contour.AllSegments[SelectedElementIndex];
            
            // Generate G-Code description from segment
            SelectedGCode = GetSegmentGCode(segment);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Generate G-Code description from path segment
        /// Prefers original G-Code if available, otherwise generates from coordinates
        /// </summary>
        private string GetSegmentGCode(PathSegment segment)
        {
            // Use original G-Code if available
            if (!string.IsNullOrEmpty(segment.OriginalGCode))
            {
                return segment.OriginalGCode;
            }
            
            // Otherwise generate from coordinates
            if (segment is LineSegment line)
            {
                return $"G01 X{line.End.X:F3} Y{line.End.Y:F3}";
            }
            else if (segment is ArcSegment arc)
            {
                string gcode = arc.Clockwise ? "G02" : "G03";
                return $"{gcode} X{arc.End.X:F3} Y{arc.End.Y:F3} I{arc.I:F3} J{arc.J:F3}";
            }
            return "Unknown";
        }

        // Event for preview changes
        public event EventHandler ElementPreviewChanged;
    }
}
