using System;
using System.Drawing;
using System.Windows.Forms;

namespace RealtimeITagControl.UI
{
    /// <summary>
    /// 프로그램 정보 패널 (300x550)
    /// </summary>
    public class ProgramInfoPanel : UserControl
    {
        #region UI 컨트롤

        private GroupBox grpCoordinates;
        private Label lblCoordinates;
        private TextBox txtCoordinates;

        private GroupBox grpProgress;
        private Label lblProgressDistance;
        private TextBox txtProgressDistance;
        private Label lblCurrentPart;
        private Label lblCurrentContour;
        private TextBox txtCurrentPart;
        private TextBox txtCurrentContour;

        private GroupBox grpWorkInfo;
        private Label lblWorkDir;
        private Label lblWorkMpfName;
        private Label lblWorkStatus;
        private TextBox txtWorkDir;
        private TextBox txtWorkMpfName;
        private TextBox txtWorkStatus;

        private GroupBox grpLineInfo;
        private Label lblActLineCode;
        private Label lblActLineNum;
        private TextBox txtActLineCode;
        private TextBox txtActLineNum;

        private CheckBox chkShowPartNumber;
        private CheckBox chkShowContourNumber;

        private Button btnSimulation;
        private Button btnElementSelect;
        private Button btnITagTest;

        // ITag 서버 상태 표시
        private GroupBox grpITagStatus;
        private Label lblConnectionStatus;
        private Label lblCyclicStatus;
        private TextBox txtConnectionStatus;
        private TextBox txtCyclicStatus;

        #endregion

        #region 이벤트

        public event EventHandler SimulationClicked;
        public event EventHandler ElementSelectClicked;
        public event EventHandler ITagTestClicked;
        public event EventHandler<bool> ShowPartNumberChanged;
        public event EventHandler<bool> ShowContourNumberChanged;

        #endregion

        #region 생성자

        public ProgramInfoPanel()
        {
            InitializeComponent();
        }

        #endregion

        #region UI 초기화

        private void InitializeComponent()
        {
            this.Size = new Size(300, 630);
            this.BackColor = Color.FromArgb(240, 240, 240);

            int y = 10;
            int padding = 10;

            // ITag 서버 상태 (최상단)
            grpITagStatus = CreateGroup("ITag 서버 상태", 10, y, 280, 80);
            CreateITagStatusInputs(grpITagStatus);
            this.Controls.Add(grpITagStatus);
            y += grpITagStatus.Height + padding;

            // 좌표 정보 (X, Y 같은 행에 표시)
            grpCoordinates = CreateGroup("좌표 정보", 10, y, 280, 50);
            lblCoordinates = new Label { Text = "X, Y:", Location = new Point(10, 23), Size = new Size(40, 20) };
            txtCoordinates = new TextBox { Location = new Point(55, 21), Size = new Size(205, 20), ReadOnly = true };
            grpCoordinates.Controls.AddRange(new Control[] { lblCoordinates, txtCoordinates });
            this.Controls.Add(grpCoordinates);
            y += grpCoordinates.Height + padding;

            // 진행 정보 (높이 줄임: 105 → 80)
            grpProgress = CreateGroup("진행 정보", 10, y, 280, 80);
            CreateProgressInputs(grpProgress);
            this.Controls.Add(grpProgress);
            y += grpProgress.Height + padding;

            // 작업 정보
            grpWorkInfo = CreateGroup("작업 정보", 10, y, 280, 105);
            CreateWorkInfoInputs(grpWorkInfo);
            this.Controls.Add(grpWorkInfo);
            y += grpWorkInfo.Height + padding;

            // 현재 실행 정보
            grpLineInfo = CreateGroup("실행 정보", 10, y, 280, 80);
            CreateLineInfoInputs(grpLineInfo);
            this.Controls.Add(grpLineInfo);
            y += grpLineInfo.Height + padding;

            // 체크박스
            chkShowPartNumber = new CheckBox
            {
                Text = "파트 번호 표시",
                Location = new Point(20, y),
                Size = new Size(260, 25),
                Checked = true
            };
            chkShowPartNumber.CheckedChanged += (s, e) => ShowPartNumberChanged?.Invoke(this, chkShowPartNumber.Checked);
            this.Controls.Add(chkShowPartNumber);
            y += 30;

            chkShowContourNumber = new CheckBox
            {
                Text = "컨투어 번호 표시",
                Location = new Point(20, y),
                Size = new Size(260, 25),
                Checked = true
            };
            chkShowContourNumber.CheckedChanged += (s, e) => ShowContourNumberChanged?.Invoke(this, chkShowContourNumber.Checked);
            this.Controls.Add(chkShowContourNumber);
            y += 35;

            // 버튼 (2열 배치, 크기 절반)
            int btnWidth = 125;  // 절반 크기
            int btnHeight = 30;
            int btnGap = 10;
            
            // 1행: 시뮬레이션, 엘리먼트 선택
            btnSimulation = new Button
            {
                Text = "시뮬레이션",
                Location = new Point(20, y),
                Size = new Size(btnWidth, btnHeight),
                BackColor = Color.LightBlue
            };
            btnSimulation.Click += (s, e) => SimulationClicked?.Invoke(this, EventArgs.Empty);
            this.Controls.Add(btnSimulation);

            btnElementSelect = new Button
            {
                Text = "엘리먼트 선택",
                Location = new Point(20 + btnWidth + btnGap, y),
                Size = new Size(btnWidth, btnHeight),
                BackColor = Color.LightGreen
            };
            btnElementSelect.Click += (s, e) => ElementSelectClicked?.Invoke(this, EventArgs.Empty);
            this.Controls.Add(btnElementSelect);
            y += btnHeight + btnGap;

            // 2행: ITag 연결 테스트, (예비 버튼)
            btnITagTest = new Button
            {
                Text = "ITag 테스트",
                Location = new Point(20, y),
                Size = new Size(btnWidth, btnHeight),
                BackColor = Color.LightCoral
            };
            btnITagTest.Click += (s, e) => ITagTestClicked?.Invoke(this, EventArgs.Empty);
            this.Controls.Add(btnITagTest);
            
            // 추후 확장용 버튼 공간 (현재는 비워둠)
        }

        private GroupBox CreateGroup(string title, int x, int y, int w, int h)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(x, y),
                Size = new Size(w, h)
            };
        }



        private void CreateProgressInputs(GroupBox parent)
        {
            lblProgressDistance = new Label { Text = "진행 거리:", Location = new Point(10, 25), Size = new Size(80, 20) };
            txtProgressDistance = new TextBox { Location = new Point(95, 23), Size = new Size(165, 20), ReadOnly = true };

            // 파트/컨투어 같은 행에 표시
            lblCurrentPart = new Label { Text = "파트/컨투어:", Location = new Point(10, 50), Size = new Size(80, 20) };
            txtCurrentPart = new TextBox { Location = new Point(95, 48), Size = new Size(70, 20), ReadOnly = true };
            
            Label lblSlash = new Label { Text = "/", Location = new Point(170, 50), Size = new Size(10, 20), TextAlign = ContentAlignment.MiddleCenter };
            
            txtCurrentContour = new TextBox { Location = new Point(185, 48), Size = new Size(75, 20), ReadOnly = true };

            // 라벨 변수는 유지하되 사용 안함
            lblCurrentContour = new Label { Visible = false };

            parent.Controls.AddRange(new Control[] {
                lblProgressDistance, txtProgressDistance,
                lblCurrentPart, txtCurrentPart,
                lblSlash, txtCurrentContour
            });
        }

        private void CreateWorkInfoInputs(GroupBox parent)
        {
            lblWorkDir = new Label { Text = "작업 폴더:", Location = new Point(10, 25), Size = new Size(80, 20) };
            txtWorkDir = new TextBox { Location = new Point(95, 23), Size = new Size(165, 20), ReadOnly = true };

            lblWorkMpfName = new Label { Text = "MPF 파일:", Location = new Point(10, 50), Size = new Size(80, 20) };
            txtWorkMpfName = new TextBox { Location = new Point(95, 48), Size = new Size(165, 20), ReadOnly = true };

            lblWorkStatus = new Label { Text = "작업 상태:", Location = new Point(10, 75), Size = new Size(80, 20) };
            txtWorkStatus = new TextBox { Location = new Point(95, 73), Size = new Size(165, 20), ReadOnly = true };

            parent.Controls.AddRange(new Control[] {
                lblWorkDir, txtWorkDir,
                lblWorkMpfName, txtWorkMpfName,
                lblWorkStatus, txtWorkStatus
            });
        }

        private void CreateLineInfoInputs(GroupBox parent)
        {
            lblActLineCode = new Label { Text = "실행 코드:", Location = new Point(10, 25), Size = new Size(80, 20) };
            txtActLineCode = new TextBox { Location = new Point(95, 23), Size = new Size(165, 20), ReadOnly = true };

            lblActLineNum = new Label { Text = "실행 라인:", Location = new Point(10, 50), Size = new Size(80, 20) };
            txtActLineNum = new TextBox { Location = new Point(95, 48), Size = new Size(165, 20), ReadOnly = true };

            parent.Controls.AddRange(new Control[] {
                lblActLineCode, txtActLineCode,
                lblActLineNum, txtActLineNum
            });
        }

        private void CreateITagStatusInputs(GroupBox parent)
        {
            lblConnectionStatus = new Label { Text = "서버 연결:", Location = new Point(10, 25), Size = new Size(80, 20) };
            txtConnectionStatus = new TextBox 
            { 
                Location = new Point(95, 23), 
                Size = new Size(165, 20), 
                ReadOnly = true,
                BackColor = Color.LightGray,
                Text = "연결 안됨"
            };

            lblCyclicStatus = new Label { Text = "주기 읽기:", Location = new Point(10, 50), Size = new Size(80, 20) };
            txtCyclicStatus = new TextBox 
            { 
                Location = new Point(95, 48), 
                Size = new Size(165, 20), 
                ReadOnly = true,
                BackColor = Color.LightGray,
                Text = "중지됨"
            };

            parent.Controls.AddRange(new Control[] {
                lblConnectionStatus, txtConnectionStatus,
                lblCyclicStatus, txtCyclicStatus
            });
        }

        #endregion

        #region 데이터 업데이트

        /// <summary>
        /// Tag 데이터로 UI 업데이트
        /// </summary>
        public void UpdateTagData(TagData data)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<TagData>(UpdateTagData), data);
                return;
            }

            // 좌표 정보 (X, Y 같은 행에 표시)
            txtCoordinates.Text = $"X: {data.X_WCS:F3}, Y: {data.Y_WCS:F3}";

            // 진행 정보
            txtProgressDistance.Text = data.ProgressDistance.ToString("F3");
            txtCurrentPart.Text = data.CurrentPart.ToString();
            txtCurrentContour.Text = data.CurrentContour.ToString();

            // 작업 정보
            txtWorkDir.Text = data.WorkDir;
            txtWorkMpfName.Text = data.WorkMpfName;
            txtWorkStatus.Text = GetWorkStatusText(data.WorkStatus);

            // 실행 정보
            txtActLineCode.Text = data.ActLineCode;
            txtActLineNum.Text = data.ActLineNum.ToString();
        }

        private string GetWorkStatusText(TagDefinitions.WorkStatus status)
        {
            switch (status)
            {
                case TagDefinitions.WorkStatus.End:
                    return "종료";
                case TagDefinitions.WorkStatus.Start:
                    return "시작";
                case TagDefinitions.WorkStatus.Reset:
                    return "리셋";
                case TagDefinitions.WorkStatus.FeedHold:
                    return "일시정지";
                default:
                    return "알 수 없음";
            }
        }

        /// <summary>
        /// ITag 서버 연결 및 주기 읽기 상태 업데이트
        /// </summary>
        public void UpdateConnectionStatus(bool connected, bool cyclicReading, string errorMessage = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool, bool, string>(UpdateConnectionStatus), connected, cyclicReading, errorMessage);
                return;
            }

            // 서버 연결 상태
            if (connected)
            {
                txtConnectionStatus.Text = "✅ 연결됨";
                txtConnectionStatus.BackColor = Color.LightGreen;
            }
            else
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    txtConnectionStatus.Text = $"❌ 연결 안됨: {errorMessage}";
                }
                else
                {
                    txtConnectionStatus.Text = "❌ 연결 안됨";
                }
                txtConnectionStatus.BackColor = Color.LightCoral;
            }

            // 주기 읽기 상태
            if (cyclicReading)
            {
                txtCyclicStatus.Text = "🔄 실행 중";
                txtCyclicStatus.BackColor = Color.LightGreen;
            }
            else
            {
                txtCyclicStatus.Text = "⏸️ 중지됨";
                txtCyclicStatus.BackColor = Color.LightGray;
            }
        }

        #endregion
    }
}
