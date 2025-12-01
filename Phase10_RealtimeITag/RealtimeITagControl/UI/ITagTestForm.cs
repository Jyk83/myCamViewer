using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Siemens.Runtime.ITag;

namespace RealtimeITagControl.UI
{
    /// <summary>
    /// ITag 연결 및 테스트 팝업 폼
    /// IITagManager를 통해 ITag 인스턴스를 공유받아 사용
    /// </summary>
    public class ITagTestForm : Form
    {
        #region 멤버 변수

        private IITagManager itagManager;  // ITag 관리자 (RealtimeITagControl)
        private ITag m_ITag;                // 공유받은 ITag 인스턴스
        private long m_RegisterCookie;      // 이 폼의 등록 쿠키

        #endregion

        #region UI 컨트롤

        private GroupBox grpConnection;
        private Label lblStatus;
        private TextBox txtConnectionStatus;
        private TextBox txtCyclicStatus;

        private GroupBox grpTagTest;
        private Label lblTagName;
        private TextBox txtTagName;
        private Label lblValue;
        private TextBox txtValue;
        private Button btnRead;
        private Button btnWrite;

        private GroupBox grpCyclicRead;
        private Label lblCycle;
        private TextBox txtCycle;
        private Button btnStartCyclic;
        private Button btnStopCyclic;

        private GroupBox grpLog;
        private TextBox txtLog;
        private Button btnClearLog;

        #endregion

        #region 생성자

        public ITagTestForm(IITagManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager), "ITagManager가 null입니다.");

            itagManager = manager;
            m_ITag = manager.ITag;  // ITag 인스턴스 공유

            InitializeComponent();

            // ITagManager 이벤트 구독
            itagManager.DataChanged += ITagManager_DataChanged;
            itagManager.ConnectionChanged += ITagManager_ConnectionChanged;

            // 초기 상태 표시
            UpdateConnectionStatus();

            LogMessage($"📡 ITag 테스트 폼 초기화 (공유 ITag: {m_ITag != null})");
        }

        #endregion

        #region UI 초기화

        private void InitializeComponent()
        {
            this.Text = "ITag 연결 테스트 (공유 ITag)";
            this.Size = new Size(600, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 20;

            // 연결 상태 그룹
            grpConnection = new GroupBox
            {
                Text = "연결 상태 (공유 ITag)",
                Location = new Point(20, y),
                Size = new Size(540, 100)
            };

            lblStatus = new Label
            {
                Text = "상태:",
                Location = new Point(20, 30),
                Size = new Size(60, 20)
            };

            txtConnectionStatus = new TextBox
            {
                Location = new Point(85, 28),
                Size = new Size(200, 20),
                ReadOnly = true,
                BackColor = Color.LightGray
            };

            txtCyclicStatus = new TextBox
            {
                Location = new Point(300, 28),
                Size = new Size(220, 20),
                ReadOnly = true,
                BackColor = Color.LightGray
            };

            Label lblInfo = new Label
            {
                Text = "※ 메인 폼의 ITag 인스턴스를 공유합니다",
                Location = new Point(20, 60),
                Size = new Size(500, 20),
                ForeColor = Color.Blue
            };

            grpConnection.Controls.AddRange(new Control[] {
                lblStatus, txtConnectionStatus, txtCyclicStatus, lblInfo
            });
            this.Controls.Add(grpConnection);
            y += grpConnection.Height + 20;

            // Tag 테스트 그룹
            grpTagTest = new GroupBox
            {
                Text = "Tag 읽기/쓰기",
                Location = new Point(20, y),
                Size = new Size(540, 120)
            };

            lblTagName = new Label
            {
                Text = "Tag 이름:",
                Location = new Point(20, 30),
                Size = new Size(80, 20)
            };

            txtTagName = new TextBox
            {
                Location = new Point(105, 28),
                Size = new Size(300, 20),
                Text = "HMI_VIEW_X_WCS"
            };

            lblValue = new Label
            {
                Text = "값:",
                Location = new Point(20, 60),
                Size = new Size(80, 20)
            };

            txtValue = new TextBox
            {
                Location = new Point(105, 58),
                Size = new Size(300, 20)
            };

            btnRead = new Button
            {
                Text = "읽기",
                Location = new Point(415, 25),
                Size = new Size(100, 30),
                BackColor = Color.LightBlue
            };
            btnRead.Click += BtnRead_Click;

            btnWrite = new Button
            {
                Text = "쓰기",
                Location = new Point(415, 60),
                Size = new Size(100, 30),
                BackColor = Color.LightYellow
            };
            btnWrite.Click += BtnWrite_Click;

            grpTagTest.Controls.AddRange(new Control[] {
                lblTagName, txtTagName, lblValue, txtValue, btnRead, btnWrite
            });
            this.Controls.Add(grpTagTest);
            y += grpTagTest.Height + 20;

            // 주기적 읽기 그룹
            grpCyclicRead = new GroupBox
            {
                Text = "주기적 읽기 제어 (메인 폼과 공유)",
                Location = new Point(20, y),
                Size = new Size(540, 80)
            };

            lblCycle = new Label
            {
                Text = "주기(ms):",
                Location = new Point(20, 30),
                Size = new Size(80, 20)
            };

            txtCycle = new TextBox
            {
                Location = new Point(105, 28),
                Size = new Size(100, 20),
                Text = "500"
            };

            btnStartCyclic = new Button
            {
                Text = "시작",
                Location = new Point(220, 25),
                Size = new Size(100, 30),
                BackColor = Color.LightGreen
            };
            btnStartCyclic.Click += BtnStartCyclic_Click;

            btnStopCyclic = new Button
            {
                Text = "중지",
                Location = new Point(330, 25),
                Size = new Size(100, 30),
                BackColor = Color.LightCoral
            };
            btnStopCyclic.Click += BtnStopCyclic_Click;

            grpCyclicRead.Controls.AddRange(new Control[] {
                lblCycle, txtCycle, btnStartCyclic, btnStopCyclic
            });
            this.Controls.Add(grpCyclicRead);
            y += grpCyclicRead.Height + 20;

            // 로그 그룹
            grpLog = new GroupBox
            {
                Text = "로그",
                Location = new Point(20, y),
                Size = new Size(540, 200)
            };

            txtLog = new TextBox
            {
                Location = new Point(10, 25),
                Size = new Size(520, 135),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };

            btnClearLog = new Button
            {
                Text = "로그 지우기",
                Location = new Point(415, 165),
                Size = new Size(110, 25)
            };
            btnClearLog.Click += (s, e) => txtLog.Clear();

            grpLog.Controls.AddRange(new Control[] { txtLog, btnClearLog });
            this.Controls.Add(grpLog);

            // Form 닫힐 때 이벤트 구독 해제
            this.FormClosing += (s, e) =>
            {
                if (itagManager != null)
                {
                    itagManager.DataChanged -= ITagManager_DataChanged;
                    itagManager.ConnectionChanged -= ITagManager_ConnectionChanged;
                }
            };
        }

        #endregion

        #region ITagManager 이벤트 핸들러

        private void ITagManager_ConnectionChanged(object sender, bool connected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, bool>(ITagManager_ConnectionChanged), sender, connected);
                return;
            }

            UpdateConnectionStatus();
            LogMessage($"📡 연결 상태 변경: {(connected ? "✅ 연결됨" : "❌ 연결 해제")}");
        }

        private void ITagManager_DataChanged(object sender, TagDataEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, TagDataEventArgs>(ITagManager_DataChanged), sender, e);
                return;
            }

            var data = e.Data;
            LogMessage($"📥 OnDataChanged: X_WCS={data.X_WCS:F3}, Y_WCS={data.Y_WCS:F3}, Part={data.CurrentPart}, Cont={data.CurrentContour}");
        }

        #endregion

        #region 버튼 이벤트 핸들러

        private void BtnRead_Click(object sender, EventArgs e)
        {
            try
            {
                if (!itagManager.IsConnected)
                {
                    MessageBox.Show("먼저 메인 폼에서 ITag 서버에 연결하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string tagName = txtTagName.Text.Trim();
                if (string.IsNullOrEmpty(tagName))
                {
                    MessageBox.Show("Tag 이름을 입력하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 여러 Tag 읽기 (쉼표 또는 세미콜론 구분)
                string[] tagNames = tagName.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(t => t.Trim())
                                           .ToArray();

                if (tagNames.Length == 1)
                {
                    // 단일 Tag 읽기
                    if (itagManager.ReadTag(tagNames[0], out object value))
                    {
                        txtValue.Text = value?.ToString() ?? "NULL";
                        LogMessage($"📖 ReadTag '{tagNames[0]}' = {value}");
                    }
                    else
                    {
                        LogMessage($"❌ ReadTag '{tagNames[0]}' 실패");
                    }
                }
                else
                {
                    // 여러 Tag 읽기
                    if (itagManager.ReadTags(tagNames, out object[] values))
                    {
                        txtValue.Text = values[0]?.ToString() ?? "NULL";
                        for (int i = 0; i < values.Length; i++)
                        {
                            LogMessage($"📖 ReadTag '{tagNames[i]}' = {values[i]}");
                        }
                    }
                    else
                    {
                        LogMessage($"❌ ReadTags 실패");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ ReadTag 오류: {ex.Message}");
                MessageBox.Show($"Tag 읽기 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnWrite_Click(object sender, EventArgs e)
        {
            try
            {
                if (!itagManager.IsConnected)
                {
                    MessageBox.Show("먼저 메인 폼에서 ITag 서버에 연결하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string tagName = txtTagName.Text.Trim();
                string valueStr = txtValue.Text.Trim();

                if (string.IsNullOrEmpty(tagName) || string.IsNullOrEmpty(valueStr))
                {
                    MessageBox.Show("Tag 이름과 값을 입력하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 자동 타입 변환
                object value = valueStr;
                if (int.TryParse(valueStr, out int intVal))
                    value = intVal;
                else if (double.TryParse(valueStr, out double dblVal))
                    value = dblVal;

                if (itagManager.WriteTag(tagName, value))
                {
                    LogMessage($"✍️ WriteTag '{tagName}' = {value}");
                }
                else
                {
                    LogMessage($"❌ WriteTag '{tagName}' 실패");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ WriteTag 오류: {ex.Message}");
                MessageBox.Show($"Tag 쓰기 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStartCyclic_Click(object sender, EventArgs e)
        {
            try
            {
                if (!itagManager.IsConnected)
                {
                    MessageBox.Show("먼저 메인 폼에서 ITag 서버에 연결하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(txtCycle.Text, out int cycleMs) || cycleMs <= 0)
                {
                    MessageBox.Show("올바른 주기(ms)를 입력하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (itagManager.StartCyclicRead(cycleMs))
                {
                    LogMessage($"🔄 주기적 읽기 시작 (14개 Tag, {cycleMs}ms)");
                    UpdateConnectionStatus();
                }
                else
                {
                    LogMessage("❌ 주기적 읽기 시작 실패");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 주기적 읽기 시작 오류: {ex.Message}");
            }
        }

        private void BtnStopCyclic_Click(object sender, EventArgs e)
        {
            try
            {
                itagManager.StopCyclicRead();
                LogMessage("⏸️ 주기적 읽기 중지");
                UpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 주기적 읽기 중지 오류: {ex.Message}");
            }
        }

        #endregion

        #region UI 업데이트

        private void UpdateConnectionStatus()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateConnectionStatus));
                return;
            }

            bool isConnected = itagManager.IsConnected;
            bool isCyclicReading = itagManager.IsCyclicReading;

            // 연결 상태
            if (isConnected)
            {
                txtConnectionStatus.Text = "✅ 연결됨 (공유)";
                txtConnectionStatus.BackColor = Color.LightGreen;
            }
            else
            {
                string errorMsg = itagManager.LastErrorMessage;
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    txtConnectionStatus.Text = $"❌ 연결 안됨: {errorMsg}";
                }
                else
                {
                    txtConnectionStatus.Text = "❌ 연결 안됨";
                }
                txtConnectionStatus.BackColor = Color.LightCoral;
            }

            // 주기 읽기 상태
            if (isCyclicReading)
            {
                txtCyclicStatus.Text = "🔄 주기 읽기 실행 중 (공유)";
                txtCyclicStatus.BackColor = Color.LightGreen;
            }
            else
            {
                txtCyclicStatus.Text = "⏸️ 주기 읽기 중지됨";
                txtCyclicStatus.BackColor = Color.LightGray;
            }
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message);
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            txtLog.AppendText($"[{timestamp}] {message}\r\n");
        }

        #endregion
    }
}
