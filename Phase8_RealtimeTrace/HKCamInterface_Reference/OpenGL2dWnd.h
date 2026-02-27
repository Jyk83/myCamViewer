#pragma once

/////////////////////////////////////////////////////////////////////////////
// Prerequisites for OpenGL functionalities
#include "gl\gl.h"
#include "gl\glu.h"
#pragma comment( lib, "opengl32" )
#pragma comment( lib, "glu32" )
// End of prerequisites
/////////////////////////////////////////////////////////////////////////////


// COpenGL2dWnd

class COpenGL2dWnd : public CWnd
{
	DECLARE_DYNAMIC(COpenGL2dWnd)

public:
	COpenGL2dWnd();
	virtual ~COpenGL2dWnd();

	LPCTSTR GetOpenGLVersion() const;

// Operation
public:
	virtual BOOL Create(const RECT& rect, CWnd* pParentWnd, UINT nID);
	void SetClippingRect(double x1, double x2, double y1, double y2);	// (x1 < x2 and y1 < y2)
	void SetBkgndColor(GLclampf r, GLclampf g, GLclampf b, GLclampf a=1.0f);
	void EnableAntialiasing(bool bEnable=true);
	virtual void Repaint();

// implementation
protected:
	// device/rendering contexts
	HGLRC  m_hRC;	// OpenGL's Rendering Context
	HDC    m_hDC;	// Window's Device Context
	bool   m_bDoubleBuffered;	// double buffering flag
	// variables used in determining the background color
	GLfloat m_red, m_green, m_blue, m_alpha;

	// OpenGL version information
	CString m_strOpenGLVersion;

	// Viewing rectangular region
	int m_cxViewport, m_cyViewport;		// device viewport position and size
	double m_xFrom, m_xTo, m_yFrom, m_yTo;	// clipping region

	// OpenGL implementation
	BOOL InitOpenGLContext();
	BOOL SetupPixelFormat();
	void SetupDefaultScene();

	// Drawing functions
	virtual void OnSizeChanged(int cx, int cy);
	virtual void OnDoRender() = 0;

protected:
	DECLARE_MESSAGE_MAP()
public:
	afx_msg int OnCreate(LPCREATESTRUCT lpCreateStruct);
	afx_msg void OnDestroy();
	afx_msg BOOL OnEraseBkgnd(CDC* pDC);
	afx_msg void OnPaint();
	afx_msg void OnSize(UINT nType, int cx, int cy);
};

inline void COpenGL2dWnd::SetBkgndColor(GLclampf r, GLclampf g, GLclampf b, GLclampf a) { m_red=r, m_green=g, m_blue=b, m_alpha=a; }
inline void COpenGL2dWnd::Repaint() { if (m_hWnd) { Invalidate(0); UpdateWindow(); } }
inline LPCTSTR COpenGL2dWnd::GetOpenGLVersion() const { return m_strOpenGLVersion; }
