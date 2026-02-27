#include "stdafx.h"
#include "MPFInterface.h"
#include "CAMViewerFactory.h"

//////////////////////////////////////////////////////////////////////////

using namespace std;

#ifdef _DEBUG
#define new DEBUG_NEW
#endif // _DEBUG

#pragma warning(disable : 4996)

//////////////////////////////////////////////////////////////////////////

extern bool g_showErrMessage;

//////////////////////////////////////////////////////////////////////////

#define iGAS_OXYGEN		1
#define iGAS_NITROGEN	2
#define iGAS_AIR		3
#define iGAS_LAST		(iGAS_AIR)
#define iGASTYPE_DBNAME	4

#define iPIPE_CIRCLE	0
#define iPIPE_RECTANGLE	1


TCHAR s_szMaterialName[][16] =
{
	_T("Undefined"),		// 0
	_T("Mild Steel"),		// 1
	_T("Stainless Steel"),	// 2
	_T("Aluminum"),			// 3
	_T("Brass"),			// 4
	_T("Copper"),			// 5
	_T("Undefined"),		// 6
	_T("Undefined"),		// 7
	_T("Undefined"),		// 8
	_T("User defined"),		// 9
};

#define iMATERIAL_UNDEFINED	0
#define iMATERIAL_MS		1
#define iMATERIAL_SUS		2
#define iMATERIAL_AL		3
#define iMATERIAL_BRASS		4
#define iMATERIAL_COPPER	5
#define iMATERIAL_USER		9
#define iMATERIAL_END		(iMATERIAL_USER)

TCHAR s_szVerionName[][8] =
{
	_T("Unknown"),
	_T("V08"),
	_T("V16"),
	_T("V16A05")
};

TCHAR s_szPiercingName[][16] =
{
	_T("No piercing"),		// 0
	_T("Pulse piercing"),	// 1
	_T("CW piercing"),		// 2
	_T("Down piercing"),	// 3
	_T("MP piercing"),		// 4
	_T("Quick piercing"),	// 5
	_T("Undefined"),		// 6
	_T("Undefined"),		// 7
	_T("Shot marking"),		// 8
};

TCHAR s_szCuttingName[][32] =
{
	_T("Shot marking"),			// 0
	_T("High speed cutting"),	// 1
	_T("Middle speed cutting"),	// 2
	_T("Low speed cutting"),	// 3
	_T("Special cutting"),		// 4
	_T("Undefined"),			// 5
	_T("Undefined"),			// 6
	_T("Undefined"),			// 7
	_T("Undefined"),			// 8
	_T("Undefined"),			// 9
	_T("Undefined"),			// 10
	_T("Engrave marking"),		// 11
};

inline LPCTSTR PiercingName(int iPiercing)
{
	return (0 <= iPiercing && iPiercing < 9)? s_szPiercingName[iPiercing]: _T("Unknown");
}
inline LPCTSTR CuttingName(int iCutting)
{
	return (0 <= iCutting && iCutting < 11)? s_szCuttingName[iCutting]: _T("Unknown");
}

enum enHKProc
{
	enNotRegisteredHKSPF,
	enHKCUT,	// 1
	enHKEND,	// 2
	enHKINI,	// 3
	enHKLDB,	// 4
	enHKLEA,	// 5
	enHKOST,	// 6
	enHKPED,	// 7
	enHKPIE,	// 8
	enHKPPP,	// 9
	enHKSCRC,	// 10
	enHKSTO,	// 11
	enHKSTR,	// 12
};

inline enHKProc discriminateHKSPF(const CString& strProc)
{
	if (_T('H') != strProc[0] || _T('K') != strProc[1])
		return enNotRegisteredHKSPF;

	switch (strProc[2])
	{
	case _T('C'):
		if (_T('U')==strProc[3] && _T('T')==strProc[4])	// HKCUT
			return enHKCUT;
		break;
	case _T('E'):
		if (_T('N')==strProc[3] && _T('D')==strProc[4])	// HKEND
			return enHKEND;
		break;
	case _T('I'):
		if (_T('N')==strProc[3] && _T('I')==strProc[4])	// HKINI
			return enHKINI;
		break;
	case _T('L'):
		if (_T('D')==strProc[3] && _T('B')==strProc[4])	// HKLDB
			return enHKLDB;
		if (_T('E')==strProc[3] && _T('A')==strProc[4])	// HKLEA
			return enHKLEA;
		break;
	case _T('O'):
		if (_T('S')==strProc[3] && _T('T')==strProc[4])	// HKOST
			return enHKOST;
		break;
	case _T('P'):
		if (_T('E')==strProc[3] && _T('D')==strProc[4])	// HKPED
			return enHKPED;
		if (_T('I')==strProc[3] && _T('E')==strProc[4])	// HKPIE
			return enHKPIE;
		if (_T('P')==strProc[3] && _T('P')==strProc[4])	// HKPPP
			return enHKPPP;
		break;
	case _T('S'):
		if (_T('C')==strProc[3] && _T('R')==strProc[4] && _T('C')==strProc[5])	// HKSCRC
			return enHKSCRC;
		if (_T('T')==strProc[3] && _T('O')==strProc[4])	// HKSTO
			return enHKSTO;
		if (_T('T')==strProc[3] && _T('R')==strProc[4])	// HKSTR
			return enHKSTR;
		break;

	default: break;
	}

	return enNotRegisteredHKSPF;
}

// Restart information markups with their tags and elements
//                                   1234567
const TCHAR csz_RestartHead[] ={ _T("<RSTRT>") };
const TCHAR csz_RestartTail[] ={ _T("</RSTRT>") };
const TCHAR csz_TagOriginalPart[] ={ _T("ORG") };
const TCHAR csz_TagModifiedPart[] ={ _T("MOD") };
const TCHAR csz_ElemOrgPartNo[] ={ _T("PRT#") };
const TCHAR csz_ElemOrgContNo[] ={ _T("CNT#") };
const TCHAR csz_ElemOrgCodeIndex[] ={ _T("iELM") };
const TCHAR csz_ElemBlockNo[] ={ _T("BLC#") };
const TCHAR csz_ElemReverse[] ={ _T("isReverse") };
const TCHAR csz_ElemLineNo[] ={ _T("LNE#") };
const TCHAR csz_ModContourLineNo[] ={ _T("CNTLNE#") };
const TCHAR csz_ElemLineNoOffset[] ={ _T("LN#OFFSET") };
const TCHAR csz_RestartWCSx[] ={ _T("WCSX") };
const TCHAR csz_RestartWCSy[] ={ _T("WCSY") };
const TCHAR csz_RestartPartHead[] ={ _T("<RSTRT_PART>") };
const TCHAR csz_RestartPartTail[] ={ _T("</RSTRT_PART>") };


//////////////////////////////////////////////////////////////////////////

LPCTSTR getPiercingTypeName(int iPiercing)
{
	return PiercingName(iPiercing);
}
LPCTSTR getCuttingTypeName(int iCutting)
{
	return CuttingName(iCutting);
}
LPCTSTR getFileVersionName(fileVersion version)
{
	return s_szVerionName[version];
}

//////////////////////////////////////////////////////////////////////////

inline bool skipEqualAndSpace(const CString& str, int& iStart)
{
	const int ccLen = str.GetLength();
	while (_T('=') == str[iStart] || _T(' ') == str[iStart] || _T('\t') == str[iStart])
	{
		++iStart;
		if (ccLen <= iStart)
			return false;
	}
	return true;
}

inline bool checkGcodeCoordsOf(TCHAR cAddr, const CString& str)
{
	int iStart = 1;
	if (str[0] != cAddr || !skipEqualAndSpace(str, iStart) || !IsNumeric(str, iStart))
		return false;
	return true;
}

inline bool isSpaceChar(TCHAR ch)
{
	return (_T(' ') == ch || _T('\t') == ch || _T('\r') == ch || _T('\n') == ch);
}

inline bool IsCommentLine(const CString& sLine, int& i)
{
	const int nLength = sLine.GetLength();
	for (i = 0; i < nLength; ++i)
	{
		if (_T(' ') != sLine[i] && _T('\t') != sLine[i])
		{
			if (_T(';') == sLine[i])
			{
				++i;
				return true;
			}
			return false;
		}
	}
	return false;
}

inline bool isBlankLine(const CString& sLine)
{
	for (int i = 0; i < sLine.GetLength(); ++i)
	{
		if (!isSpaceChar(sLine.GetAt(i)))
			return false;
	}
	return true;
}

inline bool getVersionString(const CString& sLine, CString& sVersion)
{
	CString str(sLine);
	str.Trim();
	const int ccLen = str.GetLength();
	if (2 < ccLen && _T(';') == str[0] && _T('!') == str[1])
	{
		sVersion = str.Mid(2);
	}
	else
	{
		sVersion.Empty();
	}
	return (0 < sVersion.GetLength());
}

inline bool GetNextArg(const CStringList& strArgList, POSITION& pos, CString& sArg)
{
	if (pos)
	{
		sArg = strArgList.GetNext(pos);
		return (0 < sArg.GetLength());
	}
	return false;
}

inline bool GetIntArg(const CStringList& strArgList, POSITION& pos, int& nValue)
{
	if (pos)
	{
		CString str = strArgList.GetNext(pos);
		if (str.GetLength() && IsNumber(str, 0))
		{
			nValue = _tstoi(str);
			return true;
		}
	}
	return false;
}

inline bool GetfloatArg(const CStringList& strArgList, POSITION& pos, double& value)
{
	if (pos)
	{
		CString str = strArgList.GetNext(pos);
		if (str.GetLength() && IsNumeric(str))
		{
			value = _tstof(str);
			return true;
		}
	}
	return false;
}

inline bool GetGcodeDestination(const CString& strGcode, double& x, double& y)
{
	int N = -1;
	if (!GetGcodeAddress(strGcode, N))
		return false;

	int iX = strGcode.Find(_T(' ')) + 1;
	int iY = strGcode.Find(_T(' '), iX+1) + 1;
	if (0 >= iX || 0 >= iY
		|| _T('X') != strGcode[iX] || _T('Y') != strGcode[iY])
	{
		ASSERT(FALSE);
		return false;
	}
	N = iY - (iX+1);
	if (1 > N)
	{
		ASSERT(FALSE);
		return false;
	}

	CString strX = strGcode.Mid(iX+1, N);
	N = strGcode.Find(_T(' '), iY+1);
	CString strY = (iY+1 < N)? strGcode.Mid(iY+1, N-(iY+1)): strGcode.Mid(iY+1);
	strX.Trim(), strY.Trim();

	if (!IsNumeric(strX) || !IsNumeric(strY))
		return false;

	x = _tstof(strX);
	y = _tstof(strY);

	return true;
}

inline bool GetNumericArg(const CStringList& strArgList, POSITION& pos, int& n, double& f, bool& isFloating)
{
	if (pos)
	{
		CString str = strArgList.GetNext(pos);
		int nDecimals = 0;
		if (str.GetLength() && IsNumeric(str, 0, &nDecimals))
		{
			if (1 == nDecimals)
			{
				isFloating = true;
				f = _tstof(str);
			}
			else
			{
				ASSERT(0 == nDecimals);
				isFloating = false;
				n = _tstoi(str);
			}
			return true;
		}
	}
	return false;
}

bool GetMaterialName(const CString& strInfo, CString& strMaterial)
{
	CString str(strInfo);
	str.Trim();
	str.MakeUpper();
	switch (str[0])
	{
	case _T('A'):
		if (_T('L') == str[1])
		{
			strMaterial = s_szMaterialName[iMATERIAL_AL];
			return true;
		}
		break;
	case _T('B'):
		if (_T('R') == str[1])
		{
			strMaterial = s_szMaterialName[iMATERIAL_BRASS];
			return true;
		}
		break;
	case _T('C'):
		if (_T('O') == str[1] || _T('U') == str[1])
		{
			strMaterial = s_szMaterialName[iMATERIAL_COPPER];
			return true;
		}
		break;
	case _T('M'):
		if (_T('S') == str[1])
		{
			strMaterial = s_szMaterialName[iMATERIAL_MS];
			return true;
		}
		break;
	case _T('S'):
		if (_T('T') == str[1] || (_T('0') <= str[1] && str[1] <= _T('9')))
		{
			strMaterial = s_szMaterialName[iMATERIAL_SUS];
			return true;
		}
		break;
	default:
		strMaterial = s_szMaterialName[iMATERIAL_UNDEFINED];
		break;
	}
	return true;
}

bool isMarkUpString(const CString& sLine, LPCTSTR pszTag, const int ccLenTag)
{
	int iCharFrom = 0;
	const int ccLen = sLine.GetLength();

	// Check the comment line specifier first.
	// All the markup strings are supposed to be written in the form of comment
	if (!IsCommentLine(sLine, iCharFrom) || ccLen < iCharFrom + ccLenTag)
		return false;
	ASSERT(0 < iCharFrom && _T(';') == sLine.GetAt(iCharFrom-1));

	// Slip preceding space characters
	while (iCharFrom < ccLen)
	{
		if (isSpaceChar(sLine.GetAt(iCharFrom)))
			++iCharFrom;
		else
			break;
	}
	if (ccLen <= iCharFrom)
		return false;

	// Check whether the markup is the one that we are finding with 'pszTag'
	int i = 0;
	for (int j = iCharFrom; i < ccLenTag && j < ccLen; ++i, ++j)
	{
		if (pszTag[i] != sLine.GetAt(j))
			return false;
	}
	return (ccLenTag == i);
}

bool getMarkupString(const CString& sLine, int& iFrom, LPCTSTR pszTag, CString& strAttrs)
{
	CString sHead(CString(_T('<')) + pszTag);
	CString sTail(CString(_T('/')) + pszTag + CString(_T('>')));

	iFrom = sLine.Find(sHead, iFrom);
	if (0 > iFrom)
		return false;
	int iTo = sLine.Find(sTail, iFrom+1);
	if (0 > iTo)
		return false;
	ASSERT(iFrom < iTo);

	strAttrs = sLine.Mid(iFrom, iTo - iFrom + sTail.GetLength());

	iFrom = iTo + sTail.GetLength();
	return true;
}

bool getRestartLogText(const CStringArray& fileText, int iRestartLine, CString& strLog, int& nLines)
{
	if (0 > iRestartLine || fileText.GetCount() <= iRestartLine)
	{
		ASSERT(FALSE);
		return false;
	}

	CString str = fileText.GetAt(iRestartLine);
	if (!isMarkUpString(str, csz_RestartHead, _countof(csz_RestartHead)-1))
	{
		ASSERT(FALSE);
		return false;
	}
	nLines = 1;
	strLog = str;
	if (0 <= str.Find(csz_RestartTail))
		return true;

	for (int i = iRestartLine + 1; i < fileText.GetCount(); ++i)
	{
		str = fileText.GetAt(i);
		if (IsCommentLine(str))
		{
			strLog += str;
			if (0 <= str.Find(csz_RestartTail))
				break;
		}
		++nLines;
	}

	return true;
}

bool getRestartLogInfo(const CString& strOrgInfo, const CString& strModInfo, RESTART_LOG& resLog)
{
	int ccLenPartNo = _countof(csz_ElemOrgPartNo) - 1;
	int ccLenContNo = _countof(csz_ElemOrgContNo) - 1;
	int ccLenElemIndex = _countof(csz_ElemOrgCodeIndex) - 1;
	int ccLenBlckNo = _countof(csz_ElemBlockNo) - 1;
	int ccLenReverse = _countof(csz_ElemReverse) - 1;
	int ccLenLineNo = _countof(csz_ElemLineNo) - 1;
	int ccLenModCntLineNo = _countof(csz_ModContourLineNo) - 1;
	int ccLenModLineOffset = _countof(csz_ElemLineNoOffset) - 1;
	int ccLenWCSXY = _countof(csz_RestartWCSx) - 1;

	int iPartNo = strOrgInfo.Find(csz_ElemOrgPartNo);
	int iContourNo = strOrgInfo.Find(csz_ElemOrgContNo, iPartNo + ccLenPartNo + 2);
	int iElemIndex = strOrgInfo.Find(csz_ElemOrgCodeIndex, iContourNo + ccLenContNo + 2);
	int iBlockNo = strOrgInfo.Find(csz_ElemBlockNo, iElemIndex + ccLenElemIndex + 2);
	int iReverseFlag = strOrgInfo.Find(csz_ElemReverse, iBlockNo + ccLenBlckNo + 2);
	if (0 > iPartNo || 0 > iElemIndex || 0 > iContourNo || 0 > iBlockNo || 0 > iReverseFlag)
	{
		ASSERT(FALSE);
		return false;
	}
	iPartNo += ccLenPartNo;
	iContourNo += ccLenContNo;
	iElemIndex += ccLenElemIndex;
	iBlockNo += ccLenBlckNo;
	iReverseFlag += ccLenReverse;
	if (!GetCAMNumber(strOrgInfo, iPartNo, resLog.partNo)
		|| !GetCAMNumber(strOrgInfo, iContourNo, resLog.contourNo)
		|| !GetCAMNumber(strOrgInfo, iElemIndex, resLog.GcodeIndex)
		|| !GetCAMNumber(strOrgInfo, iBlockNo, resLog.shapeNBlockNo)
		|| !GetCAMNumber(strOrgInfo, iReverseFlag, resLog.isReverse))
	{
		ASSERT(FALSE);
		return false;
	}

	iBlockNo = strModInfo.Find(csz_ElemBlockNo);
	int iModLineNo = strModInfo.Find(csz_ElemLineNo, iBlockNo + ccLenBlckNo + 2);
	int iModCntLineNo = strModInfo.Find(csz_ModContourLineNo, iModLineNo + ccLenLineNo + 2);
	int iModLineOffset = strModInfo.Find(csz_ElemLineNoOffset, iModCntLineNo + ccLenModCntLineNo + 2);
	int iWCSx = strModInfo.Find(csz_RestartWCSx, iModLineOffset + ccLenModLineOffset + 2);
	int iWCSy = strModInfo.Find(csz_RestartWCSy, iWCSx + ccLenWCSXY + 2);
	if (0 > iBlockNo || 0 > iModLineNo || 0 > iModCntLineNo|| 0 > iModLineOffset
		|| 0 > iWCSx || 0 > iWCSy)
	{
		ASSERT(FALSE);
		return false;
	}
	iBlockNo += ccLenBlckNo;
	iModLineNo += ccLenLineNo;
	iModCntLineNo += ccLenModCntLineNo;
	iModLineOffset += ccLenModLineOffset;
	iWCSx += ccLenWCSXY;
	iWCSy += ccLenWCSXY;
	if (!GetCAMNumber(strModInfo, iBlockNo, resLog.modShapeNBlockNo)
		|| !GetCAMNumber(strModInfo, iModLineNo, resLog.modStartLine)
		|| !GetCAMNumber(strModInfo, iModCntLineNo, resLog.modContourStartLine)
		|| !GetCAMNumber(strModInfo, iModLineOffset, resLog.offsetModLines)
		|| !GetCAMCoords(strModInfo, iWCSx, resLog.wcsX)
		|| !GetCAMCoords(strModInfo, iWCSy, resLog.wcsY))
	{
		ASSERT(FALSE);
		return false;
	}

	return true;
}


//////////////////////////////////////////////////////////////////////////

CAM_PART::~CAM_PART()
{
	delete[] pDetour;
}


//////////////////////////////////////////////////////////////////////////
// Implementation of InterfaceMPF class

LogLevel InterfaceMPF::s_logErrLevel = enLogWarning;
TCHAR	 InterfaceMPF::s_szError[1024];
fLOGFUNC InterfaceMPF::SaveLogMessage;

void InterfaceMPF::SetErrorLogLevel(LogLevel level)
{
	s_logErrLevel = level;
}

void InterfaceMPF::SetLogFunction(fLOGFUNC f)
{
	SaveLogMessage = f;
}

fLOGFUNC InterfaceMPF::GetLogFunction()
{
	return SaveLogMessage;
}

void InterfaceMPF::PrintLog(LogLevel logLevel, LPCTSTR pszFormat, ...)
{
	va_list args;
	va_start(args, pszFormat);
	_vstprintf_s(s_szError, _countof(s_szError), pszFormat, args);

	if (logLevel <= s_logErrLevel && SaveLogMessage)
	{
		SaveLogMessage(s_szError);
	}
	return;
}

LPCTSTR InterfaceMPF::GetLastErrorMessage()
{
	return s_szError;
}

bool InterfaceMPF::SaveStrListToFile(CStringList& strList, LPCTSTR pszSavePath)
{
	CStdioFile file;
	CFileException ex;
	UINT openFlags = CFile::modeCreate|CFile::modeWrite|CFile::shareDenyRead|CFile::shareDenyWrite|CFile::typeText;

	if (!file.Open(pszSavePath, openFlags, &ex))
	{
		ex.GetErrorMessage(s_szError, 1024);
		PrintLog(enLogError, s_szError);
		return false;
	}

	POSITION pos = strList.GetHeadPosition();
	while (pos)
	{
		file.WriteString(strList.GetNext(pos));
		file.WriteString(_T("\n"));
	}
	file.Close();

	return true;
}


//////////////////////////////////////////////////////////////////////////

InterfaceMPF::InterfaceMPF()
{
	_numPartsTotal = 0;	// number of parts in the MPF file
	_cpPartInfo = NULL;	// temporary pointer which is owned by CAM_DATA class handled by a caller
}

InterfaceMPF::~InterfaceMPF()
{
}


//////////////////////////////////////////////////////////////////////////

bool InterfaceMPF::Load(LPCTSTR pszFile, CAMContainer& container)
{
	CAM_DATA& camInfo = container.camData;
	camInfo.Delete();
	ASSERT(!camInfo.HasContents());

	FileMPF& loader = container.loader;

	s_szError[0] = 0;
	if (!loader.read(pszFile, s_szError, _countof(s_szError)))
	{
		PrintLog(enLogError, s_szError);
		return false;
	}
	camInfo.strFileName = pszFile;

	bool bLoaded = false;

	switch (loader.getVersion())
	{
	case verV08:
		camInfo.version = verV08;
		bLoaded = LoadV08(loader, camInfo);
		break;
	case verV16:
		camInfo.version = verV16;
		bLoaded = LoadV16(loader, camInfo);
		break;
	case verV16A05:
		camInfo.version = verV16A05;
		bLoaded = LoadV16A05(loader, camInfo);
		break;
	default:
		camInfo.version = verUnknown;
		_tcscpy(s_szError, _T("File version is unknown."));
		break;
	}

#if (0) //#ifdef _DEBUG
	if (bLoaded)
	{
		CString strLine;
		for (int i = 0, iLast = 0; i < camInfo.num_Shapes; ++i)
		{
			CAM_SHAPE& part = camInfo.pCAMShape[i];
			for (int j = 0; j < part.numContours; ++j)
			{
				CAM_CONTOUR& contour = part.pContour[j];
				int iBegin = contour.iCmdBlockStr, iEnd = contour.iCmdBlockStrLast;
				ASSERT(iLast < iBegin && iBegin < iEnd);

				if (loader.getLineText(iBegin, strLine))
					TRACE(_T("[%6d:%s\n"), iBegin+1, strLine);
				else
					TRACE(_T("Can't get start block string at line#%d\n"), iBegin+1);
				if (loader.getLineText(iEnd, strLine))
					TRACE(_T("]%6d:%s\n"), iEnd+1, strLine);
				else
					TRACE(_T("Can't get end block string at line#%d\n"), iEnd+1);
				iLast = iEnd;

				for (int k = 0; k < contour.numCodes; ++k)
				{
					TRACE(_T("%8d: %s\n"), contour.pCode[k].nLineNo, contour.pCode[k].sBlockCmd);
				}
			}
		}
	}
#endif // _DEBUG
	return bLoaded;
}


bool InterfaceMPF::LoadV08(FileMPF& loader, CAM_DATA& camInfo)
{
	//
	// First, get part nesting information
	//

	// i. HKLDB & HKINI : initialization information
	if (!ReadHKLDB(loader, camInfo) || !ReadHKINI(loader, camInfo))
		return false;

	// ii. HKOST list: CAM part geometries and attributes
	CStringList strHKOSTList;
	bool bPartsFinished = false;
	do
	{
		if (!ReadHKOSTv08(loader, strHKOSTList, bPartsFinished))
			return false;
	}
	while (!bPartsFinished);

	ASSERT(NULL == camInfo.pCAMShape && NULL == _cpPartInfo);

	int countParts = strHKOSTList.GetCount();
	camInfo.pPart = new CAM_PART[countParts];
	if (NULL == camInfo.pPart)
		return false;
	camInfo.num_Parts = countParts;
	_numPartsTotal = camInfo.num_Parts;
	_cpPartInfo = camInfo.pPart;

	// CAM part nesting information completed.
	//     Check the end of CAM information and initialize information buffers
	int numPartShapes = 0;
	if (!ReadProcEndMcode(loader, numPartShapes)
		|| !InitCAMPartBuf(verV08, strHKOSTList, camInfo, numPartShapes))
		return false;

	//
	// Secondly, get CAM information itself
	//
	CPtrContourList ptrListContour;

	int iCAMPart = 0;
	bool bReachEnd = false, bIsNCPart = false, bPartComplete = false;

	do
	{
		CAM_CONTOUR contour;

		// i. HKLON or HKSCR at the end of file
		if (!ReadHKLON(loader, contour, bReachEnd))
			break;

		if (bReachEnd)	// Met HKSCR and got the last remnant cut information
		{
			if (camInfo.IsNCPart(contour.N_blockNo) || contour.bIsRemnant)
			{
				if (!AddToContourList(ptrListContour, contour))
					return false;

				ASSERT(iCAMPart+1 == camInfo.num_Shapes);// && 1 == ptrListContour.GetCount());
				if (!UpdateCAMPartInfo(ptrListContour, loader.getLastLineNo(), camInfo, iCAMPart))
				{
					DeleteContourList(ptrListContour);
					return false;
				}
			}
			break;
		}
		else if (contour.bIsRemnant)
		{
			if (!AddToContourList(ptrListContour, contour))
				return false;
			continue;
		}

		if (!bIsNCPart)
			bIsNCPart = camInfo.IsNCPart(contour.N_blockNo);
		if (bIsNCPart)
		{
			// ii. HKTON
			if (!ReadHKTON(loader, contour))
				break;
			// iii. Read G-code list until finding HKTOF/HKLOF
			if (!FindHKTOF(loader, contour))
				break;
			// iv. Find the end of contour, checking if it's the end of the part
			if (!FindHKLOF(loader, bPartComplete))
				break;

			contour.iCmdBlockStrLast = loader.getLastLineNo() - 1;
			if (!AddToContourList(ptrListContour, contour))
				return false;

			if (bPartComplete)
			{
				bool bOk = UpdateCAMPartInfo(ptrListContour, loader.getLastLineNo(), camInfo, iCAMPart);
				DeleteContourList(ptrListContour);
				if (!bOk)
					return false;

				bIsNCPart = bPartComplete = false;
			}
		}
		else
		{
			if (!ReadThroughHKLOF(loader))
				return false;
		}
	}
	while (1);

	DeleteContourList(ptrListContour);

	// Check if the while-loop ends up with any errors.
	if (!bReachEnd)
		return false;

	ASSERT(iCAMPart == camInfo.num_Shapes);

	for (int i = 0; i < camInfo.num_Parts; ++i)
	{
		int No = camInfo.pPart[i].nCAMpart_BlockNo;
		camInfo.pPart[i].iCAMShape = -1;
		for (int j = 0; j < camInfo.num_Shapes; ++j)
		{
			if (No == camInfo.pCAMShape[j].nBlockIdNo)
			{
				camInfo.pPart[i].iCAMShape = j;
				break;
			}
		}
		if (0 > camInfo.pPart[i].iCAMShape)
		{
			PrintLog(enLogError, _T("No CAD part information for NCPart[%d]."), i);
			camInfo.Delete();
			return false;
		}
	}

	return true;
}


bool InterfaceMPF::LoadV16(FileMPF& loader, CAM_DATA& camInfo)
{
	//
	// First, get part nesting information
	//

	// i. HKLDB & HKINI : initialization information
	if (!ReadHKLDB(loader, camInfo) || !ReadHKINI(loader, camInfo))
		return false;
	if (0 >= camInfo.num_Parts)
		return false;
	ASSERT(NULL == camInfo.pCAMShape && NULL == _cpPartInfo);

	camInfo.pPart = new CAM_PART[camInfo.num_Parts];
	if (NULL == camInfo.pPart)
		return false;
	_numPartsTotal = camInfo.num_Parts;
	_cpPartInfo = camInfo.pPart;

	// ii. HKOST list: CAM part geometries and attributes
	CStringList strHKOSTList;
	bool bPartsFinished = false;
	do
	{
		if (!ReadHKOSTv16(loader, strHKOSTList, bPartsFinished))
			return false;
	}
	while (!bPartsFinished);

	// CAM part nesting information completed.
	//     Check the end of CAM information and initialize information buffers
	int numPartShapes = 0;
	if (!ReadProcEndMcode(loader, numPartShapes)
		|| !InitCAMPartBuf(verV16, strHKOSTList, camInfo, numPartShapes))
		return false;

	//
	// Secondly, get CAM information itself
	//
	CPtrContourList ptrListContour;

	int iCAMPart = 0;
	bool bReachEnd = false, bIsNCPart = false;
	bPartsFinished = false;

	do
	{
		CAM_CONTOUR contour;

		if (!ReadContour(loader, contour, bReachEnd, bPartsFinished))
			break;

		if (!AddToContourList(ptrListContour, contour))
			return false;

		if (bPartsFinished)
		{
			bool bOk = UpdateCAMPartInfo(ptrListContour, loader.getLastLineNo(), camInfo, iCAMPart);
			DeleteContourList(ptrListContour);
			if (!bOk)
				return false;

			//bIsNCPart = bPartComplete = false;
			bPartsFinished = false;
			continue;
		}

		if (bReachEnd)	// Met HKSCR and got the last remnant cut information
		{
			if (camInfo.IsNCPart(contour.N_blockNo))
			{
				ASSERT(iCAMPart+1 == camInfo.num_Shapes && 1 == ptrListContour.GetCount());
				if (!UpdateCAMPartInfo(ptrListContour, loader.getLastLineNo(), camInfo, iCAMPart))
				{
					DeleteContourList(ptrListContour);
					return false;
				}
			}
			break;
		}
	}
	while (1);

	DeleteContourList(ptrListContour);

	//Check if the while-loop ends up with any errors.
	if (!bReachEnd)
		return false;

	for (int i = 0; i < camInfo.num_Parts; ++i)
	{
		int No = camInfo.pPart[i].nCAMpart_BlockNo;
		camInfo.pPart[i].iCAMShape = -1;
		for (int j = 0; j < camInfo.num_Shapes; ++j)
		{
			if (No == camInfo.pCAMShape[j].nBlockIdNo)
			{
				camInfo.pPart[i].iCAMShape = j;
				break;
			}
		}

		if (0 > camInfo.pPart[i].iCAMShape)
		{
			PrintLog(enLogError, _T("No CAD part information for NCPart[%d]"), i);
			camInfo.Delete();
			return false;
		}
	}

	return true;
}


bool InterfaceMPF::LoadV16A05(FileMPF& loader, CAM_DATA& camInfo)
{
	//
	// First, get part nesting information
	//

	// i. HKLDB & HKINI : initialization information
	if (!ReadHKLDB(loader, camInfo) || !ReadHKINI(loader, camInfo))
		return false;
	if (0 >= camInfo.num_Parts)
		return false;
	ASSERT(NULL == camInfo.pCAMShape && NULL == _cpPartInfo);

	camInfo.pPart = new CAM_PART[camInfo.num_Parts];
	if (NULL == camInfo.pPart)
		return false;
	_numPartsTotal = camInfo.num_Parts;
	_cpPartInfo = camInfo.pPart;

	// ii. HKOST list: CAM part geometries and attributes
	CStringList strHKOSTList;
	bool bPartsFinished = false;
	do
	{
		if (!ReadHKOSTv16(loader, strHKOSTList, bPartsFinished))
			return false;
	}
	while (!bPartsFinished);

	// CAM part nesting information completed.
	//     Check the end of CAM information and initialize information buffers
	//		while getting the number of part shape objects whose information is listed after the M30, program end code
	int numPartShapes = 0;
	if (!ReadProcEndMcode(loader, numPartShapes)
		|| !InitCAMPartBuf(verV16, strHKOSTList, camInfo, numPartShapes))
		return false;

	//
	// Secondly, get CAM information itself
	//
	CPtrContourList ptrListContour;

	int iCAMPart = 0;
	bool bReachEnd = false, bIsNCPart = false;
	bPartsFinished = false;

	do
	{
		CAM_CONTOUR contour;

		if (!ReadContourV16A05(loader, contour, bReachEnd, bPartsFinished))
			break;

		if (!AddToContourList(ptrListContour, contour))
			return false;

		if (bPartsFinished)
		{
			bool bOk = UpdateCAMPartInfo(ptrListContour, loader.getLastLineNo(), camInfo, iCAMPart);
			DeleteContourList(ptrListContour);
			if (!bOk)
				return false;

			//bIsNCPart = bPartComplete = false;
			bPartsFinished = false;
			continue;
		}

		if (bReachEnd)	// Met HKSCR and got the last remnant cut information
		{
			if (camInfo.IsNCPart(contour.N_blockNo))
			{
				ASSERT(iCAMPart+1 == camInfo.num_Shapes && 1 == ptrListContour.GetCount());
				if (!UpdateCAMPartInfo(ptrListContour, loader.getLastLineNo(), camInfo, iCAMPart))
				{
					DeleteContourList(ptrListContour);
					return false;
				}
			}
			break;
		}
	}
	while (1);

	DeleteContourList(ptrListContour);

	//Check if the while-loop ends up with any errors.
	if (!bReachEnd)
		return false;

	for (int i = 0; i < camInfo.num_Parts; ++i)
	{
		int No = camInfo.pPart[i].nCAMpart_BlockNo;
		camInfo.pPart[i].iCAMShape = -1;
		for (int j = 0; j < camInfo.num_Shapes; ++j)
		{
			if (No == camInfo.pCAMShape[j].nBlockIdNo)
			{
				camInfo.pPart[i].iCAMShape = j;
				break;
			}
		}

		if (0 > camInfo.pPart[i].iCAMShape)
		{
			PrintLog(enLogError, _T("No CAD part information for NCPart[%d]"), i);
			camInfo.Delete();
			return false;
		}
	}

	return true;
}

bool InterfaceMPF::ReadHKLDB(FileMPF& loader, CAM_DATA& camInfo)
{
	CString str;
	int nBlockNo;
	while (loader.getNextBlock(str))
	{
		CString strProc;
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall == token && _T("HKLDB") == strProc)
		{
			switch (loader.getVersion())
			{
			case verV08:
				if (2 <= strArgs.GetCount())
				{
					GetHKLDBv08(strArgs, camInfo);
					return true;
				}
				else
					return true;
				break;
			case verV16: case verV16A05:
				if (3 <= strArgs.GetCount())
				{
					GetHKLDBv16(strArgs, camInfo);
					return true;
				}
				break;
			default:
				break;
			}
			PrintLog(enLogError, _T("Reading HKLDB() in %s: MPF file version is not specified."), loader.getFilename());
			return false;
		}
	}

	PrintLog(enLogError, _T("HKLDB() is missing from this MPF file, %s."), loader.getFilename());
	return false;
}


bool InterfaceMPF::ReadHKINI(FileMPF& loader, CAM_DATA& camInfo)
{
	CString str;

	while (loader.getNextBlock(str))
	{
		int nBlockNo;
		CString strProc;
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall != token || !(_T("HKINI")==strProc))
		{
			if (tokenNull != token)
				PrintLog(enLogWarning, _T("  Warning: unexpected token(%s) where HKINI() is expceted"), strProc);
			continue;
		}

		if (3 <= strArgs.GetCount())
		{
			switch (loader.getVersion())
			{
			case verV08:
				GetHKINIv08(strArgs, camInfo);
				break;
			case verV16:	case verV16A05:
				GetHKINIv16(strArgs, camInfo);
				break;
			default:
				PrintLog(enLogError, _T("Reading HKINI(), MPF file Version is not specified."));
				return false;
				break;
			}
		}
		else
		{
			PrintLog(enLogError, _T("Reading HKINI(), invalid number of arguments in HKINI()."));
			return false;
		}
		return true;
	}

	PrintLog(enLogError, _T("HKINI() is missing from this MPF file, %s."), loader.getFilename());
	return false;
}


bool InterfaceMPF::ReadHKOSTv08(FileMPF& loader, CStringList& strList_HKOST, bool& bPartsFinished)
{
	CString str;

	while (loader.getNextBlock(str))
	{
		int nBlockNo;
		CString strProc;
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall != token)
		{
			if (tokenNull != token)
				PrintLog(enLogWarning, _T("  Warning: unexpected token(%s)"), strProc);
			continue;
		}

		// token is 'tokenProcCall'
		if (_T("HKEND") == strProc)
		{
			bPartsFinished = true;
			return true;
		}
		if (_T("HKOST")==strProc)
		{
			return GetHKOSTv08(loader.getLastLineNo(), strProc, strArgs, strList_HKOST);
		}
		PrintLog(enLogWarning, _T("  Warning: unexpected procedure call(%s) where HKOST() is expected"), strProc);
	}

	PrintLog(enLogError, _T("HKOST() is missing from this MPF file, %s."), loader.getFilename());
	return false;
}

inline void DeleteCAMElemList(CPtrList& elemList)
{
	POSITION pos = elemList.GetHeadPosition();
	while (pos)
	{
		CAM_CODE* p = (CAM_CODE*)elemList.GetNext(pos);
		delete p;
	}
}

inline bool CopyCAMElement(CAM_PART& partInfo, CPtrList& elemList)
{
	partInfo.nDetours = elemList.GetCount();
	partInfo.pDetour = new CAM_CODE[partInfo.nDetours];
	if (partInfo.pDetour)
	{
		POSITION pos = elemList.GetHeadPosition();
		for (int i = 0; i < partInfo.nDetours && pos; ++i)
		{
			partInfo.pDetour[i] = *(CAM_CODE*)elemList.GetNext(pos);
		}
		return true;
	}
	return false;
}

bool InterfaceMPF::ReadHKOSTv16(FileMPF& loader, CStringList& strList_HKOST, bool& bPartsFinished)
{
	CString str;
	CPtrList camElemList;

	int iOST = strList_HKOST.GetCount();

	while (loader.getNextBlock(str))
	{
		int nBlockNo, nLineNo = loader.getLastLineNo();
		CString strProc;
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall != token)
		{
			if (0 < iOST && iOST < _numPartsTotal && tokenGcode == token)
				GetGCodeLastPos(strProc, strArgs, camElemList);
			continue;
		}

		// token is 'tokenProcCall'

		if (_T("HKPPP") == strProc)
			continue;

		if (_T("HKEND") == strProc)
		{
			if (0 < iOST && 0 < camElemList.GetCount())
				CopyCAMElement(_cpPartInfo[iOST-1], camElemList);
			DeleteCAMElemList(camElemList);
			bPartsFinished = true;
			return true;
		}

		if (_T("HKOST")==strProc)
		{
			if (_numPartsTotal <= iOST)
			{
				PrintLog(enLogError,
						 _T("Error at line#%d: the number of %s() exceeds that specified in HKINI()."),
						 loader.getLastLineNo(), strProc);
				return false;
			}
			_cpPartInfo[iOST].iCmdBlockStr = nLineNo - 1;
			bool bOk = GetHKOSTv16(nLineNo, strProc, strArgs, strList_HKOST);
			if (bOk && 0 < camElemList.GetCount())
			{
				ASSERT(0 < iOST);
				CopyCAMElement(_cpPartInfo[iOST-1], camElemList);
			}
			DeleteCAMElemList(camElemList);
			return bOk;
		}
		PrintLog(enLogWarning,
				 _T("  Warning: unexpected procedure call(%s) at line %d where HKOST() is expected."),
				 strProc, nLineNo);
	}

	PrintLog(enLogError, _T("HKOST() is missing from this MPF file, %s."), loader.getFilename());
	DeleteCAMElemList(camElemList);
	return false;
}


bool InterfaceMPF::ReadProcEndMcode(FileMPF& loader, int& numPartShapes)
{
	CString str;

	while (loader.getNextBlock(str))
	{
		int nBlockNo;
		CString strProc;
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenMcode == token)
		{
			if (_T("M30") == strProc)
				goto countPartShapes;
		}
	}
	PrintLog(enLogError, _T("The procedure-end M code, M30 has not been found."));
	return false;

countPartShapes:

	int iLineProgramEnd = loader.getCurrLineIndex();

	numPartShapes = 0;
	while (loader.getNextBlock(str))
	{
		if (0 < str.Find(_T("HKPED")))
			++numPartShapes;
	}

	loader.moveToLine(iLineProgramEnd);

	return true;
}


bool InterfaceMPF::InitCAMPartBuf(fileVersion version, const CStringList& strHKOSTList, CAM_DATA& camInfo, const int numPartShapes)
{
	int countNCParts = strHKOSTList.GetCount();
	if (0 >= countNCParts)
	{
		PrintLog(enLogError, _T("Invalid number of NC parts = %d."), countNCParts);
		return false;
	}
	ASSERT(countNCParts <= camInfo.num_Parts);
	if (countNCParts < camInfo.num_Parts)	// restart MPF can have less number of parts than the original MPF
	{
		camInfo.num_Parts = countNCParts;
	}

	CAM_PART* pNCPart = camInfo.pPart;
	POSITION pos = strHKOSTList.GetHeadPosition();
	if (!GetHKOSTPartInfo(version, strHKOSTList.GetNext(pos), pNCPart[0]))
	{
		PrintLog(enLogError, _T("Error in getting NC part information of the first HKOST()."));
		delete[] pNCPart;
		return false;
	}

	for (int i = 1; i < countNCParts; ++i)
	{
		if (!GetHKOSTPartInfo(version, strHKOSTList.GetNext(pos), pNCPart[i]))
		{
			PrintLog(enLogError, _T("Error in getting NC part information of the HKOST() #%d."), i+1);
			delete[] pNCPart;
			return false;
		}
	}

	ASSERT(!camInfo.HasContents());
	if (0 >= numPartShapes)
	{
		ASSERT(FALSE);
		PrintLog(enLogError, _T("Invalid number of CAM parts = %d (excluding duplicated start block numbers)."), numPartShapes);
		delete[] pNCPart;
		return false;
	}
	camInfo.pCAMShape = new CAM_SHAPE[numPartShapes];
	if (!camInfo.pCAMShape)
	{
		PrintLog(enLogError, _T("Out of memory in allocating CAD part information buffer."));
		return false;
	}
	camInfo.num_Shapes = numPartShapes;

	return true;
}


//////////////////////////////////////////////////////////////////////////


bool InterfaceMPF::ReadHKLON(FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd)
{
	CString str;

	while (loader.getNextBlock(str))
	{
		int nBlockNo, nLineNo = loader.getLastLineNo();
		CString strProc;
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall == token)
		{
			if (_T("HKLON")==strProc)
			{
				if (0 >= nBlockNo || 6 > strArgs.GetCount())
				{
					PrintLog(enLogError, _T("Invalid HKLON() while getting the contour information."));
					return false;
				}
				contour.N_blockNo = nBlockNo;
				contour.iCmdBlockStr = nLineNo - 1;
				return GetHKLON(nLineNo, strArgs, contour);
			}
			else if (_T("HKSCR") == strProc)
			{
				if (0 >= nBlockNo || 4 > strArgs.GetCount())
				{
					PrintLog(enLogError, _T("Invalid HKSCR() while getting the contour information."));
					return false;
				}
				contour.N_blockNo = nBlockNo;
				return ReadHKSCR(strArgs, loader, contour, bReachEnd);
			}
		}
	}

	bReachEnd = true;
	return true;
}


bool InterfaceMPF::ReadHKTON(FileMPF& loader, CAM_CONTOUR& contour)
{
	CString str;

	while (loader.getNextBlock(str))
	{
		int nBlockNo;
		CString strProc;
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall == token && _T("HKTON")==strProc)
		{
			if (1 <= strArgs.GetCount() && IsNumber(strArgs.GetHead(), 0))
			{
				contour.nToolComp = _tstoi(strArgs.GetHead());
				return true;
			}
			else
			{
				PrintLog(enLogError, _T("Invalid HKTON argument in the first contour."));
				return false;
			}
		}
	}

	PrintLog(enLogError, _T("HKTON() is missing from this MPF file, %s."), loader.getFilename());
	return false;
}


bool InterfaceMPF::FindHKTOF(FileMPF& loader, CAM_CONTOUR& contour)
{
	CString str, strGcode;
	CStringList strGcodeList, strBlockList;
	CIntArray arrLineNo;
	arrLineNo.setSize(1024);

	while (loader.getNextBlock(str))
	{
		int nBlockNo, nLineNo = loader.getLastLineNo();
		CString strProc;
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);

		switch (token)
		{
		case tokenGcode:
			if (!AddGcodeBlockToList(nLineNo, strProc, strArgs, strGcodeList))
				return false;
			strBlockList.AddTail(str);
			arrLineNo.add(nLineNo);
			break;
		case tokenProcCall:
			if (_T("HKTOF") == strProc)
			{
				if (GetHKTOFArgsGcmd(nLineNo, strArgs, strGcode))
				{
					strGcodeList.AddTail(strGcode);
					strBlockList.AddTail(str);
					arrLineNo.add(nLineNo);
					return BuildContourPath(strGcodeList, strBlockList, arrLineNo, contour);
				}
				return false;
			}
			break;
		case tokenNull: case tokenStateNo:
			// Just continue to the next line
			break;
		case tokenMcode:
			PrintLog(enLogWarning, _T("  Warning: unexpected M-code %s() at Line#%d."), strProc, nLineNo);
			break;	// nothing to be done for M code here
		default:
			PrintLog(enLogError, _T("Unknown token at Line#%d: %s"), nLineNo, str);
			return false;
			break;
		}
	}

	PrintLog(enLogError, _T("HKTOF() is missing from this MPF file, %s."), loader.getFilename());
	return false;
}


bool InterfaceMPF::FindHKLOF(FileMPF& loader, bool& bReachEnd)
{
	CString str;

	while (loader.getNextBlock(str))
	{
		int nBlockNo;
		CString strProc;
		CStringList strArgs;

		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall == token)
		{
			if (_T("HKPED") == strProc)
				bReachEnd = true;
			if (_T("HKLOF") == strProc)
				return true;
		}
	}

	PrintLog(enLogError, _T("HKLOF() is missing from this MPF file, %s."), loader.getFilename());
	return false;
}


bool InterfaceMPF::ReadHKSCR(CStringList& strArgs, FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd)
{
	ASSERT(NULL == contour.pCode);
	contour.nPiercing = -1;
	contour.bIsRemnant = true;

	// Get the first HSKCR arguments
	POSITION pos = strArgs.GetHeadPosition();

	int nMode;
	if (!GetIntArg(strArgs, pos, nMode) || 0 != nMode)
	{
		PrintLog(enLogError, _T("Invalid mode HKLON() \'%s\'."), strArgs.GetHead());
		return false;
	}
	if (!GetIntArg(strArgs, pos, nMode))
	{
		PrintLog(enLogError, _T("Invalid cutting condition of HKSCR()."));
		return false;
	}
	contour.nCutting = nMode;

	if (!GetfloatArg(strArgs, pos, contour.x_start))
	{
		PrintLog(enLogError, _T("Invalid x-start position in HKLON()."));
		return false;
	}
	if (!GetfloatArg(strArgs, pos, contour.y_start))
	{
		PrintLog(enLogError, _T("Invalid y-start position in HKLON()."));
		return false;
	}

	CString str, strProc;
	int nBlockNo, nLineNo;
	CStringList strListGcode, strListBlock;
	CIntArray arrLineNo;
	arrLineNo.setSize(256);

	while (loader.getNextBlock(str))
	{
		nLineNo = loader.getLastLineNo();

		strArgs.RemoveAll();

		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall == token)
		{
			if (_T("HKLOF") == strProc)
			{
				return BuildContourPath(strListGcode, strListBlock, arrLineNo, contour);
			}
			else if (_T("HKPED") == strProc)
			{
				bReachEnd = true;
			}
			else if (_T("HKSCR") != strProc)
			{
				PrintLog(enLogWarning, _T("  Warning: unexpected procedure name %s while getting remnant-cut information."), strProc);
			}
		}
		else if (tokenGcode == token)
		{
			if (!AddGcodeBlockToList(nLineNo, strProc, strArgs, strListGcode))
				return false;
			strListBlock.AddTail(str);
			arrLineNo.add(nLineNo);
		}
	}

	PrintLog(enLogError, _T("Cannot complete remnant-cut information with HKSCR()."));
	return false;
}


bool InterfaceMPF::ReadThroughHKLOF(FileMPF& loader)
{
	int nBlockNo;
	CString str, strProc;

	// HKTON
	while (loader.getNextBlock(str))
	{
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall == token && _T("HKTON")==strProc)
		{
			if (1 <= strArgs.GetCount() && IsNumber(strArgs.GetHead(), 0))
				goto HKTOF;
			PrintLog(enLogError, _T("Invalid HKTON() argument at line#%d."), loader.getLastLineNo());
			return false;
		}
	}

	PrintLog(enLogError, _T("HKLON() is missing from this MPF file, %s."), loader.getFilename());
	return false;

HKTOF:
	while (loader.getNextBlock(str))
	{
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);

		switch (token)
		{
		case tokenGcode:
			if (!CheckGcodeBlock(loader.getLastLineNo(), strProc, strArgs))
				return false;
			break;
		case tokenProcCall:
			if (_T("HKTOF") == strProc)
			{
				if (GetHKTOFArgsGcmd(loader.getLastLineNo(), strArgs, str))
					goto HKLOF;
				else
					return false;
			}
			break;
		case tokenNull: case tokenStateNo:
			// Just continue to the next line
			break;
		case tokenMcode:
			PrintLog(enLogWarning, _T("  Warning: unexpected M-code %s() at Line#%d."), strProc, loader.getLastLineNo());
			break;	// nothing to be done for M code here
		default:
			PrintLog(enLogError, _T("While looking for HKTOF(), unknown token at Line#%d: %s."), loader.getLastLineNo(), str);
			return false;
			break;
		}
	}

	PrintLog(enLogError, _T("HKTOF() is missing from this MPF file, %s."), loader.getFilename());
	return false;

HKLOF:
	while (loader.getNextBlock(str))
	{
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall == token)
		{
			if (_T("HKLOF") == strProc)
				return true;
			else if (_T("HKPED") != strProc)
				PrintLog(enLogWarning, _T("  Warning unexpected procedure call %s at line#%d."), strProc, loader.getLastLineNo());
		}
	}

	PrintLog(enLogError, _T("HKLOF() is missing from this MPF file, %s."), loader.getFilename());
	return false;
}


bool InterfaceMPF::GetHKLON(int nLineNo, const CStringList& strArgs, CAM_CONTOUR& contour)
{
	POSITION pos = strArgs.GetHeadPosition();

	int N;
	// Cutting condition
	if (GetIntArg(strArgs, pos, N))
	{
		contour.nCutting = N;
	}
	else
	{
		PrintLog(enLogError, _T("Invalid cutting condition in HKLON() at line#%d."), nLineNo);
		return false;
	}

	// Piercing condition
	if (GetIntArg(strArgs, pos, N))
	{
		contour.nPiercing = N;
	}
	else
	{
		PrintLog(enLogError, _T("Invalid piercing method in HKLON() at line#%d."), nLineNo);
		return false;
	}

	// Part start position X
	if (!GetfloatArg(strArgs, pos, contour.x_start))
	{
		PrintLog(enLogError, _T("Invalid x-start position in HKLON() at line#%d."), nLineNo);
		return false;
	}
	// Part start position Y
	if (!GetfloatArg(strArgs, pos, contour.y_start))
	{
		PrintLog(enLogError, _T("Invalid y-start position in HKLON() at line#%d."), nLineNo);
		return false;
	}

	// Part dimension in X
	if (!GetfloatArg(strArgs, pos, contour.width))
	{
		PrintLog(enLogError, _T("Invalid contour width in HKLON() at line#%d."), nLineNo);
		return false;
	}
	// Part dimension in Y
	if (!GetfloatArg(strArgs, pos, contour.height))
	{
		PrintLog(enLogError, _T("Invalid contour height in HKLON() at line#%d."), nLineNo);
		return false;
	}

	return true;
}


//////////////////////////////////////////////////////////////////////////


bool InterfaceMPF::ReadContour(FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd, bool& bPartCompleted)
{
	CString str;
	CStringList strGcodeList, strBlockList;
	CIntArray arrLineNo;
	arrLineNo.setSize(1024);

	bool bContourStarted = false;

	while (loader.getNextBlock(str))
	{
		int nBlockNo, nLineNo = loader.getLastLineNo();	// 1-based index of the block line
		CString strProc;
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		switch (token)
		{
		case tokenType::tokenProcCall:
			if (_T("HKSTR") == strProc)
			{
				if (0 >= nBlockNo || 4 > strArgs.GetCount())
				{
					PrintLog(enLogError, _T("Invalid HKSTR() at line %d."), nLineNo);
					return false;
				}
				contour.N_blockNo = nBlockNo;
				if (!GetHKSTR(nLineNo, strArgs, contour))
					return false;

				contour.iCmdBlockStr = nLineNo - 1;
				bContourStarted = true;
			}
			else if (_T("HKSTO") == strProc)	// End of Contour
			{
				bContourStarted = false;
				contour.iCmdBlockStrLast = nLineNo - 1;
				return BuildContourPath(loader, strGcodeList, strBlockList, arrLineNo, contour, bPartCompleted);
			}
			else if (_T("HKSCRC") == strProc)
			{
				if (0 >= nBlockNo || 4 > strArgs.GetCount())
				{
					PrintLog(enLogError, _T("Invalid HKSCRC() at line %d"), nLineNo);
					return false;
				}
				contour.N_blockNo = nBlockNo;
				bReachEnd = ReadHKSCRC(strArgs, loader, contour);
				return bReachEnd;
			}
			break;

		case tokenType::tokenGcode:
			if (!bContourStarted)		// Detour path
			{
				ASSERT(FALSE);
				break;
			}
			// G42, G43은 절폭 보정량 관련이므로 미리보기, 경로보기에 대하여 크게 영향을 주지 않으므로 보여주지 않는다. 2017.04.03 Add By ET
			// G0~3이 아닐 경우 모두 무시할 수 있도록 변경해달라는 김효상 프로 요청.	2017.04.10 Edit By ET
			if (_T("G0") != strProc && _T("G1") != strProc && _T("G2") != strProc && _T("G3") != strProc)
				break;

			if (!AddGcodeBlockToList(nLineNo, strProc, strArgs, strGcodeList))
				return false;
			strBlockList.AddTail(str);
			arrLineNo.add(nLineNo);	// the previous line was the G-code block line
			break;
		}
	}
	bReachEnd = true;
	return true;
}

inline CString& getEffectiveGcodeBlock(enHKProc proc, G_Code code, CString& strBlock)
{
	switch (proc)
	{
	case enHKLEA:
		switch (code)
		{
		case G_Line:	strBlock = _T("GC11: G1 X=_LEA_X1 Y=_LEA_Y1");	break;
		case G_ArcCW:	strBlock = _T("GC12: G2 X=_LEA_X1 Y=_LEA_Y1 I=_LEA_I1 J=_LEA_J1");	break;
		case G_ArcCCW:	strBlock = _T("GC13: G3 X=_LEA_X1 Y=_LEA_Y1 I=_LEA_I1 J=_LEA_J1");	break;
		default:		break;
		}
		break;
	case enHKSTO:
		switch (code)
		{
		case G_Line:	strBlock = _T("GC11: G1 X=_STO_X1 Y=_STO_Y1");	break;
		case G_ArcCW:	strBlock = _T("GC12: G2 X=_STO_X1 Y=_STO_Y1 I=_STO_I1 J=_STO_J1");	break;
		case G_ArcCCW:	strBlock = _T("GC13: G3 X=_STO_X1 Y=_STO_Y1 I=_STO_I1 J=_STO_J1");	break;
		default:		break;
		}
		break;
	default:
		break;
	}
	return strBlock;
}


bool InterfaceMPF::GetHKLEAV16A05Args(CString& str, CStringList& strArgs, CStringList& strGcodeList, CStringList& strBlockList, CAM_CONTOUR& contour)
{
	CString strGcode;
	G_Code Gcode = GetContourParamsInArg(strArgs, strGcode);
	if (G_Unknown == Gcode)
		return false;

	if (G_Jump == Gcode)
	{
		// No lead-in, skip over this block
		contour.bHasLeadIn = false;
	}
	else
	{
		contour.bHasLeadIn = true;
		strGcodeList.AddTail(strGcode);
		strBlockList.AddTail(getEffectiveGcodeBlock(enHKLEA, Gcode, str));
	}

	return true;
}

bool InterfaceMPF::GetHKCUTV16A05Args(CString& str, CStringList& strArgs, CAM_CONTOUR& contour)
{
	POSITION pos = strArgs.GetHeadPosition();

	int nScancut = 0;
	if (!GetIntArg(strArgs, pos, nScancut))
		return false;

	contour.bIsScancut = nScancut == 1 ? true : false;
	return true;
}

bool InterfaceMPF::GetHKSCRC_HKSTOArgs(CString& str, CStringList& strArgs, CStringList& strGcodeList, CStringList& strBlockList, CAM_CONTOUR& contour)
{
	CString strGcode;
	G_Code Gcode = GetContourParamsInArg(strArgs, strGcode);
	if (G_Jump == Gcode)
	{// do nothing. HKSTO(0,0,0) in HKSCRC block is just the terminal punctuation.
		double x, y;
		if (GetGcodeDestination(strGcode, x, y) && Point2d(x,y) == Point2d(0,0))
			return true;
	}
	else
	{
		if (!(G_Line == Gcode || G_ArcCW == Gcode || G_ArcCCW == Gcode))
			return false;
	}

	strGcodeList.AddTail(strGcode);
	strBlockList.AddTail(getEffectiveGcodeBlock(enHKSTO, Gcode, str));

	return true;
}


bool InterfaceMPF::GetHKSTOV16A05Args(CString& str, CStringList& strArgs, CStringList& strGcodeList, CStringList& strBlockList, CAM_CONTOUR& contour)
{
	CString strGcode;
	G_Code Gcode = GetContourParamsInArg(strArgs, strGcode);
	if (!(G_Line == Gcode || G_ArcCW == Gcode || G_ArcCCW == Gcode))
		return false;

	strGcodeList.AddTail(strGcode);
	strBlockList.AddTail(getEffectiveGcodeBlock(enHKSTO, Gcode, str));
	return true;
}


bool InterfaceMPF::ReadContourV16A05(FileMPF& loader, CAM_CONTOUR& contour, bool& bReachEnd, bool& bPartCompleted)
{
	CString str;
	int nBlockNo, nLineNo;
	CStringList strGcodeList, strBlockList;
	CIntArray arrLineNo;
	arrLineNo.setSize(1024);

	bool bContourStarted = false;

	while (loader.getNextBlock(str))
	{
		nLineNo = loader.getLastLineNo();

		CString strProc;
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		switch (token)
		{
		case tokenType::tokenProcCall:
			switch (discriminateHKSPF(strProc))
			{
			case enHKSTR:
				if (0 >= nBlockNo || 4 > strArgs.GetCount())
				{
					PrintLog(enLogError, _T("Invalid HKSTR() at line %d."), nLineNo);
					return false;
				}
				contour.N_blockNo = nBlockNo;
				if (!GetHKSTR(nLineNo, strArgs, contour))
					return false;
				contour.iCmdBlockStr = nLineNo - 1;
				bContourStarted = true;
				break;
			case enHKLEA:
				if (!GetHKLEAV16A05Args(str, strArgs, strGcodeList, strBlockList, contour))
				{
					PrintLog(enLogError, _T("Invalid HKLEA() at line %d."), nLineNo);
					return false;
				}
				if (contour.bHasLeadIn)
					arrLineNo.add(nLineNo);	// the previous line is HKLEA line
				break;
			case enHKCUT:	//To get something that 'Scan Cut' is included or not.
				if (!GetHKCUTV16A05Args(str, strArgs, contour))
				{
					PrintLog(enLogError, _T("Invalid HKCUT() at line %d."), nLineNo);
					return false;
				}
				break;
			case enHKSTO:
				if (!GetHKSTOV16A05Args(str, strArgs, strGcodeList, strBlockList, contour))
				{
					PrintLog(enLogError, _T("Invalid HKSTO() at line %d."), nLineNo);
					return false;
				}
				bContourStarted = false;
				contour.iCmdBlockStrLast = nLineNo - 1;	// zero-based index of the previous line numbers
				arrLineNo.add(nLineNo);	// the previous line is HKSTO line
				return BuildContourPath(loader, strGcodeList, strBlockList, arrLineNo, contour, bPartCompleted);
				break;
			case enHKSCRC:
				if (0 >= nBlockNo || 4 > strArgs.GetCount())
				{
					PrintLog(enLogError, _T("Invalid HKSCRC() at line %d"), nLineNo);
					return false;
				}
				contour.N_blockNo = nBlockNo;
				return ReadHKSCRCV16A05(strArgs, loader, contour, bPartCompleted);
				break;
			default: break;
			}
			break;

		case tokenType::tokenGcode:
			if (!bContourStarted)		// Detour path
			{
				ASSERT(FALSE);
				break;
			}
			// All the G-codes other than G0~G03 are intentionally ignored by request from Hyo-Sang Kim.
			// This modification work from its original version was conducted by EunTack Choi @	2017.04.10
			if (_T("G0") != strProc && _T("G1") != strProc && _T("G2") != strProc && _T("G3") != strProc)
				break;

			if (!AddGcodeBlockToList(nLineNo, strProc, strArgs, strGcodeList))
				return false;
			strBlockList.AddTail(str);
			arrLineNo.add(nLineNo);	// the previous line is G-code block
			break;

		default:
			break;
		}
	}
	bReachEnd = true;
	return true;
}


bool InterfaceMPF::ReadHKSCRC(CStringList& strArgs, FileMPF& loader, CAM_CONTOUR& contour)
{
	ASSERT(NULL == contour.pCode);
	contour.nPiercing = PIERCING_V16_NONE;
	contour.nToolComp = 0;
	contour.bIsRemnant = true;
	contour.iCmdBlockStr = loader.getLastLineNo() - 1;

	// Get the first HSKCR arguments
	POSITION pos = strArgs.GetHeadPosition();

	int N;

	// Scrap cutting approach mode
	if (!GetIntArg(strArgs, pos, N) || 0 != N)
	{
		PrintLog(enLogError, _T("Invalid scrap cutting start info at line %d"), loader.getLastLineNo());
		return false;
	}

	// Cutting condition
	if (GetIntArg(strArgs, pos, N))
	{
		contour.nCutting = N;
	}
	else
	{
		PrintLog(enLogError, _T("Invalid cutting condition in HKSTR() at line %d"), loader.getLastLineNo());
		return false;
	}

	// Part start position X
	if (!GetfloatArg(strArgs, pos, contour.x_start))
	{
		PrintLog(enLogError, _T("Invalid x-start position in HKSTR() at line %d"), loader.getLastLineNo());
		return false;
	}
	// Part start position Y
	if (!GetfloatArg(strArgs, pos, contour.y_start))
	{
		PrintLog(enLogError, _T("Invalid y-start position in HKSTR() at line %d"), loader.getLastLineNo());
		return false;
	}

	CString str, strProc;
	int nBlockNo, nLineNo = loader.getLineNoToRead();
	CStringList strListGcode, strListBlock;
	CIntArray arrLineNo;
	arrLineNo.setSize(256);

	while (loader.getNextBlock(str))
	{
		nLineNo = loader.getLastLineNo();	// returns the zero-based index of the next line == 1-based previous line number
		strArgs.RemoveAll();

		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall == token)
		{
			if (_T("HKSTO") == strProc)
			{
				contour.iCmdBlockStrLast = nLineNo - 1;
				return BuildContourPath(strListGcode, strListBlock, arrLineNo, contour);
			}
			else if (_T("HKSCRC") != strProc && _T("HKPED") != strProc)
			{
				PrintLog(enLogWarning, _T("  Warning: unexpected procedure name %s while getting remnant-cut information"), strProc);
			}
		}
		else if (tokenGcode == token)
		{
			if (!AddGcodeBlockToList(nLineNo, strProc, strArgs, strListGcode))
				return false;
			strListBlock.AddTail(str);
			arrLineNo.add(nLineNo);	// the previous line was the G-code block
		}
	}

	PrintLog(enLogError, _T("Cannot complete remnant-cut information with HKSCRC() at line %d"), nLineNo);
	return false;
}


bool InterfaceMPF::ReadHKSCRCV16A05(CStringList& strArgs, FileMPF& loader, CAM_CONTOUR& contour, bool& bPartCompleted)
{
	ASSERT(NULL == contour.pCode);
	contour.nPiercing = PIERCING_V16_NONE;
	contour.nToolComp = 0;
	contour.bIsRemnant = true;
	contour.iCmdBlockStr = loader.getLastLineNo() - 1; 	// returns the zero-based index of the next line == 1-based previous line number

	// Get the first HSKCR arguments
	POSITION pos = strArgs.GetHeadPosition();

	int N;

	// Scrap cutting approach mode
	if (!GetIntArg(strArgs, pos, N) || 0 != N)
	{
		PrintLog(enLogError, _T("Invalid scrap cutting start info at line %d"), loader.getLastLineNo());
		return false;
	}

	// Cutting condition
	if (GetIntArg(strArgs, pos, N))
	{
		contour.nCutting = N;
	}
	else
	{
		PrintLog(enLogError, _T("Invalid cutting condition in HKSTR() at line %d"), loader.getLastLineNo());
		return false;
	}

	// Part start position X
	if (!GetfloatArg(strArgs, pos, contour.x_start))
	{
		PrintLog(enLogError, _T("Invalid x-start position in HKSTR() at line %d"), loader.getLastLineNo());
		return false;
	}
	// Part start position Y
	if (!GetfloatArg(strArgs, pos, contour.y_start))
	{
		PrintLog(enLogError, _T("Invalid y-start position in HKSTR() at line %d"), loader.getLastLineNo());
		return false;
	}

	CString str, strProc;
	int nBlockNo, nLineNo;
	CStringList strListGcode, strListBlock;
	CIntArray arrLineNo;
	arrLineNo.setSize(256);

	while (loader.getNextBlock(str))
	{
		nLineNo = loader.getLastLineNo();	// returns the zero-based index of the next line == 1-based previous line number
		strArgs.RemoveAll();

		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		if (tokenProcCall == token)
		{
			switch (discriminateHKSPF(strProc))
			{
			case enHKLEA:
				if (!GetHKLEAV16A05Args(str, strArgs, strListGcode, strListBlock, contour))
				{
					PrintLog(enLogError, _T("Invalid HKLEA() at line %d."), nLineNo);
					return false;
				}
				if (contour.bHasLeadIn)
					arrLineNo.add(nLineNo);	// the previous line was HKLEA line
				break;
			case enHKSTO:
				if (!GetHKSCRC_HKSTOArgs(str, strArgs, strListGcode, strListBlock, contour))
				{
					PrintLog(enLogError, _T("Invalid HKSTO() at line %d."), nLineNo);
					return false;
				}
				contour.iCmdBlockStrLast = nLineNo - 1;	// zero-based index of the previous line
				arrLineNo.add(nLineNo);
				return BuildContourPath(loader, strListGcode, strListBlock, arrLineNo, contour, bPartCompleted);
				break;
			default:
				break;
			}
		}
		else if (tokenGcode == token)
		{
			if (!AddGcodeBlockToList(nLineNo, strProc, strArgs, strListGcode))
				return false;
			strListBlock.AddTail(str);
			arrLineNo.add(nLineNo);	// the previous line was the G-code block line
		}
	}

	PrintLog(enLogError, _T("Cannot complete remnant-cut information with HKSCRC() at line %d"), nLineNo);
	return false;
}

bool InterfaceMPF::UpdateCAMPartInfo(const CPtrContourList& ptrListContour, int nLineNo, CAM_DATA& camInfo, int& iPart)
{
	if (0 > iPart || 0 >= camInfo.num_Shapes || camInfo.num_Shapes <= iPart
		|| !AfxIsValidAddress(camInfo.pCAMShape, sizeof(CAM_SHAPE)*camInfo.num_Shapes)
		|| ptrListContour.IsEmpty())
	{
		ASSERT(FALSE);
		return false;
	}

	CAM_SHAPE& part = camInfo.pCAMShape[iPart];
	ASSERT(0 == part.numContours && NULL == part.pContour);
	int nContours = ptrListContour.GetCount();
	POSITION pos = ptrListContour.GetHeadPosition();
	ASSERT(0 < nContours && NULL != pos);

	CAM_CONTOUR* pC = new CAM_CONTOUR[nContours];
	if (!pC)
	{
		PrintLog(enLogError, _T("Out of memory while allocating contour buffer %d."), nContours);
		return false;
	}

	for (int i = 0; i < nContours && pos; ++i)
	{
		CAM_CONTOUR& c = *ptrListContour.GetNext(pos);
		ASSERT(0 < c.numCodes && NULL != c.pCode);
		pC[i] = c;
		ASSERT(0 == c.numCodes && NULL == c.pCode);
	}

	part.nBlockIdNo = pC[0].N_blockNo;
	part.numContours = nContours;
	part.nEndOfPartLineNo = nLineNo;
	part.pContour = pC;
	++iPart;

	return true;
}

inline bool isGcodeCoords(TCHAR ch)
{
	_totupper(ch);
	return (_T('I')==ch || _T('J')==ch || _T('X')==ch || _T('Y')==ch || _T('Z')==ch);
}

void InterfaceMPF::MakeString(CString& str)
{
	str.Trim();

	int length = str.GetLength();
	if (0 >= length)
		return;

	if (_T('N') == str[0] || _T('n') == str[0])
	{
		if (10 >= length)
			return;

		for (int i = 1; i < length; ++i)
		{
			if (_T('0') > str[i] || _T('9') < str[i])
			{
				if (_T(' ') != str[i])	// if no space at the end of digit, insert a space before the non-digit string
				{
					str.Insert(i, _T(' '));
					return;
				}
			}
		}
	}
	else if (_T('G') == str[0] || _T('g') == str[0])
	{
		int bufcount = 2*length;
		TCHAR* p = new TCHAR[bufcount];
		if (!p)
			return;
		memset(p, 0, sizeof(TCHAR)*bufcount);
		p[0] = _T('G');

		for (int i = 1, j = 1; i < length; ++i)
		{
			ASSERT(j < bufcount);
			if (isGcodeCoords(str[i]))
			{
				if (_T(' ') != str[i-1])	// if no space before a G-code coordinate argument, insert one
				{
					p[j++] = _T(' ');
				}
			}
			else if (_T('.') == str[i])
			{
				if (_T('0') > str[i-1] || _T('9') < str[i-1])	// if no zero('0') before a decimal point, insert zero.
				{
					p[j++] = _T('0');
				}
			}
			p[j++] = str[i];
		}
		str = p;
		delete[] p;
	}
	return;
}

InterfaceMPF::tokenType InterfaceMPF::GetToken(CString& str, int& nBlockNo, CString& strProc, CStringList& strArgs)
{
	MakeString(str);

	CString strLine(str);
	strLine.Trim();
	strLine.MakeUpper();
	ASSERT(-1 == strLine.Find(_T(';')));

	int iPos = 0;
	CString strToken = strLine.Tokenize(_T(" (\t\n"), iPos);
	if (strToken.IsEmpty())
		return tokenNull;

	if (2 > strToken.GetLength())
		return tokenUnknown;
	ASSERT(strArgs.IsEmpty());

	//
	// Check the state number first,
	// if it is, take it and try to get the next statement
	//
	if (_T('N') == strToken[0] && GetNumber(strToken, 1, nBlockNo))
	{
		if (strLine.GetLength() > iPos+1)
		{
			strToken = strLine.Tokenize(_T(" (\t\n"), iPos);
			strToken.Trim();
			if (strToken.IsEmpty())
				return tokenStateNo;
		}
		else
		{
			return tokenStateNo;
		}
	}

	//
	// Check the token if it is G-code, M-code, or any of process calls
	//
	if (_T('G') == strToken[0] && IsNumber(strToken, 1))	// G-codes
	{
		strProc = strToken;
		if (iPos < strLine.GetLength())
			GetCodeArgs(strLine, iPos, strArgs);
		return tokenGcode;
	}
	else if (_T('M') == strToken[0] && IsNumber(strToken, 1))	// M-codes
	{
		strProc = strToken;
		if (iPos < strLine.GetLength())
			GetCodeArgs(strLine, iPos, strArgs);
		return tokenMcode;
	}
	else
	{
		strProc = strToken;
		if (iPos < str.GetLength())
			GetProcArgs(str, iPos, strArgs);	// keep the lower/upper cases of the original string in the argument list
		return tokenProcCall;
	}
	return tokenUnknown;
}


void InterfaceMPF::GetCodeArgs(const CString& strStatement, int iPos, CStringList& strArgs)
{
	do
	{
		CString strToken = strStatement.Tokenize(_T(" \t\n"), iPos);
		strToken.Trim();
		if (strToken.IsEmpty())
			return;

		strArgs.AddTail(strToken);
	}
	while (1);
	return;
}


void InterfaceMPF::GetProcArgs(const CString& strStatement, int iPos, CStringList& strArgList)
{
	int iBraceStart = -1, iBraceEnd = -1;

	for (int i = iPos-1; i < strStatement.GetLength(); ++i)
	{
		if (_T('(') == strStatement[i])
		{
			iBraceStart = i;
			for (i = strStatement.GetLength()-1; i > iBraceStart; --i)
			{
				if (_T(')') == strStatement[i])
				{
					iBraceEnd = i;
					break;
				}
			}
			break;
		}
	}

	CString strText, strArg;
	if (0 <= iBraceStart && iBraceStart < iBraceEnd)	// sub-function arguments
	{
		if (iBraceStart+1 == iBraceEnd)
			return;
		strText = strStatement.Mid(iBraceStart+1, iBraceEnd-iBraceStart-1);
	}
	else
	{
		ASSERT(strStatement.GetLength() > iPos+1);
		strText = strStatement.Mid(iPos);
	}
	strText.Trim();
	int iNext = 0, iEnd = strText.GetLength();

	while (iNext < iEnd)
	{
		if (_T(',') == strText[iNext])
		{
			++iNext;
			strArgList.AddTail(_T(""));
		}
		else
		{
			strArg = strText.Tokenize(_T(","), iNext);
			strArg.Trim();
			if (strArg.GetLength() && _T('\"') == strArg[0]
				&& _T('\"') == strArg[strArg.GetLength()-1])
				strArg = strArg.Mid(1, strArg.GetLength()-2);
			strArgList.AddTail(strArg);
		}
	}

	return;
}


void InterfaceMPF::GetHKLDBv08(const CStringList& strArgList, CAM_DATA& camInfo)
{
	ASSERT(2 <= strArgList.GetCount());

	//
	// The first argument of HKLDB: Gas folder information
	//
	bool bGasFolderOk = false;
	int nGasFolder = -1;
	CString strGas = strArgList.GetAt(strArgList.FindIndex(0));
	if (1 == strGas.GetLength() && IsNumber(strGas[0]))
	{
		nGasFolder = strGas[0] - _T('0');
		bGasFolderOk = (0 < nGasFolder && nGasFolder <= iGAS_LAST);
	}
	if (!bGasFolderOk)
		PrintLog(enLogWarning, _T("  Warning: invalid gas folder index = %d."), nGasFolder);

	//
	// The second argument of HKLDB: DB filename
	//
	camInfo.strDBName = strArgList.GetAt(strArgList.FindIndex(1));
	if (camInfo.strDBName.GetLength() <= 0)
		return;
	// Get the material name
	VERIFY(::GetMaterialName(camInfo.strDBName, camInfo.strMaterial));

	// Check whether gas information in DB name coincides with the gas folder
	if (bGasFolderOk)
	{
		switch (nGasFolder)
		{
		case iGAS_OXYGEN:	camInfo.strAssistGas = _T("Oxygen");	break;
		case iGAS_NITROGEN:	camInfo.strAssistGas = _T("Nitrogen");	break;
		case iGAS_AIR:		camInfo.strAssistGas = _T("Air");		break;
		default:	break;
		}
	}

	//
	// Material thickness from DB name
	//
	CString strThickness = camInfo.strDBName.Right(3);
	if (strThickness.GetLength() != 3)
		return;

	int iStart = 0;
	for (; iStart < 3; ++iStart)
	{
		if (_T('0') <= strThickness[iStart] && strThickness[iStart] <= _T('9'))
			break;
	}
	if (0 < iStart)
	{
		if (3 <= iStart)
		{
			camInfo.nMaterialThickness = 0;
		}
		else
		{
			camInfo.nMaterialThickness = _tstoi(strThickness.Mid(iStart));
		}
		PrintLog(enLogWarning, _T("  Warning: invalid thickness convention in DB name[%s] => %d."), strThickness, camInfo.nMaterialThickness);
	}
	else
	{
		camInfo.nMaterialThickness = _tstoi(strThickness);
	}

	return;
}


void InterfaceMPF::GetHKLDBv16(const CStringList& strArgList, CAM_DATA& camInfo)
{
	ASSERT(3 <= strArgList.GetCount());

	//
	// The first argument of HKLDB: sheet material information
	//
	int iMaterial = -1;
	CString strGas = strArgList.GetAt(strArgList.FindIndex(0));
	if (1 == strGas.GetLength() && IsNumber(strGas[0]))
		iMaterial = strGas[0] - _T('0');
	switch (iMaterial)
	{
	case iMATERIAL_MS:	case iMATERIAL_SUS:	case iMATERIAL_AL:
	case iMATERIAL_BRASS:	case iMATERIAL_COPPER:	case iMATERIAL_USER:
		camInfo.strMaterial = s_szMaterialName[iMaterial];
		break;
	default:
		camInfo.strMaterial = s_szMaterialName[iMATERIAL_UNDEFINED];
		PrintLog(enLogWarning, _T("  Warning: invalid material index at HKLDB() parameter."));
		break;
	}

	//
	// The second argument of HKLDB: DB filename (implicitly having material and thickness)
	//
	camInfo.strDBName = strArgList.GetAt(strArgList.FindIndex(1));
	if (camInfo.strDBName.GetLength() <= 0)
		return;

	// Get the material name
	CString str;
	::GetMaterialName(camInfo.strDBName, str);
	if (str != camInfo.strMaterial)
		PrintLog(enLogWarning, _T("  Warning: material specified in HKLDB() does not match the one specified in DB name."));
	const CString& dbName = camInfo.strDBName;
	camInfo.nMaterialThickness = 0;
	if (3 < dbName.GetLength())
	{// check if thickness information is given in the name of DB
		str = dbName.Mid(dbName.GetLength()-3);
		GetNumber(str, 0, camInfo.nMaterialThickness);
	}
	if (0 >= camInfo.nMaterialThickness)
		PrintLog(enLogWarning, _T("  Warning: material thickness information is missing from HKLDB()."));

	//
	// The third argument of HKLDB: assistant gas information
	//
	int iAssistGas = 0;
	str = strArgList.GetAt(strArgList.FindIndex(2));
	switch (_tstoi(str))
	{
	case iGAS_OXYGEN:	camInfo.strAssistGas = _T("Oxygen");	break;
	case iGAS_NITROGEN:	camInfo.strAssistGas = _T("Nitrogen");	break;
	case iGAS_AIR:		camInfo.strAssistGas = _T("Air");		break;
	default:			camInfo.strAssistGas = _T("");			break;
	}

	return;
}


void InterfaceMPF::GetHKINIv08(const CStringList& strArgList, CAM_DATA& camInfo)
{
	POSITION pos = strArgList.GetHeadPosition();
	CString strArg1, strArg2;
	if (2 <= strArgList.GetCount())
	{
		strArg1 = strArgList.GetNext(pos);
		strArg2 = strArgList.GetNext(pos);
	}
	else
	{
		PrintLog(enLogWarning, _T("  Warning: no dimension information is specified."));
		return;
	}
	camInfo.sheetWidth = _tstof(strArg1);
	camInfo.sheetHeight = _tstof(strArg2);
	if (0 >= camInfo.sheetWidth || 0 >= camInfo.sheetHeight)
	{
		PrintLog(enLogWarning, _T("  Warning: invalid dimension information is specified [%.2f x %.2f]."),
				 camInfo.sheetWidth, camInfo.sheetHeight);
	}
	return;
}


void InterfaceMPF::GetHKINIv16(const CStringList& strArgList, CAM_DATA& camInfo)
{
	POSITION pos = strArgList.GetHeadPosition();
	CString strArg1, strArg2, strArg3;
	if (3 <= strArgList.GetCount())
	{
		strArg1 = strArgList.GetNext(pos);
		strArg2 = strArgList.GetNext(pos);
		strArg3 = strArgList.GetNext(pos);
	}
	else
	{
		PrintLog(enLogWarning, _T("  Warning: HKINI() has invalid number of arguments."));
		return;
	}
	camInfo.num_Parts = _tstoi(strArg1);
	camInfo.sheetWidth = _tstof(strArg2);
	camInfo.sheetHeight = _tstof(strArg3);
	if (0 >= camInfo.num_Parts)
	{
		PrintLog(enLogWarning,
				 _T("  Warning: invalid number of parts(=%d) in HKINI() argument.."),
				 camInfo.num_Parts);
	}
	if (0 >= camInfo.sheetWidth || 0 >= camInfo.sheetHeight)
	{
		PrintLog(enLogWarning,
				 _T("  Warning: invalid dimension information is specified [%.2f x %.2f]."),
				 camInfo.sheetWidth, camInfo.sheetHeight);
	}
	return;
}


bool InterfaceMPF::GetHKOSTv08(int nLineNo, const CString& strProc, const CStringList& strArgList, CStringList& strList_HKOST)
{
	if (4 > strArgList.GetCount())
	{
		PrintLog(enLogError, _T("Invalid number of HKOST() arguments at line#%d."), nLineNo);
		return false;
	}

	//
	// Get HKOST() arguments
	//

	// X offset, Y offset, R offset
	POSITION pos = strArgList.GetHeadPosition();
	CString strHKOSTArg, strT;
	for (int i = 0; i < 3; ++i)
	{
		ASSERT(NULL != pos);
		strT = strArgList.GetNext(pos);
		if (!IsNumeric(strT))
		{
			PrintLog(enLogError, _T("Non-numeric argument in the coordinate parameters of HKOST() at line#%d"), nLineNo);
			return false;
		}
		strHKOSTArg += (strT + _T(','));
	}

	// Block number information
	if (!pos)
	{
		ASSERT(FALSE);
		PrintLog(enLogError, _T("No block number information in HKOST() at line#%d"), nLineNo);
		return false;
	}
	strT = strArgList.GetNext(pos);
	if (!IsNumber(strT, 0))
	{
		PrintLog(enLogError, _T("No block number information in HKOST(): [%s] at line#%d"), strT, nLineNo);
		return false;
	}
	strHKOSTArg += (strT + _T(','));

	strList_HKOST.AddTail(strHKOSTArg);

	return true;

}


bool InterfaceMPF::GetHKOSTv16(int nLineNo, const CString& strProc, const CStringList& strArgList, CStringList& strList_HKOST)
{
	if (5 > strArgList.GetCount())
	{
		PrintLog(enLogError, _T("Invalid number of arguments at line %d."), nLineNo);
		return false;
	}

	//
	// Get HKOST() arguments
	//

	// X offset, Y offset, R offset
	POSITION pos = strArgList.GetHeadPosition();
	CString strHKOSTArg, strT;
	for (int i = 0; i < 3; ++i)
	{
		ASSERT(NULL != pos);
		strT = strArgList.GetNext(pos);
		if (!IsNumeric(strT))
		{
			PrintLog(enLogError, _T("Non-numeric argument in the coordinate parameters of HKOST() at line %d."), nLineNo);
			return false;
		}
		strHKOSTArg += (strT + _T(','));
	}

	// Block number information
	if (!pos)
	{
		ASSERT(FALSE);
		PrintLog(enLogError, _T("No block number information in HKOST() at line %d."), nLineNo);
		return false;
	}
	strT = strArgList.GetNext(pos);
	if (!IsNumber(strT, 0))
	{
		PrintLog(enLogError, _T("No block number information in HKOST(): [%s] at line %d."), strT, nLineNo);
		return false;
	}
	strHKOSTArg += (strT + _T(','));

	// Number of contours
	if (!pos)
	{
		ASSERT(FALSE);
		PrintLog(enLogError, _T("No contour number information in HKOST() at line %d."), nLineNo);
		return false;
	}
	strT = strArgList.GetNext(pos);
	if (!IsNumber(strT, 0))
	{
		PrintLog(enLogError, _T("No contour number information in HKOST(): [%s] at line %d."), strT, nLineNo);
		return false;
	}
	strHKOSTArg += (strT + _T(','));

	strList_HKOST.AddTail(strHKOSTArg);

	return true;

}


bool InterfaceMPF::GetHKOSTPartInfo(fileVersion verMPF, const CString& strArgs, CAM_PART& info)
{
	int iPos = 0, nLength = strArgs.GetLength();

	CString strNum = strArgs.Tokenize(_T(","), iPos);
	if (iPos >= nLength)
		return false;
	if (strNum.IsEmpty())
		info.origin_X = 0;
	else if (IsNumeric(strNum))
		info.origin_X = _tstof(strNum);
	else
		return false;

	strNum = strArgs.Tokenize(_T(","), iPos);
	if (iPos >= nLength)
		return false;
	if (strNum.IsEmpty())
		info.origin_Y = 0;
	else if (IsNumeric(strNum))
		info.origin_Y = _tstof(strNum);
	else
		return false;

	strNum = strArgs.Tokenize(_T(","), iPos);
	if (iPos >= nLength)
		return false;
	if (strNum.IsEmpty())
		info.origin_R = 0;
	else if (IsNumeric(strNum))
		info.origin_R = _tstof(strNum);
	else
		return false;

	strNum = strArgs.Tokenize(_T(","), iPos);
	if (strNum.IsEmpty() || !IsNumber(strNum, 0))
		return false;

	info.nCAMpart_BlockNo = _tstoi(strNum);

	if (verV16 == verMPF)
	{
		strNum = strArgs.Tokenize(_T(","), iPos);
		if (strNum.GetLength() && IsNumber(strNum, 0))
			info.nContoursInPart = _tstoi(strNum);
	}
	return true;
}


bool InterfaceMPF::GetHKSTR(int nLineNo, const CStringList& strArgs, CAM_CONTOUR& contour)
{
	POSITION pos = strArgs.GetHeadPosition();

	int N;

	// Piercing condition
	if (GetIntArg(strArgs, pos, N))
	{
		contour.nPiercing = N;
	}
	else
	{
		PrintLog(enLogError, _T("Invalid piercing method in HKSTR() at line %d."), nLineNo);
		return false;
	}

	// Cutting condition
	if (GetIntArg(strArgs, pos, N))
	{
		contour.nCutting = N;
	}
	else
	{
		PrintLog(enLogError, _T("Invalid cutting condition in HKSTR() at line %d."), nLineNo);
		return false;
	}

	// Part start position X
	if (!GetfloatArg(strArgs, pos, contour.x_start))
	{
		PrintLog(enLogError, _T("Invalid x-start position in HKSTR() at line %d."), nLineNo);
		return false;
	}
	// Part start position Y
	if (!GetfloatArg(strArgs, pos, contour.y_start))
	{
		PrintLog(enLogError, _T("Invalid y-start position in HKSTR() at line %d."), nLineNo);
		return false;
	}

	// Tool compensation
	double V;
	bool isFloating = false;
	if (!GetNumericArg(strArgs, pos, N, V, isFloating))
	{
		PrintLog(enLogError, _T("No tool compensation code in HKSTR() at line %d."), nLineNo);
		return false;
	}
	if (isFloating)
	{
		contour.nToolComp = 0;
		contour.width = V;
	}
	else
	{
		contour.nToolComp = N;
		// Part dimension in X
		if (!GetfloatArg(strArgs, pos, contour.width))
			PrintLog(enLogWarning, _T("  WARNING: invalid contour width in HKSTR() at line %d."), nLineNo);
	}

	// Part dimension in Y
	if (!GetfloatArg(strArgs, pos, contour.height))
		PrintLog(enLogWarning, _T("  WARNING: invalid contour height in HKSTR() at line %d."), nLineNo);

	return true;
}


G_Code InterfaceMPF::GetContourParamsInArg(const CStringList& strArgs, CString& strGcode)
{
	POSITION pos = strArgs.GetHeadPosition();

	G_Code code = G_Unknown;
	int nAddress = 0;
	if (!GetIntArg(strArgs, pos, nAddress))
		return code;

	bool bOk = false;
	switch (nAddress)
	{
	case 0: strGcode = _T("G0"); code = G_Jump;		break;
	case 1: strGcode = _T("G1"); code = G_Line;		break;
	case 2: strGcode = _T("G2"); code = G_ArcCW;	break;
	case 3: strGcode = _T("G3"); code = G_ArcCCW;	break;
	default: return G_Unknown; break;
	}

	CString sGArg;
	if (!GetNextArg(strArgs, pos, sGArg))
		return G_Unknown;
	strGcode += (_T(" X") + sGArg);
	if (!GetNextArg(strArgs, pos, sGArg))
		return G_Unknown;
	strGcode += (_T(" Y") + sGArg);

	if (G_ArcCW == code || G_ArcCCW == code)
	{
		if (!GetNextArg(strArgs, pos, sGArg))
			return G_Unknown;
		strGcode += (_T(" I") + sGArg);
		if (!GetNextArg(strArgs, pos, sGArg))
			return G_Unknown;
		strGcode += (_T(" J") + sGArg);
	}

	return code;
}


inline bool ParseLinearCoords(const CStringList& strListArgs, CString& strGcode)
{
	bool bX = false, bY = false;
	POSITION pos = strListArgs.GetHeadPosition();
	while (pos)
	{
		CString strArg = strListArgs.GetNext(pos);
		switch (strArg[0])
		{
		case 'X':	strGcode += _T(' ') + strArg;	bX = true;	break;
		case 'Y':	strGcode += _T(' ') + strArg;	bY = true;	break;
		case 'Z':	strGcode += _T(' ') + strArg;	break;
		case 'F':	strGcode += _T(' ') + strArg;	break;
		default:
			return false;
			break;
		}
	}
	return (bX && bY);
}

inline bool ParseCircularCoords(const CStringList& strListArgs, CString& strGcode)
{
	bool bX = false, bY = false, bI = false, bJ = false;
	POSITION pos = strListArgs.GetHeadPosition();
	while (pos)
	{
		CString strArg = strListArgs.GetNext(pos);
		switch (strArg[0])
		{
		case 'X':	strGcode += _T(' ') + strArg;	bX = true;	break;
		case 'Y':	strGcode += _T(' ') + strArg;	bY = true;	break;
		case 'I':	strGcode += _T(' ') + strArg;	bI = true;	break;
		case 'J':	strGcode += _T(' ') + strArg;	bJ = true;	break;
		case 'F':	strGcode += _T(' ') + strArg;	break;
		default:
			return false;
		}
	}
	if (!bI)
		strGcode += _T(" I0");
	if (!bJ)
		strGcode += _T(" J0");
	return true;
}


bool InterfaceMPF::GetGCodeLastPos(const CString& strGcode, const CStringList& strCoordsList, CPtrList& elemList)
{
	int nAddr = -1;
	if (!GetGcodeAddress(strGcode, nAddr))
	{
		ASSERT(FALSE);
		return false;
	}

	CString str;
	bool bOk = false;
	switch (nAddr)
	{
	case 0:	str = _T("G0");	bOk = ParseLinearCoords(strCoordsList, str); break;
	case 1:	str = _T("G1");	bOk = ParseLinearCoords(strCoordsList, str); break;
	case 2:	str = _T("G2");	bOk = ParseCircularCoords(strCoordsList, str);	break;
	case 3:	str = _T("G3");	bOk = ParseCircularCoords(strCoordsList, str);	break;
	default:
		PrintLog(enLogError, _T("Unknown G-code [%s]."), str);
		break;
	}
	if (!bOk)
		return false;

	CAM_CODE* p = new CAM_CODE;
	if (!p->FromString(str))
		return false;
	elemList.AddTail(p);

	return true;
}

bool InterfaceMPF::AddGcodeBlockToList(int nLineNo, const CString& strGcode, const CStringList& strCoordsList, CStringList& strGcodeList)
{
	int nAddr = -1;
	if (!GetGcodeAddress(strGcode, nAddr))
	{
		ASSERT(FALSE);
		PrintLog(enLogError, _T("G-code format error [%s] while trying to rebuild G-code at line %d."), strGcode, nLineNo);
		return false;
	}

	CString str;
	bool bOk = false;
	switch (nAddr)
	{
	case 0:	str = _T("G0");	bOk = G_LineCoords(nLineNo, strCoordsList, str); break;
	case 1:	str = _T("G1");	bOk = G_LineCoords(nLineNo, strCoordsList, str); break;
	case 2:	str = _T("G2");	bOk = G_ArcCoords(nLineNo, strCoordsList, str);	break;
	case 3:	str = _T("G3");	bOk = G_ArcCoords(nLineNo, strCoordsList, str);	break;
	default:
		PrintLog(enLogError, _T("Unknown G-code [%s]."), str);
		break;
	}
	if (bOk)
	{
		strGcodeList.AddTail(str);
		return true;
	}
	return false;
}


bool InterfaceMPF::G_LineCoords(int nLineNo, const CStringList& strListArgs, CString& strGcode)
{
	bool bX = false, bY = false;
	POSITION pos = strListArgs.GetHeadPosition();
	while (pos)
	{
		CString strArg = strListArgs.GetNext(pos);
		switch (strArg[0])
		{
		case 'X':	strGcode += _T(' ') + strArg;	bX = true;	break;
		case 'Y':	strGcode += _T(' ') + strArg;	bY = true;	break;
		case 'Z':	strGcode += _T(' ') + strArg;	break;
		case 'F':	strGcode += _T(' ') + strArg;	break;
		default:
			PrintLog(enLogError, _T("Undefined coordinate character[%c] in G-code arguments at line %d."), strArg[0], nLineNo);
			return false;
			break;
		}
	}
	if (bX && bY)
		return true;

	PrintLog(enLogError, _T("Invalid coordinate information for G1 command at line %d."), nLineNo);
	return false;
}


bool InterfaceMPF::G_ArcCoords(int nLineNo, const CStringList& strListArgs, CString& strGcode)
{
	bool bX = false, bY = false, bI = false, bJ = false;
	POSITION pos = strListArgs.GetHeadPosition();
	while (pos)
	{
		CString strArg = strListArgs.GetNext(pos);
		switch (strArg[0])
		{
		case 'X':	strGcode += _T(' ') + strArg;	bX = true;	break;
		case 'Y':	strGcode += _T(' ') + strArg;	bY = true;	break;
		case 'I':	strGcode += _T(' ') + strArg;	bI = true;	break;
		case 'J':	strGcode += _T(' ') + strArg;	bJ = true;	break;
		case 'F':	strGcode += _T(' ') + strArg;	break;
		default:
			PrintLog(enLogError, _T("Undefined coordinate character[%c] in G-code arguments at line %d."), strArg[0], nLineNo);
			return false;
		}
	}

	if (!bI)
		strGcode += _T(" I0");

	if (!bJ)
		strGcode += _T(" J0");

	//if (bX && bY && bI && bJ)
	return true;

	//PrintLog(enLogError, _T("Invalid G-code coordinate information at line %d"), s_nLineNo);
	//return false;
}


bool InterfaceMPF::CheckGcodeBlock(int nLineNo, const CString& strGcode, const CStringList& strCoordsList)
{
	int nAddr = -1;
	if (_T('G') != strGcode.GetAt(0) || !GetNumber(strGcode, 1, nAddr))
	{
		PrintLog(enLogError, _T("G-code format error [%s] while trying to rebuild G-code at line %d."), strGcode, nLineNo);
		return false;
	}

	POSITION pos = strCoordsList.GetHeadPosition();
	CString strX = strCoordsList.GetNext(pos);
	CString strY = strCoordsList.GetNext(pos);
	if (!checkGcodeCoordsOf(_T('X'), strX) || !checkGcodeCoordsOf(_T('Y'), strY))
	{
		PrintLog(enLogError, _T("G-code format error [%s] while trying to rebuild G-code at line %d."), strGcode, nLineNo);
		return false;
	}

	if (2 == nAddr || 3 == nAddr)	// Arc
	{
		CString strI = strCoordsList.GetNext(pos);
		CString strJ = strCoordsList.GetNext(pos);
		if (!checkGcodeCoordsOf(_T('I'), strI) || !checkGcodeCoordsOf(_T('J'), strJ))
		{
			PrintLog(enLogError, _T("G-code format error [%s] while trying to rebuild G-code at line %d."), strGcode, nLineNo);
			return false;
		}
	}
	return true;
}


bool InterfaceMPF::GetHKTOFArgsGcmd(int nLineNo, const CStringList& strHKTOFAgrs, CString& strGcommand)
{
	int nAddr;
	POSITION pos = strHKTOFAgrs.GetHeadPosition();
	if (!GetNumber(strHKTOFAgrs.GetNext(pos), 0, nAddr))
	{
		PrintLog(enLogError, _T("Invalid first argument of HKTOF() %s at line %d."),
				 strHKTOFAgrs.GetHead(), nLineNo);
		return false;
	}

	CString strX = strHKTOFAgrs.GetNext(pos);
	CString strY = strHKTOFAgrs.GetNext(pos);
	if (!IsNumeric(strX) || !IsNumeric(strY))
	{
		PrintLog(enLogError, _T("Invalid X, Y argument of HKTOF() (%s, %s) at line %d."),
				 strX, strY, nLineNo);
		return false;
	}

	if (0==nAddr || 1==nAddr)
	{
		strGcommand.Format(_T("G%d X%s Y%s"), nAddr, strX, strY);
		return true;
	}

	CString strI = strHKTOFAgrs.GetNext(pos);
	CString strJ = strHKTOFAgrs.GetNext(pos);
	if (!IsNumeric(strI) || !IsNumeric(strJ))
	{
		PrintLog(enLogError, _T("Invalid I, J argument of G%d in HKTOF() (%s, %s) at line %d."),
				 nAddr, strI, strJ, nLineNo);
		return false;
	}
	strGcommand.Format(_T("G%d X%s Y%s I%s J%s"), nAddr, strX, strY, strI, strJ);
	return true;
}


bool InterfaceMPF::BuildContourPath(const CStringList& strGcmdList, const CStringList& strBlockList, CIntArray& arrLineNo, CAM_CONTOUR& contour)
{
	if (strGcmdList.IsEmpty())
	{
		ASSERT(FALSE);
		PrintLog(enLogError, _T("No G-code list for contour."));
		return false;
	}

	int nContours = strGcmdList.GetCount();
	ASSERT(0 < nContours);
	contour.numCodes = max(0, nContours);
	if (!contour.numCodes)
		return true;

	ASSERT(0 < nContours);

	if (contour.pCode)
		delete[] contour.pCode;
	contour.pCode = new CAM_CODE[nContours];
	if (!contour.pCode)
	{
		PrintLog(enLogError, _T("Out of memory while allocating contour Block buffer."));
		contour.numCodes = 0;
		return false;
	}

	POSITION pos = strGcmdList.GetHeadPosition();
	POSITION poB = strBlockList.GetHeadPosition();
	int iPosLine = 0;
	CString  strGcode;

	for (int i = 0; i < nContours && pos; ++i)
	{
		strGcode = strGcmdList.GetNext(pos);
		if (!contour.pCode[i].FromString(strGcode))
		{
			PrintLog(enLogError, _T("Invalid G-code block %s."), strGcode);
			return false;
		}
		contour.pCode[i].sBlockCmd = strBlockList.GetNext(poB);
		contour.pCode[i].nLineNo = arrLineNo[iPosLine++];
	}

	return true;
}


inline bool MoveDetourInfo(CAM_CONTOUR& contour, CPtrList& elemList)
{
	ASSERT(0 == contour.num_detours && NULL == contour.pDetour);

	if (!elemList.IsEmpty())
	{
		contour.num_detours = elemList.GetCount();
		contour.pDetour = new CAM_CODE[contour.num_detours];
		if (contour.pDetour)
		{
			POSITION pos = elemList.GetHeadPosition();
			for (int i = 0; i < contour.num_detours && pos; ++i)
			{
				CAM_CODE* p = (CAM_CODE*)elemList.GetNext(pos);
				contour.pDetour[i] = *p;
				delete p;
			}
			elemList.RemoveAll();
			return true;
		}
	}
	return false;
}

bool InterfaceMPF::BuildContourPath(FileMPF& loader, const CStringList& strGcmdList, const CStringList& strBlockList, CIntArray& arrLineNo, CAM_CONTOUR& contour, bool& bPartCompleted)
{
	if (!BuildContourPath(strGcmdList, strBlockList, arrLineNo, contour))
		return false;

	//
	// Check detours and part-end
	//
	CPtrList camElemList;
	CString str, strProc;
	int nBlockNo;
	while (loader.getNextBlock(str))
	{
		CStringList strArgs;
		tokenType token = GetToken(str, nBlockNo, strProc, strArgs);
		switch (token)
		{
		case tokenType::tokenProcCall:
			switch (discriminateHKSPF(strProc))
			{
			case enHKSTR:
				loader.back();
#ifdef _DEBUG
				loader.getCurrLine(str);
				ASSERT(0 <= str.Find(_T("HKSTR")));
#endif // _DEBUG
				MoveDetourInfo(contour, camElemList);
				return true;
				break;

			case enHKSCRC:
				loader.back();
#ifdef _DEBUG
				loader.getCurrLine(str);
				ASSERT(0 <= str.Find(_T("HKSCRC")));
#endif // _DEBUG
				MoveDetourInfo(contour, camElemList);
				return true;
				break;

			case enHKPED:
				MoveDetourInfo(contour, camElemList);
				bPartCompleted = true;
				return true;
				break;
			default:
				break;
			}
			break;
		case tokenType::tokenGcode:
			GetGCodeLastPos(strProc, strArgs, camElemList);
			break;
		default:
			ASSERT(FALSE);
			break;
		}
	}

	return true;
}



//////////////////////////////////////////////////////////////////////////

CAM_DATA::CAM_DATA()
{
	nMaterialThickness = 0;
	sheetWidth = sheetHeight = 0;
	num_Parts = 0;
	pPart = NULL;
	num_Shapes = 0;
	pCAMShape = NULL;
}

CAM_DATA::~CAM_DATA()
{
	Delete();
}

bool CAM_DATA::IsNCPart(int nBlockNo) const
{
	if (nBlockNo <= 0)
		return false;

	for (int i = 0; i < num_Parts; ++i)
	{
		if (pPart[i].nCAMpart_BlockNo == nBlockNo)
			return true;
	}
	return false;
}

void CAM_DATA::Delete()
{
	delete[] pPart;
	delete[] pCAMShape;

	strFileName.Empty();
	strDBName.Empty();
	strMaterial.Empty();
	strAssistGas.Empty();

	nMaterialThickness = 0;

	sheetWidth = sheetHeight = 0;

	num_Parts = 0;
	pPart = NULL;
	num_Shapes = 0;
	pCAMShape = NULL;
}


inline CString GcodeName(G_Code code)
{
	CString str;
	switch (code)
	{
	case G_Jump: str = _T("G0: Jump");	break;
	case G_Line: str = _T("G1: Line");	break;
	case G_ArcCW: str = _T("G2: Arc(CW)");	break;
	case G_ArcCCW:str = _T("G3: Arc(CCW)");	break;
	default:
		break;
	}
	return str;
}


bool CAM_DATA::DumpContents(LPCTSTR pszFile) const
{
	if (!HasContents())
	{
		if (g_showErrMessage)
			AfxMessageBox(_T("No data to dump to a file"));
		return false;
	}

	CStringList strList;
	CString str;
	str.Format(_T("Total %d number of parts in %.3f x %.3f size sheet"),
			   num_Parts, sheetWidth, sheetHeight);
	strList.AddTail(str);

	for (int i = 0; i < num_Parts; ++i)
	{
		const CAM_PART& info = pPart[i];

		str.Format(_T("Part#%03d: origin(%9.4f,%9.4f; %.1f) BlockNO: %6d (index#%d)"), i,
				   info.origin_X, info.origin_Y, info.origin_R,
				   info.nCAMpart_BlockNo, info.iCAMShape);
		strList.AddTail(str);
	}
	strList.AddTail(_T(" "));
	strList.AddTail(_T(" "));

	for (int i = 0; i < num_Shapes; ++i)
	{
		strList.AddTail(_T(" "));

		const CAM_SHAPE& part = pCAMShape[i];
		str.Format(_T("BlockNO:%-10d  number of contours = %d"), part.nBlockIdNo, part.numContours);
		strList.AddTail(str);

		for (int j = 0; j < part.numContours; ++j)
		{
			const CAM_CONTOUR& c = part.pContour[j];
			str.Format(_T("BlockNO:%-10d  contour#%03d: Xo,Yo=(%.4f, %.4f)"),
					   c.N_blockNo, j, c.x_start, c.y_start);
			strList.AddTail(str);
			str.Format(_T("                    Piercing(%s), Cutting(%s), toolComp(%d), %s"),
					   PiercingName(c.nPiercing), CuttingName(c.nCutting), c.nToolComp,
					   c.bIsRemnant? _T("Remnant"):_T("Normal Cut"));
			strList.AddTail(str);

			for (int k = 0; k < c.numCodes; ++k)
			{
				const CAM_CODE& b = c.pCode[k];
				if (G_ArcCW == b.code || G_ArcCCW == b.code)
				{
					if (DBL_MAX != b.F)
						str.Format(_T("         Block#%03d  %-12s X=%.4f Y=%.4f I=%.4f J=%.4f F=%.4f"),
						k, GcodeName(b.code), b.X, b.Y, b.I, b.J, b.F);
					else
						str.Format(_T("         Block#%03d  %-12s X=%.4f Y=%.4f I=%.4f J=%.4f"),
						k, GcodeName(b.code), b.X, b.Y, b.I, b.J);
				}
				else
				{
					if (DBL_MAX != b.F)
						str.Format(_T("         Block#%03d  %-12s X=%.4f Y=%.4f F=%.4f"),
						k, GcodeName(b.code), b.X, b.Y, b.F);
					else
						str.Format(_T("         Block#%03d  %-12s X=%.4f Y=%.4f"),
						k, GcodeName(b.code), b.X, b.Y);
				}
				strList.AddTail(str);
			}
		}
	}

	if (!InterfaceMPF::SaveStrListToFile(strList, pszFile))
	{
		if (g_showErrMessage)
		{
			CString strErr;
			strErr.Format(_T("Can't create the file: %s\nERROR: %s"), pszFile, InterfaceMPF::GetLastErrorMessage());
			AfxMessageBox(strErr);
		}
		return false;
	}
	return true;
}


void CAM_DATA::GetDBInfo(CString& str) const
{
	if (HasContents())
	{
		str.Format(_T("Material = %s, thickness = %.1f, assist gas = %s"), strMaterial, 0.1*nMaterialThickness, strAssistGas);
	}
	else
	{
		str.Empty();
	}
}


void CAM_DATA::GetVersion(CString& strVersion) const
{
	switch (version)
	{
	case verV08:		strVersion = _T("V08 Compatible");	break;
	case verV16:		strVersion = _T("V16 Compatible");	break;
	case verV16A05:	strVersion = _T("V16 A05");			break;
	default:		strVersion = _T("Unknown");			break;
	}
}


//////////////////////////////////////////////////////////////////////////

CAM_CONTOUR::CAM_CONTOUR(CAM_CONTOUR& c)
{
	N_blockNo	= c.N_blockNo;
	x_start		= c.x_start;
	y_start		= c.y_start;
	u_start		= c.u_start;
	width		= c.width;
	height		= c.height;
	nPiercing	= c.nPiercing;
	nCutting	= c.nCutting;
	nToolComp	= c.nToolComp;
	bHasLeadIn	= c.bHasLeadIn;
	bIsRemnant	= c.bIsRemnant;
	bIsScancut	= c.bIsScancut;
	numCodes= c.numCodes;
	pCode		= c.pCode;
	iCmdBlockStr= c.iCmdBlockStr;
	num_detours = c.num_detours;
	pDetour		= c.pDetour;
	iCmdBlockStrLast = c.iCmdBlockStrLast;

	c.numCodes= 0;
	c.pCode	= NULL;
	c.num_detours = 0;
	c.pDetour = NULL;
}

void CAM_CONTOUR::operator=(CAM_CONTOUR& c)
{
	if (this != &c)
	{
		N_blockNo	= c.N_blockNo;
		x_start		= c.x_start;
		y_start		= c.y_start;
		u_start		= c.u_start;
		width		= c.width;
		height		= c.height;
		nPiercing	= c.nPiercing;
		nCutting	= c.nCutting;
		nToolComp	= c.nToolComp;
		bHasLeadIn	= c.bHasLeadIn;
		bIsRemnant	= c.bIsRemnant;
		bIsScancut	= c.bIsScancut;
		numCodes= c.numCodes;
		pCode		= c.pCode;
		iCmdBlockStr= c.iCmdBlockStr;
		num_detours = c.num_detours;
		pDetour		= c.pDetour;
		iCmdBlockStrLast = c.iCmdBlockStrLast;

		c.numCodes= 0;
		c.pCode	= NULL;
		c.num_detours = 0;
		c.pDetour = NULL;
	}
}

bool CAM_CONTOUR::GetEndPos(double& x, double& y) const
{
	if (0 >= numCodes + num_detours)
		return false;

	CAM_CODE* pLast = NULL;
	if (0 < num_detours && AfxIsValidAddress(pDetour, sizeof(CAM_CODE)*num_detours))
	{
		pLast = pDetour + num_detours - 1;
	}
	else
	{
		pLast = pCode + numCodes - 1;
	}

	if (!AfxIsValidAddress(pLast, sizeof(CAM_CODE)))
		return false;

	x = pLast->X;
	y = pLast->Y;
	return true;
}


//////////////////////////////////////////////////////////////////////////

bool CAM_CODE::FromString(const CString& strGcmd)
{
	int iPos = 0, nAddr;
	CString G__ = strGcmd.Tokenize(_T(" "), iPos);
	if (G__.IsEmpty() || _T('G') != G__[0] || !GetNumber(G__, 1, nAddr))
	{
		ASSERT(FALSE);
		return false;
	}

	BOOL bX(0), bY(0), bI(0), bJ(0), bF(0);

	CString str = strGcmd.Tokenize(_T(" "), iPos);
	while (!str.IsEmpty())
	{
		switch (str[0])
		{
		case _T('X'):
			if (bX || !IsNumeric(str, X, 1))
				return false;
			bX = TRUE;
			break;
		case _T('Y'):
			if (bY || !IsNumeric(str, Y, 1))
				return false;
			bY = TRUE;
			break;
		case _T('I'):
			if (bI || !IsNumeric(str, I, 1))
				return false;
			bI = TRUE;
			break;
		case _T('J'):
			if (bJ || !IsNumeric(str, J, 1))
				return false;
			bJ = TRUE;
			break;
		case _T('F'):
			if (bF)	// || !IsNumeric(str, F, 1))	// Fly-cut block strings have a type of 'F=R-variable' arguments.
				return false;
			if (!IsNumeric(str, F, 1))
				F = 0;
			bF = TRUE;
			break;
		default: ASSERT(FALSE); return false;
			break;
		}
		str = strGcmd.Tokenize(_T(" "), iPos);
	}

	if (!bX || !bY)
	{
		ASSERT(FALSE);
		return false;
	}

	switch (nAddr)
	{
	case 0:	code = G_Jump;	break;
	case 1:	code = G_Line;	break;
	case 2:	case 3:
		code = (2==nAddr)? G_ArcCW: G_ArcCCW;
		if (!bI || !bJ)
		{
			ASSERT(FALSE);
			return false;
		}
		break;
	default:
		ASSERT(FALSE);
		return false;
		break;
	}
	return true;
}

double CAM_CODE::CalcLength(double start_x, double start_y) const
{
	if (IsArc())
	{
		double xc = start_x + I, yc = start_y + J;
		double x1 = -I, y1 = -J;			// start point in the center of circle's coordinate system
		double x2 = X - xc, y2 = Y - yc;	// end point in the center of circle's coordinate system
		double r = sqrt(I*I + J*J);

		double theta1 = NormalizeRad(atan2(y1, x1));
		double theta2 = NormalizeRad(atan2(y2, x2));
		if (theta1 < theta2)	// positive cross product
		{
			if (G_ArcCCW == code)
				return r*(theta2 - theta1);
			else
				return r*(2*π + theta1 - theta2);
		}
		else if (theta1 > theta2)
		{
			if (G_ArcCCW)
				return r*(2*π + theta2 - theta1);
			else
				return r*(theta1 - theta2);
		}
		else
		{
			return 2*π*r;
		}
	}
	else
	{
		double dx = X - start_x, dy = Y - start_y;
		return sqrt(dx*dx + dy*dy);
	}
}

double getDividngAngle(double θ1, double θ2, double ratioFromStart, double offset, bool isPositiveDirection)
{
	// θ1 and θ2 must be normalized within [0, 2π)
	ASSERT(GE(θ1, 0) && LT(θ1, π2) && GE(θ2, 0) && LT(θ2, π2));
	// And ratioFromStart must be normalized in the range of [0, 1]
	ASSERT(0 <= ratioFromStart && 1 >= ratioFromStart);

	double θ = 0;

	if (IsSame(θ1, θ2))
	{
		if (IsZero(offset))
		{
			if (isPositiveDirection)
				θ = θ1 + π2*ratioFromStart;
			else
				θ = θ1 - π2*ratioFromStart;
		}
		else
		{
			ASSERT(LE(offset, π2));

			if (isPositiveDirection)
			{
				double dθ = π2*ratioFromStart + offset;
				if (0 < offset)
					θ = θ1 + minof(π2, dθ);
				else
					θ = θ1 + maxof(0.0, dθ);
			}
			else
			{
				double dθ = - π2*ratioFromStart + offset;
				if (0 < offset)
					θ = θ1 + minof(0.0, dθ);
				else
					θ = θ1 + maxof(-π2, dθ);
			}
		}
	}
	else if (θ1 < θ2)
	{
		double dθ = θ2 - θ1;	// > 0

		if (IsZero(offset))
		{
			if (isPositiveDirection)	// θ1 <= θ <= θ2
			{
				θ = θ1 + dθ*ratioFromStart;
				ASSERT(LE(θ1, θ) && LE(θ, θ2));
			}
			else					// (0 <= θ && θ <= θ1) || (θ < 0 && θ2 <= θ + 2π)
			{
				θ = θ1 - (π2 - dθ)*ratioFromStart;
				ASSERT(GE(θ1, θ) || (0 > θ && GE(π2 + θ, θ2)));
			}
		}
		else
		{
			if (isPositiveDirection)	// θ1 <= θ <= θ2
			{
				ASSERT(LE(offset, dθ));

				θ = θ1 + dθ*ratioFromStart + offset;
				if (0 > offset)
				{
					ASSERT(θ < θ2);
					if (θ < θ1)
						θ = θ1;
				}
				else
				{
					ASSERT(θ1 < θ);
					if (θ2 < θ)
						θ = θ2;
				}
				ASSERT(LE(θ1, θ) && LE(θ, θ2));
			}
			else					// (0 <= θ && θ <= θ1) || (θ < 0 && θ2 <= θ + 2π)
			{
				ASSERT(LE(offset, π2 - dθ));

				θ = θ1 - (π2 - dθ)*ratioFromStart + offset;
				if (0 <= θ)
				{
					if (θ1 < θ)
					{
						ASSERT(0 < offset);
						θ = θ1;
					}
				}
				else	// θ < 0
				{
					if (θ + π2 < θ2)
					{
						ASSERT(0 > offset);
						θ = θ2;
					}
				}
				ASSERT(GE(θ1, θ) || (0 > θ && GE(π2 + θ, θ2)));
			}
		}
	}
	else	//	θ2 < θ1
	{
		double dθ = θ1 - θ2;	// > 0

		if (IsZero(offset))
		{
			if (isPositiveDirection) // (0 <= θ && θ <= θ2) || (θ1 < θ && θ < 2π)
				θ = θ1 + (π2 - dθ)*ratioFromStart;
			else
				θ = θ1 - dθ*ratioFromStart;
		}
		else
		{
			if (isPositiveDirection)
			{
				ASSERT(LE(offset, π2 - dθ));

				θ = θ1 + (π2 - dθ)*ratioFromStart + offset;
				if (π2 <= θ)
				{
					if (θ2 < θ)
					{
						ASSERT (0 < offset);
						θ = θ2;
					}
				}
				else
				{
					if (θ < θ1)
					{
						ASSERT (0 > offset);
						θ = θ1;
					}
				}
			}
			else
			{
				ASSERT(LE(offset, dθ));

				θ = θ1 - dθ*ratioFromStart + offset;
				if (0 > offset)
				{
					ASSERT(θ < θ1);
					if (θ < θ2)
						θ = θ2;
				}
				else	// 0 < offset
				{
					ASSERT(θ2 < θ);
					if (θ1 < θ)
						θ = θ1;
				}
			}
		}
	}

	return NormalizeRad(θ);
}

bool CAM_CODE::CalcPos(double start_x, double start_y, double ratioFromStart, double& x, double& y, double& newI, double& newJ, double offset/*=0*/) const
{
	if (0 > ratioFromStart || 1 < ratioFromStart)
	{
		ASSERT(FALSE);
		ratioFromStart = min(max(0, ratioFromStart), 1.0);
	}

	if (IsArc())
	{
		double r = sqrt(I*I + J*J);
		if (IsZero(r))
		{
			ASSERT(FALSE);
			return false;
		}

		double xc = start_x + I, yc = start_y + J;
		double x1 = -I, y1 = -J;			// start point in the center of circle's coordinate system
		double x2 = X - xc, y2 = Y - yc;	// end point in the center of circle's coordinate system
		double theta1 = NormalizeRad(atan2(y1, x1));
		double theta2 = NormalizeRad(atan2(y2, x2));
		double offsetRad = (G_ArcCCW == code)? offset/r: -offset/r;

		double angleRad = getDividngAngle(theta1, theta2, ratioFromStart, offsetRad, (G_ArcCCW == code));
		x = r * cos(angleRad) + xc;
		y = r * sin(angleRad) + yc;
		newI = xc - x;
		newJ = yc - y;
	}
	else	// G_Line == code
	{
		double dx = X - start_x, dy = Y - start_y;
		double angleRad = atan2(dy, dx);
		double offsetX = offset * cos(angleRad);
		double offsetY = offset * sin(angleRad);

		x = start_x + dx*ratioFromStart + offsetX;
		y = start_y + dy*ratioFromStart + offsetY;

		// Check offset position
		Point2d vFromStart(x - start_x, y - start_y), vFromTarget(x - X, y - Y);
		if (vFromStart.isNull())	// the next position is coincide with the start position
		{
			x = start_x, y = start_y;
			return true;
		}
		if (vFromTarget.isNull())	// the next position is coincide with the target position
		{
			x = X, y = Y;
			return true;
		}
		// Otherwise the next position is on the line connecting the start position to the target position 

		if (0 < vFromStart.Dot(vFromTarget))
		{	// the current position lies outside the block segment
			Point2d vBlock(X - start_x, Y - start_y);
			if (0 < vBlock.Dot(vFromStart))	// offset position lies beyond the target position
			{
				x = X;
				y = Y;
			}
			else			// offset position lies prior to the starting position
			{
				ASSERT(0 > vBlock.Dot(vFromStart));
				x = start_x;
				y = start_y;
			}
		}
#ifdef _DEBUG
		else
		{
			ASSERT(0 > vFromStart.Dot(vFromTarget));
		}
#endif // _DEBUG
	}

	return true;
}


//////////////////////////////////////////////////////////////////////////
// FileMPF class implementation

bool FileMPF::read(LPCTSTR pszFile, TCHAR* pszError, size_t errBufCount)
{
	CStdioFile file;
	CFileException ex;

	if (!file.Open(pszFile, CFile::modeRead|CFile::typeText|CFile::shareDenyWrite, &ex))
	{
		ex.GetErrorMessage(pszError, errBufCount);
		return false;
	}
	setFilePath(pszFile);

	_N1_lineNo = 0;
	_iCurrLine = -1;
	_version = verUnknown;
	_strLines.RemoveAll();
	restartLog.clear();

	CString sLine;
	bool versionNotVerified = true;
	int iRestart = -1;
	for (int i = 0; file.ReadString(sLine); ++i)
	{
		sLine.TrimRight();
		_strLines.Add(sLine);

		// N1 line number check
		if (0 == _N1_lineNo)
		{
			if (_T("N1") == sLine)
			{
				_N1_lineNo = i + 1;
			}
		}

		// Version check-up
		if (versionNotVerified)
		{
			versionNotVerified = (false == getVersion(sLine));
		}
		// Restart check-up
		if (0 > iRestart)
		{
			iRestart = isMarkUpString(sLine, csz_RestartHead, _countof(csz_RestartHead) - 1)? i: -1;
		}
	}

	// Remove the extra blank lines at the end of file content
	int linesTotal = _strLines.GetCount(), countBlankLines = 0;
	for (int i = linesTotal - 1; 0 <= i; --i)
	{
		if (!isBlankLine(_strLines.GetAt(i)))
			break;
		++countBlankLines;
	}
	if (0 < countBlankLines)
		_strLines.RemoveAt(linesTotal - countBlankLines, countBlankLines);
	ASSERT(linesTotal - countBlankLines == _strLines.GetCount());

	// Check the 'Restart' information
	if (0 <= iRestart && !getRestartInfo(iRestart))
		restartLog.partNo = 0;

	if (0 < lines())
	{
		_iCurrLine = 0;
		return true;
	}

	if (NULL != pszError && 0 < errBufCount)
	{
		CString strErr;
		strErr.Format(_T("The file \"%s\" is empty"), _strFileName);
		if (size_t(strErr.GetLength()) >= errBufCount-1)
			strErr.ReleaseBuffer(errBufCount-1);
		_tcscpy(pszError, strErr);
	}
	return false;
}

void FileMPF::setFilePath(LPCTSTR pszFile)
{
	_strFilePath = pszFile;
	int iCut = _strFilePath.ReverseFind(_T('\\'));
	if (0 < iCut)
		_strFileName = _strFilePath.Mid(iCut+1);
	else
		_strFileName = pszFile;
	return;
}

bool FileMPF::getVersion(const CString& sLine)
{
	ASSERT(verUnknown == _version);

	if (IsCommentLine(sLine))
	{
		CString sV;
		if (getVersionString(sLine, sV))
		{
			if (_T("V16A05") == sV)
			{
				_version = verV16A05;
			}
		}
	}
	else
	{
		if (0 <= sLine.Find(_T("HKPPP")) || 0 <= sLine.Find(_T("HKSTR")))
		{
			_version = verV16;
		}
		else if (0 <= sLine.Find(_T("HKLON")))
		{
			_version = verV08;
		}
	}

	return (verUnknown != _version);
}

bool FileMPF::getRestartInfo(const int iRestart)
{
	//
	// Put the restart log lines into a single string
	//
	CString sRestart;
	int nLines = 0;
	if (!getRestartLogText(_strLines, iRestart, sRestart, nLines))
		return false;
	restartLog.iRestartStart = iRestart;
	restartLog.nRestartLogLines = nLines;

	//
	// Get restart part-contour information for the original part and
	// the modified part shape
	//
	CString str1, str2;
	int iFrom = _countof(csz_RestartHead);
	if (!getMarkupString(sRestart, iFrom, csz_TagOriginalPart, str1)
		|| !getMarkupString(sRestart, iFrom, csz_TagModifiedPart, str2))
	{
		ASSERT(FALSE);
		return false;
	}
	if (!getRestartLogInfo(str1, str2, restartLog))
		return false;

	//
	// Restart position and number of block lines of modified part shape information
	//
	const int iLine = restartLog.modStartLine - 1;
	if (0 <= iLine && iLine < _strLines.GetCount()) // has a valid modified shape information
	{
		str1 = _strLines.GetAt(iLine);
		if (!isMarkUpString(str1, csz_RestartPartHead, _countof(csz_RestartPartHead)-1))
		{
			ASSERT(FALSE);
			return false;
		}
	}
	else	// doesn't have any modified shape information because the restart work is one of
	{													// restart part / piercing / lead-in
		if (0 != restartLog.GcodeIndex)
		{
			ASSERT(FALSE);
			return false;
		}
		ASSERT(restartLog.modShapeNBlockNo == restartLog.shapeNBlockNo);
	}
	return true;
}

bool FileMPF::write(int iLineTo, FileWriter& file)
{
	const int nLastLineNo = minof(_strLines.GetCount(), iLineTo + 1);
	if (nLastLineNo <= _iCurrLine)
		return false;

	while (_iCurrLine < nLastLineNo)
	{
		file.WriteString(_strLines.GetAt(_iCurrLine) + _T('\n'));
		++_iCurrLine;
	}
	return true;
}

bool FileMPF::writeToEnd(FileWriter& file)
{
	const int nLinesTotal = _strLines.GetCount();
	if (nLinesTotal <= _iCurrLine)
		return false;

	while (_iCurrLine < nLinesTotal)
	{
		file.WriteString(_strLines.GetAt(_iCurrLine) + _T('\n'));
		++_iCurrLine;
	}
	return true;
}

bool FileMPF::moveToLine(int iLineTo)
{
	if (!isValidIndex(iLineTo))
		return false;
	_iCurrLine = iLineTo;
	return true;
}

bool FileMPF::getCurrLine(CString& str)
{
	if (0 <= _iCurrLine && _iCurrLine < _strLines.GetCount())
	{
		str = _strLines.GetAt(_iCurrLine);
		return true;
	}
	return false;
}

bool FileMPF::feed(CString& str)
{
	if (0 <= _iCurrLine && _iCurrLine < _strLines.GetCount())
	{
		str = _strLines.GetAt(_iCurrLine);
		++_iCurrLine;
		return true;
	}
	return false;
}

bool FileMPF::getNextLine(CString& str)
{
	if (0 <= _iCurrLine && _iCurrLine + 1 < _strLines.GetCount())
	{
		++_iCurrLine;
		str = _strLines.GetAt(_iCurrLine);
		return true;
	}
	return false;
}

bool FileMPF::getNextBlock(CString& str)
{
	while (feed(str))
	{
		InterfaceMPF::PrintLog(enLogInfo, _T("Line#%3d: %s"), _iCurrLine, str);

		str.Trim();
		if (str.IsEmpty() || _T(';') == str[0])
			continue;

		int iComment = str.Find(_T(';'));
		if (0 < iComment)
		{
			str.ReleaseBuffer(iComment);
			str.Trim();
			if (str.IsEmpty())
				continue;
		}
		return true;
	}
	return false;
}

bool FileMPF::getLineText(int iLine, CString& str)
{
	if (isValidIndex(iLine))
	{
		str = _strLines.GetAt(iLine);
		return true;
	}
	return false;
}

bool FileMPF::setLineText(int iLine, const CString& str)
{
	if (isValidIndex(iLine))
	{
		_strLines.SetAt(iLine, str);
		return true;
	}
	return false;
}

bool FileMPF::getWholeContent(CStringArray& arrLine)
{
	if (_strLines.IsEmpty())
		return false;

	if (!arrLine.IsEmpty())
		arrLine.RemoveAll();

	arrLine.Copy(_strLines);
	return true;
}

bool FileMPF::rewind()
{
	if (_strLines.GetCount())
	{
		_iCurrLine = 0;
		return true;
	}
	return false;
}

bool FileMPF::next()
{
	if (_iCurrLine + 1 < _strLines.GetCount())
	{
		++_iCurrLine;
		return true;
	}
	return false;
}

bool FileMPF::back()
{
	if (0 < _iCurrLine)
	{
		--_iCurrLine;
		return true;
	}
	return false;
}
