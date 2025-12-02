using System;
using System.Drawing;
using System.Windows.Forms;

namespace RealtimeITagControl.Trace
{
    /// <summary>
    /// Phase 8.2: 실시간 트레이스 진행 상황 표시 패널
    /// </summary>
    public class TraceStatusPanel : Panel
    {
        private Label _lblTitle;
        private Label _lblPartContour;
        private Label _lblProgress;
        private Label _lblPosition;
        private Label _lblCurrentBlock;
        private ProgressBar _progressBar;
        private Button _btnStartStop;
        
        // 이벤트
        public event EventHandler StartTraceRequested;
        public event EventHandler StopTraceRequested;

        public TraceStatusPanel()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // 패널 기본 설정
            this.Size = new Size(300, 200);
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Padding = new Padding(10);

            // 제목 라벨
            _lblTitle = new Label
            {
                Text = "실시간 트레이스",
                Location = new Point(10, 10),
                Size = new Size(280, 25),
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };

            // Part/Contour 정보
            _lblPartContour = new Label
            {
                Text = "Part: -, Contour: -",
                Location = new Point(10, 45),
                Size = new Size(280, 20),
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.Black
            };

            // 진행률 텍스트
            _lblProgress = new Label
            {
                Text = "진행률: 0.0%",
                Location = new Point(10, 70),
                Size = new Size(280, 20),
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.Black
            };

            // 진행률 바
            _progressBar = new ProgressBar
            {
                Location = new Point(10, 95),
                Size = new Size(280, 25),
                Minimum = 0,
                Maximum = 1000,  // 0.1% 단위로 표시하기 위해
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            // 위치 정보
            _lblPosition = new Label
            {
                Text = "위치: X: 0.000, Y: 0.000",
                Location = new Point(10, 130),
                Size = new Size(280, 20),
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.Black
            };

            // 현재 블록 정보
            _lblCurrentBlock = new Label
            {
                Text = "블록: -",
                Location = new Point(10, 155),
                Size = new Size(280, 20),
                Font = new Font("맑은 고딕", 9F, FontStyle.Italic),
                ForeColor = Color.DarkGray,
                AutoEllipsis = true
            };

            // 시작/중지 버튼
            _btnStartStop = new Button
            {
                Text = "시작",
                Location = new Point(10, 180),
                Size = new Size(280, 30),
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnStartStop.FlatAppearance.BorderSize = 0;
            _btnStartStop.Click += BtnStartStop_Click;

            // 컨트롤 추가
            Controls.Add(_lblTitle);
            Controls.Add(_lblPartContour);
            Controls.Add(_lblProgress);
            Controls.Add(_progressBar);
            Controls.Add(_lblPosition);
            Controls.Add(_lblCurrentBlock);
            Controls.Add(_btnStartStop);

            // 패널 크기 조정
            this.Size = new Size(300, 220);
        }

        /// <summary>
        /// 진행 상황 업데이트
        /// </summary>
        public void UpdateStatus(CuttingProgressData progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<CuttingProgressData>(UpdateStatus), progress);
                return;
            }

            if (progress == null || !progress.IsActive)
            {
                // 비활성 상태
                _lblPartContour.Text = "Part: -, Contour: -";
                _lblProgress.Text = "진행률: 0.0%";
                _progressBar.Value = 0;
                _lblPosition.Text = "위치: X: 0.000, Y: 0.000";
                _lblCurrentBlock.Text = "블록: -";
                _lblCurrentBlock.ForeColor = Color.DarkGray;
                
                _btnStartStop.Text = "시작";
                _btnStartStop.BackColor = Color.FromArgb(0, 120, 215);
                _btnStartStop.Enabled = true;
                return;
            }

            // 활성 상태
            _lblPartContour.Text = $"Part: {progress.CurrentPart}, Contour: {progress.CurrentContour}";
            _lblProgress.Text = $"진행률: {progress.ProgressPercent:F1}%";
            _progressBar.Value = (int)(progress.Progress * 1000);  // 0.1% 정밀도
            _lblPosition.Text = $"위치: X: {progress.PositionX:F3}, Y: {progress.PositionY:F3}";
            
            if (!string.IsNullOrEmpty(progress.CurrentBlock))
            {
                _lblCurrentBlock.Text = $"블록: {progress.CurrentBlock}";
                _lblCurrentBlock.ForeColor = Color.Black;
            }
            else
            {
                _lblCurrentBlock.Text = "블록: -";
                _lblCurrentBlock.ForeColor = Color.DarkGray;
            }

            _btnStartStop.Text = "중지";
            _btnStartStop.BackColor = Color.FromArgb(232, 17, 35);
            _btnStartStop.Enabled = true;

            // 진행률에 따른 프로그레스 바 색상 변경 (선택사항)
            if (progress.Progress < 0.3)
            {
                _progressBar.ForeColor = Color.Red;
            }
            else if (progress.Progress < 0.7)
            {
                _progressBar.ForeColor = Color.Orange;
            }
            else
            {
                _progressBar.ForeColor = Color.Green;
            }
        }

        /// <summary>
        /// 트레이스 비활성 상태로 리셋
        /// </summary>
        public void ResetStatus()
        {
            UpdateStatus(new CuttingProgressData { IsActive = false });
        }

        /// <summary>
        /// 버튼 활성화/비활성화
        /// </summary>
        public void SetButtonEnabled(bool enabled)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(SetButtonEnabled), enabled);
                return;
            }

            _btnStartStop.Enabled = enabled;
        }

        /// <summary>
        /// 시작/중지 버튼 클릭 이벤트
        /// </summary>
        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (_btnStartStop.Text == "시작")
            {
                StartTraceRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StopTraceRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 버튼 텍스트 변경
        /// </summary>
        public void SetButtonText(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetButtonText), text);
                return;
            }

            _btnStartStop.Text = text;
            
            if (text == "시작")
            {
                _btnStartStop.BackColor = Color.FromArgb(0, 120, 215);
            }
            else if (text == "중지")
            {
                _btnStartStop.BackColor = Color.FromArgb(232, 17, 35);
            }
        }

        /// <summary>
        /// 상세 정보 표시 여부
        /// </summary>
        public bool ShowDetails
        {
            get => _lblCurrentBlock.Visible;
            set
            {
                _lblCurrentBlock.Visible = value;
                if (!value)
                {
                    this.Height = 180;
                }
                else
                {
                    this.Height = 220;
                }
            }
        }
    }
}
