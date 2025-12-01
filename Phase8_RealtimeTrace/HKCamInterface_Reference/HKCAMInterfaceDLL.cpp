#include "stdafx.h"
#include "HKCAMInterface.h"
#include "CAMViewerFactory.h"
#include "HKCAMInterfaceDLL.h"

#include "VersionInfo.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif //_DEBUG

//////////////////////////////////////////////////////////////////////////

static CAMViewerFactory s_ViewerFactory;
bool g_showErrMessage = false;


//////////////////////////////////////////////////////////////////////////

DLLSPEC void CVTurnMessageON()
{
	g_showErrMessage = true;
}

DLLSPEC void CVTurnMessageOFF()
{
	g_showErrMessage = false;
}

DLLSPEC bool CVGetVersionVals(stVersion& prodVer, stVersion& fileVer)
{
	LPCTSTR pszFilename = theApp.GetDLLFilePath();
	return getAppVersions(pszFilename, prodVer, fileVer);
}

DLLSPEC bool CVGetVersionStrs(TCHAR szProd[c_verInfoStrBufLen], TCHAR szFile[c_verInfoStrBufLen])
{
	CString sP, sF;
	if (getAppVersions(theApp.GetDLLFilePath(), sP, sF))
	{
		if (c_verInfoStrBufLen > sP.GetLength())
			_tcscpy_s(szProd, c_verInfoStrBufLen, sP);
		else
			_tcsncpy_s(szProd, c_verInfoStrBufLen, sP, c_verInfoStrBufLen-1);
		szProd[c_verInfoStrBufLen-1] = 0;

		if (c_verInfoStrBufLen > sF.GetLength())
			_tcscpy_s(szFile, c_verInfoStrBufLen, sF);
		else
			_tcsncpy_s(szFile, c_verInfoStrBufLen, sF, c_verInfoStrBufLen-1);
		szFile[c_verInfoStrBufLen-1] = 0;
		return true;
	}
	return false;
}

DLLSPEC void CVGetBuildInfo(TCHAR szBuild[c_verInfoStrBufLen])
{
#ifdef _DEBUG
	_tcscpy_s(szBuild, c_verInfoStrBufLen, _T("Debug"));
#else
	_tcscpy_s(szBuild, c_verInfoStrBufLen, _T("Release"));
#endif // _DEBUG or _RELEASE
}


//////////////////////////////////////////////////////////////////////////


DLLSPEC HWND CVCreateWindow(HWND hWndParent, UINT ID, int x, int y, int cx, int cy, TraceMouseCoords pfn)
{
	return s_ViewerFactory.CreateViewer(hWndParent, ID, x, y, cx, cy, pfn);
}

DLLSPEC int CVDestroyWindow(HWND hWndCV)
{
	return s_ViewerFactory.DestroyViewer(hWndCV);
}

DLLSPEC int CVLoadCAMFile(HWND hWndCV, LPCTSTR pszFilePath, char* pszError, int nMaxBuffer)
{
	CString strError;
	int ret = s_ViewerFactory.Load(hWndCV, pszFilePath, strError);
	if (ret != CV_NOERROR)
	{
		if (strError.GetLength() > 0)
		{
			CStringA str(strError);
			strncpy_s(pszError, nMaxBuffer, str.GetBuffer(), str.GetLength());
		}
	}
	return ret;
}

DLLSPEC int CVGetCAMFileInfo(HWND hWndCV, CAMFILEINFO* pInfo)
{
	return s_ViewerFactory.GetFileInfo(hWndCV, pInfo);
}

DLLSPEC int CVDeleteContents(HWND hWndCV)
{
	return s_ViewerFactory.DeleteContents(hWndCV);
}

DLLSPEC int CVRelocateWindow(HWND hWndCV, int x, int y, int cx, int cy)
{
	return s_ViewerFactory.RelocateView(hWndCV, x, y, cx, cy);
}

DLLSPEC int CVZoomContens(HWND hWndCV, BOOL bScaleUp)
{
	return s_ViewerFactory.Zoom(hWndCV, bScaleUp ? enZoomIn : enZoomOut);
}

DLLSPEC int CVZoomFitToWindow(HWND hWndCV)
{
	return s_ViewerFactory.Zoom(hWndCV, enFullView);
}

DLLSPEC int CVZoomActualSize(HWND hWndCV)
{
	return s_ViewerFactory.Zoom(hWndCV, enActualSize);
}

DLLSPEC double CVGetZoomScale(HWND hWndCV)
{
	return s_ViewerFactory.GetZoomScale(hWndCV);
}

DLLSPEC int CVSetOption(HWND hWndCV, int iOption, unsigned int nValue)
{
	return s_ViewerFactory.SetOption(hWndCV, iOption, nValue);
}

DLLSPEC int CVGetOption(HWND hWndCV, int iOption, unsigned int& nValue)
{
	return s_ViewerFactory.GetOption(hWndCV, iOption, nValue);
}

DLLSPEC int CVGetOptions(HWND hWndCV, DrawingOption& opt)
{
	return s_ViewerFactory.GetOptions(hWndCV, opt);
}

DLLSPEC int CVGetWholeSize(HWND hWndCV, double& width, double& height)
{
	return s_ViewerFactory.GetWholeSize(hWndCV, width, height);
}

DLLSPEC void CVSetProgressMonitor(progressLog pfn, int nLogLevel)
{
	InterfaceMPF::SetLogFunction(pfn);
	switch (nLogLevel)
	{
	case LOGLEVEL_WARNING:
		InterfaceMPF::SetErrorLogLevel(enLogWarning);
		break;
	case LOGLEVEL_ERROR:
		InterfaceMPF::SetErrorLogLevel(enLogError);
		break;
	default:
		if (pfn)
			InterfaceMPF::SetErrorLogLevel(enLogInfo);
		break;
	}
	return;
}

DLLSPEC int CVStartCutting(HWND hWndCV, int nPartFrom, int nContourFrom, int nIsReverse)
{
	return s_ViewerFactory.StartCutting(hWndCV, nPartFrom, nContourFrom, (0 != nIsReverse));
}

DLLSPEC int CVUpdateCutting(HWND hWndCV, int part, int contour, const char* currentBlock, double progress, double xwcs, double ywcs, int isGcodeBlock)
{
	return s_ViewerFactory.UpdateCutting(hWndCV, part, contour, currentBlock, progress, xwcs, ywcs, (isGcodeBlock != 0));
}

DLLSPEC int CVUpdateCutting2(HWND hWndCV, int part, int contour, int mpfLineNo, double progress, double xwcs, double ywcs)
{
	return s_ViewerFactory.UpdateCutting(hWndCV, part, contour, mpfLineNo, progress, xwcs, ywcs);
}

DLLSPEC int CVGetContourLength(HWND hWndCV, int part, int contour, double& length, bool isReset)
{
	return s_ViewerFactory.GetContourLength(hWndCV, part, contour, length, isReset);
}

DLLSPEC int CVGetPartCount(HWND hWndCV, int& partCount)
{
	return s_ViewerFactory.GetPartCount(hWndCV, partCount);
}

DLLSPEC int CVGetContourCount(HWND hWndCV, int part, int& contourCount)
{
	return s_ViewerFactory.GetContourCount(hWndCV, part, contourCount);
}

DLLSPEC int CVGetElementCount(HWND hWndCV, int part, int contour, int& elementCount)
{
	return s_ViewerFactory.GetElementCount(hWndCV, part, contour, elementCount);
}

DLLSPEC int CVGetElementInfo(HWND hWndCV, int part, int contour, int element, int& lineNo, double& length)
{
	return s_ViewerFactory.GetElementInfo(hWndCV, part, contour, element, lineNo, length);
}

DLLSPEC int CVGetElementPos(HWND hWndCV, int part, int contour, int element, double progress, double& wcsX, double& wcsY)
{
	return s_ViewerFactory.GetElementPos(hWndCV, part, contour, element, progress, wcsX, wcsY);
}

DLLSPEC int CVGetBlockProgress(HWND hWndCV, int partNo, int contourNo, int lineNo, double wcsX, double wcsY, double& cutDone, double& cutRemains)
{
	return s_ViewerFactory.GetBlockProgress(hWndCV, partNo, contourNo, lineNo, wcsX, wcsY, cutDone, cutRemains);
}

DLLSPEC int CVGetElementBlockCode(HWND hWndCV, int part, int contour, int element, char* pszBlock, int maxbuffer)
{
	return s_ViewerFactory.GetElementBlockCode(hWndCV, part, contour, element, pszBlock, maxbuffer);
}

DLLSPEC int CVGetBlockString(HWND hWndCV, int lineNo, PTSTR pszBlock, int bufferLength)
{
	return s_ViewerFactory.GetBlockString(hWndCV, lineNo, pszBlock, bufferLength);
}

DLLSPEC int CVGetMPFContent(HWND hWndCV, CStringArray& arrLine)
{
	return s_ViewerFactory.GetMPFContent(hWndCV, arrLine);
}

DLLSPEC void CVClearMPFContent(CStringArray& arrLine)
{
	arrLine.RemoveAll();
}

DLLSPEC int CVGetScanCut(HWND hWndCV, int& scancut)
{
	return s_ViewerFactory.GetScanCut(hWndCV, scancut);
}

DLLSPEC int CVFinishCutting(HWND hWndCV)
{
	return s_ViewerFactory.FinishCutting(hWndCV);
}

DLLSPEC int CVCompleteLastElement(HWND hWndCV)
{
	return s_ViewerFactory.CompleteLastElement(hWndCV);
}

DLLSPEC int CVCompleteLastContour(HWND hWndCV)
{
	return s_ViewerFactory.CompleteLastContour(hWndCV);
}

DLLSPEC int CVStopCutting(HWND hWndCV)
{
	return s_ViewerFactory.StopCutting(hWndCV);
}

DLLSPEC int CVResetCutting(HWND hWndCV)
{
	return s_ViewerFactory.ResetCutting(hWndCV);
}

DLLSPEC int CVSetCuttingDone(HWND hWndCV, int fromPart, int fromContour, int toPart, int toContour, int isReverse)
{
	return s_ViewerFactory.SetCuttingDone(hWndCV, fromPart, fromContour, toPart, toContour, isReverse);
}

DLLSPEC int CVSetCuttingProgressDone(HWND hWndCV, int fromPart, int fromContour, int toPart, int toContour, int toElement, double toProgress)
{
	return s_ViewerFactory.SetCuttingProgressDone(hWndCV, fromPart, fromContour, toPart, toContour, toElement, toProgress);
}

DLLSPEC int CVCreateOptimalCAMFile(HWND hWndCV, int nOptimizationCode, LPVOID pParam, LPCTSTR pszFile)
{
	CString strError;
	int nRet = CVERR_INVALID_OPTIMIAZTION;
	switch (optimizationType(nOptimizationCode))
	{
	case optJump:
		nRet = s_ViewerFactory.WriteJumpOptimizedFile(hWndCV, pszFile, pParam, strError);
		if (CV_NOERROR != nRet && g_showErrMessage)
			AfxMessageBox(_T("CAM Flying-mode optimization error: ")+strError);
		break;
	case optRPP:
		nRet = s_ViewerFactory.WriteRPPOptimizedFile(hWndCV, pszFile, strError);
		if (CV_NOERROR != nRet && g_showErrMessage)
			AfxMessageBox(_T("CAM RP Piercing optimization error: ")+strError);
		break;
	case optJumpRPP:
		nRet = s_ViewerFactory.WriteJumpRPPOptimizedFile(hWndCV, pszFile, pParam, strError);
		if (CV_NOERROR != nRet && g_showErrMessage)
			AfxMessageBox(_T("CAM Flying-mode RPP optimization error: ")+strError);
		break;
	default:
		if (g_showErrMessage)
			AfxMessageBox(_T("Invalid MPF optimization request"));
		break;
	}

	return nRet;
}

DLLSPEC int CVCreateOptimalCAMFileWithOrgFile(int nOptimizationCode, LPVOID pParam, LPCTSTR pszOrgFile, LPCTSTR pszDestFile, char* pszError, int maxErrorBuffer)
{
	CString strError;
	int nRet = CVERR_INVALID_OPTIMIAZTION;
	switch (optimizationType(nOptimizationCode))
	{
	case optJump:
		nRet = s_ViewerFactory.WriteJumpOptimizedFileWithOrgFile(pszOrgFile, pszDestFile, pParam, strError);
		if (CV_NOERROR != nRet && g_showErrMessage)
			AfxMessageBox(_T("CAM Flying-mode optimization error: ") + strError);
		break;
	case optRPP:
		nRet = s_ViewerFactory.WriteRPPOptimizedFileWithOrgFile(pszOrgFile, pszDestFile, strError);
		if (CV_NOERROR != nRet && g_showErrMessage)
			AfxMessageBox(_T("CAM RP Piercing optimization error: ") + strError);
		break;
	case optJumpRPP:
		nRet = s_ViewerFactory.WriteJumpRPPOptimizedFileWithOrgFile(pszOrgFile, pszDestFile, pParam, strError);
		if (CV_NOERROR != nRet && g_showErrMessage)
			AfxMessageBox(_T("CAM Flying-mode RPP optimization error: ") + strError);
		break;
	default:
		if (g_showErrMessage)
			AfxMessageBox(_T("Invalid MPF optimization request"));
		break;
	}

	if (CV_NOERROR != nRet)
	{
		CStringA error(strError);
		strncpy_s(pszError, maxErrorBuffer, error.GetBuffer(), error.GetLength());
	}
	return nRet;
}

DLLSPEC int CVTraceFlyingModeJump(HWND hWndCV, LPVOID pParam, LPCTSTR pszFile)
{
	CString strErr;
	int nRet = s_ViewerFactory.WriteFlyingJumpTraceFile(hWndCV, pszFile, pParam, strErr);
	if (CV_NOERROR != nRet && g_showErrMessage)
		AfxMessageBox(_T("Trace file creation error: ")+strErr);
	return nRet;
}


DLLSPEC int CVCreateRestartCAMFile(RestartInfo& option, LPCTSTR pszCAMFile, LPCTSTR pszRestartFile)
{
	CString strErr;
	int nRet = s_ViewerFactory.WriteRestartWorkFile(option, pszCAMFile, pszRestartFile, strErr);
	if (CV_NOERROR != nRet && g_showErrMessage)
		AfxMessageBox(_T("Restart work file error: ") + strErr);
	return nRet;
}

DLLSPEC int CVRestartSetLogFolder(LPCTSTR pszFolder)
{
	return CAMViewerFactory::setRestartLogFolder(pszFolder)? CV_NOERROR: CVERR_RESTART_LOGFOLDER;
}

DLLSPEC int CVSetViewLogFolder(LPCTSTR pszFolder)
{
	return CCAMViewWnd::SetLogFolder(pszFolder)? CV_NOERROR: CVERR_VIEWER_LOGFOLDER;
}

DLLSPEC int CVGetCuttingProgressInfo(HWND hWndCV, LPARAM& pInfo, int& nCount)
{
	CutProgress* pcpinfo = NULL;
	int ret = s_ViewerFactory.GetCuttingProgressInfo(hWndCV, pcpinfo, nCount);
	pInfo = (LPARAM)pcpinfo;
	return ret;
}

DLLSPEC int CVGetCurrCutDistance(HWND hWndCV, double& cutDistance)
{
	return s_ViewerFactory.GetCurrCutDistance(hWndCV, cutDistance);
}
