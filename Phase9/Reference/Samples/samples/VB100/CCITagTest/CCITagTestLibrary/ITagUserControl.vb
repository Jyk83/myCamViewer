Option Explicit On

Public Class ITagUserControl : Implements ITagSink
    Dim m_Tagname As String ' tag name
    Dim m_Delimiter As String ' Delimiter between the tag name and the tag member 
    Dim m_Active As Boolean ' Activate/deActivate the Tags connection
    Dim m_RegisterCookie As Long

    Dim ActualViewFlag As Integer ' local variable to hold the actual view flag
    Dim ActualViewFrame As Integer ' local variable to hold the actual view frame displayed 

    ' ITag interface
    Dim m_ITag As ITag

    ' ITagSink implementation
    Sub OnCanceled(ByVal RegisterCookie As Integer) Implements ITagSink.OnCanceled
        Trace.WriteLine("OnCanceled called")
    End Sub

    Sub OnDataChanged(ByVal RegisterCookie As Integer, ByVal TagNames As Object, ByVal Values As Object, ByVal Qualities As Object, ByVal VarStates As Object, ByVal TimeStamps As Object, ByVal Cookies As Object) Implements ITagSink.OnDataChanged
        Trace.WriteLine("OnDataChanged called")

        Dim i As Integer
        For i = 0 To TagNames.Length() - 1
            txtOnDataChangedTagName.Text = TagNames(i)
            txtOnDataChangedValue.Text = Values(i)
            txtOnDataChangedState.Text = GetVarStateText(VarStates(i))
            txtOnDataChagedCookie.Text = Cookies(i)
        Next
    End Sub

    Sub OnError(ByVal RegisterCookie As Integer, ByVal TagNames As Object, ByVal Cookies As Object, ByVal Errors As Object) Implements ITagSink.OnError
        Trace.WriteLine("OnError called")

        Dim i As Integer
        For i = 0 To TagNames.Length() - 1
            txtOnErrorTagName.Text = TagNames(i)
            txtOnErrorCookie.Text = Cookies(i)
            txtOnErrorError.Text = GetErrorText(Errors(i))
        Next
    End Sub

    Public Sub OnRemoved(ByVal RegisterCookie As Integer, ByVal Cookies As Object) Implements ITagSink.OnRemoved
        Trace.WriteLine("OnRemoved called")
    End Sub

    Sub OnWriteComplete(ByVal RegisterCookie As Integer, ByVal TagNames As Object, ByVal Cookies As Object) Implements ITagSink.OnWriteComplete
        Trace.WriteLine("OnWriteComplete called")

        Dim i As Integer
        For i = 0 To TagNames.Length() - 1
            txtOnWriteCompleteTagName.Text = TagNames(i)
            txtOnWriteCompleteCookie.Text = Cookies(i)
        Next
    End Sub

    Private Sub ITagUserControl_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Disposed
        On Error Resume Next
        ' You must always Close the Server when terminating  
        If m_ITag IsNot Nothing Then
            m_ITag.Unregister(m_RegisterCookie)
            m_ITag = Nothing
        End If
    End Sub

    Private Sub ITagUserControl_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        m_Delimiter = "."
        m_Active = True
        ActualViewFlag = 1

        ' Create the Tag Service
        ChangeTagServer(False)
    End Sub

    ' TagsDescriptor
    Dim TagsDescr As Object

    Enum TDP 'TagDescrPosition
        tdTagname = 0
        tdTagType = 1
        tdCycle = 2
        tdDefaultValue = 3
        tdActualValue = 4
        tdQuality = 5
        tdViewFlags = 6
        tdCookie = 7
    End Enum
  
    Private Sub UpdateTag(ByVal Cookie As Integer, ByVal pVar As Object)
        Dim prefix As String

        If m_ITag IsNot Nothing Then
            On Error Resume Next
            ' get the tag prefix
            prefix = GetPrefix()
            ' update the tag
            m_ITag.WriteTag(m_RegisterCookie, prefix + TagsDescr(Cookie)(TDP.tdTagname), pVar)
        End If
    End Sub

    Private Function GetPrefix() As String
        Dim delimit As String
        If Not (m_Tagname = "") Then
            ' default delimiter is "."
            delimit = IIf(m_Delimiter = "", ".", m_Delimiter)
            GetPrefix = m_Tagname & delimit
        Else
            GetPrefix = ""
        End If
    End Function
  
    Private Sub AddItemsToGroup()
        Dim prefix As String

        If TagsDescr.Length() < 1 Then Exit Sub

        Dim nTagSize As Integer = TagsDescr.Length() - 1
        Dim TagNames(nTagSize) As Object
        Dim TagCycles(nTagSize) As Object
        Dim TagCookies(nTagSize) As Object
        Dim ServerCookies(nTagSize) As Object
        Dim i As Integer = 0

        ' get the tag prefix
        prefix = GetPrefix()

        ' Add all items for the specific view
        Dim count As Integer
        For count = LBound(TagsDescr) To UBound(TagsDescr)
            If ActualViewFlag And TagsDescr(count)(TDP.tdViewFlags) Then

                ' Read tags step by step
                'm_ITag.ReadTagCyclic(m_RegisterCookie, prefix + TagsDescr(count)(TDP.tdTagname), TagsDescr(count)(TDP.tdCycle), count)

                TagNames(i) = prefix + TagsDescr(count)(TDP.tdTagname)
                TagCycles(i) = TagsDescr(count)(TDP.tdCycle)
                TagCookies(i) = TagsDescr(count)(TDP.tdCookie)
                i = i + 1
            End If
        Next count

        ' Read tags all in one
        m_ITag.ReadTagCyclic(m_RegisterCookie, TagNames, TagCycles, TagCookies, ServerCookies)
    End Sub

    Private Sub ChangeTagServer(ByVal bRemove As Boolean)
        ' At runtime change the Data Server only if the Active property is set
        If Not DesignMode And m_Active Then
            On Error Resume Next

            ' if nested in WinCC: get the Tagset Object from the container
            m_ITag = Site.GetService(GetType(ITag))

            ' or if nested in Windows From
            If m_ITag Is Nothing Then
                m_ITag = CreateObject("CCITagControl.ITagControl.1")
            End If

            ' Connect to server            
            m_RegisterCookie = m_ITag.Register(Me)
            ' Add all items in the current view
            'AddItemsToGroup()
        End If
    End Sub

    Public ReadOnly Property DeviceType() As String
        Get
            DeviceType = "<Unknown>"
            Try
                If Not Site Is Nothing Then
                    Dim runtimeContext As IRuntimeContext = Site.GetService(GetType(IRuntimeContext))
                    Return runtimeContext.DeviceType
                End If
            Catch ex As Exception
                ' MsgBox("get_DeviceType:" & vbCrLf & ex.Message)
                Trace.WriteLine("ITagUserControl: get_DeviceType: " & ex.Message)
            End Try
        End Get
    End Property


    Private Sub btnReadTagCyclic_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnReadTagCyclic.Click
        Try
            Dim TagName As Object = txtReadTagCyclicTagName.Text
            Dim TagCycle As Object = CInt(txtReadTagCyclicUpdateRate.Text)
            Dim TagCookie As Object = CInt(txtReadTagCyclicCookie.Text)
            Dim ServerCookie As Object = 0L

            m_ITag.ReadTagCyclic(m_RegisterCookie, TagName, TagCycle, TagCookie, ServerCookie)

            radioReadTagCyclic.Checked = True
        Catch ex As Exception
            MsgBox("ReadTagCyclic:" & vbCrLf & ex.Message)
        End Try
    End Sub

    Private Sub btnReadTagCyclicCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnReadTagCyclicCancel.Click
        Try
            m_ITag.Cancel(m_RegisterCookie)

            radioReadTagCyclic.Checked = False
        Catch ex As Exception
            MsgBox("Cancel:" & vbCrLf & ex.Message)
        End Try
    End Sub

    Private Sub btnReadTagStart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnReadTagStart.Click
        Try
            Dim TagName As Object = txtReadTagTagName.Text
            Dim Values As Object = m_ITag.ReadTag(m_RegisterCookie, TagName)
            Dim Value
            For Each Value In Values
                txtReadTagValue.Text = CStr(Value)
            Next
        Catch ex As Exception
            MsgBox("ReadTag:" & vbCrLf & ex.Message)
        End Try

    End Sub

    Private Sub btnReadTagAsyncStart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnReadTagAsyncStart.Click
        Try
            Dim TagName As Object = txtReadTagAsyncTagName.Text
            Dim TagCookie As Object = CInt(txtReadTagAsyncCookie.Text)
            Dim TagCache As Object = CInt(txtReadTagAsyncCache.Text)

            m_ITag.ReadTagAsync(m_RegisterCookie, TagName, TagCookie, TagCache)
        Catch ex As Exception
            MsgBox("ReadTagCyclic:" & vbCrLf & ex.Message)
        End Try
    End Sub

    Private Sub btnWriteTagStart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnWriteTagStart.Click
        Dim TagName As Object = txtWriteTagTagName.Text
        Dim Value As Object = txtWriteTagValue.Text

        Try
            m_ITag.WriteTag(m_RegisterCookie, TagName, Value)
        Catch ex As Exception
            MsgBox("WriteTag:" & vbCrLf & ex.Message)
        End Try
    End Sub

    Private Sub btnWriteTagAsyncStart_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnWriteTagAsyncStart.Click
        Try
            Dim TagName As Object = txtWriteTagAsyncTagName.Text
            Dim TagCookie As Object = CInt(txtWriteTagAsyncCookie.Text)
            Dim Value As Object = txtWriteTagAsyncValue.Text

            m_ITag.WriteTagAsync(m_RegisterCookie, TagName, TagCookie, Value)
        Catch ex As Exception
            MsgBox("WriteTagAsync:" & vbCrLf & ex.Message)
        End Try
    End Sub
End Class