#pragma once

#include "CAMString.h"
#include "OpenGLCAMViewWnd.h"
#include "MotionPlanning.h"

enum enZoom { enActualSize, enZoomIn, enZoomOut, enFullView };

inline double mmToInch(double mm) { return 0.039370*mm; }
inline double inchTomm(double in) { return 25.40*in; }

enum enOptimization
{
	optNone,		// no optimization
	optJump,		// flying jump optimization (Ping-Pong jump)
	optRPP,			// Rapid piercing optimization
	optJumpRPP,		// flying jump with Rapid piercing optimization
};

inline enOptimization optimizationType(int nOptCode)
{
	if (iOPTIMIZATION_JUMP == (iOPTIMIZATION_JUMP&nOptCode))
	{
		return (iOPTIMIZATION_RPP == (iOPTIMIZATION_RPP&nOptCode))? optJumpRPP: optJump;
	}
	else if (iOPTIMIZATION_RPP == (iOPTIMIZATION_RPP&nOptCode))
	{
		return optRPP;
	}
	else
	{
		return optNone;
	}
}

inline void applyMotionGainFactor(const MotionSpec& cfg, MotionDef& param)
{
	if (0 < cfg.jerkLimitScale)
		param.jerk *= cfg.jerkLimitScale;
	if (0 < cfg.acclLimitScale)
		param.accl *= cfg.acclLimitScale;
	return;
}

enum enRestartPos
{
	restartUnDefined,
	restartFile,		// restart from the beginning of the MPF file
	restartPart,		// restart from a nesting part
	restartPiercing,	// restart from a contour of a part with piercing
	restartLeadIn,		// restart from the lead-in
	restartCutting,		// restart cutting skipping the piercing
	restartReachEnd,	// restart reach the end of nesting parts5
};

struct RESTART
{
	// Input information copied from 'RestartInfo'
	int		partNo;
	int		contourNo;	// restart position contour number in the original part shape
	int		lineNo;		// restart line number in the original part of MPF
	double	wcsX, wcsY;	// restart position in the workpiece coordinates

	double	startingOffset;	// restart beginning option
	bool	isReverseOrder;	// part processing order in CAM nesting

	// Restart information derived from the 'RestartInfo'
	int orgShapeBlockNo;	// nesting part original block number, N10001, N20001,...
	int modShapeBlockNo;	// modified part block number, NXXXX
	int restartShapeLineNo;	// modified restart shape starting line number
	int restartContourLineNo;	// modified contour starting line number (HKSTR)
	int orgModLineOffset;	// offset between the original shape line numbers and the modified ones
	int iBlockCode;			// G-code block index

	// Generated rewrite information for restart work
	enRestartPos pos;		// restart position
	CString strMarkUp;		// restart part & contour number specifiers, or error message if failed
	CString strHKOST;		// restart nesting-part block
	CString strHKSTR;		// restart contour block

	void clone(const RestartInfo& param)
	{
		partNo			= param.partNo;
		contourNo		= param.contourNo;
		lineNo			= param.lineNo;
		wcsX			= param.stopXwcs;
		wcsY			= param.stopYwcs;
		startingOffset	= param.startingOffset;
		isReverseOrder	= param.isReverseOrder;

		orgShapeBlockNo = modShapeBlockNo = 0;
		restartShapeLineNo = restartContourLineNo = orgModLineOffset = 0;
		iBlockCode = 0;
		pos = restartUnDefined;
		strMarkUp.Empty();
		strHKOST.Empty();
		strHKSTR.Empty();
	}
};

//////////////////////////////////////////////////////////////////////////

int  ErrCode_PROF2CAM(int code);
int  trimZeroTail(CString& str);
bool isGcodeBlock(LPCTSTR szBlock);

int generateFlyingMode(FlyingParam& param, FlyingProfile& jump, bool isInches, const Point2d& pt1, const Point2d& pt2, double& h1, double& h2, double& r);
void initFlyingParamMetrics(const JumpParam& cfg, FlyingParam& param);

void getJumpFloatString(double hUp, double hDown, double residual, bool inches, CString& str, int& nJumpType);
void getJumpSpotTypeV08(int nP1, int nC1, int nP2, int nC2, FlyingParam& param);
void getJumpSpotTypeV16(int nP1, int nC1, int nP2, int nC2, FlyingParam& param);
bool getHKCutString(CString& strHKCUT, CAM_CONTOUR& c);
bool writeThroughHKCUT(FileMPF& loader, CAM_CONTOUR& c, FileWriter& file, CString& strErr);
bool writeHKSCRCBlocks(FileMPF& loader, FileWriter& file, CString& strErr);
bool writeTraceBuf(const FlyingTrace& buf, FileWriter& file, const CString& strBlock);


//////////////////////////////////////////////////////////////////////////

class CAMContainer
{
public:
	FileMPF		loader;
	CAM_DATA	camData;
	CCAMViewWnd viewer;

	int  WriteFlyingOptimization(const JumpParam& param, FileWriter& file, CString& strErr);
	int  WriteRPPOptimization(FileWriter& file, CString& strErr);
	int  WriteFlyingRPPOptimization(const JumpParam& param, FileWriter& file, CString& strErr);
	bool WriteFlyingJumpTrace(const JumpParam& param, UINT ms_sampling, FileWriter& file, CString& strErr);
	int  WriteRestartWorkFile(const RestartInfo& param, FileWriter& file, CString& strErr);

	int  GetBlockString(int lineNo, LPTSTR pszBlock, int bufferLength);
	int  GetBlockProgress(int iPart, int iContour, int nLineNo, double wcsX, double wcsY, double& cutDone, double& cutRemains);
	int  GetFileContent(CStringArray& arrLine);

	void ArrangeRestartFile();

	int UpdateCuttingProgress(int nPart, int nContour, const int mpfLineNo, double progress, double xwcs, double ywcs);

protected:
	// Restart work functions called from WriteRestartWorkFile()
	int restartGetRestartPos(const RestartInfo& param, RESTART& info);
		int restartGetPosAtNesting(RESTART& info, CString& strLog);
		int restartGetPosAtPart(RESTART& info, CString& strLog);
		int restartGetPosAtBlock(RESTART& info, CString& strLog);
	int restartWriteModPart(RESTART& info, FileWriter& file);
	// end of Restart functions

	bool GetPartStartInfo(int iPart, int& piercing, int& cutting, double& x, double& y);
	bool GetPartEndInfo(int iPart, int& piercing, int& cutting, double& x, double& y);
	int GetJumpInfoString(int iPart, FlyingParam& param, bool inches, fileVersion ver, CString& strJumpParam);
	int GetJumpInfoString(int iPart, int iCont, FlyingParam& param, bool inches, fileVersion ver, CString& strJumpParam);
};

class CAMViewerFactory
{
public:
	CAMViewerFactory() {}
	virtual ~CAMViewerFactory();

	// Operation
public:
	HWND CreateViewer(HWND hWndParent, UINT ID, int x, int y, int cx, int cy, TraceMouseCoords pfn);
	int  DestroyViewer(HWND hWnd);
	int  Load(HWND hWnd, LPCTSTR pszFile, CString& strError);
	int  GetFileInfo(HWND hWnd, CAMFILEINFO* pInfo);

	// Optimized MPF creation
	int  WriteJumpOptimizedFile(HWND hWnd, LPCTSTR pszFile, LPVOID pParam, CString& strError);
	int  WriteRPPOptimizedFile(HWND hWnd, LPCTSTR pszFile, CString& strError);
	int  WriteJumpRPPOptimizedFile(HWND hWnd, LPCTSTR pszFile, LPVOID pParam, CString& strError);

	int  WriteJumpOptimizedFileWithOrgFile(LPCTSTR pszOrgFile, LPCTSTR pszDestFile, LPVOID pParam, CString& strError);
	int  WriteRPPOptimizedFileWithOrgFile(LPCTSTR pszOrgFile, LPCTSTR pszDestFile, CString& strError);
	int  WriteJumpRPPOptimizedFileWithOrgFile(LPCTSTR pszOrgFile, LPCTSTR pszDestFile, LPVOID pParam, CString& strError);

	// Trace file creation
	int  WriteFlyingJumpTraceFile(HWND hWnd, LPCTSTR pszFile, LPVOID pParam, CString& strError);

	// Restart-work support
	int	 WriteRestartWorkFile(const RestartInfo& option, LPCTSTR pszSrcFile, LPCTSTR pszDstFile, CString& strError);

	// Viewer functions
	int  DeleteContents(HWND hWnd);
	int  RelocateView(HWND hWnd, int x, int y, int cx, int cy);
	int  Zoom(HWND hWnd, enZoom how);
	int  SetOption(HWND hWnd, int iOption, unsigned int nValue);
	int  GetOption(HWND hWnd, int iOption, unsigned int& nValue);
	int  GetOptions(HWND hWnd, DrawingOption& opt);
	int  GetWholeSize(HWND hWnd, double& width, double& height);
	int	 StartCutting(HWND hWnd, int nPartFrom, int nContourFrom, bool IsReverse);
	int  UpdateCutting(HWND hWnd, int part, int contour, const char* currentBlock, double progress, double xwcs, double ywcs, bool isGcodeBlock);
	int  UpdateCutting(HWND hWnd, int part, int contour, int mpfLineNo, double progress, double xwcs, double ywcs);
	int	 GetContourLength(HWND hWnd, int part, int contour, double& length, bool isReset);
	int  GetPartCount(HWND hWnd, int& partCount);
	int	 GetContourCount(HWND hWnd, int part, int& contourCount);
	int	 GetElementCount(HWND hWnd, int part, int contour, int& elementCount);
	int  GetElementInfo(HWND hWnd, int part, int contour, int element, int& lineNo, double& length);
	int  GetElementPos(HWND hWnd, int part, int contour, int element, double progress, double& wcsX, double& wcsY);
	int  GetBlockProgress(HWND hWnd, int partNo, int contourNo, int lineNo, double wcsX, double wcsY, double& cutDone, double& cutRemains);
	int  GetElementBlockCode(HWND hWnd, int part, int contour, int element, char* pszBlock, int maxbuffer);
	int  GetBlockString(HWND hWnd, int lineNo, LPTSTR pszBlock, int bufferLength);
	int  GetMPFContent(HWND hWnd, CStringArray& arrLine);
	int	 GetScanCut(HWND hWnd, int& scancut);
	int  FinishCutting(HWND hWnd);
	int  CompleteLastElement(HWND hWnd);
	int  CompleteLastContour(HWND hWnd);
	int  StopCutting(HWND hWnd);
	int  ResetCutting(HWND hWnd);
	int  SetCuttingDone(HWND hWnd, int fromPart, int fromContour, int toPart, int toContour, int isReverse);
	int  SetCuttingProgressDone(HWND hWnd, int fromPart, int fromContour, int toPart, int toContour, int toElement, double toProgress);

	double GetZoomScale(HWND hWnd);
	int GetCuttingProgressInfo(HWND hWnd, CutProgress*& pInfo, int& nCount);
	int GetCurrCutDistance(HWND hWnd, double& distance);

	static bool isRestartLogEnabled();
	static bool setRestartLogFolder(LPCTSTR pszFolder);
	static void writeRestartLog(LPCTSTR pszFormat, ...);
	static void appendRestartLog(LPCTSTR pszFormat, ...);

protected:
	CPtrList m_listContainer;

	CAMContainer* FindContainer(HWND hWnd, POSITION* posFound=NULL)
	{
		POSITION pos = m_listContainer.GetHeadPosition();
		while (pos)
		{
			POSITION posCurr = pos;
			CAMContainer* pContainer = (CAMContainer*)m_listContainer.GetNext(pos);
			if (AfxIsValidAddress(pContainer, sizeof(CAMContainer))
				&& ::IsWindow(pContainer->viewer.m_hWnd)
				&& pContainer->viewer.m_hWnd == hWnd)
			{
				if (posFound)
					*posFound = posCurr;
				return pContainer;
			}
		}
		return NULL;
	}

	static CString s_logFolderRestart;
};

//
// Restart work related helpers
//

inline LPCTSTR restartPosToString(enRestartPos pos)
{
	switch (pos)
	{
	case restartUnDefined:	return _T("restartUnDefined");	break;
	case restartFile:		return _T("restartFile");		break;
	case restartPart:		return _T("restartPart");		break;
	case restartPiercing:	return _T("restartPiercing");	break;
	case restartLeadIn:		return _T("restartLeadIn");		break;
	case restartCutting:	return _T("restartCutting");	break;
	case restartReachEnd:	return _T("restartReachEnd");	break;
	default: ASSERT(FALSE); return _T("**internal error: unknown enRestartPos enum value");	break;
	}
	return _T("");
}

inline void appendRestartInfoLog(const RestartInfo& info, CString& strLog)
{
	if (CAMViewerFactory::isRestartLogEnabled())
	{
		CString str;
		str.Format(
			_T("          ,# Restart information parameter, \n")
			_T("          ,        Part No., %d\n")
			_T("          ,     Contour No., %d\n")
			_T("          ,    MPF line No., %d\n")
			_T("          ,   Stop position, %.4f, %.4f\n")
			_T("          ,  Restart offset, %.4f\n")
			_T("          ,     Reverse cut, %s\n\n"),
			info.partNo, info.contourNo, info.lineNo,
			info.stopXwcs, info.stopYwcs, info.startingOffset,
			info.isReverseOrder? _T("Yes") : _T("No"));
		strLog += str;
	}
}

inline void appendRestartModLineOffset(const RestartInfo& param, const RESTART& info, CString& strLog)
{
	if (CAMViewerFactory::isRestartLogEnabled())
	{
		CString str;
		str.Format(_T("    ** Restart On Restart: adjusting restart-line\n")
				   _T("          line#%d (modified shape) -> %d(original shape)\n"),
				   param.lineNo, info.lineNo);
		strLog += str;
	}
}

inline void appendRestartLinesLog(fileVersion ver, int nFirstNestingLine, int nLastNestingLine, int nFirstShapeLine, CString& strLog)
{
	if (CAMViewerFactory::isRestartLogEnabled())
	{
		CString str;
		str.Format(
			_T("# Source MPF configuration, \n")
			_T("          ,             MPF version,  %s\n")
			_T("          ,     Nesting Start line#, %d\n")
			_T("          ,       Nesting End line#, %d\n")
			_T("          ,   CAM Shape start line#, %d\n\n"),
			getFileVersionName(ver),
			nFirstNestingLine, nLastNestingLine, nFirstShapeLine);
		strLog += str;
	}
}

inline void appendRestartLog(CString& strLog, LPCTSTR pszFormat, ...)
{
	extern TCHAR s_szLogBuf[4096];

	if (CAMViewerFactory::isRestartLogEnabled())
	{
		va_list args;
		va_start(args, pszFormat);
		_vstprintf_s(s_szLogBuf, _countof(s_szLogBuf), pszFormat, args);

		CString str(_T("          , "));
		strLog += (str + s_szLogBuf);
	}
}

inline void addLogStrRestartFile(RESTART& info, int nFirstNestingLine, CString& strLog)
{
	appendRestartLog(strLog, _T("Restart position, restartFile\n"));
	appendRestartLog(strLog, _T("MPF line No(%d) < First nesting line(%d)\n"), info.lineNo, nFirstNestingLine);
	appendRestartLog(strLog, _T("  restart partNo = %d\n  restart contourNo = %d\n"), info.partNo, info.contourNo);
	if (1 < info.partNo || 1 < info.contourNo)
	{
		appendRestartLog(strLog, _T("*Warning, Both of partNo and contourNo should have been 1\n"));
	}
}

inline void addLogStrRestartAtNesting(RESTART& info, int nLastNestingLine, CString& strLog)
{
	appendRestartLog(strLog, _T("Restart position, restartPart\n"));
	appendRestartLog(strLog, _T("MPF line No(%d) <= Last nesting line(%d)\n"), info.lineNo, nLastNestingLine);
	if (1 != info.contourNo)
	{
		appendRestartLog(strLog, _T("*Warning, contourNo(%d) should have been 1\n"), info.contourNo);
		info.strMarkUp.Format(
			_T("Restart information integrity failure: restart-part with line No(%d), partNo(%d) and contourNo(%d)"),
			info.lineNo, info.partNo, info.contourNo);
	}
}

inline void addLogStrRestartBlock(const RESTART& info, int nFirstPartStartLine, CString& strLog)
{
	appendRestartLog(strLog, _T("First part shape line(%d) <= MPF line No(%d)\n"), nFirstPartStartLine, info.lineNo);
	appendRestartLog(strLog, _T("    going on further checks for part#%d-contour#%d\n"), info.partNo, info.contourNo);
}

inline void addLogStrRestartNone(CString& strLog)
{
	appendRestartLog(strLog, _T("*Warning, the end of nesing lines < the Restart line < the beginnning of part information lines\n"));
	appendRestartLog(strLog, _T(" No more work to do and returns, CVERR_RESTART_FINISHED(%d)\n"), CVERR_RESTART_FINISHED);
}
