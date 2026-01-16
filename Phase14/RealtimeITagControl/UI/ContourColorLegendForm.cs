using System;
using System.Drawing;
using System.Windows.Forms;

namespace RealtimeITagControl.UI
{
    /// <summary>
    /// 컨투어 선택 박스 색상 범례 폼
    /// </summary>
    public class ContourColorLegendForm : Form
    {
        private TableLayoutPanel tableLayout;
        public System.Collections.Generic.HashSet<int> VisibleContours { get; private set; }
        public event EventHandler ContourVisibilityChanged;

        public ContourColorLegendForm(int totalContours)
        {
            VisibleContours = new System.Collections.Generic.HashSet<int>();
            InitializeForm(totalContours);
        }

        private void InitializeForm(int totalContours)
        {
            this.Text = "컨투어 선택 영역 색상";
            this.Size = new Size(300, Math.Min(500, 100 + totalContours * 30));
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = false;  // 다른 영역 클릭 가능하도록
            this.ShowInTaskbar = false;

            // Rainbow colors
            Color[] colors = new Color[]
            {
                Color.FromArgb(255, 0, 0),      // Red
                Color.FromArgb(255, 127, 0),    // Orange
                Color.FromArgb(255, 255, 0),    // Yellow
                Color.FromArgb(0, 255, 0),      // Green
                Color.FromArgb(0, 255, 255),    // Cyan
                Color.FromArgb(0, 0, 255),      // Blue
                Color.FromArgb(139, 0, 255),    // Purple
                Color.FromArgb(255, 0, 255)     // Magenta
            };

            string[] colorNames = new string[]
            {
                "빨강",
                "주황",
                "노랑",
                "초록",
                "하늘",
                "파랑",
                "보라",
                "자홍"
            };

            // TableLayoutPanel
            tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));  // 체크박스
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // Contour 번호
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));  // 색상 박스
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // 색상 이름

            // Header
            tableLayout.Controls.Add(CreateLabel("표시", true), 0, 0);
            tableLayout.Controls.Add(CreateLabel("컨투어", true), 1, 0);
            tableLayout.Controls.Add(CreateLabel("색상", true), 2, 0);
            tableLayout.Controls.Add(CreateLabel("이름", true), 3, 0);

            // Contour rows
            int globalIndex = 0;
            for (int i = 0; i < totalContours; i++)
            {
                int row = i + 1;
                Color color = colors[globalIndex % colors.Length];
                string colorName = colorNames[globalIndex % colorNames.Length];
                int contourIndex = i;

                // Checkbox
                CheckBox chkContour = new CheckBox
                {
                    Checked = false,
                    Size = new Size(20, 20),
                    Margin = new Padding(10, 2, 2, 2),
                    Tag = contourIndex
                };
                chkContour.CheckedChanged += (s, e) =>
                {
                    CheckBox cb = (CheckBox)s;
                    int idx = (int)cb.Tag;
                    if (cb.Checked)
                        VisibleContours.Add(idx);
                    else
                        VisibleContours.Remove(idx);
                    ContourVisibilityChanged?.Invoke(this, EventArgs.Empty);
                };
                tableLayout.Controls.Add(chkContour, 0, row);

                // Contour number
                tableLayout.Controls.Add(CreateLabel($"Contour {i + 1}"), 1, row);

                // Color box
                Panel colorBox = new Panel
                {
                    BackColor = color,
                    Size = new Size(40, 20),
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(5)
                };
                tableLayout.Controls.Add(colorBox, 2, row);

                // Color name
                tableLayout.Controls.Add(CreateLabel(colorName), 3, row);

                globalIndex++;
            }

            this.Controls.Add(tableLayout);

            // Close button
            Button btnClose = new Button
            {
                Text = "닫기",
                Dock = DockStyle.Bottom,
                Height = 35,
                BackColor = Color.LightGray
            };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private Label CreateLabel(string text, bool bold = false)
        {
            Label label = new Label
            {
                Text = text,
                AutoSize = false,
                Size = new Size(70, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(2)
            };

            if (bold)
            {
                label.Font = new Font(label.Font, FontStyle.Bold);
            }

            return label;
        }
    }
}
