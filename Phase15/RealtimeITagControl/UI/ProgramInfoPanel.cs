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
        private Label lblITagCoordinates;  // Phase 13: ITag 실시간 좌표
        private TextBox txtITagCoordinates;

        private GroupBox grpWorkInfo;
        private Label lblWorkMpfName;
        private Label lblWorkStatus;
        private TextBox txtWorkMpfName;
        private TextBox txtWorkStatus;

        private GroupBox grpLineInfo;
        private Label lblActLineCode;
        private Label lblActLineNum;
        private TextBox txtActLineCode;
        private TextBox txtActLineNum;

        private CheckBox chkShowPartNumber;
        private CheckBox chkShowContourNumber;
        private CheckBox chkEnableContourSelection;  // 컨투어 선택 활성화 체크박스
        private Button btnShowContourSelectionBoxes;  // 컨투어 선택 박스 표시 버튼

        private Button btnSimulation;
        private Button btnStopSimulation;
        private Button btnElementSelect;

        // ITag 서버 상태 표시
        private GroupBox grpITagStatus;
        private Label lblConnectionStatus;
        private TextBox txtConnectionStatus;

        // Phase 15.3: 성능 정보 표시
        private GroupBox grpPerformance;
        private TextBox txtPerformance;

        #endregion

        #region 이벤트

        public event EventHandler SimulationClicked;
        public event EventHandler StopSimulationClicked;
        public event EventHandler ElementSelectClicked;
        public event EventHandler<bool> ShowPartNumberChanged;
        public event EventHandler<bool> ShowContourNumberChanged;
        public event EventHandler<bool> EnableContourSelectionChanged;  // 컨투어 선택 활성화 이벤트
        public event EventHandler ShowContourSelectionBoxesClicked;  // 컨투어 선택 박스 표시 버튼 클릭

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
            this.Size = new Size(300, 730);  // Phase 15.3: 성능 정보 추가로 높이 증가
            this.BackColor = Color.FromArgb(240, 240, 240);

            int y = 10;
            int padding = 10;

            // ITag 서버 상태 (최상단) - Phase 13: 주기 읽기 제거로 높이 감소
            grpITagStatus = CreateGroup("ITag 서버 상태", 10, y, 280, 55);
            CreateITagStatusInputs(grpITagStatus);
            this.Controls.Add(grpITagStatus);
            y += grpITagStatus.Height + padding;

            // Phase 15.3: 성능 정보
            grpPerformance = CreateGroup("성능 정보", 10, y, 280, 90);
            txtPerformance = new TextBox
            {
                Location = new Point(10, 20),
                Size = new Size(260, 60),
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 8.5f, FontStyle.Regular),
                BackColor = Color.Black,
                ForeColor = Color.Yellow,
                BorderStyle = BorderStyle.Fixed3D
            };
            grpPerformance.Controls.Add(txtPerformance);
            this.Controls.Add(grpPerformance);
            y += grpPerformance.Height + padding;

            // 좌표 정보 (X, Y 같은 행에 표시)
            grpCoordinates = CreateGroup("좌표 정보", 10, y, 280, 50);
            lblCoordinates = new Label { Text = "X, Y:", Location = new Point(10, 23), Size = new Size(40, 20) };
            txtCoordinates = new TextBox { Location = new Point(55, 21), Size = new Size(205, 20), ReadOnly = true };
            grpCoordinates.Controls.AddRange(new Control[] { lblCoordinates, txtCoordinates });
            this.Controls.Add(grpCoordinates);
            y += grpCoordinates.Height + padding;

            // 진행 정보 (Phase 13: ITag 실시간 좌표 추가로 높이 증가)
            grpProgress = CreateGroup("진행 정보", 10, y, 280, 105);
            CreateProgressInputs(grpProgress);
            this.Controls.Add(grpProgress);
            y += grpProgress.Height + padding;

            // 작업 정보 (Phase 13: 작업 폴더 제거로 높이 감소)
            grpWorkInfo = CreateGroup("작업 정보", 10, y, 280, 80);
            CreateWorkInfoInputs(grpWorkInfo);
            this.Controls.Add(grpWorkInfo);
            y += grpWorkInfo.Height + padding;

            // 현재 실행 정보
            grpLineInfo = CreateGroup("실행 정보", 10, y, 280, 80);
            CreateLineInfoInputs(grpLineInfo);
            this.Controls.Add(grpLineInfo);
            y += grpLineInfo.Height + padding;

            // 체크박스 (2열 배치)
            int chkWidth = 130;
            int chkHeight = 22;
            int chkGap = 10;
            int chkX1 = 15;
            int chkX2 = chkX1 + chkWidth + chkGap;

            // 1행: 파트 번호, 컨투어 번호
            chkShowPartNumber = new CheckBox
            {
                Text = "파트번호",
                Location = new Point(chkX1, y),
                Size = new Size(chkWidth, chkHeight),
                Checked = true
            };
            chkShowPartNumber.CheckedChanged += (s, e) => ShowPartNumberChanged?.Invoke(this, chkShowPartNumber.Checked);
            this.Controls.Add(chkShowPartNumber);

            chkShowContourNumber = new CheckBox
            {
                Text = "컨투어번호",
                Location = new Point(chkX2, y),
                Size = new Size(chkWidth, chkHeight),
                Checked = true
            };
            chkShowContourNumber.CheckedChanged += (s, e) => ShowContourNumberChanged?.Invoke(this, chkShowContourNumber.Checked);
            this.Controls.Add(chkShowContourNumber);
            y += 25;

            // 2행: 컨투어 선택, 선택 영역 보기
            chkEnableContourSelection = new CheckBox
            {
                Text = "컨투어선택",
                Location = new Point(chkX1, y),
                Size = new Size(chkWidth, chkHeight),
                Checked = false
            };
            chkEnableContourSelection.CheckedChanged += (s, e) => EnableContourSelectionChanged?.Invoke(this, chkEnableContourSelection.Checked);
            this.Controls.Add(chkEnableContourSelection);

            btnShowContourSelectionBoxes = new Button
            {
                Text = "선택영역",
                Location = new Point(chkX2, y),
                Size = new Size(chkWidth, chkHeight),
                BackColor = Color.FromArgb(255, 200, 100)
            };
            btnShowContourSelectionBoxes.Click += (s, e) => ShowContourSelectionBoxesClicked?.Invoke(this, EventArgs.Empty);
            this.Controls.Add(btnShowContourSelectionBoxes);
            y += 30;

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

            // 2행: 중단 버튼
            btnStopSimulation = new Button
            {
                Text = "시뮬레이션 중단",
                Location = new Point(20, y),
                Size = new Size(btnWidth, btnHeight),
                BackColor = Color.LightCoral,
                Enabled = false  // 초기에는 비활성화
            };
            btnStopSimulation.Click += (s, e) => StopSimulationClicked?.Invoke(this, EventArgs.Empty);
            this.Controls.Add(btnStopSimulation);

            y += btnHeight + btnGap;
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

            // Phase 13: ITag 실시간 좌표 (X_WCS, Y_WCS)
            lblITagCoordinates = new Label { Text = "실시간 좌표:", Location = new Point(10, 75), Size = new Size(80, 20) };
            txtITagCoordinates = new TextBox { Location = new Point(95, 73), Size = new Size(165, 20), ReadOnly = true };

            // 라벨 변수는 유지하되 사용 안함
            lblCurrentContour = new Label { Visible = false };

            parent.Controls.AddRange(new Control[] {
                lblProgressDistance, txtProgressDistance,
                lblCurrentPart, txtCurrentPart,
                lblSlash, txtCurrentContour,
                lblITagCoordinates, txtITagCoordinates  // Phase 13
            });
        }

        private void CreateWorkInfoInputs(GroupBox parent)
        {
            // Phase 13: 작업 폴더 항목 제거

            lblWorkMpfName = new Label { Text = "MPF 파일:", Location = new Point(10, 25), Size = new Size(80, 20) };
            txtWorkMpfName = new TextBox { Location = new Point(95, 23), Size = new Size(165, 20), ReadOnly = true };

            lblWorkStatus = new Label { Text = "작업 상태:", Location = new Point(10, 50), Size = new Size(80, 20) };
            txtWorkStatus = new TextBox { Location = new Point(95, 48), Size = new Size(165, 20), ReadOnly = true };

            parent.Controls.AddRange(new Control[] {
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

            // Phase 13: 주기 읽기 항목 제거

            parent.Controls.AddRange(new Control[] {
                lblConnectionStatus, txtConnectionStatus
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

            // Phase 13: 좌표 정보는 마우스 좌표만 표시 (ITag 좌표는 진행 정보로 이동)
            // txtCoordinates는 UpdateMouseCoordinates에서만 업데이트됨

            // 진행 정보
            txtProgressDistance.Text = data.ProgressDistance.ToString("F3");
            txtCurrentPart.Text = data.CurrentPart.ToString();
            txtCurrentContour.Text = data.CurrentContour.ToString();
            
            // Phase 13: ITag 실시간 좌표 (X_WCS, Y_WCS)
            txtITagCoordinates.Text = $"X: {data.X_WCS:F3}, Y: {data.Y_WCS:F3}";

            // 작업 정보 (Phase 13: 작업 폴더 제거)
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

            // Phase 13: 주기 읽기 상태 표시 제거
        }

        /// <summary>
        /// 시뮬레이션 버튼 상태 업데이트
        /// </summary>
        public void UpdateSimulationButtonState(bool isRunning, bool isPaused)
        {
            if (btnSimulation.InvokeRequired)
            {
                btnSimulation.Invoke(new Action(() => UpdateSimulationButtonState(isRunning, isPaused)));
                return;
            }

            if (isRunning)
            {
                btnSimulation.Text = "일시정지";
                btnSimulation.BackColor = Color.LightYellow;
                btnStopSimulation.Enabled = true;
            }
            else if (isPaused)
            {
                btnSimulation.Text = "재개";
                btnSimulation.BackColor = Color.LightGreen;
                btnStopSimulation.Enabled = true;
            }
            else
            {
                btnSimulation.Text = "시뮬레이션";
                btnSimulation.BackColor = Color.LightBlue;
                btnStopSimulation.Enabled = false;
            }
        }

        /// <summary>
        /// Phase 13: 마우스 실시간 좌표 업데이트
        /// </summary>
        /// <param name="x">X 좌표 (mm 단위)</param>
        /// <param name="y">Y 좌표 (mm 단위)</param>
        public void UpdateMouseCoordinates(double x, double y)
        {
            if (txtCoordinates.InvokeRequired)
            {
                txtCoordinates.Invoke(new Action(() => UpdateMouseCoordinates(x, y)));
                return;
            }

            // mm 단위로 표시 (소수점 3자리)
            txtCoordinates.Text = $"X: {x:F3} mm, Y: {y:F3} mm";
        }

        /// <summary>
        /// Phase 13: 좌표 표시 초기화
        /// </summary>
        public void ClearMouseCoordinates()
        {
            if (txtCoordinates.InvokeRequired)
            {
                txtCoordinates.Invoke(new Action(() => ClearMouseCoordinates()));
                return;
            }

            txtCoordinates.Text = "---";
        }

        /// <summary>
        /// Phase 15.3: 성능 정보 업데이트
        /// </summary>
        public void UpdatePerformanceInfo(string performanceSummary)
        {
            if (txtPerformance.InvokeRequired)
            {
                txtPerformance.Invoke(new Action(() => UpdatePerformanceInfo(performanceSummary)));
                return;
            }

            txtPerformance.Text = performanceSummary;
        }

        #endregion
    }
}
