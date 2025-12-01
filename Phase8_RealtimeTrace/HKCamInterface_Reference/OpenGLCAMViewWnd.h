#pragma once

#include "MPFInterface.h"
#include "OpenGL2dWnd.h"
#include "CAMViewData.h"
#include "OpenGLNumber.h"
#include "HKCAMInterfaceDLL.h"

//////////////////////////////////////////////////////////////////////////

inline COLORREF MakeRGBColor(byte R, byte G, byte B)
{
	return (((unsigned int)R)|(((unsigned int)G)<<8)|(((unsigned int)B)<<16));
}

inline COLORREF MakeRGBColor(GLclampf r, GLclampf g, GLclampf b)
{
	return MakeRGBColor(BYTE(255*r), BYTE(255*g), BYTE(255*b));
}

const float c_255 = float(1.0/255.0);

struct Color4f
{
	GLclampf red, green, blue, alpha;

	Color4f() : red(0), green(0), blue(0), alpha(1) {}
	Color4f(COLORREF c) : red(c_255*GetRValue(c)), green(c_255*GetGValue(c)), blue(c_255*GetBValue(c)), alpha(1) {}
	Color4f(GLclampf r, GLclampf g, GLclampf b, GLclampf a) : red(r), green(g), blue(b), alpha(a) {}
	void SetAlpha(GLclampf a) { alpha = a; }
	COLORREF InCOLORREF() const { return MakeRGBColor(red, green, blue); }
	operator float*() { return (float*)this; }
	const Color4f& operator=(COLORREF c)
	{
		red = c_255*GetRValue(c);
		green = c_255*GetGValue(c);
		blue = c_255*GetBValue(c);
		return *this;
	}
	const Color4f& operator=(const Color4f& o)
	{
		if (this != &o)
		{
			red = o.red, green = o.green, blue = o.blue, alpha = o.alpha;
		}
		return *this;
	}
};


//////////////////////////////////////////////////////////////////////////
// CCAMViewWnd

class CCAMViewWnd : public COpenGL2dWnd
{
	DECLARE_DYNAMIC(CCAMViewWnd)

public:
	CCAMViewWnd();
	virtual ~CCAMViewWnd();

// Operation
public:
	BOOL UpdateCAMData(const CAM_DATA& data);
	void DeleteContents();
	void SetCallbackForTraceMouseCoords(TraceMouseCoords pfn);

	void DisplayPartNo(bool bDisplay);
	void DisplayContourNo(bool bDisplay);
	void SetPartNoHeight(int nHeight);
	void SetContourNoHeight(int nHeight);
	void LimitPartContourNumbers(int nMaxCount);
	void DrawPiercingSpot(bool bDraw);
	void EnsureCuttingSpotVisible(bool bVisible);
	void UpdateColor(DrawingColor index, COLORREF color);
	void GetWholeSize(double& width, double& height);

	BOOL Zoom(bool bEnlarge);
	BOOL ActualSize();
	BOOL CanZoom(bool bEnlarge) const;
	void FitDrawingToWindow();

	double GetZoomScale() const;

	BOOL HasContents() const;
	bool IsPartNoEnabled() const { return m_bShowPartNo; }
	bool IsContourNoEnabled() const { return m_bShowContourNo; }
	bool IsPiercingDisplayed() const { return m_bShowPiercing; }
	bool IsCuttingSpotTracking() const { return m_bEnsureSpotVisible; }
	int  GetPartNoHeight() const { return m_nPartNoHeight; }
	int  GetContourNoHeight() const { return m_nContourNoHeight; }

	COLORREF GetColor(DrawingColor iColor) const { return m_colors[iColor].InCOLORREF(); }

	BOOL StartCuttingProgress(int nPart, int nContour, bool bReverse=false);
	BOOL UpdateCuttingProgress(int nPart, int nContour, const char* currentBlock, double progress, double xwcs, double ywcs, bool isBlockInMpf);
	BOOL UpdateCuttingProgress(int nPart, int nContour, int mpfLineNo, double progress, double xwcs, double ywcs);
	BOOL UpdateCuttingProgress(int nPart, int nContour, int iBlock, double xwcs, double ywcs);
	int GetContourLength(int part, int contour, double& length, bool isReset);
	int GetPartCount(int& partCount);
	int GetContourCount(int part, int& contourCount);
	int GetElementCount(int part, int contour, int& elementCount);
	int GetElementBlockCode(int part, int contour, int element, char* pszBlock, int maxbuffer);
	int GetScanCut(int& scancut);
	void StopCuttingProgress();
	void ResetCuttingProress();
	void SetCuttingProressDone(int fromPart, int fromContour, int toPart, int toContour, bool isReverse);
	void SetCuttingProgressDone(int fromPart, int fromContour, int toPart, int toContour, int toElement, double toProgress);
	BOOL IsUnderCuttingProgress() const;
	void FinishCuttingProgress();
	void CompleteLastElement();
	void CompleteLastContour();

	void GetCuttingProgressInfo(CutProgress*& pInfo, int& nCount);
	void GetCurrCutDistance(double& distance) const { distance = m_cutDistance; }

// static interfaces
	static bool SetLogFolder(LPCTSTR pszFolder);

// Attributes
	const camLayout& GetCAMLayoutInfo() const;
	static void GetDrawingOptions(DrawingOption& option);
	void GetCurrDrawingOptions(DrawingOption& option);

// Implementation
protected:
	// Drawing attributes 1
	Color4f	m_colors[OPT_NUM_COLORS];	// Color attribute of piercing, contour, number, and backgrounds
	bool	m_bShowPartNo;			// Part number drawing flag
	bool	m_bShowContourNo;		// Contour number drawing flag
	bool	m_bShowPiercing;		// Piercing spot drawing flag
	bool	m_bEnsureSpotVisible;	// Make sure the current cutting simulation spot visible during the simulation
	int		m_nPartNoHeight;		// Part number font height in pixels
	int		m_nContourNoHeight;		// Contour number font height in pixels
	int		m_nMaxPartContourNumbers;// Maximum number of parts plus contours for showing their index numbers
	// Drawing attributes 2
	Rect2d		m_clipBox;				// OpenGL clipping area
	CGLNumber	m_glNumber;				// Part number drawing class
	float		m_spotSize;				// Piercing spot size
	bool		m_bUnderCuttingProgress;// Simulation is in progress
	bool		m_bHasCuttingProgress;	// Has simulation drawing contents
	bool		m_bHasPreviousCutProgress;	// Has the previous simulation drawing contents
	bool		m_bLpointDown;			// Mouse left button status flag
	CPoint		m_anchorPoint;			// The most recent position of the mouse pointer
	HCURSOR		m_hCursorCross, m_hCursorDrag;	// cursors

	TraceMouseCoords	m_cbTraceMouseCoords;

	// CAM data properties
	camLayout	m_camLayout;	// CAM data buffer
	double		m_viewScale;	// Drawing scale

	// OpenGL list variables
	UINT	m_idListObjects;	// start index of the lists
	int		m_countGLLists;		// the number of lists
	//UINT	m_idListCutting;	// index of the cutting progress list
	//UINT	m_idListPreviousCut;	//index of the previous cut list

	// Simulation properties
	int	m_iStartPart, m_iStartContour;	// simulation starting position
	int m_iLastPart, m_iLastContour;	// the last simulation position
	int m_iCurrElement;					// the current element under cutting
	int m_iCurrPart;
	int m_iCurrContour;
	double m_elemProgress;				// cutting progress of the current element
	
	bool m_IsReverse;
	double m_cutDistance;				//the distance cut to now
	double CalcCutDistance();

	void SaveSettings(int nOption);

	void DeleteDrawingObjects();
	BOOL CreateGLlists();
	void CreatePiercingList(UINT idList);
	void CreateContourList(UINT idList);
	void CreatePartBoundList(UINT idList);
	void RecalcViewport();
	void RecalcZoomScale();
	void OnZoomOnTheSpot(int nRepeat, CPoint point);
	BOOL ZoomContents(const CPoint& center, double scale);
	void OnPanningByDrag(CPoint point);
	void TraceMouse(CPoint point);
	void OnViewScaleChanged();

	BOOL DPtoLP(const CPoint& point, Point2d& spot) const;

	void OnDoRender();
	void DoPostRender();
	void SetupViewport();
	virtual void OnSizeChanged(int cx, int cy);
	void UpdateViewport();

	bool HasCuttingProgress() const;
	void UpdateCuttingIndices(int iPart, int iContour);
	BOOL UpdateProgress(int iCurrPart, int iCurrContour, CStringA& block, double progress, Point2d& pos, bool isGcodeBlock);
	BOOL UpdateProgress(int iCurrPart, int iCurrContour, int iBlock, Point2d& pos);
	BOOL UpdateProgress(int iCurrPart, int iCurrContour, int nLineNo, double progress, double Xwcs, double Ywcs);
	void DrawProgress(camContour& cLast, int& iLastElement, double& progress);
	bool GetRenmantCutInfo(camContour*& contour, int& iCurrPart, int& iCurrContour, int& iLastElement, const CStringA& block);
	void SetToDrawMissingProgress(int iCurrPart, int iCurrContour, int iLastElement);

	void DrawPreviousCutElements();
	void ResetCut();

	void UpdateProgressView(const Point2d& point);
	bool GetNextCutStartPartContour(int startPart, int startContour, int& part, int& contour);
	bool GetNextCutEndPartContour(int startPart, int startContour, int& part, int& contour, int& element, double& progress);

	bool CanDrawNumbers(BOOL& partNumber, BOOL& contourNumber) const;

	static CString s_logFolder;

	static bool isLogEnabled();
	static void writeLog(LPCTSTR pszFormat, ...);
	static void appendLog(LPCTSTR pszFormat, ...);

protected:
	DECLARE_MESSAGE_MAP()

public:
	afx_msg void OnMouseMove(UINT nFlags, CPoint point);
	afx_msg BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint pt);
	afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
	afx_msg void OnLButtonUp(UINT nFlags, CPoint point);
	afx_msg void OnLButtonDblClk(UINT nFlags, CPoint point);
	afx_msg BOOL OnSetCursor(CWnd* pWnd, UINT nHitTest, UINT message);
	afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
};


//////////////////////////////////////////////////////////////////////////

inline BOOL CCAMViewWnd::HasContents() const { return (true==m_camLayout.HasContents()); }
inline BOOL CCAMViewWnd::DPtoLP(const CPoint& point, Point2d& spot) const
{
	if (0 < m_cxViewport && 0 < m_cyViewport)
	{
		spot.x = ((m_cxViewport-point.x)*m_clipBox.x1+point.x*m_clipBox.x2)/m_cxViewport;
		spot.y = ((m_cyViewport-point.y)*m_clipBox.y2+point.y*m_clipBox.y1)/m_cyViewport;
		return TRUE;
	}
	return FALSE;
}
inline BOOL CCAMViewWnd::IsUnderCuttingProgress() const { return m_bUnderCuttingProgress; }
inline bool CCAMViewWnd::HasCuttingProgress() const { return m_bHasCuttingProgress; }
inline const camLayout& CCAMViewWnd::GetCAMLayoutInfo() const { return m_camLayout; }
inline double CCAMViewWnd::GetZoomScale() const { return m_viewScale; }
