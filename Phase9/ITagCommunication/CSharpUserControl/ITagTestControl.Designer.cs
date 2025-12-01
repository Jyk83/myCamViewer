namespace ITagTestControl
{
    partial class ITagTestControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (m_ITag != null && isConnected)
                {
                    // 주기적 읽기 중이면 취소
                    if (isCyclicReading)
                    {
                        m_ITag.Cancel(checked((int)this.m_RegisterCookie));
                    }
                    
                    m_ITag.Unregister(checked((int)this.m_RegisterCookie));
                    m_ITag = null;
                }
            }
            catch
            {
                // Unregister 실패 무시
            }

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpConnection = new System.Windows.Forms.GroupBox();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.grpTagOperation = new System.Windows.Forms.GroupBox();
            this.btnWrite = new System.Windows.Forms.Button();
            this.btnRead = new System.Windows.Forms.Button();
            this.txtValue = new System.Windows.Forms.TextBox();
            this.lblValue = new System.Windows.Forms.Label();
            this.txtTagName = new System.Windows.Forms.TextBox();
            this.lblTagName = new System.Windows.Forms.Label();
            this.grpCyclicRead = new System.Windows.Forms.GroupBox();
            this.lblCyclicStatus = new System.Windows.Forms.Label();
            this.btnStopCyclic = new System.Windows.Forms.Button();
            this.btnStartCyclic = new System.Windows.Forms.Button();
            this.txtCycle = new System.Windows.Forms.TextBox();
            this.lblCycle = new System.Windows.Forms.Label();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.grpConnection.SuspendLayout();
            this.grpTagOperation.SuspendLayout();
            this.grpCyclicRead.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpConnection
            // 
            this.grpConnection.Controls.Add(this.btnDisconnect);
            this.grpConnection.Controls.Add(this.lblStatus);
            this.grpConnection.Controls.Add(this.btnConnect);
            this.grpConnection.Location = new System.Drawing.Point(12, 9);
            this.grpConnection.Name = "grpConnection";
            this.grpConnection.Size = new System.Drawing.Size(560, 65);
            this.grpConnection.TabIndex = 0;
            this.grpConnection.TabStop = false;
            this.grpConnection.Text = "Connection";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(146, 23);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(117, 28);
            this.btnDisconnect.TabIndex = 2;
            this.btnDisconnect.Text = "연결 해제";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Red;
            this.lblStatus.Location = new System.Drawing.Point(290, 30);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(82, 12);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Disconnected";
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(23, 23);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(117, 28);
            this.btnConnect.TabIndex = 0;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // grpTagOperation
            // 
            this.grpTagOperation.Controls.Add(this.btnWrite);
            this.grpTagOperation.Controls.Add(this.btnRead);
            this.grpTagOperation.Controls.Add(this.txtValue);
            this.grpTagOperation.Controls.Add(this.lblValue);
            this.grpTagOperation.Controls.Add(this.txtTagName);
            this.grpTagOperation.Controls.Add(this.lblTagName);
            this.grpTagOperation.Location = new System.Drawing.Point(12, 83);
            this.grpTagOperation.Name = "grpTagOperation";
            this.grpTagOperation.Size = new System.Drawing.Size(560, 111);
            this.grpTagOperation.TabIndex = 1;
            this.grpTagOperation.TabStop = false;
            this.grpTagOperation.Text = "Tag Operation (Single Read/Write)";
            // 
            // btnWrite
            // 
            this.btnWrite.Location = new System.Drawing.Point(233, 78);
            this.btnWrite.Name = "btnWrite";
            this.btnWrite.Size = new System.Drawing.Size(117, 23);
            this.btnWrite.TabIndex = 5;
            this.btnWrite.Text = "Write";
            this.btnWrite.UseVisualStyleBackColor = true;
            this.btnWrite.Click += new System.EventHandler(this.btnWrite_Click);
            // 
            // btnRead
            // 
            this.btnRead.Location = new System.Drawing.Point(105, 78);
            this.btnRead.Name = "btnRead";
            this.btnRead.Size = new System.Drawing.Size(117, 23);
            this.btnRead.TabIndex = 4;
            this.btnRead.Text = "Read";
            this.btnRead.UseVisualStyleBackColor = true;
            this.btnRead.Click += new System.EventHandler(this.btnRead_Click);
            // 
            // txtValue
            // 
            this.txtValue.Location = new System.Drawing.Point(105, 51);
            this.txtValue.Name = "txtValue";
            this.txtValue.Size = new System.Drawing.Size(431, 21);
            this.txtValue.TabIndex = 3;
            this.txtValue.Text = "0";
            // 
            // lblValue
            // 
            this.lblValue.AutoSize = true;
            this.lblValue.Location = new System.Drawing.Point(23, 54);
            this.lblValue.Name = "lblValue";
            this.lblValue.Size = new System.Drawing.Size(37, 12);
            this.lblValue.TabIndex = 2;
            this.lblValue.Text = "Value";
            // 
            // txtTagName
            // 
            this.txtTagName.Location = new System.Drawing.Point(105, 23);
            this.txtTagName.Name = "txtTagName";
            this.txtTagName.Size = new System.Drawing.Size(431, 21);
            this.txtTagName.TabIndex = 1;
            this.txtTagName.Text = "TestTag1,TestTag2,TestTag3";
            // 
            // lblTagName
            // 
            this.lblTagName.AutoSize = true;
            this.lblTagName.Location = new System.Drawing.Point(23, 26);
            this.lblTagName.Name = "lblTagName";
            this.lblTagName.Size = new System.Drawing.Size(65, 12);
            this.lblTagName.TabIndex = 0;
            this.lblTagName.Text = "Tag Names (쉼표 구분)";
            // 
            // grpCyclicRead
            // 
            this.grpCyclicRead.Controls.Add(this.lblCyclicStatus);
            this.grpCyclicRead.Controls.Add(this.btnStopCyclic);
            this.grpCyclicRead.Controls.Add(this.btnStartCyclic);
            this.grpCyclicRead.Controls.Add(this.txtCycle);
            this.grpCyclicRead.Controls.Add(this.lblCycle);
            this.grpCyclicRead.Location = new System.Drawing.Point(12, 203);
            this.grpCyclicRead.Name = "grpCyclicRead";
            this.grpCyclicRead.Size = new System.Drawing.Size(560, 85);
            this.grpCyclicRead.TabIndex = 2;
            this.grpCyclicRead.TabStop = false;
            this.grpCyclicRead.Text = "주기적 읽기 (값 변경 시 자동 업데이트)";
            // 
            // lblCyclicStatus
            // 
            this.lblCyclicStatus.AutoSize = true;
            this.lblCyclicStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblCyclicStatus.Location = new System.Drawing.Point(370, 57);
            this.lblCyclicStatus.Name = "lblCyclicStatus";
            this.lblCyclicStatus.Size = new System.Drawing.Size(52, 12);
            this.lblCyclicStatus.TabIndex = 4;
            this.lblCyclicStatus.Text = "Stopped";
            // 
            // btnStopCyclic
            // 
            this.btnStopCyclic.Enabled = false;
            this.btnStopCyclic.Location = new System.Drawing.Point(233, 52);
            this.btnStopCyclic.Name = "btnStopCyclic";
            this.btnStopCyclic.Size = new System.Drawing.Size(117, 23);
            this.btnStopCyclic.TabIndex = 3;
            this.btnStopCyclic.Text = "중지";
            this.btnStopCyclic.UseVisualStyleBackColor = true;
            this.btnStopCyclic.Click += new System.EventHandler(this.btnStopCyclic_Click);
            // 
            // btnStartCyclic
            // 
            this.btnStartCyclic.Enabled = false;
            this.btnStartCyclic.Location = new System.Drawing.Point(105, 52);
            this.btnStartCyclic.Name = "btnStartCyclic";
            this.btnStartCyclic.Size = new System.Drawing.Size(117, 23);
            this.btnStartCyclic.TabIndex = 2;
            this.btnStartCyclic.Text = "시작";
            this.btnStartCyclic.UseVisualStyleBackColor = true;
            this.btnStartCyclic.Click += new System.EventHandler(this.btnStartCyclic_Click);
            // 
            // txtCycle
            // 
            this.txtCycle.Location = new System.Drawing.Point(105, 23);
            this.txtCycle.Name = "txtCycle";
            this.txtCycle.Size = new System.Drawing.Size(117, 21);
            this.txtCycle.TabIndex = 1;
            this.txtCycle.Text = "500";
            // 
            // lblCycle
            // 
            this.lblCycle.AutoSize = true;
            this.lblCycle.Location = new System.Drawing.Point(23, 26);
            this.lblCycle.Name = "lblCycle";
            this.lblCycle.Size = new System.Drawing.Size(64, 12);
            this.lblCycle.TabIndex = 0;
            this.lblCycle.Text = "Cycle (ms)";
            // 
            // grpLog
            // 
            this.grpLog.Controls.Add(this.btnClearLog);
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Location = new System.Drawing.Point(12, 297);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(560, 212);
            this.grpLog.TabIndex = 3;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "Log";
            // 
            // btnClearLog
            // 
            this.btnClearLog.Location = new System.Drawing.Point(420, 180);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(117, 23);
            this.btnClearLog.TabIndex = 1;
            this.btnClearLog.Text = "Clear Log";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(23, 18);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(513, 157);
            this.txtLog.TabIndex = 0;
            // 
            // ITagTestControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpLog);
            this.Controls.Add(this.grpCyclicRead);
            this.Controls.Add(this.grpTagOperation);
            this.Controls.Add(this.grpConnection);
            this.Name = "ITagTestControl";
            this.Size = new System.Drawing.Size(583, 520);
            this.Load += new System.EventHandler(this.ITagTestControl_Load);
            this.grpConnection.ResumeLayout(false);
            this.grpConnection.PerformLayout();
            this.grpTagOperation.ResumeLayout(false);
            this.grpTagOperation.PerformLayout();
            this.grpCyclicRead.ResumeLayout(false);
            this.grpCyclicRead.PerformLayout();
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpConnection;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.GroupBox grpTagOperation;
        private System.Windows.Forms.Label lblTagName;
        private System.Windows.Forms.TextBox txtTagName;
        private System.Windows.Forms.Label lblValue;
        private System.Windows.Forms.TextBox txtValue;
        private System.Windows.Forms.Button btnRead;
        private System.Windows.Forms.Button btnWrite;
        private System.Windows.Forms.GroupBox grpCyclicRead;
        private System.Windows.Forms.Button btnStartCyclic;
        private System.Windows.Forms.Button btnStopCyclic;
        private System.Windows.Forms.TextBox txtCycle;
        private System.Windows.Forms.Label lblCycle;
        private System.Windows.Forms.Label lblCyclicStatus;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnClearLog;
    }
}
