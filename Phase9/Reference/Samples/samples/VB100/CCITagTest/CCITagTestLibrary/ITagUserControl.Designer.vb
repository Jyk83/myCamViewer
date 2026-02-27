<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ITagUserControl
    Inherits System.Windows.Forms.UserControl

    'UserControl1 überschreibt den Löschvorgang, um die Komponentenliste zu bereinigen.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            '            If disposing AndAlso components IsNot Nothing Then
            '            components.Dispose()
            '           End If
            If disposing Then
                If m_ITag IsNot Nothing Then
                    m_ITag.Unregister(m_RegisterCookie)
                    m_ITag = Nothing
                End If
                If components IsNot Nothing Then
                    components.Dispose()
                End If
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Wird vom Windows Form-Designer benötigt.
    Private components As System.ComponentModel.IContainer

    'Hinweis: Die folgende Prozedur ist für den Windows Form-Designer erforderlich.
    'Das Bearbeiten ist mit dem Windows Form-Designer möglich.  
    'Das Bearbeiten mit dem Code-Editor ist nicht möglich.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox
        Me.radioReadTagCyclic = New System.Windows.Forms.RadioButton
        Me.btnReadTagCyclicCancel = New System.Windows.Forms.Button
        Me.btnReadTagCyclic = New System.Windows.Forms.Button
        Me.txtReadTagCyclicCookie = New System.Windows.Forms.TextBox
        Me.Label3 = New System.Windows.Forms.Label
        Me.txtReadTagCyclicUpdateRate = New System.Windows.Forms.TextBox
        Me.Label2 = New System.Windows.Forms.Label
        Me.Label1 = New System.Windows.Forms.Label
        Me.txtReadTagCyclicTagName = New System.Windows.Forms.TextBox
        Me.GroupBox2 = New System.Windows.Forms.GroupBox
        Me.txtOnDataChagedCookie = New System.Windows.Forms.TextBox
        Me.txtOnDataChangedState = New System.Windows.Forms.TextBox
        Me.txtOnDataChangedValue = New System.Windows.Forms.TextBox
        Me.txtOnDataChangedTagName = New System.Windows.Forms.TextBox
        Me.Label7 = New System.Windows.Forms.Label
        Me.Label6 = New System.Windows.Forms.Label
        Me.Label5 = New System.Windows.Forms.Label
        Me.Label4 = New System.Windows.Forms.Label
        Me.GroupBox3 = New System.Windows.Forms.GroupBox
        Me.txtOnErrorError = New System.Windows.Forms.TextBox
        Me.txtOnErrorCookie = New System.Windows.Forms.TextBox
        Me.txtOnErrorTagName = New System.Windows.Forms.TextBox
        Me.Label10 = New System.Windows.Forms.Label
        Me.Label9 = New System.Windows.Forms.Label
        Me.Label8 = New System.Windows.Forms.Label
        Me.GroupBox4 = New System.Windows.Forms.GroupBox
        Me.txtOnWriteCompleteCookie = New System.Windows.Forms.TextBox
        Me.txtOnWriteCompleteTagName = New System.Windows.Forms.TextBox
        Me.Label12 = New System.Windows.Forms.Label
        Me.Label11 = New System.Windows.Forms.Label
        Me.GroupBox5 = New System.Windows.Forms.GroupBox
        Me.btnReadTagStart = New System.Windows.Forms.Button
        Me.txtReadTagValue = New System.Windows.Forms.TextBox
        Me.txtReadTagTagName = New System.Windows.Forms.TextBox
        Me.Label14 = New System.Windows.Forms.Label
        Me.Label13 = New System.Windows.Forms.Label
        Me.GroupBox6 = New System.Windows.Forms.GroupBox
        Me.btnReadTagAsyncStart = New System.Windows.Forms.Button
        Me.txtReadTagAsyncCache = New System.Windows.Forms.TextBox
        Me.txtReadTagAsyncCookie = New System.Windows.Forms.TextBox
        Me.txtReadTagAsyncTagName = New System.Windows.Forms.TextBox
        Me.Label17 = New System.Windows.Forms.Label
        Me.Label16 = New System.Windows.Forms.Label
        Me.Label15 = New System.Windows.Forms.Label
        Me.GroupBox7 = New System.Windows.Forms.GroupBox
        Me.btnWriteTagStart = New System.Windows.Forms.Button
        Me.txtWriteTagValue = New System.Windows.Forms.TextBox
        Me.txtWriteTagTagName = New System.Windows.Forms.TextBox
        Me.Label19 = New System.Windows.Forms.Label
        Me.Label18 = New System.Windows.Forms.Label
        Me.GroupBox8 = New System.Windows.Forms.GroupBox
        Me.btnWriteTagAsyncStart = New System.Windows.Forms.Button
        Me.txtWriteTagAsyncValue = New System.Windows.Forms.TextBox
        Me.txtWriteTagAsyncCookie = New System.Windows.Forms.TextBox
        Me.txtWriteTagAsyncTagName = New System.Windows.Forms.TextBox
        Me.Label22 = New System.Windows.Forms.Label
        Me.Label21 = New System.Windows.Forms.Label
        Me.Label20 = New System.Windows.Forms.Label
        Me.GroupBox9 = New System.Windows.Forms.GroupBox
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        Me.GroupBox6.SuspendLayout()
        Me.GroupBox7.SuspendLayout()
        Me.GroupBox8.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.radioReadTagCyclic)
        Me.GroupBox1.Controls.Add(Me.btnReadTagCyclicCancel)
        Me.GroupBox1.Controls.Add(Me.btnReadTagCyclic)
        Me.GroupBox1.Controls.Add(Me.txtReadTagCyclicCookie)
        Me.GroupBox1.Controls.Add(Me.Label3)
        Me.GroupBox1.Controls.Add(Me.txtReadTagCyclicUpdateRate)
        Me.GroupBox1.Controls.Add(Me.Label2)
        Me.GroupBox1.Controls.Add(Me.Label1)
        Me.GroupBox1.Controls.Add(Me.txtReadTagCyclicTagName)
        Me.GroupBox1.Location = New System.Drawing.Point(3, 3)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(223, 146)
        Me.GroupBox1.TabIndex = 6
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "ReadTagCyclic"
        '
        'radioReadTagCyclic
        '
        Me.radioReadTagCyclic.AutoSize = True
        Me.radioReadTagCyclic.Enabled = False
        Me.radioReadTagCyclic.Location = New System.Drawing.Point(10, 112)
        Me.radioReadTagCyclic.Name = "radioReadTagCyclic"
        Me.radioReadTagCyclic.Size = New System.Drawing.Size(14, 13)
        Me.radioReadTagCyclic.TabIndex = 8
        Me.radioReadTagCyclic.TabStop = True
        Me.radioReadTagCyclic.UseVisualStyleBackColor = True
        '
        'btnReadTagCyclicCancel
        '
        Me.btnReadTagCyclicCancel.Location = New System.Drawing.Point(110, 107)
        Me.btnReadTagCyclicCancel.Name = "btnReadTagCyclicCancel"
        Me.btnReadTagCyclicCancel.Size = New System.Drawing.Size(63, 22)
        Me.btnReadTagCyclicCancel.TabIndex = 7
        Me.btnReadTagCyclicCancel.Text = "Cancel"
        Me.btnReadTagCyclicCancel.UseVisualStyleBackColor = True
        '
        'btnReadTagCyclic
        '
        Me.btnReadTagCyclic.Location = New System.Drawing.Point(30, 107)
        Me.btnReadTagCyclic.Name = "btnReadTagCyclic"
        Me.btnReadTagCyclic.Size = New System.Drawing.Size(66, 22)
        Me.btnReadTagCyclic.TabIndex = 6
        Me.btnReadTagCyclic.Text = "Start"
        Me.btnReadTagCyclic.UseVisualStyleBackColor = True
        '
        'txtReadTagCyclicCookie
        '
        Me.txtReadTagCyclicCookie.Location = New System.Drawing.Point(98, 75)
        Me.txtReadTagCyclicCookie.Name = "txtReadTagCyclicCookie"
        Me.txtReadTagCyclicCookie.Size = New System.Drawing.Size(109, 20)
        Me.txtReadTagCyclicCookie.TabIndex = 5
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(41, 75)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(40, 13)
        Me.Label3.TabIndex = 4
        Me.Label3.Text = "Cookie"
        '
        'txtReadTagCyclicUpdateRate
        '
        Me.txtReadTagCyclicUpdateRate.Location = New System.Drawing.Point(98, 49)
        Me.txtReadTagCyclicUpdateRate.Name = "txtReadTagCyclicUpdateRate"
        Me.txtReadTagCyclicUpdateRate.Size = New System.Drawing.Size(109, 20)
        Me.txtReadTagCyclicUpdateRate.TabIndex = 3
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(16, 49)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(65, 13)
        Me.Label2.TabIndex = 2
        Me.Label2.Text = "UpdateRate"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(27, 26)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(54, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "TagName"
        '
        'txtReadTagCyclicTagName
        '
        Me.txtReadTagCyclicTagName.Location = New System.Drawing.Point(98, 23)
        Me.txtReadTagCyclicTagName.Name = "txtReadTagCyclicTagName"
        Me.txtReadTagCyclicTagName.Size = New System.Drawing.Size(109, 20)
        Me.txtReadTagCyclicTagName.TabIndex = 0
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.txtOnDataChagedCookie)
        Me.GroupBox2.Controls.Add(Me.txtOnDataChangedState)
        Me.GroupBox2.Controls.Add(Me.txtOnDataChangedValue)
        Me.GroupBox2.Controls.Add(Me.txtOnDataChangedTagName)
        Me.GroupBox2.Controls.Add(Me.Label7)
        Me.GroupBox2.Controls.Add(Me.Label6)
        Me.GroupBox2.Controls.Add(Me.Label5)
        Me.GroupBox2.Controls.Add(Me.Label4)
        Me.GroupBox2.Location = New System.Drawing.Point(3, 323)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(223, 144)
        Me.GroupBox2.TabIndex = 7
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "OnDataChanged"
        '
        'txtOnDataChagedCookie
        '
        Me.txtOnDataChagedCookie.Location = New System.Drawing.Point(86, 57)
        Me.txtOnDataChagedCookie.Name = "txtOnDataChagedCookie"
        Me.txtOnDataChagedCookie.ReadOnly = True
        Me.txtOnDataChagedCookie.Size = New System.Drawing.Size(121, 20)
        Me.txtOnDataChagedCookie.TabIndex = 7
        '
        'txtOnDataChangedState
        '
        Me.txtOnDataChangedState.Location = New System.Drawing.Point(86, 83)
        Me.txtOnDataChangedState.Name = "txtOnDataChangedState"
        Me.txtOnDataChangedState.ReadOnly = True
        Me.txtOnDataChangedState.Size = New System.Drawing.Size(121, 20)
        Me.txtOnDataChangedState.TabIndex = 6
        '
        'txtOnDataChangedValue
        '
        Me.txtOnDataChangedValue.Location = New System.Drawing.Point(86, 109)
        Me.txtOnDataChangedValue.Name = "txtOnDataChangedValue"
        Me.txtOnDataChangedValue.ReadOnly = True
        Me.txtOnDataChangedValue.Size = New System.Drawing.Size(121, 20)
        Me.txtOnDataChangedValue.TabIndex = 5
        '
        'txtOnDataChangedTagName
        '
        Me.txtOnDataChangedTagName.Location = New System.Drawing.Point(86, 28)
        Me.txtOnDataChangedTagName.Name = "txtOnDataChangedTagName"
        Me.txtOnDataChangedTagName.ReadOnly = True
        Me.txtOnDataChangedTagName.Size = New System.Drawing.Size(121, 20)
        Me.txtOnDataChangedTagName.TabIndex = 4
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(30, 57)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(40, 13)
        Me.Label7.TabIndex = 3
        Me.Label7.Text = "Cookie"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(38, 83)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(32, 13)
        Me.Label6.TabIndex = 2
        Me.Label6.Text = "State"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(36, 110)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(34, 13)
        Me.Label5.TabIndex = 1
        Me.Label5.Text = "Value"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(16, 28)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(54, 13)
        Me.Label4.TabIndex = 0
        Me.Label4.Text = "TagName"
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.txtOnErrorError)
        Me.GroupBox3.Controls.Add(Me.txtOnErrorCookie)
        Me.GroupBox3.Controls.Add(Me.txtOnErrorTagName)
        Me.GroupBox3.Controls.Add(Me.Label10)
        Me.GroupBox3.Controls.Add(Me.Label9)
        Me.GroupBox3.Controls.Add(Me.Label8)
        Me.GroupBox3.Location = New System.Drawing.Point(232, 323)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(192, 144)
        Me.GroupBox3.TabIndex = 8
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "OnError"
        '
        'txtOnErrorError
        '
        Me.txtOnErrorError.Location = New System.Drawing.Point(67, 81)
        Me.txtOnErrorError.Name = "txtOnErrorError"
        Me.txtOnErrorError.ReadOnly = True
        Me.txtOnErrorError.Size = New System.Drawing.Size(115, 20)
        Me.txtOnErrorError.TabIndex = 5
        '
        'txtOnErrorCookie
        '
        Me.txtOnErrorCookie.Location = New System.Drawing.Point(67, 54)
        Me.txtOnErrorCookie.Name = "txtOnErrorCookie"
        Me.txtOnErrorCookie.ReadOnly = True
        Me.txtOnErrorCookie.Size = New System.Drawing.Size(115, 20)
        Me.txtOnErrorCookie.TabIndex = 4
        '
        'txtOnErrorTagName
        '
        Me.txtOnErrorTagName.Location = New System.Drawing.Point(67, 26)
        Me.txtOnErrorTagName.Name = "txtOnErrorTagName"
        Me.txtOnErrorTagName.ReadOnly = True
        Me.txtOnErrorTagName.Size = New System.Drawing.Size(115, 20)
        Me.txtOnErrorTagName.TabIndex = 3
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(32, 83)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(29, 13)
        Me.Label10.TabIndex = 2
        Me.Label10.Text = "Error"
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(21, 57)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(40, 13)
        Me.Label9.TabIndex = 1
        Me.Label9.Text = "Cookie"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(7, 29)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(54, 13)
        Me.Label8.TabIndex = 0
        Me.Label8.Text = "TagName"
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.txtOnWriteCompleteCookie)
        Me.GroupBox4.Controls.Add(Me.txtOnWriteCompleteTagName)
        Me.GroupBox4.Controls.Add(Me.Label12)
        Me.GroupBox4.Controls.Add(Me.Label11)
        Me.GroupBox4.Location = New System.Drawing.Point(431, 323)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(198, 144)
        Me.GroupBox4.TabIndex = 9
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "OnWriteComplete"
        '
        'txtOnWriteCompleteCookie
        '
        Me.txtOnWriteCompleteCookie.Location = New System.Drawing.Point(78, 57)
        Me.txtOnWriteCompleteCookie.Name = "txtOnWriteCompleteCookie"
        Me.txtOnWriteCompleteCookie.ReadOnly = True
        Me.txtOnWriteCompleteCookie.Size = New System.Drawing.Size(107, 20)
        Me.txtOnWriteCompleteCookie.TabIndex = 3
        '
        'txtOnWriteCompleteTagName
        '
        Me.txtOnWriteCompleteTagName.Location = New System.Drawing.Point(78, 26)
        Me.txtOnWriteCompleteTagName.Name = "txtOnWriteCompleteTagName"
        Me.txtOnWriteCompleteTagName.ReadOnly = True
        Me.txtOnWriteCompleteTagName.Size = New System.Drawing.Size(107, 20)
        Me.txtOnWriteCompleteTagName.TabIndex = 2
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(32, 57)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(40, 13)
        Me.Label12.TabIndex = 1
        Me.Label12.Text = "Cookie"
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(18, 28)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(54, 13)
        Me.Label11.TabIndex = 0
        Me.Label11.Text = "TagName"
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.btnReadTagStart)
        Me.GroupBox5.Controls.Add(Me.txtReadTagValue)
        Me.GroupBox5.Controls.Add(Me.txtReadTagTagName)
        Me.GroupBox5.Controls.Add(Me.Label14)
        Me.GroupBox5.Controls.Add(Me.Label13)
        Me.GroupBox5.Location = New System.Drawing.Point(232, 6)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(200, 143)
        Me.GroupBox5.TabIndex = 10
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "ReadTag"
        '
        'btnReadTagStart
        '
        Me.btnReadTagStart.Location = New System.Drawing.Point(15, 104)
        Me.btnReadTagStart.Name = "btnReadTagStart"
        Me.btnReadTagStart.Size = New System.Drawing.Size(59, 22)
        Me.btnReadTagStart.TabIndex = 4
        Me.btnReadTagStart.Text = "Start"
        Me.btnReadTagStart.UseVisualStyleBackColor = True
        '
        'txtReadTagValue
        '
        Me.txtReadTagValue.Location = New System.Drawing.Point(72, 46)
        Me.txtReadTagValue.Name = "txtReadTagValue"
        Me.txtReadTagValue.ReadOnly = True
        Me.txtReadTagValue.Size = New System.Drawing.Size(110, 20)
        Me.txtReadTagValue.TabIndex = 3
        '
        'txtReadTagTagName
        '
        Me.txtReadTagTagName.Location = New System.Drawing.Point(72, 20)
        Me.txtReadTagTagName.Name = "txtReadTagTagName"
        Me.txtReadTagTagName.Size = New System.Drawing.Size(110, 20)
        Me.txtReadTagTagName.TabIndex = 2
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.Location = New System.Drawing.Point(32, 51)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(34, 13)
        Me.Label14.TabIndex = 1
        Me.Label14.Text = "Value"
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Location = New System.Drawing.Point(12, 26)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(54, 13)
        Me.Label13.TabIndex = 0
        Me.Label13.Text = "TagName"
        '
        'GroupBox6
        '
        Me.GroupBox6.Controls.Add(Me.btnReadTagAsyncStart)
        Me.GroupBox6.Controls.Add(Me.txtReadTagAsyncCache)
        Me.GroupBox6.Controls.Add(Me.txtReadTagAsyncCookie)
        Me.GroupBox6.Controls.Add(Me.txtReadTagAsyncTagName)
        Me.GroupBox6.Controls.Add(Me.Label17)
        Me.GroupBox6.Controls.Add(Me.Label16)
        Me.GroupBox6.Controls.Add(Me.Label15)
        Me.GroupBox6.Location = New System.Drawing.Point(438, 6)
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Size = New System.Drawing.Size(191, 143)
        Me.GroupBox6.TabIndex = 11
        Me.GroupBox6.TabStop = False
        Me.GroupBox6.Text = "ReadTagAsync"
        '
        'btnReadTagAsyncStart
        '
        Me.btnReadTagAsyncStart.Location = New System.Drawing.Point(12, 104)
        Me.btnReadTagAsyncStart.Name = "btnReadTagAsyncStart"
        Me.btnReadTagAsyncStart.Size = New System.Drawing.Size(59, 22)
        Me.btnReadTagAsyncStart.TabIndex = 6
        Me.btnReadTagAsyncStart.Text = "Start"
        Me.btnReadTagAsyncStart.UseVisualStyleBackColor = True
        '
        'txtReadTagAsyncCache
        '
        Me.txtReadTagAsyncCache.Location = New System.Drawing.Point(69, 75)
        Me.txtReadTagAsyncCache.Name = "txtReadTagAsyncCache"
        Me.txtReadTagAsyncCache.Size = New System.Drawing.Size(110, 20)
        Me.txtReadTagAsyncCache.TabIndex = 5
        '
        'txtReadTagAsyncCookie
        '
        Me.txtReadTagAsyncCookie.Location = New System.Drawing.Point(69, 48)
        Me.txtReadTagAsyncCookie.Name = "txtReadTagAsyncCookie"
        Me.txtReadTagAsyncCookie.Size = New System.Drawing.Size(110, 20)
        Me.txtReadTagAsyncCookie.TabIndex = 4
        '
        'txtReadTagAsyncTagName
        '
        Me.txtReadTagAsyncTagName.Location = New System.Drawing.Point(69, 23)
        Me.txtReadTagAsyncTagName.Name = "txtReadTagAsyncTagName"
        Me.txtReadTagAsyncTagName.Size = New System.Drawing.Size(110, 20)
        Me.txtReadTagAsyncTagName.TabIndex = 3
        '
        'Label17
        '
        Me.Label17.AutoSize = True
        Me.Label17.Location = New System.Drawing.Point(25, 78)
        Me.Label17.Name = "Label17"
        Me.Label17.Size = New System.Drawing.Size(38, 13)
        Me.Label17.TabIndex = 2
        Me.Label17.Text = "Cache"
        '
        'Label16
        '
        Me.Label16.AutoSize = True
        Me.Label16.Location = New System.Drawing.Point(23, 51)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(40, 13)
        Me.Label16.TabIndex = 1
        Me.Label16.Text = "Cookie"
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.Location = New System.Drawing.Point(9, 29)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(54, 13)
        Me.Label15.TabIndex = 0
        Me.Label15.Text = "TagName"
        '
        'GroupBox7
        '
        Me.GroupBox7.Controls.Add(Me.btnWriteTagStart)
        Me.GroupBox7.Controls.Add(Me.txtWriteTagValue)
        Me.GroupBox7.Controls.Add(Me.txtWriteTagTagName)
        Me.GroupBox7.Controls.Add(Me.Label19)
        Me.GroupBox7.Controls.Add(Me.Label18)
        Me.GroupBox7.Location = New System.Drawing.Point(232, 155)
        Me.GroupBox7.Name = "GroupBox7"
        Me.GroupBox7.Size = New System.Drawing.Size(200, 139)
        Me.GroupBox7.TabIndex = 12
        Me.GroupBox7.TabStop = False
        Me.GroupBox7.Text = "WriteTag"
        '
        'btnWriteTagStart
        '
        Me.btnWriteTagStart.Location = New System.Drawing.Point(15, 100)
        Me.btnWriteTagStart.Name = "btnWriteTagStart"
        Me.btnWriteTagStart.Size = New System.Drawing.Size(64, 23)
        Me.btnWriteTagStart.TabIndex = 4
        Me.btnWriteTagStart.Text = "Start"
        Me.btnWriteTagStart.UseVisualStyleBackColor = True
        '
        'txtWriteTagValue
        '
        Me.txtWriteTagValue.Location = New System.Drawing.Point(73, 48)
        Me.txtWriteTagValue.Name = "txtWriteTagValue"
        Me.txtWriteTagValue.Size = New System.Drawing.Size(109, 20)
        Me.txtWriteTagValue.TabIndex = 3
        '
        'txtWriteTagTagName
        '
        Me.txtWriteTagTagName.Location = New System.Drawing.Point(73, 22)
        Me.txtWriteTagTagName.Name = "txtWriteTagTagName"
        Me.txtWriteTagTagName.Size = New System.Drawing.Size(109, 20)
        Me.txtWriteTagTagName.TabIndex = 2
        '
        'Label19
        '
        Me.Label19.AutoSize = True
        Me.Label19.Location = New System.Drawing.Point(29, 52)
        Me.Label19.Name = "Label19"
        Me.Label19.Size = New System.Drawing.Size(34, 13)
        Me.Label19.TabIndex = 1
        Me.Label19.Text = "Value"
        '
        'Label18
        '
        Me.Label18.AutoSize = True
        Me.Label18.Location = New System.Drawing.Point(11, 26)
        Me.Label18.Name = "Label18"
        Me.Label18.Size = New System.Drawing.Size(54, 13)
        Me.Label18.TabIndex = 0
        Me.Label18.Text = "TagName"
        '
        'GroupBox8
        '
        Me.GroupBox8.Controls.Add(Me.btnWriteTagAsyncStart)
        Me.GroupBox8.Controls.Add(Me.txtWriteTagAsyncValue)
        Me.GroupBox8.Controls.Add(Me.txtWriteTagAsyncCookie)
        Me.GroupBox8.Controls.Add(Me.txtWriteTagAsyncTagName)
        Me.GroupBox8.Controls.Add(Me.Label22)
        Me.GroupBox8.Controls.Add(Me.Label21)
        Me.GroupBox8.Controls.Add(Me.Label20)
        Me.GroupBox8.Location = New System.Drawing.Point(438, 155)
        Me.GroupBox8.Name = "GroupBox8"
        Me.GroupBox8.Size = New System.Drawing.Size(191, 139)
        Me.GroupBox8.TabIndex = 13
        Me.GroupBox8.TabStop = False
        Me.GroupBox8.Text = "WriteTagAsync"
        '
        'btnWriteTagAsyncStart
        '
        Me.btnWriteTagAsyncStart.Location = New System.Drawing.Point(14, 100)
        Me.btnWriteTagAsyncStart.Name = "btnWriteTagAsyncStart"
        Me.btnWriteTagAsyncStart.Size = New System.Drawing.Size(64, 23)
        Me.btnWriteTagAsyncStart.TabIndex = 6
        Me.btnWriteTagAsyncStart.Text = "Start"
        Me.btnWriteTagAsyncStart.UseVisualStyleBackColor = True
        '
        'txtWriteTagAsyncValue
        '
        Me.txtWriteTagAsyncValue.Location = New System.Drawing.Point(73, 71)
        Me.txtWriteTagAsyncValue.Name = "txtWriteTagAsyncValue"
        Me.txtWriteTagAsyncValue.Size = New System.Drawing.Size(106, 20)
        Me.txtWriteTagAsyncValue.TabIndex = 5
        '
        'txtWriteTagAsyncCookie
        '
        Me.txtWriteTagAsyncCookie.Location = New System.Drawing.Point(72, 45)
        Me.txtWriteTagAsyncCookie.Name = "txtWriteTagAsyncCookie"
        Me.txtWriteTagAsyncCookie.Size = New System.Drawing.Size(107, 20)
        Me.txtWriteTagAsyncCookie.TabIndex = 4
        '
        'txtWriteTagAsyncTagName
        '
        Me.txtWriteTagAsyncTagName.Location = New System.Drawing.Point(71, 19)
        Me.txtWriteTagAsyncTagName.Name = "txtWriteTagAsyncTagName"
        Me.txtWriteTagAsyncTagName.Size = New System.Drawing.Size(108, 20)
        Me.txtWriteTagAsyncTagName.TabIndex = 3
        '
        'Label22
        '
        Me.Label22.AutoSize = True
        Me.Label22.Location = New System.Drawing.Point(31, 71)
        Me.Label22.Name = "Label22"
        Me.Label22.Size = New System.Drawing.Size(34, 13)
        Me.Label22.TabIndex = 2
        Me.Label22.Text = "Value"
        '
        'Label21
        '
        Me.Label21.AutoSize = True
        Me.Label21.Location = New System.Drawing.Point(25, 48)
        Me.Label21.Name = "Label21"
        Me.Label21.Size = New System.Drawing.Size(40, 13)
        Me.Label21.TabIndex = 1
        Me.Label21.Text = "Cookie"
        '
        'Label20
        '
        Me.Label20.AutoSize = True
        Me.Label20.Location = New System.Drawing.Point(11, 22)
        Me.Label20.Name = "Label20"
        Me.Label20.Size = New System.Drawing.Size(54, 13)
        Me.Label20.TabIndex = 0
        Me.Label20.Text = "TagName"
        '
        'GroupBox9
        '
        Me.GroupBox9.Location = New System.Drawing.Point(8, 304)
        Me.GroupBox9.Name = "GroupBox9"
        Me.GroupBox9.Size = New System.Drawing.Size(623, 8)
        Me.GroupBox9.TabIndex = 14
        Me.GroupBox9.TabStop = False
        '
        'ITagUserControl
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.GroupBox9)
        Me.Controls.Add(Me.GroupBox8)
        Me.Controls.Add(Me.GroupBox7)
        Me.Controls.Add(Me.GroupBox6)
        Me.Controls.Add(Me.GroupBox5)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.Name = "ITagUserControl"
        Me.Size = New System.Drawing.Size(635, 478)
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.GroupBox6.ResumeLayout(False)
        Me.GroupBox6.PerformLayout()
        Me.GroupBox7.ResumeLayout(False)
        Me.GroupBox7.PerformLayout()
        Me.GroupBox8.ResumeLayout(False)
        Me.GroupBox8.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents txtReadTagCyclicCookie As System.Windows.Forms.TextBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents txtReadTagCyclicUpdateRate As System.Windows.Forms.TextBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents txtReadTagCyclicTagName As System.Windows.Forms.TextBox
    Friend WithEvents btnReadTagCyclicCancel As System.Windows.Forms.Button
    Friend WithEvents btnReadTagCyclic As System.Windows.Forms.Button
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents txtOnDataChangedTagName As System.Windows.Forms.TextBox
    Friend WithEvents txtOnDataChagedCookie As System.Windows.Forms.TextBox
    Friend WithEvents txtOnDataChangedState As System.Windows.Forms.TextBox
    Friend WithEvents txtOnDataChangedValue As System.Windows.Forms.TextBox
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents txtOnErrorCookie As System.Windows.Forms.TextBox
    Friend WithEvents txtOnErrorTagName As System.Windows.Forms.TextBox
    Friend WithEvents Label10 As System.Windows.Forms.Label
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents txtOnErrorError As System.Windows.Forms.TextBox
    Friend WithEvents GroupBox4 As System.Windows.Forms.GroupBox
    Friend WithEvents txtOnWriteCompleteCookie As System.Windows.Forms.TextBox
    Friend WithEvents txtOnWriteCompleteTagName As System.Windows.Forms.TextBox
    Friend WithEvents Label12 As System.Windows.Forms.Label
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents GroupBox5 As System.Windows.Forms.GroupBox
    Friend WithEvents btnReadTagStart As System.Windows.Forms.Button
    Friend WithEvents txtReadTagValue As System.Windows.Forms.TextBox
    Friend WithEvents txtReadTagTagName As System.Windows.Forms.TextBox
    Friend WithEvents Label14 As System.Windows.Forms.Label
    Friend WithEvents Label13 As System.Windows.Forms.Label
    Friend WithEvents GroupBox6 As System.Windows.Forms.GroupBox
    Friend WithEvents txtReadTagAsyncCookie As System.Windows.Forms.TextBox
    Friend WithEvents txtReadTagAsyncTagName As System.Windows.Forms.TextBox
    Friend WithEvents Label17 As System.Windows.Forms.Label
    Friend WithEvents Label16 As System.Windows.Forms.Label
    Friend WithEvents Label15 As System.Windows.Forms.Label
    Friend WithEvents btnReadTagAsyncStart As System.Windows.Forms.Button
    Friend WithEvents txtReadTagAsyncCache As System.Windows.Forms.TextBox
    Friend WithEvents GroupBox7 As System.Windows.Forms.GroupBox
    Friend WithEvents btnWriteTagStart As System.Windows.Forms.Button
    Friend WithEvents txtWriteTagValue As System.Windows.Forms.TextBox
    Friend WithEvents txtWriteTagTagName As System.Windows.Forms.TextBox
    Friend WithEvents Label19 As System.Windows.Forms.Label
    Friend WithEvents Label18 As System.Windows.Forms.Label
    Friend WithEvents GroupBox8 As System.Windows.Forms.GroupBox
    Friend WithEvents txtWriteTagAsyncTagName As System.Windows.Forms.TextBox
    Friend WithEvents Label22 As System.Windows.Forms.Label
    Friend WithEvents Label21 As System.Windows.Forms.Label
    Friend WithEvents Label20 As System.Windows.Forms.Label
    Friend WithEvents txtWriteTagAsyncValue As System.Windows.Forms.TextBox
    Friend WithEvents txtWriteTagAsyncCookie As System.Windows.Forms.TextBox
    Friend WithEvents btnWriteTagAsyncStart As System.Windows.Forms.Button
    Friend WithEvents radioReadTagCyclic As System.Windows.Forms.RadioButton
    Friend WithEvents GroupBox9 As System.Windows.Forms.GroupBox

End Class
