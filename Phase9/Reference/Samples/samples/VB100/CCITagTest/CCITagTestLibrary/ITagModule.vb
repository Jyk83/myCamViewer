Option Strict Off
Option Explicit On
Module ITagModule
    '--------------------------------------------------------------------------------
    ' Industrial ActiveX Control Template
    ' file IXMod.bas
    ' Version : 1.0 March 1998 : created by Elias Nelson Makram
    ' Changed : 01.02.2000 : IX Support
    ' Purpose :
    '  Constant definition and helper functions
    '----------------------------------------------------------------------
    ' Copyright (c) 1998, Siemens Corporation
    '              All Rights Reserved
    '
    '--------------------------------------------------------------------------------

    '------------------------------------------------
    ' Const:
    ' Purpose:  Language IDs definition
    ' Revision History:  03/98 ENM
    '
    Public Const LANG_GERMAN As Integer = &H400S + &H7S
    Public Const LANG_ENGLISH As Integer = &H400S + &H9S
    Public Const LANG_FRENCH As Integer = &H400S + &HCS
    Public Const LANG_BRAZIL As Integer = &H400S + &H16S
    Public Const LANG_ITALIAN As Integer = &H400S + &H10S
    Public Const LANG_JAPANESE As Integer = &H400S + &H11S
    Public Const LANG_SPANISH As Integer = &H400S + &HAS
    Public Const LANG_CHINESE As Integer = &H400S + &H4S

    '------------------------------------------------
    ' Const:
    ' Purpose:  DMC Tag Quality / Tag Statusflags
    ' Revision History:  03/99 ENM
    '
    Public Const ITAG_VARSTATE_NOT_ESTABLISHED As Short = &H1S '  Connection not established
    Public Const ITAG_VARSTATE_HANDSHAKE_ERROR As Short = &H2S '  Protocol error
    Public Const ITAG_VARSTATE_HARDWARE_ERROR As Short = &H4S '  Network error
    Public Const ITAG_VARSTATE_MAX_LIMIT As Short = &H8S '  Limit overflow
    Public Const ITAG_VARSTATE_MIN_LIMIT As Short = &H10S '  Limit underflow
    Public Const ITAG_VARSTATE_MAX_RANGE As Short = &H20S '  Format Limit overflow
    Public Const ITAG_VARSTATE_MIN_RANGE As Short = &H40S '  Format Limit underflow
    Public Const ITAG_VARSTATE_CONVERSION_ERROR As Short = &H80S '  Conversion error
    Public Const ITAG_VARSTATE_STARTUP_VALUE As Short = &H100S '  Initialial value
    Public Const ITAG_VARSTATE_DEFAULT_VALUE As Short = &H200S '  Tag Alternate value
    Public Const ITAG_VARSTATE_ADDRESS_ERROR As Short = &H400S '  Adressing error in the channel
    Public Const ITAG_VARSTATE_INVALID_KEY As Short = &H800S '  Tag not found
    Public Const ITAG_VARSTATE_ACCESS_FAULT As Short = &H1000S '  Tag read not allowed
    Public Const ITAG_VARSTATE_TIMEOUT As Short = &H2000S '  Timeout / no responce from the channel

    Public Const ITAG_VARSTATE_ERROR As Short = ITAG_VARSTATE_NOT_ESTABLISHED Or ITAG_VARSTATE_HANDSHAKE_ERROR Or ITAG_VARSTATE_HARDWARE_ERROR Or ITAG_VARSTATE_MAX_LIMIT Or ITAG_VARSTATE_MIN_LIMIT Or ITAG_VARSTATE_MAX_RANGE Or ITAG_VARSTATE_CONVERSION_ERROR Or ITAG_VARSTATE_ADDRESS_ERROR Or ITAG_VARSTATE_INVALID_KEY Or ITAG_VARSTATE_ACCESS_FAULT Or ITAG_VARSTATE_TIMEOUT

    Public Const ITAG_E_BADTYPE = &HC0048001
    Public Const ITAG_E_INVALIDITEM = &HC0048002
    Public Const ITAG_E_TIMEOUT = &HC0048003
    Public Const ITAG_E_SERVER_DOWN = &HC0048004
    Public Const ITAG_E_BADRIGHTS = &HC0048005
    Public Const ITAG_E_COMMUNICATION = &HC0048006
    Public Const ITAG_E_RANGE_VIOLATION_HIGH = &HC0048007
    Public Const ITAG_E_RANGE_VIOLATION_LOW = &HC0048008
    Public Const ITAG_E_CONVERSION = &HC0048009
    Public Const ITAG_E_LIMIT_VIOLATION_HIGH = &HC004800A
    Public Const ITAG_E_LIMIT_VIOLATION_LOW = &HC004800B

    '------------------------------------------------
    '------------------------------------------------
    ' Const:
    ' Purpose:  Bit Masks
    ' Revision History:  03/98 ENM
    '
    Public Const Bit00 As Short = &H1S
    Public Const Bit01 As Short = &H2S
    Public Const Bit02 As Short = &H4S
    Public Const Bit03 As Short = &H8S
    Public Const Bit04 As Short = &H10S
    Public Const Bit05 As Short = &H20S
    Public Const Bit06 As Short = &H40S
    Public Const Bit07 As Short = &H80S
    Public Const Bit08 As Short = &H100S
    Public Const Bit09 As Short = &H200S
    Public Const Bit10 As Short = &H400S
    Public Const Bit11 As Short = &H800S
    Public Const Bit12 As Short = &H1000S
    Public Const Bit13 As Short = &H2000S
    Public Const Bit14 As Short = &H4000S
    Public Const Bit15 As Integer = 32768 ' use for = &H8000
    Public Const Bit16 As Integer = &H10000
    Public Const Bit17 As Integer = &H20000
    Public Const Bit18 As Integer = &H40000
    Public Const Bit19 As Integer = &H80000
    Public Const Bit20 As Integer = &H100000
    Public Const Bit21 As Integer = &H200000
    Public Const Bit22 As Integer = &H400000
    Public Const Bit23 As Integer = &H800000
    Public Const Bit24 As Integer = &H1000000
    Public Const Bit25 As Integer = &H2000000
    Public Const Bit26 As Integer = &H4000000
    Public Const Bit27 As Integer = &H8000000
    Public Const Bit28 As Integer = &H10000000
    Public Const Bit29 As Integer = &H20000000
    Public Const Bit30 As Integer = &H40000000
    Public Const Bit31 As Integer = &H80000000

    Private bitArr(32) As Integer
    Private bitarrInit As Boolean

    Private Sub init_BitArr()
        If Not bitarrInit Then
            bitArr(0) = Bit00 : bitArr(1) = Bit01 : bitArr(2) = Bit02 : bitArr(3) = Bit03 : bitArr(4) = Bit04 : bitArr(5) = Bit05 : bitArr(6) = Bit06 : bitArr(7) = Bit07 : bitArr(8) = Bit08 : bitArr(9) = Bit09 : bitArr(10) = Bit10 : bitArr(11) = Bit11 : bitArr(12) = Bit12 : bitArr(13) = Bit13 : bitArr(14) = Bit14 : bitArr(15) = Bit15 : bitArr(16) = Bit16 : bitArr(17) = Bit17 : bitArr(18) = Bit18 : bitArr(19) = Bit19 : bitArr(20) = Bit20 : bitArr(21) = Bit21 : bitArr(22) = Bit22 : bitArr(23) = Bit23 : bitArr(24) = Bit24 : bitArr(25) = Bit25 : bitArr(26) = Bit26 : bitArr(27) = Bit27 : bitArr(28) = Bit28 : bitArr(29) = Bit29 : bitArr(30) = Bit30 : bitArr(31) = Bit31
            bitarrInit = True
        End If
    End Sub

    '------------------------------------------------
    ' Procedure: SetBitLong
    ' Purpose:  Set state bState of lValue at lBitPos
    ' Revision History:  03/98 ENM
    '
    Sub SetBitLong(ByVal bState As Boolean, ByRef lValue As Integer, ByVal lBitPos As Integer)
        init_BitArr()
        If bState Then
            lValue = lValue Or bitArr(lBitPos - 1) '(2 ^ lBitPos)
        Else
            lValue = lValue And Not bitArr(lBitPos - 1) '(2 ^ lBitPos)
        End If
    End Sub

    '------------------------------------------------
    ' Function: GetBit
    ' Purpose:  Get state of lBitPos bit in lValue
    ' Revision History:  03/98 ENM
    '
    Function GetBit(ByVal lValue As Integer, ByVal lBitPos As Integer) As Boolean
        init_BitArr()
        GetBit = lValue And bitArr(lBitPos - 1) '(2 ^ lBitPos)
    End Function

    Function IsSet(ByRef v As Object) As Boolean
        IsSet = IsReference(v)
        If IsSet Then IsSet = Not (v Is Nothing)
    End Function

    '------------------------------------------------
    ' Function: ErrorBox
    ' Purpose:  Display an error box and returns which button the user clicked
    ' Revision History:  03/98 ENM
    '
    Function ErrorBox(ByRef e As Object) As Short
        Dim s As String
        s = "Number: " & e.Number & vbCrLf
        s = s & "Description: " & e.Description & vbCrLf
        s = s & "Source: " & e.Source & vbCrLf
        ErrorBox = MsgBox(s)
    End Function




    '------------------------------------------------
    ' Function: IsQualityOK
    ' Purpose:  Checks the Quality Code 
    ' Q      : Quality code to check found in TagsDescr(Cookie)(TDP.tdQuality)

    Function IsQualityOK(ByVal Q As Integer) As Boolean
        ' check the quality code
        If (Q And ITAG_VARSTATE_NOT_ESTABLISHED) Or (Q And ITAG_VARSTATE_HANDSHAKE_ERROR) Or (Q And ITAG_VARSTATE_HARDWARE_ERROR) Or (Q And ITAG_VARSTATE_MAX_LIMIT) Or (Q And ITAG_VARSTATE_MIN_LIMIT) Or (Q And ITAG_VARSTATE_MAX_RANGE) Or (Q And ITAG_VARSTATE_MIN_RANGE) Or (Q And ITAG_VARSTATE_CONVERSION_ERROR) Or (Q And ITAG_VARSTATE_ADDRESS_ERROR) Or (Q And ITAG_VARSTATE_INVALID_KEY) Or (Q And ITAG_VARSTATE_ACCESS_FAULT) Or (Q And ITAG_VARSTATE_TIMEOUT) Then
            IsQualityOK = False
        Else
            IsQualityOK = True
        End If
    End Function

    Function GetVarStateText(ByVal VarState As Object) As String        
        Select Case (VarState)
            Case ITAG_VARSTATE_ACCESS_FAULT
                GetVarStateText = "ACCESS_FAULT"
            Case ITAG_VARSTATE_ADDRESS_ERROR
                GetVarStateText = "ADDRESS_ERROR"
            Case ITAG_VARSTATE_CONVERSION_ERROR
                GetVarStateText = "CONVERSION_ERROR"
            Case ITAG_VARSTATE_DEFAULT_VALUE
                GetVarStateText = "DEFAULT_VALUE"
            Case ITAG_VARSTATE_ERROR
                GetVarStateText = "ERROR"
            Case ITAG_VARSTATE_HANDSHAKE_ERROR
                GetVarStateText = "HANDSHAKE_ERROR"
            Case ITAG_VARSTATE_HARDWARE_ERROR
                GetVarStateText = "HARDWARE_ERROR"
            Case ITAG_VARSTATE_INVALID_KEY
                GetVarStateText = "INVALID_KEY"
            Case ITAG_VARSTATE_MAX_LIMIT
                GetVarStateText = "MAX_LIMIT"
            Case ITAG_VARSTATE_MAX_RANGE
                GetVarStateText = "MAX_RANGE"
            Case ITAG_VARSTATE_MIN_LIMIT
                GetVarStateText = "MIN_LIMIT"
            Case ITAG_VARSTATE_MIN_RANGE
                GetVarStateText = "MIN_RANGE"
            Case ITAG_VARSTATE_NOT_ESTABLISHED
                GetVarStateText = "ACCESS_FAULT"
            Case ITAG_VARSTATE_STARTUP_VALUE
                GetVarStateText = "STARTUP_VALUE"
            Case ITAG_VARSTATE_TIMEOUT
                GetVarStateText = "TIMEOUT"
            Case 0
                GetVarStateText = "OK"
            Case Else
                GetVarStateText = VarState.ToString
        End Select
    End Function

    Function GetErrorText(ByVal ErrorNumber As Integer) As String
        Select Case (ErrorNumber)
            Case ITAG_E_BADTYPE
                GetErrorText = "BADTYPE"
            Case ITAG_E_INVALIDITEM
                GetErrorText = "INVALIDITEM"
            Case ITAG_E_TIMEOUT
                GetErrorText = "TIMEOUT"
            Case ITAG_E_SERVER_DOWN
                GetErrorText = "SERVER_DOWN"
            Case ITAG_E_BADRIGHTS
                GetErrorText = "BADRIGHTS"
            Case ITAG_E_COMMUNICATION
                GetErrorText = "COMMUNICATION"
            Case ITAG_E_RANGE_VIOLATION_HIGH
                GetErrorText = "RANGE_VIOLATION_HIGH"
            Case ITAG_E_RANGE_VIOLATION_LOW
                GetErrorText = "RANGE_VIOLATION_LOW"
            Case ITAG_E_CONVERSION
                GetErrorText = "CONVERSION"
            Case ITAG_E_LIMIT_VIOLATION_HIGH
                GetErrorText = "LIMIT_VIOLATION_HIGH"
            Case ITAG_E_LIMIT_VIOLATION_LOW
                GetErrorText = "LIMIT_VIOLATION_LOW"
            Case Else
                GetErrorText = ErrorNumber.ToString
        End Select
    End Function
End Module