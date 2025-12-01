// OpenGLCAMViewWnd.cpp : implementation file
//

#include "stdafx.h"
#include "OpenGLCAMViewWnd.h"
#include "Resource.h"	// for the custom cursor IDC_DRAG

#include <vector>

#ifdef _DEBUG
#include "CAMViewerFactory.h"
#define new DEBUG_NEW
#endif // _DEBUG

const double c_maxScale = 10000.0;

#define iGLLIST_PIERCING	0
#define iGLLIST_CONTOUR		1
#define iGLLIST_BOUNDINGS	2
#define iGLIST_CUTTING		3
#define iGLIST_PREVIOUS_CUT	4
#define nGLLIST_COUNT		5

#define STR_SECTION_DRAWING		_T("Drawing Options")
#define STR_KEY_OPT_PARTNO		_T("DrawPartNo")
#define STR_KEY_OPT_CONTOURNO	_T("DrawContourNo")
#define STR_KEY_OPT_PIERCING	_T("DrawPiercing")
#define STR_KEY_OPT_LIMIT_NOS	_T("LimitNumberOfContourPartNumbersToDisplay")
#define STR_KEY_SIMUL_TRACE		_T("EnsureVisible")
#define STR_KEY_SIMUL_REVERSE	_T("ReverseOrder")
#define STR_KEY_CLR_BKGND		_T("ColorBackGround")
#define STR_KEY_CLR_SHEET		_T("ColorSheet")
#define STR_KEY_CLR_PARTNO		_T("ColorPartNo")
#define STR_KEY_PARTNO_HEIGHT	_T("PartNo Height")
#define STR_KEY_CONTNO_HEIGHT	_T("ContourNo Height")
#define STR_KEY_CLR_PIERCING	_T("ColorPiercing")
#define STR_KEY_CLR_LEADIN		_T("ColorLeadIn")
#define STR_KEY_CLR_CONTOUR		_T("ColorCuttingContour")
#define STR_KEY_CLR_ENGRAVING	_T("ColorEngraving")
#define STR_KEY_CLR_CUTTING		_T("ColorUnderCutting")
#define STR_KEY_CLR_CONTOURNO	_T("ColorContourNo")

#define COLOR_BKGND			RGB(  0,   0,   0)
#define COLOR_WORKPIECE		RGB(  0,  64,  64)
#define COLOR_PARTNO		RGB(255, 255,   0)
#define COLOR_CONTOURNO		RGB(50,  255,  25)
#define COLOR_PIERCING		RGB(255,   0, 255)
#define COLOR_LEADIN		RGB(  0, 255, 255)
#define COLOR_CONTOUR		RGB(192, 192, 192)
#define COLOR_MARKING		RGB(128, 128,   0)
#define COLOR_CUTTING		RGB(200,   0,   0)

extern bool g_showErrMessage;

//////////////////////////////////////////////////////////////////////////

CString CCAMViewWnd::s_logFolder;
static TCHAR s_szLogText[4096];
const int s_ccLenLogBuf = _countof(s_szLogText);

bool CCAMViewWnd::isLogEnabled()
{
	return (0 < s_logFolder.GetLength());
}

bool CCAMViewWnd::SetLogFolder(LPCTSTR pszFolder)
{
	if (nullptr == pszFolder)
	{
		writeLog(_T("CAM viewer logging disabled.\n"));
		s_logFolder.Empty();
		return true;
	}

	DWORD attrib = ::GetFileAttributes(pszFolder);
	if (INVALID_FILE_ATTRIBUTES == attrib
		|| FILE_ATTRIBUTE_DIRECTORY != (FILE_ATTRIBUTE_DIRECTORY&attrib))
	{
		writeLog(_T("Restart log folder setting error:\n"));
		appendLog(_T("invalid folder[%s]"), pszFolder);
		ASSERT(FALSE);
		return false;
	}

	s_logFolder = pszFolder;
	if (_T('\\') == s_logFolder.GetAt(s_logFolder.GetLength()-1))
		s_logFolder.ReleaseBuffer(s_logFolder.GetLength()-1);

	writeLog(_T("CAM viewer logging enabled.\n"));

	return true;
}

void CCAMViewWnd::writeLog(LPCTSTR pszFormat, ...)
{
	if (false == isLogEnabled())
		return;

	// construct log message
	va_list args;
	va_start(args, pszFormat);
	_vstprintf_s(s_szLogText, s_ccLenLogBuf, pszFormat, args);

	// write log message to the log file
	CTime t = CTime::GetCurrentTime();
	TCHAR szFilePath[MAX_PATH];
	_stprintf_s(szFilePath, MAX_PATH, _T("%s\\%s_CAMViewer.log"), s_logFolder, t.Format(_T("%Y-%m-%d")));
	try
	{	// CStdioFile -> FILE* & CRT fopen, fprintf 로 바꿀것!     
		CStdioFile file(szFilePath, CFile::modeCreate | CFile::modeNoTruncate | CFile::modeWrite | CFile::typeText);
		file.SeekToEnd();

		CString strLog;
		strLog.Format(_T("[%s], %s\n"), t.Format(_T("%H:%M:%S")), s_szLogText);
		file.WriteString(strLog);
	}
	catch (CFileException* pe)
	{
		CString strErr;
		strErr.Format(_T("File could not be opened, cause = %d\n"), pe->m_cause);
		pe->Delete();
		AfxMessageBox(strErr);
	}

	return;
}

void CCAMViewWnd::appendLog(LPCTSTR pszFormat, ...)
{
	if (false == isLogEnabled())
		return;

	// construct log message
	va_list args;
	va_start(args, pszFormat);
	_vstprintf_s(s_szLogText, s_ccLenLogBuf, pszFormat, args);

	// write log message to the log file
	CTime t = CTime::GetCurrentTime();
	TCHAR szFilePath[MAX_PATH];
	_stprintf_s(szFilePath, MAX_PATH, _T("%s\\%s_CAMViewer.log"), s_logFolder, t.Format(_T("%Y-%m-%d")));
	try
	{	// CStdioFile -> FILE* & CRT fopen, fprintf 로 바꿀것!     
		CStdioFile file(szFilePath, CFile::modeCreate | CFile::modeNoTruncate | CFile::modeWrite | CFile::typeText);
		file.SeekToEnd();

		CString strLog, strLine(s_szLogText);
		while (strLine.GetLength())
		{
			int iNewLine = strLine.Find(_T('\n')) + 1;
			if (0 < iNewLine)
			{
				strLog = strLine.Left(iNewLine);
				if (strLine.GetLength() == iNewLine)
					strLine.Empty();
				else
					strLine = strLine.Mid(iNewLine);
			}
			else
			{
				strLog = strLine;
				strLine.Empty();
			}
			file.WriteString(_T("          , ") + strLog);
		}
	}
	catch (CFileException* pe)
	{
		CString strErr;
		strErr.Format(_T("File could not be opened, cause = %d\n"), pe->m_cause);
		pe->Delete();
		AfxMessageBox(strErr);
	}

	return;
}


//////////////////////////////////////////////////////////////////////////

inline void OpenGLListErrorHandler()
{
	if (g_showErrMessage)
	{
		CString strError;
		GLenum error = ::glGetError();
		if (GL_INVALID_VALUE == error)
			strError = _T("Can't create OpenGL list due to invalid number of contour lists");
		else if (GL_INVALID_OPERATION == error)
			strError = _T("Error in creating OpenGL list. No valid OpenGL Rendering context available");
		else
			strError = _T("Failed to create OpenGL List for unknown error");
		AfxMessageBox(strError);
	}
	return;
}

inline void drawGLContour(camContour& c, int& iLast, double& p, double scale)
{
	ASSERT(GE(p, 0) && LE(p, 1));

	glBegin(GL_LINE_STRIP);

	c._bCuttingDone = true;

	glVertex2d(c._xo, c._yo);
	if (IsSame(p, 1))
	{
		iLast = minof(iLast+1, c._numElements);
		p = 0;
	}

	for (int i = 0; i < iLast; ++i)
	{
		camElement& e = c._pElement[i];
		if (e.IsArc())
			e._pArc->glDraw(e._length, scale);
		glVertex2d(e._x2, e._y2);
		e._cutDone = true;
	}

	if (0 < p)
	{
		camElement& e = c._pElement[iLast];
		e._progress = p;
		if (e.IsArc())
		{
			e._pArc->glDraw(e._length, scale, p);
		}
		else
		{
			double q = 1.0 - p;
			glVertex2d(q*e._x1+p*e._x2, q*e._y1+p*e._y2);
		}
	}

	glEnd();
}


//////////////////////////////////////////////////////////////////////////

// CCAMViewWnd

IMPLEMENT_DYNAMIC(CCAMViewWnd, COpenGL2dWnd)

void CCAMViewWnd::GetDrawingOptions(DrawingOption& option)
{
	CWinApp* pApp = AfxGetApp();

	option.show_part_number = (1==pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_OPT_PARTNO, 1));
	option.show_cont_number = (1 == pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_OPT_CONTOURNO, 0));
	option.show_piercing = (1==pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_OPT_PIERCING, 1));
	option.show_ensure_visible = (1==pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_SIMUL_TRACE, TRUE));
	option.pixelheight_part_number = pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_PARTNO_HEIGHT, 25);
	option.pixelheight_cont_number = pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CONTNO_HEIGHT, 12);

	option.color[enColorBackground] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_BKGND, COLOR_BKGND));
	option.color[enColorCanvas] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_SHEET, COLOR_WORKPIECE));
	option.color[enColorPartNo] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_PARTNO, COLOR_PARTNO));
	option.color[enColorContourNo] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_CONTOURNO, COLOR_CONTOURNO));
	option.color[enColorPiercing] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_PIERCING, COLOR_PIERCING));
	option.color[enColorLeadIn] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_LEADIN, COLOR_LEADIN));
	option.color[enColorContour] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_CONTOUR, COLOR_CONTOUR));
	option.color[enColorEngraving] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_ENGRAVING, COLOR_MARKING));
	option.color[enColorCutProgress] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_CUTTING, COLOR_CUTTING));
}


CCAMViewWnd::CCAMViewWnd() : m_idListObjects(0), m_countGLLists(0)
{
	ASSERT(OPT_NUM_COLORS==c_last_color+1);
	m_viewScale = m_viewScale = 1;

	m_cbTraceMouseCoords = NULL;
	m_hCursorCross = ::LoadCursor(NULL, IDC_CROSS);

	m_iStartPart = m_iStartContour = -1;
	m_bHasCuttingProgress = m_bUnderCuttingProgress = m_bHasPreviousCutProgress = false;
	m_bLpointDown = false;
	m_cutDistance = 0;

	m_nMaxPartContourNumbers = 0;
}

CCAMViewWnd::~CCAMViewWnd()
{
	DeleteDrawingObjects();
}

void CCAMViewWnd::SaveSettings(int nOption)
{
	int nValue = -1;
	CString strKey;
	switch (nOption)
	{
	case iOPT_DRAW_PIERCING:
		strKey = STR_KEY_OPT_PIERCING;
		nValue = m_bShowPiercing? 1:0;
		break;
	case iOPT_PART_NUMBER:
		strKey = STR_KEY_OPT_PARTNO;
		nValue = m_bShowPartNo? 1:0;
		break;
	case iOPT_TRACE_CUTTINGSPOT:
		strKey = STR_KEY_SIMUL_TRACE;
		nValue = m_bEnsureSpotVisible? 1:0;
		break;
	case iOPT_PARTNO_HEIGHT:
		strKey = STR_KEY_PARTNO_HEIGHT;
		nValue = m_nPartNoHeight;
		break;
	case iOPT_CONTOUR_NUMBER:
		strKey = STR_KEY_OPT_CONTOURNO;
		nValue = m_bShowContourNo ? 1 : 0;
		break;
	case iOPT_CONTOURNO_HEIGHT:
		strKey = STR_KEY_CONTNO_HEIGHT;
		nValue = m_nContourNoHeight;
		break;
	case iOPT_LIMIT_PART_CONT_NO:
		strKey = STR_KEY_OPT_LIMIT_NOS;
		nValue = m_nMaxPartContourNumbers;
		break;
	case iOPT_COLOR_BACKGROUND:
		strKey = STR_KEY_CLR_BKGND;
		nValue = m_colors[enColorBackground].InCOLORREF();
		break;
	case iOPT_COLOR_WORKPIECE:
		strKey = STR_KEY_CLR_SHEET;
		nValue = m_colors[enColorCanvas].InCOLORREF();
		break;
	case iOPT_COLOR_PART_NUMBER:
		strKey = STR_KEY_CLR_PARTNO;
		nValue = m_colors[enColorPartNo].InCOLORREF();
		break;
	case iOPT_COLOR_CONTOUR_NUMBER:
		strKey = STR_KEY_CLR_CONTOURNO;
		nValue = m_colors[enColorContourNo].InCOLORREF();
		break;
	case iOPT_COLOR_PIERCING:
		strKey = STR_KEY_CLR_PIERCING;
		nValue = m_colors[enColorPiercing].InCOLORREF();
		break;
	case iOPT_COLOR_LEADIN:
		strKey = STR_KEY_CLR_LEADIN;
		nValue = m_colors[enColorLeadIn].InCOLORREF();
		break;
	case iOPT_COLOR_CONTOUR:	
		strKey = STR_KEY_CLR_CONTOUR;
		nValue = m_colors[enColorContour].InCOLORREF();
		break;
	case iOPT_COLOR_MARKING:
		strKey = STR_KEY_CLR_ENGRAVING;
		nValue = m_colors[enColorEngraving].InCOLORREF();
		break;
	case iOPT_COLOR_CUTTING:	
		strKey = STR_KEY_CLR_CUTTING;
		nValue = m_colors[enColorCutProgress].InCOLORREF();
		break;
	default:	ASSERT(FALSE);	break;
	}
	if (strKey.GetLength())
	{
		CWinApp* pApp = AfxGetApp();
		if (pApp)
		{
			VERIFY (pApp->WriteProfileInt(STR_SECTION_DRAWING, strKey, nValue));
		}
	}
	return;
}


void CCAMViewWnd::DeleteDrawingObjects()
{
	if (0 < m_countGLLists)
	{
		::glDeleteLists(m_idListObjects, m_countGLLists);
		m_idListObjects = 0;
		m_countGLLists = 0;
	}
}

BOOL CCAMViewWnd::UpdateCAMData(const CAM_DATA& data)
{
	if (!m_camLayout.Init(data))
		return FALSE;

	// Reset cutting monitoring flags
	m_bHasCuttingProgress = false;
	m_bUnderCuttingProgress = false;
	m_bHasPreviousCutProgress = false;

	// Set up the viewport
	if (m_hWnd)
	{
		SetupViewport();
		m_viewScale = m_viewScale;

		// Initialize OpenGL list buffers
		if (!CreateGLlists())
			return FALSE;

		UpdateViewport();

		CWnd* pWnd = AfxGetMainWnd();
		if (pWnd && pWnd->m_hWnd)
			pWnd->SendMessage(WNM_CAMVIEW_SCALECHANGED, WPARAM(m_hWnd), LPARAM(&m_viewScale));
	}
	return TRUE;
}

void CCAMViewWnd::DeleteContents()
{
	// Delete drawing objects
	DeleteDrawingObjects();

	// Reset all the monitoring control flags
	m_bHasCuttingProgress = false;
	m_bUnderCuttingProgress = false;
	m_bHasPreviousCutProgress = false;
	m_iStartPart = -1;
	m_iStartContour = -1;
	m_iLastPart = m_iLastContour = m_iCurrElement = m_iCurrPart = m_iCurrContour = -1;
	m_elemProgress = 0;
	m_cutDistance = 0;

	Repaint();

	return;
}

void CCAMViewWnd::SetCallbackForTraceMouseCoords(TraceMouseCoords pfn)
{
	m_cbTraceMouseCoords = pfn;
}

void CCAMViewWnd::DisplayPartNo(bool bDisplay)
{
	m_bShowPartNo = bDisplay;
	if (HasContents())
		Repaint();
	SaveSettings(iOPT_PART_NUMBER);
	return;
}

void CCAMViewWnd::DisplayContourNo(bool bDisplay)
{
	m_bShowContourNo = bDisplay;
	if (HasContents())
		Repaint();
	SaveSettings(iOPT_CONTOUR_NUMBER);
	return;
}

void CCAMViewWnd::SetPartNoHeight(int nHeight)
{
	m_nPartNoHeight = nHeight;
	if (HasContents())
		Repaint();
	SaveSettings(iOPT_PARTNO_HEIGHT);
	return;
}

void CCAMViewWnd::SetContourNoHeight(int nHeight)
{
	m_nContourNoHeight = nHeight;
	if (HasContents())
		Repaint();
	SaveSettings(iOPT_CONTOURNO_HEIGHT);
	return;
}

void CCAMViewWnd::LimitPartContourNumbers(int nMaxCount)
{
	const int nNumberLimits = (0 < nMaxCount)? nMaxCount: 0;
	if (nNumberLimits == m_nMaxPartContourNumbers)
		return;

	m_nMaxPartContourNumbers = nNumberLimits;
	if (HasContents())
		Repaint();
	SaveSettings(iOPT_LIMIT_PART_CONT_NO);
	return;
}

void CCAMViewWnd::DrawPiercingSpot(bool bDraw)
{
	m_bShowPiercing = bDraw;
	if (HasContents())
		Repaint();
	SaveSettings(iOPT_DRAW_PIERCING);
	return;
}

void CCAMViewWnd::EnsureCuttingSpotVisible(bool bVisible)
{
	m_bEnsureSpotVisible = bVisible;
	SaveSettings(iOPT_TRACE_CUTTINGSPOT);
}

void CCAMViewWnd::UpdateColor(DrawingColor index, COLORREF color)
{
	int nOpt = -1;
	switch (index)
	{
	case enColorBackground:		nOpt = iOPT_COLOR_BACKGROUND;		m_colors[index] = color;	break;
	case enColorPiercing:		nOpt = iOPT_COLOR_PIERCING;			m_colors[index] = color;	break;
	case enColorLeadIn:			nOpt = iOPT_COLOR_LEADIN;			m_colors[index] = color;	break;
	case enColorContour:		nOpt = iOPT_COLOR_CONTOUR;			m_colors[index] = color;	break;
	case enColorEngraving:		nOpt = iOPT_COLOR_MARKING;			m_colors[index] = color;	break;
	case enColorCutProgress:	nOpt = iOPT_COLOR_CUTTING;			m_colors[index] = color;	break;
	case enColorCanvas:			nOpt = iOPT_COLOR_WORKPIECE;		m_colors[index] = color;	break;
	case enColorPartNo:			nOpt = iOPT_COLOR_PART_NUMBER;		m_colors[index] = color;	break;
	case enColorContourNo:		nOpt = iOPT_COLOR_CONTOUR_NUMBER;	m_colors[index] = color;	break;
	default:		return;		break;
	}

	if (enColorCanvas == index || enColorPartNo == index || enColorContourNo == index)
		m_colors[index].SetAlpha(0.5f);

	if (HasContents())
	{
		CreateGLlists();
		if (m_bHasCuttingProgress)
		{
			camContour& contour = m_camLayout._part[m_iCurrPart].pContour[m_iCurrContour];
			DrawProgress(contour, m_iCurrElement, m_elemProgress);
		}
		Repaint();
	}

	SaveSettings(nOpt);
	return;
}

void CCAMViewWnd::GetWholeSize(double& width, double& height)
{
	if (HasContents())
	{
		width = m_camLayout._width;
		height = m_camLayout._height;
	}
}

BOOL CCAMViewWnd::Zoom(bool bEnlarge)
{
	if (HasContents())
	{
		double scale = m_viewScale*(bEnlarge? 1.1: 0.9);
		if (c_maxScale >= scale)
		{
			CPoint point(m_cxViewport/2, m_cyViewport/2);
			return ZoomContents(point, scale);
		}
	}
	return FALSE;
}

BOOL CCAMViewWnd::ActualSize()
{
	if (HasContents())
	{
		CPoint point(m_cxViewport/2, m_cyViewport/2);
		return ZoomContents(point, 1.0);
	}
	return FALSE;
}

BOOL CCAMViewWnd::CanZoom(bool bEnlarge) const
{
	if (bEnlarge)
		return (HasContents() && c_maxScale > m_viewScale);
	else
		return HasContents();
}

BOOL CCAMViewWnd::StartCuttingProgress(int nPart, int nContour, bool bReverse/* =false */)
{
// << CuttingProgress Debugging
	writeLog(_T("StartCuttingProgress(%d, %d, %d)\n"), nPart, nContour, bReverse? 1:0);
// >> CuttingProgress Debugging

	if (!HasContents())
	{
		appendLog(_T("No CAM data.\n"));
		ASSERT(FALSE);
		return FALSE;
	}

	bool bHasCuttingProgress = HasCuttingProgress();

	m_IsReverse = bReverse;
	m_cutDistance = 0;

	int iPart = nPart - 1, iContour = nContour - 1;
	if (iPart >= m_camLayout._num_parts || nContour > m_camLayout._part[iPart].num_contours)
	{	// Stop showing cutting progress
		m_bHasCuttingProgress = false;
		m_bUnderCuttingProgress = false;
		m_bHasPreviousCutProgress = false;
		m_iStartPart = -1;
		m_iStartContour = -1;
		ResetCut();
		if (bHasCuttingProgress)
			Repaint();
// << CuttingProgress Debugging
		appendLog(_T("Part-contour out-of-range => Resetting\n"));
// >> CuttingProgress Debugging
		return FALSE;
	}

	m_iStartPart = iPart;
	m_iStartContour = iContour;
	m_iLastPart = m_iStartPart - 1;
	m_iLastContour = m_iStartContour - 1;
	m_iCurrElement = 0;
	m_iCurrPart = iPart;
	m_iCurrContour = iContour;
	ResetCut();
	m_bHasCuttingProgress = false;

	m_bUnderCuttingProgress = true;

	DrawPreviousCutElements();

	if (bHasCuttingProgress)
		Repaint();

// << CuttingProgress Debugging
	appendLog(_T("m_iStartPart = %d\n")
				_T("m_iStartContour = %d\n")
				_T("m_iLastPart = %d\n")
				_T("m_iLastContour = %d\n")
				_T("m_iCurrPart = %d\n")
				_T("m_iCurrContour = %d\n")
				_T("m_iCurrElement = %d\n"),
				m_iStartPart, m_iStartContour, m_iLastPart, m_iLastContour,
				m_iCurrPart, m_iCurrContour, m_iCurrElement);
// >> CuttingProgress Debugging

	return TRUE;
}

BOOL CCAMViewWnd::UpdateCuttingProgress(int nPart, int nContour, const char* currentBlock, double progress, double xwcs, double ywcs, bool isBlockInMpf)
{
// << CuttingProgress Debugging
	appendLog(_T("UpdateCuttingProgress(nPart#%d, nContour#%d, currentBlock[%s]\n")
			  _T("                      prog=%.2f, wcsX=%.3f, wcsY=%.3f, isGcode=%d\n"),
			  nPart, nContour, CString(currentBlock), progress, xwcs, ywcs, isBlockInMpf? 1: 0);
// >> CuttingProgress Debugging

	if (!m_bUnderCuttingProgress)
	{
		ASSERT(FALSE);
		return FALSE;
	}
	int iPart = nPart - 1;
	int iContour = nContour - 1;

	if (0 > iPart || iPart >= m_camLayout._num_parts
		|| 0 > iContour || iContour >= m_camLayout._part[iPart].num_contours)
	{
		ASSERT(FALSE);
		return FALSE;
	}

	UpdateCuttingIndices(iPart, iContour);

	if (progress > 0.99)
		progress = 1;

	Point2d pos(xwcs, ywcs);
	CStringA block(currentBlock);
	if (UpdateProgress(iPart, iContour, block, progress, pos, isBlockInMpf))
	{
		UpdateProgressView(pos);
		return TRUE;
	}

	return FALSE;
}

BOOL CCAMViewWnd::UpdateCuttingProgress(int nPart, int nContour, int mpfLineNo, double progress, double xwcs, double ywcs)
{
	// << CuttingProgress Debugging
	appendLog(_T("UpdateCuttingProgress(nPart#%d, nContour#%d, mpfLineNo[%d]\n")
			  _T("                      prog=%.2f, wcsX=%.3f, wcsY=%.3f\n"),
			  nPart, nContour, mpfLineNo, progress, xwcs, ywcs);
	// >> CuttingProgress Debugging

	if (!m_bUnderCuttingProgress)
	{
		ASSERT(FALSE);
		return FALSE;
	}
	int iPart = nPart - 1;
	int iContour = nContour - 1;
	if (0 > iPart || iPart >= m_camLayout._num_parts
		|| 0 > iContour || iContour >= m_camLayout._part[iPart].num_contours)
	{
		ASSERT(FALSE);
		return FALSE;
	}

	UpdateCuttingIndices(iPart, iContour);

	if (progress > 0.99)
		progress = 1;

	if (UpdateProgress(iPart, iContour, mpfLineNo, progress, xwcs, ywcs))
	{
		UpdateProgressView(Point2d(xwcs, ywcs));
		return TRUE;
	}

	return FALSE;
}

BOOL CCAMViewWnd::UpdateCuttingProgress(int nPart, int nContour, int iBlock, double xwcs, double ywcs)
{
// << CuttingProgress Debugging
	appendLog(_T("UpdateCuttingProgress(nPart#%d, nContour#%d, iBlock#%d, wcsX=%.3f, wcsY=%.3f\n"),
			  nPart, nContour, iBlock, xwcs, ywcs);
// >> CuttingProgress Debugging

	if (!m_bUnderCuttingProgress)
	{
		ASSERT(FALSE);
		return FALSE;
	}
	int iPart = nPart - 1;
	int iContour = nContour - 1;

	if (0 > iPart || iPart >= m_camLayout._num_parts
		|| 0 > iContour || iContour >= m_camLayout._part[iPart].num_contours
		|| 0 > iBlock || iBlock >= m_camLayout._part[iPart].pContour[iContour]._numElements)
	{
		ASSERT(FALSE);
		return FALSE;
	}

	UpdateCuttingIndices(iPart, iContour);

	Point2d pos(xwcs, ywcs);
	if (UpdateProgress(iPart, iContour, iBlock, pos))
	{
		UpdateProgressView(pos);
		return TRUE;
	}

	return FALSE;
}

int CCAMViewWnd::GetContourLength(int part, int contour, double& length, bool isReset)
{
	if (!HasContents())
		return CVERR_NOCONTENTS;

	--part, --contour;
	if (0 > part || part >= m_camLayout._num_parts
		|| 0 > contour || contour >= m_camLayout._part[part].num_contours)
	{
		ASSERT(FALSE);
		return CVERR_INVALID_PART_NO;
	}

	camPart& p = m_camLayout._part[part];
	camContour& c = p.pContour[contour];

	if (isReset)
	{
		if (!m_bHasCuttingProgress)
		{
			ASSERT(FALSE);
			return CVERR_INVALID_ARGS;
		}
		length = c.GetLength(m_iCurrElement);
	}
	else
		length = c.GetLength();

	return CV_NOERROR;
}

int CCAMViewWnd::GetPartCount(int& partCount)
{
	if (!HasContents())
		return CVERR_NOCONTENTS;

	partCount = m_camLayout._num_parts;
	return CV_NOERROR;
}

int CCAMViewWnd::GetContourCount(int part, int& contourCount)
{
	if (!HasContents())
		return CVERR_NOCONTENTS;

	--part;
	if (0 > part || part >= m_camLayout._num_parts)
		return CVERR_INVALID_PART_NO;

	contourCount = m_camLayout._part[part].num_contours;
	return CV_NOERROR;
}

int CCAMViewWnd::GetElementCount(int part, int contour, int& elementCount)
{
	if (!HasContents())
		return CVERR_NOCONTENTS;

	--part, --contour;
	if (0 > part || part >= m_camLayout._num_parts
		|| 0 > contour || contour >= m_camLayout._part[part].num_contours)
		return CVERR_INVALID_PART_NO;

	elementCount = m_camLayout._part[part].pContour[contour]._numElements;
	return CV_NOERROR;
}

int CCAMViewWnd::GetElementBlockCode(int part, int contour, int element, char* pszBlock, int maxbuffer)
{
	if (!HasContents())
		return CVERR_NOCONTENTS;

	--part, --contour;
	if (0 > part || part >= m_camLayout._num_parts
		|| 0 > contour || contour >= m_camLayout._part[part].num_contours
		|| 0 > element || element >= m_camLayout._part[part].pContour[contour]._numElements)
		return CVERR_INVALID_PART_NO;

	if (m_camLayout._part[part].pContour[contour]._pElement[element]._sBlock.GetLength() > 0)
	{
		strncpy_s(pszBlock, maxbuffer,
				  m_camLayout._part[part].pContour[contour]._pElement[element]._sBlock.GetBuffer(),
				  m_camLayout._part[part].pContour[contour]._pElement[element]._sBlock.GetLength());
	}
	return CV_NOERROR;
}

int CCAMViewWnd::GetScanCut(int& scancut)
{
	if (!HasContents())
		return CVERR_NOCONTENTS;
	scancut = m_camLayout._isScancutIncluded ? 1 : 0;
	return CV_NOERROR;
}

void CCAMViewWnd::StopCuttingProgress()
{
	m_bUnderCuttingProgress = false;
	return;
}


void CCAMViewWnd::FinishCuttingProgress()
{
// 	if (!m_bHasCuttingProgress)
// 	{
// 		ASSERT(FALSE);
// 		return;
// 	}
// 
// 	camPart& p = m_camLayout._part[(m_bReverseTrace? 0: m_camLayout._num_parts-1)];
// 
// 	int iContour = p.num_contours-1;
// 	camContour& c = p.pContour[iContour];
// 	m_iCurrElement = c.num_elements-1;
// 	camElement& elem = c.pElem[m_iCurrElement];

	CompleteLastElement();
	return;
}

void CCAMViewWnd::CompleteLastElement()
{
	if (!m_bHasCuttingProgress)
	{
		ASSERT(FALSE);
		return;
	}

	if (!HasContents())
		return;

	if (m_camLayout._num_parts <= m_iCurrPart)
		return;

	if (m_camLayout._part[m_iCurrPart].num_contours <= m_iCurrContour)
		return;

	camContour& cLast = m_camLayout._part[m_iCurrPart].pContour[m_iCurrContour];
	if (cLast._numElements <= m_iCurrElement)
		return;

	double progress = 1;

	SetToDrawMissingProgress(m_iCurrPart, m_iCurrContour, m_iCurrElement);	// update drawing for missed parts due to excessively fast cutting speed

	DrawProgress(cLast, m_iCurrElement, progress);

	m_cutDistance = CalcCutDistance();

	Repaint();
}

void CCAMViewWnd::CompleteLastContour()
{
	if (!m_bHasCuttingProgress)
	{
		ASSERT(FALSE);
		return;
	}

	if (!HasContents())
		return;

	if (m_camLayout._num_parts <= m_iCurrPart)
		return;

	if (m_camLayout._part[m_iCurrPart].num_contours <= m_iCurrContour)
		return;

	camContour& cLast = m_camLayout._part[m_iCurrPart].pContour[m_iCurrContour];
	int lastElement = cLast._numElements - 1;
	m_iCurrElement = lastElement;

	double progress = 1;

	SetToDrawMissingProgress(m_iCurrPart, m_iCurrContour, lastElement);	// update drawing for missed parts due to excessively fast cutting speed

	DrawProgress(cLast, lastElement, progress);

	m_cutDistance = CalcCutDistance();

	Repaint();
}

void CCAMViewWnd::ResetCuttingProress()
{
	if (m_bHasCuttingProgress)
	{
		m_bHasCuttingProgress = false;
		m_bUnderCuttingProgress = false;
		m_bHasPreviousCutProgress = false;
		m_iStartPart = -1;
		m_iStartContour = -1;
		ResetCut();
		Repaint();
	}
}

inline void SetContourProgressDone(camPart& p, int fromContour, int toContour)
{
	if (toContour >= p.num_contours)
		toContour = p.num_contours - 1;

	camContour* c = p.pContour;
	for (int ci = fromContour; ci <= toContour; ++ci)
	{
		c[ci]._bCuttingDone = true;
		camElement* e = c[ci]._pElement;
		for (int ei = 0; ei < c[ci]._numElements; ++ei)
		{
			e[ei]._cutDone = true;
			e[ei]._progress = 0;
		}
	}
}

inline void SetContourProgressDone(camPart& p, int fromContour, int toContour, int toElement, double progress)
{
	if (toContour >= p.num_contours)
		toContour = p.num_contours - 1;

	camContour* c = p.pContour;

	//시작 컨투어부터 마지막 컨투어 1개 전까지 완료 처리
	for (int ci = fromContour; ci <= toContour - 1; ++ci)
	{
		c[ci]._bCuttingDone = true;
		camElement* e = c[ci]._pElement;
		for (int ei = 0; ei < c[ci]._numElements; ++ei)
		{
			e[ei]._cutDone = true;
			e[ei]._progress = 0;
		}
	}

	//마지막 컨투어 완료 처리
	c[toContour]._bCuttingDone = true;
	camElement* e = c[toContour]._pElement;

	if (toElement >= c[toContour]._numElements)
		toElement = c[toContour]._numElements - 1;

	for (int ei = 0; ei <= toElement - 1; ++ei)
	{
		e[ei]._cutDone = true;
		e[ei]._progress = 0;
	}

	if (progress >= 1)
	{
		e[toElement]._cutDone = true;
		e[toElement]._progress = 0;
	}
	else
	{
		e[toElement]._cutDone = false;
		e[toElement]._progress = progress;
	}
}

void CCAMViewWnd::SetCuttingProressDone(int fromPart, int fromContour, int toPart, int toContour, bool isReverse)
{
	if (!HasContents())
		return;

	--fromPart, --fromContour;
	--toPart, --toContour;
	if (0 > fromPart || fromPart >= m_camLayout._num_parts
		|| 0 > fromContour || fromContour >= m_camLayout._part[fromPart].num_contours
		|| 0 > toPart || toPart >= m_camLayout._num_parts
		|| 0 > toContour || toContour >= m_camLayout._part[toPart].num_contours)
	{
		ASSERT(FALSE);
		return;
	}

	camPart* p = m_camLayout._part;
	if (fromPart == toPart)
	{
		SetContourProgressDone(p[fromPart], fromContour, toContour);
	}
	else
	{
		if (isReverse)
		{
			for (int pi = fromPart; pi >= toPart; --pi)
			{
				if (pi == fromPart)
					SetContourProgressDone(p[pi], fromContour, p[pi].num_contours - 1);
				else if (pi == toPart)
					SetContourProgressDone(p[pi], 0, toContour);
				else
					SetContourProgressDone(p[pi], 0, p[pi].num_contours - 1);
			}
		}
		else
		{
			for (int pi = fromPart; pi <= toPart; ++pi)
			{
				if (pi == fromPart)
					SetContourProgressDone(p[pi], fromContour, p[pi].num_contours - 1);
				else if (pi == toPart)
					SetContourProgressDone(p[pi], 0, toContour);
				else
					SetContourProgressDone(p[pi], 0, p[pi].num_contours - 1);
			}
		}
	}

	DrawPreviousCutElements();
}

void CCAMViewWnd::SetCuttingProgressDone(int fromPart, int fromContour, int toPart, int toContour, int toElement, double toProgress)
{
	if (!HasContents())
		return;

	--fromPart, --fromContour;
	--toPart, --toContour;
	--toElement;
	if (0 > fromPart || fromPart >= m_camLayout._num_parts
		|| 0 > fromContour || fromContour >= m_camLayout._part[fromPart].num_contours
		|| 0 > toPart || toPart >= m_camLayout._num_parts
		|| 0 > toContour || toContour >= m_camLayout._part[toPart].num_contours
		|| 0 > toElement || toElement >= m_camLayout._part[toPart].pContour[toContour]._numElements)
	{
		ASSERT(FALSE);
		return;
	}

	camPart* p = m_camLayout._part;
	if (fromPart == toPart)
	{
		SetContourProgressDone(p[fromPart], fromContour, toContour, toElement, toProgress);
	}
	else
	{
		for (int pi = fromPart; pi <= toPart; ++pi)
		{
			if (pi == fromPart)
				SetContourProgressDone(p[pi], fromContour, p[pi].num_contours - 1);
			else if (pi == toPart)
				SetContourProgressDone(p[pi], 0, toContour, toElement, toProgress);
			else
				SetContourProgressDone(p[pi], 0, p[pi].num_contours - 1);
		}
	}

	DrawPreviousCutElements();
}

void CCAMViewWnd::GetCuttingProgressInfo(CutProgress*& pInfo, int& nCount)
{
	if (!HasContents())
		return;

	std::vector<CutProgress> cpis;

	int startPart = 0;
	int startContour = 0;
	int nextCutStartPart = 0;
	int nextCutStartContour = 0;
	int nextCutEndPart = 0;
	int nextCutEndContour = 0;
	int nextCutEndElement = 0;
	double progress = 0;
	while (GetNextCutStartPartContour(startPart, startContour, nextCutStartPart, nextCutStartContour)
		&& GetNextCutEndPartContour(nextCutStartPart, nextCutStartContour, nextCutEndPart, nextCutEndContour, nextCutEndElement, progress))
	{
		CutProgress cpinfo;
		cpinfo.startPart = nextCutStartPart;
		cpinfo.startContour = nextCutStartContour;
		cpinfo.endPart = nextCutEndPart;
		cpinfo.endContour = nextCutEndContour;
		cpinfo.endElement = nextCutEndElement;
		cpinfo.endProgress = progress;
		cpis.push_back(cpinfo);

		nextCutEndElement = 0;
		progress = 0;

		startPart = nextCutEndPart;
		startContour = nextCutEndContour + 1;
	}

	nCount = (int)cpis.size();
	if (nCount <= 0)
		return;

	pInfo = new CutProgress[nCount];
	for (int i = 0; i < nCount; i++)
	{
		pInfo[i] = cpis[i];
	}
	cpis.clear();
}

bool CCAMViewWnd::GetNextCutStartPartContour(int startPart, int startContour, int& part, int& contour)
{
	camPart* p = m_camLayout._part;

	if (startContour >= p[startPart].num_contours)
	{
		startPart++;
		startContour = 0;
	}

	if (startPart >= m_camLayout._num_parts)
		return false;

	for (int pi = startPart; pi < m_camLayout._num_parts; ++pi)
	{
		camContour* c = p[pi].pContour;
		for (int ci = startContour; ci < p[pi].num_contours; ++ci)
		{
			if (c[ci]._bCuttingDone)
			{
				part = pi;
				contour = ci;
				return true;
			}
		}
		startContour = 0;
	}
	return false;
}

bool CCAMViewWnd::GetNextCutEndPartContour(int startPart, int startContour, int& part, int& contour, int& element, double& progress)
{
	camPart* p = m_camLayout._part;

	if (startPart >= m_camLayout._num_parts)
		return false;

	if (startContour >= p[startPart].num_contours)
	{
		startPart++;
		startContour = 0;
	}

	for (int pi = startPart; pi < m_camLayout._num_parts; ++pi)
	{
		camContour* c = p[pi].pContour;
		for (int ci = startContour; ci < p[pi].num_contours; ++ci)
		{
			if (c[ci]._bCuttingDone)
			{
				for (int ei = 0; ei < c[ci]._numElements; ++ei)
				{
					const camElement& e = c[ci]._pElement[ei];
					if (!e._cutDone)
					{
						if (e._progress > 0)
						{
							part = pi;
							contour = ci;
							element = ei;
							progress = e._progress;
							return true;
						}
					}
				}
			}
			else
			{	//The current part and contour is not done, so the previous one is done.
				if (ci <= 0)
				{
					part = pi - 1;
					if (part < 0)
						return false;	//invalid part

					contour = p[part].num_contours - 1;
				}
				else
				{
					part = pi;
					contour = ci - 1;
				}

				element = p[part].pContour[contour]._numElements - 1;
				progress = 1;
				return true;
			}
		}
		startContour = 0;
	}

	//If there is no incomplete area, the entire area is done with cutting.
	part = m_camLayout._num_parts - 1;
	contour = p[part].num_contours - 1;
	if (p[part].pContour)
	{
		element = p[part].pContour[contour]._numElements - 1;
		progress = 1;
	}
	return true;
}

bool CCAMViewWnd::CanDrawNumbers(BOOL& partNumber, BOOL& contourNumber) const
{
	if (0 < m_nMaxPartContourNumbers && m_nMaxPartContourNumbers < m_camLayout._num_parts + m_camLayout._numContoursTotal)
	{
		int count_part_numbers = 0, count_contour_numbers = 0;
		if (partNumber)
		{
			camPart* p = m_camLayout._part;
			for (int i = 0; i < m_camLayout._num_parts; ++i)
			{
				if (m_clipBox.hasPoint(p[i].bound.Center()))
				{
					++count_part_numbers;
				}
			}
		}
		if (m_nMaxPartContourNumbers < count_part_numbers)
			return false;

		if (contourNumber)
		{
			camPart* p = m_camLayout._part;
			for (int i = 0; i < m_camLayout._num_parts; ++i)
			{
				if (!p[i].bound.isIntersect(m_clipBox))
					continue;
				camContour* c = p[i].pContour;
				for (int j = 0; j < p[i].num_contours; ++j)
				{
					if (m_clipBox.hasPoint(c[j]._bound.Center()))
						++count_contour_numbers;
				}
			}
		}

		if (m_nMaxPartContourNumbers < count_part_numbers + count_contour_numbers)
		{
			if (partNumber)
			{
				ASSERT(0 < count_contour_numbers && 0 < count_part_numbers && count_part_numbers <= m_nMaxPartContourNumbers);
				contourNumber = FALSE;
				return true;
			}
			return false;
		}
	}
	return (partNumber || contourNumber);
}


BEGIN_MESSAGE_MAP(CCAMViewWnd, COpenGL2dWnd)
	ON_WM_MOUSEMOVE()
	ON_WM_MOUSEWHEEL()
	ON_WM_LBUTTONDOWN()
	ON_WM_LBUTTONUP()
	ON_WM_LBUTTONDBLCLK()
	ON_WM_SETCURSOR()
	ON_WM_CREATE()
END_MESSAGE_MAP()



int CCAMViewWnd::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
	if (COpenGL2dWnd::OnCreate(lpCreateStruct) == -1)
		return -1;

	CWinApp* pApp = AfxGetApp();

	m_hCursorDrag = pApp->LoadCursor(IDC_HANDDRAG);

	m_bShowPartNo = (1==pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_OPT_PARTNO, 1));
	m_bShowContourNo = (1 == pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_OPT_CONTOURNO, 0));
	m_bShowPiercing = (1==pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_OPT_PIERCING, 1));
	m_bEnsureSpotVisible = (1==pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_SIMUL_TRACE, TRUE));
	m_nPartNoHeight = pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_PARTNO_HEIGHT, 25);
	m_nContourNoHeight = pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CONTNO_HEIGHT, 12);

	m_colors[enColorBackground] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_BKGND, COLOR_BKGND));
	m_colors[enColorCanvas] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_SHEET, COLOR_WORKPIECE));
	m_colors[enColorPartNo] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_PARTNO, COLOR_PARTNO));
	m_colors[enColorContourNo] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_CONTOURNO, COLOR_CONTOURNO));
	m_colors[enColorPiercing] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_PIERCING, COLOR_PIERCING));
	m_colors[enColorLeadIn] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_LEADIN, COLOR_LEADIN));
	m_colors[enColorContour] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_CONTOUR, COLOR_CONTOUR));
	m_colors[enColorEngraving] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_ENGRAVING, COLOR_MARKING));
	m_colors[enColorCutProgress] = COLORREF(pApp->GetProfileInt(STR_SECTION_DRAWING, STR_KEY_CLR_CUTTING, COLOR_CUTTING));
	
	m_colors[enColorCanvas].SetAlpha(0.5f);
	m_colors[enColorPartNo].SetAlpha(0.5f);
	m_colors[enColorContourNo].SetAlpha(0.5f);

	return 0;
}


void CCAMViewWnd::GetCurrDrawingOptions(DrawingOption& option)
{
	option.show_part_number = m_bShowPartNo;
	option.show_cont_number = m_bShowContourNo;
	option.show_piercing = m_bShowPiercing;
	option.show_ensure_visible = m_bEnsureSpotVisible;
	option.pixelheight_part_number = m_nPartNoHeight;
	option.pixelheight_cont_number = m_nContourNoHeight;
	option.color[enColorBackground] = m_colors[enColorBackground].InCOLORREF();
	option.color[enColorCanvas]		= m_colors[enColorCanvas].InCOLORREF();
	option.color[enColorPartNo]		= m_colors[enColorPartNo].InCOLORREF();
	option.color[enColorContourNo]	= m_colors[enColorContourNo].InCOLORREF();
	option.color[enColorPiercing]	= m_colors[enColorPiercing].InCOLORREF();
	option.color[enColorLeadIn]		= m_colors[enColorLeadIn].InCOLORREF();
	option.color[enColorContour]	= m_colors[enColorContour].InCOLORREF();
	option.color[enColorEngraving]	= m_colors[enColorEngraving].InCOLORREF();
	option.color[enColorCutProgress]= m_colors[enColorCutProgress].InCOLORREF();
}


//
// OnSizeChanged() is called in the WM_SIZE message handler, OnSize(cx, cy) of the base class
//
void CCAMViewWnd::OnSizeChanged(int cx, int cy)
{
	ASSERT(0 < cx && 0 < cy && m_hRC);
	m_cxViewport = cx;
	m_cyViewport = cy;

	// compute the aspect ratio
	// this will keep all dimension scales equal
	RecalcViewport();
	OnViewScaleChanged();

	::wglMakeCurrent(m_hDC, m_hRC);

	// select the full client area
	::glViewport(0, 0, cx, cy);

	// select the projection matrix and clear it
	::glMatrixMode(GL_PROJECTION);
	::glLoadIdentity();

	// select the viewing rectangle
	::gluOrtho2D(m_clipBox.x1, m_clipBox.x2, m_clipBox.y1, m_clipBox.y2);

	// switch back to the model view matrix and clear it
	::glMatrixMode(GL_MODELVIEW);
	::glLoadIdentity();

	::wglMakeCurrent(m_hDC, NULL);

	return;
}

//
// OnDoRender() is called in the WM_PAINT message handler, OnPaint() of the base class
//
void CCAMViewWnd::OnDoRender()
{
	::glColor4fv(m_colors[enColorBackground]);
	::glBegin(GL_QUADS);
	::glVertex2d(m_clipBox.x1, m_clipBox.y1);
	::glVertex2d(m_clipBox.x2, m_clipBox.y1);
	::glVertex2d(m_clipBox.x2, m_clipBox.y2);
	::glVertex2d(m_clipBox.x1, m_clipBox.y2);
	::glEnd();

	if (0 < int(m_idListObjects) && 0 < m_countGLLists)
	{
		::glColor4fv(m_colors[enColorCanvas]);
		::glBegin(GL_QUADS);
		::glVertex2d(0, 0);
		::glVertex2d(m_camLayout._width, 0);
		::glVertex2d(m_camLayout._width, m_camLayout._height);
		::glVertex2d(0, m_camLayout._height);
		::glEnd();

		if (m_bShowPiercing)
		{
			::glPointSize(m_spotSize);
			::glCallList(m_idListObjects+iGLLIST_PIERCING);
		}

		::glCallList(m_idListObjects+iGLLIST_CONTOUR);

		if (m_bHasPreviousCutProgress)
		{
			::glCallList(m_idListObjects + iGLIST_PREVIOUS_CUT);
		}

		if (m_bHasCuttingProgress)
		{
			::glCallList(m_idListObjects + iGLIST_CUTTING);
		}

		DoPostRender();
	}

	return;
}

void CCAMViewWnd::DoPostRender()
{
	BOOL drawPartNumber = m_bShowPartNo, drawContourNumber = m_bShowContourNo;
	if (!CanDrawNumbers(drawPartNumber, drawContourNumber))
		return;

	if (drawPartNumber)
	{
		::glColor4fv(m_colors[enColorPartNo]);

		::glEnable(GL_LINE_STIPPLE);
		::glLineStipple(1, 0x9999);
		::glCallList(m_idListObjects+iGLLIST_BOUNDINGS);
		::glDisable(GL_LINE_STIPPLE);

		double scale = m_nPartNoHeight/m_viewScale;
		camPart* p = m_camLayout._part;
		for (int i = 0; i < m_camLayout._num_parts; ++i)
		{
			const Rect2d& b = p[i].bound;
			if (b.isIntersect(m_clipBox))
			{
				Point2d pt = b.Center();
				m_glNumber.Print(pt, UINT(i + 1), scale);
			}
		}
	}

	if (drawContourNumber)
	{
		::glColor4fv(m_colors[enColorContourNo]);

		double scale = m_nContourNoHeight / m_viewScale;
		camPart* p = m_camLayout._part;
		for (int pi = 0; pi < m_camLayout._num_parts; ++pi)
		{
			if (!p[pi].bound.isIntersect(m_clipBox))
				continue;

			camContour* c = p[pi].pContour;
			for (int ci = 0; ci < p[pi].num_contours; ++ci)
			{
				const Rect2d& b = c[ci]._bound;
				if (b.isIntersect(m_clipBox))
				{
					Point2d pt(b.Center().x, b.y2);
					m_glNumber.Print(pt, UINT(ci + 1), scale);
				}
			}
		}
	}
	return;
}

void CCAMViewWnd::UpdateViewport()
{
	::wglMakeCurrent(m_hDC, m_hRC);

	// select the projection matrix and clear it
	::glMatrixMode(GL_PROJECTION);
	::glLoadIdentity();

	// select the viewing rectangle
	::gluOrtho2D(m_clipBox.x1, m_clipBox.x2, m_clipBox.y1, m_clipBox.y2);

	// switch back to the model view matrix and clear it
	::glMatrixMode(GL_MODELVIEW);
	::glLoadIdentity();

	::wglMakeCurrent(m_hDC, NULL);

	Repaint();

	return;
}


void CCAMViewWnd::UpdateProgressView(const Point2d& point)
{
	if (m_bEnsureSpotVisible && !m_clipBox.hasPoint(point))
	{
		double dx(0), dy(0);
		double space = maxof(m_clipBox.width(), m_clipBox.height())*0.1;
		if (point.x < m_clipBox.x1)
			dx = point.x - m_clipBox.x1 - space;
		else if (m_clipBox.x2 < point.x)
			dx = point.x - m_clipBox.x2 + space;
		if (point.y < m_clipBox.y1)
			dy = point.y - m_clipBox.y1 - space;
		else if (m_clipBox.y2 < point.y)
			dy = point.y - m_clipBox.y2 + space;
		m_clipBox.Translate(dx, dy);
		UpdateViewport();
	}
	else
	{
		Repaint();
	}
}



//////////////////////////////////////////////////////////////////////////
// CCAMViewWnd message handlers


void CCAMViewWnd::OnMouseMove(UINT nFlags, CPoint point)
{
	// TODO: Add your message handler code here and/or call default
	if (HasContents())
	{
		if (m_bLpointDown)
			OnPanningByDrag(point);
		TraceMouse(point);
		return;
	}
	COpenGL2dWnd::OnMouseMove(nFlags, point);
}


BOOL CCAMViewWnd::OnMouseWheel(UINT nFlags, short zDelta, CPoint pt)
{
	// TODO: Add your message handler code here and/or call default
	if (HasContents())
	{
		ScreenToClient(&pt);
		CRect rect(0, 0, m_cxViewport, m_cyViewport);
		if (!rect.PtInRect(pt))
			return FALSE;
		OnZoomOnTheSpot(zDelta/WHEEL_DELTA, pt);
		return TRUE;
	}
	return COpenGL2dWnd::OnMouseWheel(nFlags, zDelta, pt);
}


void CCAMViewWnd::OnLButtonDown(UINT nFlags, CPoint point)
{
	// TODO: Add your message handler code here and/or call default
	if (HasContents())
	{
		SetFocus();
		CRect rect(0, 0, m_cxViewport, m_cyViewport);
		ClientToScreen(rect);
		::ClipCursor(rect);
		m_anchorPoint = point;
		m_bLpointDown = true;
		return;
	}
	COpenGL2dWnd::OnLButtonDown(nFlags, point);
}


void CCAMViewWnd::OnLButtonUp(UINT nFlags, CPoint point)
{
	// TODO: Add your message handler code here and/or call default
	if (m_bLpointDown)
	{
		::ClipCursor(NULL);
		m_bLpointDown = false;
		m_anchorPoint = CPoint(-1, -1);
		return;
	}
	COpenGL2dWnd::OnLButtonUp(nFlags, point);
}


void CCAMViewWnd::OnLButtonDblClk(UINT nFlags, CPoint point)
{
	// TODO: Add your message handler code here and/or call default
	if (HasContents())
	{
		FitDrawingToWindow();
		return;
	}
	COpenGL2dWnd::OnLButtonDblClk(nFlags, point);
}


BOOL CCAMViewWnd::OnSetCursor(CWnd* pWnd, UINT nHitTest, UINT message)
{
	// TODO: Add your message handler code here and/or call default
	if (HasContents())
	{
		::SetCursor(m_bLpointDown? m_hCursorDrag: m_hCursorCross);
		return TRUE;
	}
	return COpenGL2dWnd::OnSetCursor(pWnd, nHitTest, message);
}




//////////////////////////////////////////////////////////////////////////
// CCAMViewWnd implementation

BOOL CCAMViewWnd::CreateGLlists()
{
	if (!HasContents())
	{
		ASSERT(FALSE);
		return FALSE;
	}
	DeleteDrawingObjects();

	::wglMakeCurrent(m_hDC, m_hRC);

	if (!m_glNumber.IsValid())
		m_glNumber.Create(_T("Arial"), -21, true);

	m_countGLLists = nGLLIST_COUNT;	// Piercing, Contour, Part bounding boxes, Cutting progress
	m_idListObjects = ::glGenLists(m_countGLLists);
	if (!m_idListObjects)
	{
		OpenGLListErrorHandler();
		::wglMakeCurrent(m_hDC, NULL);
		return FALSE;
	}

	CreatePiercingList(m_idListObjects + iGLLIST_PIERCING);
	CreateContourList(m_idListObjects + iGLLIST_CONTOUR);
	CreatePartBoundList(m_idListObjects + iGLLIST_BOUNDINGS);

	::wglMakeCurrent(m_hDC, NULL);

	return TRUE;
}

void CCAMViewWnd::CreatePiercingList(UINT idList)
{
	::glNewList(idList, GL_COMPILE);

	::glColor4fv(m_colors[enColorPiercing]);

	::glBegin(GL_POINTS);
	camPart* p = m_camLayout._part;
	for (int i = 0; i < m_camLayout._num_parts; ++i)
	{
		camPart& part = p[i];
		if (0 >= part.num_contours)
		{
			ASSERT(FALSE);
			continue;
		}
		for (int j = 0; j < part.num_contours; ++j)
		{
			camContour& c = part.pContour[j];
			if (c._fPiercing)
			{
				glVertex2d(c._xo, c._yo);
			}
		}
	}
	::glEnd();
	::glEndList();

	return;
}

void CCAMViewWnd::CreateContourList(UINT idList)
{
	::glNewList(idList, GL_COMPILE);

	camPart* p = m_camLayout._part;
	Color4f& clr_leadin = m_colors[enColorLeadIn];

	for (int i = 0; i < m_camLayout._num_parts; ++i)
	{
		camPart& part = p[i];
		if (0 >= part.num_contours)
		{
			ASSERT(FALSE);
			continue;
		}

		double xo, yo;
		for (int j = 0, index; j < part.num_contours; ++j)
		{
			camContour& c = part.pContour[j];

			if (c._bHasLeadIn && c._pElement[0].IsLine())
			{
				xo = c._pElement[0]._x2, yo = c._pElement[0]._y2;

				// lead-in
				::glColor4fv(clr_leadin);
				::glBegin(GL_LINES);
				::glVertex2d(c._xo, c._yo);
				::glVertex2d(xo, yo);
				::glEnd();

				index = 1;
			}
			else
			{
				xo = c._xo, yo = c._yo;
				index = 0;
			}
			::glColor4fv(c._fMarking? m_colors[enColorEngraving]: m_colors[enColorContour]);

			::glBegin(GL_LINE_STRIP);
			::glVertex2d(xo, yo);
			for (int k = index; k < c._numElements; ++k)
			{
				const camElement& e = c._pElement[k];
				if (e.IsArc())
					e._pArc->glDraw(e._length, m_viewScale);
				::glVertex2d(e._x2, e._y2);
			}
			::glEnd();
		}
	}
	::glEndList();

	return;
}


void CCAMViewWnd::CreatePartBoundList(UINT idList)
{
	::glNewList(idList, GL_COMPILE);

	camPart* p = m_camLayout._part;
	for (int i = 0; i < m_camLayout._num_parts; ++i)
	{
		const Rect2d& b = p[i].bound;
		::glBegin(GL_LINE_LOOP);
		::glVertex2d(b.x1, b.y1);
		::glVertex2d(b.x2, b.y1);
		::glVertex2d(b.x2, b.y2);
		::glVertex2d(b.x1, b.y2);
		::glEnd();
	}

	::glEndList();
}


void CCAMViewWnd::SetupViewport()
{
	if (0 >= m_cxViewport || 0 >= m_cyViewport || NULL == m_hRC
		|| 0 >= m_camLayout._width || 0 >= m_camLayout._height)
	{
		m_xFrom = m_yFrom = m_xTo = m_yTo = 0;
		return;
	}
	RecalcZoomScale();
	return;
}


void CCAMViewWnd::RecalcZoomScale()
{
	double x_ratio = m_cxViewport/(m_camLayout._width*1.03);
	double y_ratio = m_cyViewport/(m_camLayout._height*1.03);
	double oldScale = m_viewScale;

	if (x_ratio >= y_ratio)
		m_viewScale = y_ratio;
	else
		m_viewScale = x_ratio;

	m_spotSize = (1 > m_viewScale)? 1.0f: 3.0f;

	double view_w(m_cxViewport/m_viewScale), view_h(m_cyViewport/m_viewScale);
	ASSERT(view_w >= m_camLayout._width && view_h >= m_camLayout._height);

	m_clipBox.x1 = 0.5*(m_camLayout._width-view_w);
	m_clipBox.y1 = 0.5*(m_camLayout._height-view_h);
	m_clipBox.x2 = m_clipBox.x1 + view_w;
	m_clipBox.y2 = m_clipBox.y1 + view_h;

	return;
}


void CCAMViewWnd::RecalcViewport()
{
	if (0 >= m_cxViewport || 0 >= m_cyViewport || 0 >= m_camLayout._width || 0 >= m_camLayout._height)
		return;

	double view_w(m_cxViewport/m_viewScale), view_h(m_cyViewport/m_viewScale);

	if (view_w > m_camLayout._width && view_h > m_camLayout._height)
	{
		double x_ratio = m_cxViewport/(m_camLayout._width*1.03), y_ratio = m_cyViewport/(m_camLayout._height*1.03);
		double scale = (x_ratio >= y_ratio)? y_ratio: x_ratio;
		if (scale > m_viewScale)
		{
			RecalcZoomScale();
			return;
		}
	}

	if (view_w >= m_camLayout._width)
		m_clipBox.x1 = 0.5*(m_camLayout._width-view_w);
	m_clipBox.x2 = m_clipBox.x1 + view_w;

	if (view_h >= m_camLayout._height)
		m_clipBox.y2 = 0.5*(view_h-m_camLayout._height) + m_camLayout._height;
	m_clipBox.y1 = m_clipBox.y2 - view_h;

	return;
}


void CCAMViewWnd::FitDrawingToWindow()
{
	RecalcZoomScale();
	UpdateViewport();
	Repaint();
	return;
}


void CCAMViewWnd::OnZoomOnTheSpot(int nRepeat, CPoint point)
{
	if (0 == nRepeat || !HasContents())
		return;

	// Determine the draw scales in the viewport
	double factor = (0 < nRepeat)? pow(1.1, nRepeat): pow(0.9, -nRepeat);
	double scale = m_viewScale*factor;
	if (c_maxScale < scale)
		return;

	ZoomContents(point, scale);

	return;
}


BOOL CCAMViewWnd::ZoomContents(const CPoint& center, double scale)
{
	Point2d spot;
	if (!DPtoLP(center, spot))
		return FALSE;

	double factor = 1.0/scale;

	m_clipBox.x1 = spot.x - center.x*factor;
	m_clipBox.x2 = m_clipBox.x1 + m_cxViewport*factor;
	m_clipBox.y2 = spot.y + center.y*factor;
	m_clipBox.y1 = m_clipBox.y2 - m_cyViewport*factor;

	m_viewScale = scale;
	m_spotSize = (1 > m_viewScale)? 1.0f: 3.0f;

	OnViewScaleChanged();
	UpdateViewport();

	return TRUE;
}


void CCAMViewWnd::OnPanningByDrag(CPoint point)
{
	if (!HasContents() || IsZero(m_viewScale)
		|| 0 > m_anchorPoint.x || 0 > m_anchorPoint.y)
		return;

	int dx = m_anchorPoint.x - point.x;
	int dy = point.y - m_anchorPoint.y;
	if (0 == dx && 0 == dy)
		return;

	m_anchorPoint = point;

	double tdx = dx/m_viewScale, tdy = dy/m_viewScale;
	m_clipBox.Translate(tdx, tdy);
	UpdateViewport();

	return;
}


void CCAMViewWnd::TraceMouse(CPoint point)
{
	Point2d spot;
	if (DPtoLP(point, spot))
	{
		if (m_cbTraceMouseCoords)
			m_cbTraceMouseCoords(spot.x, spot.y);

		CWnd* pWnd = AfxGetMainWnd();
		if (pWnd && pWnd->m_hWnd)
		{
			double pos[2] ={ spot.x, spot.y };
			::SendMessage(pWnd->m_hWnd, WNM_CAMVIEW_MOUSEMOVE, WPARAM(m_hWnd), LPARAM(pos));
		}
	}
	return;
}


void CCAMViewWnd::OnViewScaleChanged()
{
 	CWnd* pWnd = AfxGetMainWnd();
 	if (pWnd && pWnd->m_hWnd)
 		::SendMessage(pWnd->m_hWnd, WNM_CAMVIEW_SCALECHANGED, WPARAM(m_hWnd), LPARAM(&m_viewScale));
	return;
}


void CCAMViewWnd::UpdateCuttingIndices(int iPart, int iContour)
{
	if (m_iLastPart != iPart)
	{
		m_iLastPart = iPart;
		m_iLastContour = iContour;
		m_iCurrElement = 0;
		m_elemProgress = 0;
	}
	else if (m_iLastContour != iContour)
	{
		m_iLastContour = iContour;
		m_iCurrElement = 0;
		m_elemProgress = 0;
	}
// << CuttingProgress Debugging
	appendLog(  _T("    UpdateCuttingIndices(iPart=%d, iContour=%d)\n")
				_T("                        m_iStartPart = %d\n")
				_T("                        m_iStartContour = %d\n")
				_T("                        m_iLastPart = %d\n")
				_T("                        m_iLastContour = %d\n")
				_T("                        m_iCurrPart = %d\n")
				_T("                        m_iCurrContour = %d\n")
				_T("                        m_iCurrElement = %d\n"),
				iPart, iContour,
				m_iStartPart, m_iStartContour, m_iLastPart, m_iLastContour,
				m_iCurrPart, m_iCurrContour, m_iCurrElement);
// >> CuttingProgress Debugging
}

BOOL CCAMViewWnd::UpdateProgress(int iCurrPart, int iCurrContour, CStringA& block, double progress, Point2d& pos, bool isGcodeBlock)
{
// << CuttingProgress Debugging
	appendLog(_T("    UpdateProgress(iPart=%d, iContour=%d, pos=%.3f,%.3f, isBlock=0)\n"),
							iCurrPart, iCurrContour, pos.x, pos.y);
// >> CuttingProgress Debugging

	//
	// First, check the current block is valid for tracing
	//
	camContour* cLast = m_camLayout._part[iCurrPart].pContour + iCurrContour;
	int iLastElement = -1;
	
	if (isGcodeBlock && !block.IsEmpty())
	{
		iLastElement = cLast->GetElement(m_iCurrElement, block);
		if (0 > iLastElement)
		{	//The currently cutting element was not found. Then, find it in the remnant cut contours
// << CuttingProgress Debugging
			appendLog(_T("        Can't find the element has the block_string\n"));
			GetRenmantCutInfo(cLast, iCurrPart, iCurrContour, iLastElement, block);
// >> CuttingProgress Debugging
		}
	}
	if (0 > iLastElement)
	{
		iLastElement = cLast->GetElement(m_iCurrElement, pos.x, pos.y, progress);	// update progress based only on the current position
	}

	if (0 > iLastElement || (iLastElement == m_iCurrElement && progress < m_elemProgress))
	{
// << CuttingProgress Debugging
		appendLog(_T("        %s\n"), (0 > iLastElement)?
				  _T("Can't locate wcsX, wcsY on any G-code blocks"):
				  _T("No need to update - redundant progress report"));
		return FALSE;
// >> CuttingProgress Debugging
	}

	//
	// Before proceeding, set the flag for valid progress status
	//
	if (!m_bHasCuttingProgress)
		m_bHasCuttingProgress = (m_iStartPart < iCurrPart || 0 < iCurrContour || 0 <= iLastElement);
	ASSERT(m_bHasCuttingProgress);

	//
	// Draw cutting progress
	//
	SetToDrawMissingProgress(iCurrPart, iCurrContour, iLastElement);	// update drawing for missed parts due to excessively fast cutting speed

	DrawProgress(*cLast, iLastElement, progress);

	m_cutDistance = CalcCutDistance();

	m_iCurrPart = iCurrPart;
	m_iCurrContour = iCurrContour;
	m_iCurrElement = iLastElement;
	m_elemProgress = progress;

// << CuttingProgress Debugging
	appendLog(_T("    UpdateProgress() done.\n")
			  _T("      m_iCurrPart    = %d\n")
			  _T("      m_iCurrContour = %d\n")
			  _T("      m_iCurrElement = %d\n")
			  _T("      m_progress     = %.2f\n"),
			  m_iCurrPart, m_iCurrContour, m_iCurrElement, m_elemProgress);
// >> CuttingProgress Debugging

	return TRUE;
}

BOOL CCAMViewWnd::UpdateProgress(int iCurrPart, int iCurrContour, int nLineNo, double progress, double Xwcs, double Ywcs)
{
	return FALSE;
}

BOOL CCAMViewWnd::UpdateProgress(int iCurrPart, int iCurrContour, int iBlock, Point2d& pos)
{
	//
	// First, check the current block is valid for tracing
	//
	camContour& cLast = m_camLayout._part[iCurrPart].pContour[iCurrContour];

	double progress = 0;
	if (!cLast.GetProgress(iBlock, pos, progress))
	{
		return FALSE;
	}

	//
	// Before proceeding, set the flag for valid progress status
	//
	if (!m_bHasCuttingProgress)
		m_bHasCuttingProgress = (m_iStartPart < iCurrPart || 0 < iCurrContour || 0 <= iBlock);
	ASSERT(m_bHasCuttingProgress);

	//
	// Draw cutting progress
	//
	SetToDrawMissingProgress(iCurrPart, iCurrContour, iBlock);	// update drawing for missed parts due to excessively fast cutting speed

	DrawProgress(cLast, iBlock, progress);

	m_cutDistance = CalcCutDistance();

	m_iCurrPart = iCurrPart;
	m_iCurrContour = iCurrContour;
	m_iCurrElement = iBlock;
	m_elemProgress = progress;

	return TRUE;
}

bool CCAMViewWnd::GetRenmantCutInfo(camContour*& contour, int& iPart, int& iContour, int& iElement, const CStringA& block)
{
	int count = m_camLayout._vRenmantCutContours.size();
	for (int ci = 0; ci < count; ++ci)
	{
		camContour* c = m_camLayout._vRenmantCutContours[ci];
		for (int ei = 0; ei < c->_numElements; ++ei)
		{
			if (c->_pElement[ei]._sBlock.Find(block) != -1)
			{
				contour = c;
				iPart = c->_iPart;
				iContour = c->_iContour;
				iElement = ei;
				return true;
			}
		}
	}
	return false;
}

void CCAMViewWnd::SetToDrawMissingProgress(int iCurrPart, int iCurrContour, int iLastElement)
{
	if (!HasContents())
		return;

// << CuttingProgress Debugging
	appendLog(_T("        SetToDrawMissingProgress(iCurrPart=%d, iCurrContour=%d, iLastElement=%d\n"),
													iCurrPart, iCurrContour, iLastElement);
// >> CuttingProgress Debugging

	if (m_iCurrPart < 0 || m_iCurrPart >= m_camLayout._num_parts
		|| iCurrPart < 0 || iCurrPart >= m_camLayout._num_parts)
	{
// << CuttingProgress Debugging
		appendLog(_T("            Part number out of range[0, %d): m_iCurrPart=%d, iCurrPart=%d\n"),
				  m_camLayout._num_parts, m_iCurrPart, iCurrPart);
// >> CuttingProgress Debugging
		ASSERT(FALSE);
		return;
	}

	if (m_iCurrContour < 0 || m_iCurrContour >= m_camLayout._part[m_iCurrPart].num_contours
		|| iCurrContour < 0 || iCurrContour >= m_camLayout._part[iCurrPart].num_contours)
	{
// << CuttingProgress Debugging
		appendLog(_T("            Contour number out of range\n")
				  _T("               Part[m_iCurrPart=%d] range[0, %d): m_iCurrContour=%d\n")
				  _T("               Part[iCurrPart=%d] range[0, %d): iCurrContour=%d\n"),
								m_iCurrPart, m_camLayout._part[m_iCurrPart].num_contours, m_iCurrContour,
								iCurrPart, m_camLayout._part[iCurrPart].num_contours, iCurrContour);
// >> CuttingProgress Debugging
		ASSERT(FALSE);
		return;
	}

	if (m_iCurrElement < 0 || m_iCurrElement >= m_camLayout._part[m_iCurrPart].pContour[m_iCurrContour]._numElements
		|| iLastElement < 0 || iLastElement >= m_camLayout._part[iCurrPart].pContour[iCurrContour]._numElements)
	{
// << CuttingProgress Debugging
		appendLog(_T("            G-code element number out of range\n")
				  _T("               Contour[m_iCurrContour=%d] range[0, %d): m_iCurrElement=%d\n")
				  _T("               Contour[iCurrContour=%d] range[0, %d): iLastElement=%d\n"),
				  m_iCurrContour, m_camLayout._part[m_iCurrPart].pContour[m_iCurrContour]._numElements, m_iCurrElement,
				  iCurrContour, m_camLayout._part[iCurrPart].pContour[iCurrContour]._numElements, iLastElement);
// >> CuttingProgress Debugging
		ASSERT(FALSE);
		return;
	}

	// if progressing part is changed turn all the previous parts to completed ones
	if (m_iCurrPart != iCurrPart)
	{
		if (m_IsReverse)
		{
			for (int iPart = m_iCurrPart; iCurrPart < iPart; --iPart)
			{
				camPart& p = m_camLayout._part[iPart];
				for (int ci = 0; ci < p.num_contours; ci++)
				{
					camContour& contour = p.pContour[ci];
					if (!contour._bCuttingDone)
						contour._bCuttingDone = true;

					for (int ei = 0; ei < contour._numElements; ei++)
					{
						camElement& element = contour._pElement[ei];
						element._cutDone = true;
					}
				}
			}
		}
		else
		{
			for (int iPart = m_iCurrPart; iPart < iCurrPart; ++iPart)
			{
				camPart& p = m_camLayout._part[iPart];
				for (int ci = 0; ci < p.num_contours; ci++)
				{
					camContour& contour = p.pContour[ci];
					if (!contour._bCuttingDone)
						contour._bCuttingDone = true;

					for (int ei = 0; ei < contour._numElements; ei++)
					{
						camElement& element = contour._pElement[ei];
						element._cutDone = true;
					}
				}
			}
		}
// << CuttingProgress Debugging
		appendLog(_T("            Part progress: %d -> %d\n"), m_iCurrPart, iCurrPart);
		appendLog(_T("                 m_iCurrContour set to zero\n"));
// >> CuttingProgress Debugging

		m_iCurrContour = 0;
	}

	// if progressing contour is changed turn all the previous contours to completed ones
	if (m_iCurrContour != iCurrContour)
	{
		camPart& p = m_camLayout._part[iCurrPart];
		for (int ci = m_iCurrContour; ci < iCurrContour; ci++)
		{
			camContour& contour = p.pContour[ci];
			if (!contour._bCuttingDone)
				contour._bCuttingDone = true;

			for (int ei = 0; ei < contour._numElements; ei++)
			{
				camElement& element = contour._pElement[ei];
				element._cutDone = true;
			}
		}
// << CuttingProgress Debugging
		appendLog(_T("            Contour progress: %d -> %d\n"), m_iCurrContour, iCurrContour);
		appendLog(_T("                 m_iCurrElement set to zero\n"));
// >> CuttingProgress Debugging
		m_iCurrElement = 0;
	}

	// if progressing element is changed turn all the previous elements to completed ones
	if (m_iCurrElement != iLastElement)
	{
		camPart& p = m_camLayout._part[iCurrPart];
		for (int ci = m_iCurrContour; ci <= iCurrContour; ci++)
		{
			camContour& contour = p.pContour[ci];
			if (!contour._bCuttingDone)
				contour._bCuttingDone = true;

			for (int ei = 0; ei < iLastElement; ei++)
			{
				camElement& element = contour._pElement[ei];
				element._cutDone = true;
			}
		}
// << CuttingProgress Debugging
		appendLog(_T("            G-code element progress: %d -> %d\n"), m_iCurrElement, iLastElement);
// >> CuttingProgress Debugging
	}
}


void CCAMViewWnd::DrawProgress(camContour& cLast, int& iLastElement, double& progress)
{
// << CuttingProgress Debugging
	appendLog(_T("        DrawProgress(iLastElement=%d, progress=%.2f)\n"), iLastElement, progress);
// >> CuttingProgress Debugging

	::glDeleteLists(m_idListObjects + iGLIST_CUTTING, 1);

	::wglMakeCurrent(m_hDC, m_hRC);

	::glNewList(m_idListObjects + iGLIST_CUTTING, GL_COMPILE);
	::glColor4fv(m_colors[enColorCutProgress]);

	// The last contour
	drawGLContour(cLast, iLastElement, progress, m_viewScale);

	::glEndList();

	::wglMakeCurrent(m_hDC, NULL);

	DrawPreviousCutElements();

// << CuttingProgress Debugging
	appendLog(_T("        After DrawProgress(), iLastElement=%d, progress=%.2f\n"), iLastElement, progress);
// >> CuttingProgress Debugging
}


void CCAMViewWnd::DrawPreviousCutElements()
{
	if (!HasContents())
		return;

	::glDeleteLists(m_idListObjects + iGLIST_PREVIOUS_CUT, 1);

	::wglMakeCurrent(m_hDC, m_hRC);

	::glNewList(m_idListObjects + iGLIST_PREVIOUS_CUT, GL_COMPILE);
	::glColor4fv(m_colors[enColorCutProgress]);

	camPart* p = m_camLayout._part;
	for (int pi = 0; pi < m_camLayout._num_parts; ++pi)
	{
		camContour* c = p[pi].pContour;
		for (int ci = 0; ci < p[pi].num_contours; ++ci)
		{
			if (c[ci]._bCuttingDone)
			{
				glBegin(GL_LINE_STRIP);

				glVertex2d(c[ci]._xo, c[ci]._yo);
				for (int i = 0; i < c[ci]._numElements; ++i)
				{
					const camElement& e = c[ci]._pElement[i];
					if (e._cutDone)
					{
						if (e.IsArc())
							e._pArc->glDraw(e._length, m_viewScale);
						glVertex2d(e._x2, e._y2);
					}
					else
					{
						if (e._progress > 0)
						{
							if (e.IsArc())
							{
								e._pArc->glDraw(e._length, m_viewScale, e._progress);
							}
							else
							{
								double q = 1.0 - e._progress;
								glVertex2d(q*e._x1 + e._progress*e._x2, q*e._y1 + e._progress*e._y2);
							}
						}
					}
				}
				glEnd();
			}
		}
	}

	::glEndList();

	::wglMakeCurrent(m_hDC, NULL);

	m_bHasPreviousCutProgress = true;
}


void CCAMViewWnd::ResetCut()
{
	if (HasContents())
	{
		camPart* p = m_camLayout._part;

		for (int pi = 0; pi < m_camLayout._num_parts; ++pi)
		{
			camContour* c = p[pi].pContour;
			for (int ci = 0; ci < p[pi].num_contours; ++ci)
			{
				c[ci]._bCuttingDone = false;
				camElement* e = c[ci]._pElement;
				for (int ei = 0; ei < c[ci]._numElements; ++ei)
				{
					e[ei]._cutDone = false;
					e[ei]._progress = 0;
				}
			}
		}
	}
	return;
}

double CCAMViewWnd::CalcCutDistance()
{
	double cutDistance = 0;

	if (HasContents())
	{
		if (m_iStartPart < 0)	m_iStartPart = 0;
		if (m_iStartContour < 0)	m_iStartContour = 0;

		camPart* p = m_camLayout._part;

		if (m_IsReverse)
		{
			for (int pi = m_iStartPart; pi >= 0; --pi)
			{
				camContour* c = p[pi].pContour;
				if (pi == m_iStartPart)
				{
					for (int ci = m_iStartContour; ci < p[pi].num_contours; ++ci)
					{
						if (c[ci]._bCuttingDone)
						{
							camElement* e = c[ci]._pElement;
							for (int ei = 0; ei < c[ci]._numElements; ++ei)
							{
								if (e[ei]._cutDone)
									cutDistance += e[ei]._length;
								else if(0 < e[ei]._progress)
								{
									cutDistance += e[ei]._length * e[ei]._progress;
									break;
								}
							}
						}
					}
				}
				else
				{
					for (int ci = 0; ci < p[pi].num_contours; ++ci)
					{
						if (c[ci]._bCuttingDone)
						{
							camElement* e = c[ci]._pElement;
							for (int ei = 0; ei < c[ci]._numElements; ++ei)
							{
								if (e[ei]._cutDone)
									cutDistance += e[ei]._length;
								else if (0 < e[ei]._progress)
								{
									cutDistance += e[ei]._length * e[ei]._progress;
									break;
								}
							}
						}
					}
				}
			}
		}
		else
		{
			for (int pi = m_iStartPart; pi < m_camLayout._num_parts; ++pi)
			{
				camContour* c = p[pi].pContour;
				if (pi == m_iStartPart)
				{
					for (int ci = m_iStartContour; ci < p[pi].num_contours; ++ci)
					{
						if (c[ci]._bCuttingDone)
						{
							camElement* e = c[ci]._pElement;
							for (int ei = 0; ei < c[ci]._numElements; ++ei)
							{
								if (e[ei]._cutDone)
									cutDistance += e[ei]._length;
								else if (0 < e[ei]._progress)
								{
									cutDistance += e[ei]._length * e[ei]._progress;
									break;
								}
							}
						}
					}
				}
				else
				{
					for (int ci = 0; ci < p[pi].num_contours; ++ci)
					{
						if (c[ci]._bCuttingDone)
						{
							camElement* e = c[ci]._pElement;
							for (int ei = 0; ei < c[ci]._numElements; ++ei)
							{
								if (e[ei]._cutDone)
									cutDistance += e[ei]._length;
								else if (0 < e[ei]._progress)
								{
									cutDistance += e[ei]._length * e[ei]._progress;
									break;
								}
							}
						}
					}
				}
			}
		}
	}
	return cutDistance;
}