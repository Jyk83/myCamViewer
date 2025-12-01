// OpenGL2dWnd.cpp : implementation file
//

#include "stdafx.h"
#include "OpenGL2dWnd.h"


#ifdef _DEBUG
#define new DEBUG_NEW
#endif // _DEBUG


// COpenGL2dWnd

IMPLEMENT_DYNAMIC(COpenGL2dWnd, CWnd)

COpenGL2dWnd::COpenGL2dWnd() : m_hRC(0), m_hDC(0),
	m_red(0), m_green(0), m_blue(0), m_alpha(1)
{
	// double buffering
	m_bDoubleBuffered = true;
	// viewport & clipping region control variables
	m_cxViewport = m_cyViewport = 0;
	m_xFrom = m_yFrom = 0;
	m_xTo = m_yTo = 200;
}

COpenGL2dWnd::~COpenGL2dWnd()
{
}


BEGIN_MESSAGE_MAP(COpenGL2dWnd, CWnd)
	ON_WM_CREATE()
	ON_WM_DESTROY()
	ON_WM_ERASEBKGND()
	ON_WM_PAINT()
	ON_WM_SIZE()
END_MESSAGE_MAP()


BOOL COpenGL2dWnd::InitOpenGLContext()
{
	if (!::IsWindow(m_hWnd))
	{
		ASSERT(FALSE);
		return FALSE;
	}

	m_hDC = ::GetDC(m_hWnd);					// obtains a Device Context to the client area.
	if (m_hDC && SetupPixelFormat())			// then, sets up the pixel format of the DC
	{
		m_hRC = ::wglCreateContext(m_hDC);		// creates a rendering context from the DC obtained
		if (m_hRC)
		{
			if (::wglMakeCurrent(m_hDC, m_hRC))	// makes connection between the current RC and DC
			{
				// Get version information
				const GLubyte* psz_version = ::glGetString(GL_VERSION);
				GLenum res = ::glGetError();
				m_strOpenGLVersion = (NOERROR == res)? psz_version: NULL;

				SetupDefaultScene();			// and then, sets up the default attributes of the OpenGL
				::wglMakeCurrent(m_hDC, NULL);	// finally, detach the connection
				return TRUE;
			}
		}
		MessageBox(_T("Error in initializing the OpenGL context."));
	}
	return FALSE;
}

BOOL COpenGL2dWnd::SetupPixelFormat()
{
	// Sets up the initial condition of pixel format we want to have.
	PIXELFORMATDESCRIPTOR pfd =
	{
		sizeof(PIXELFORMATDESCRIPTOR),  // size of this pfd
		1,                              // version number
		PFD_DRAW_TO_WINDOW              // support window
		| PFD_SUPPORT_OPENGL            // support OpenGL
		| PFD_DOUBLEBUFFER,             // double buffered
		PFD_TYPE_RGBA,                  // RGBA type
		24,                             // 24-bit color depth
		0, 0, 0, 0, 0, 0,               // color bits ignored
		0,                              // no alpha buffer
		0,                              // shift bit ignored
		0,                              // no accumulation buffer
		0, 0, 0, 0,                     // accumulation bits ignored
		32,                             // 32-bit z-buffer
		0,                              // no stencil buffer
		0,                              // no auxiliary buffer
		PFD_MAIN_PLANE,                 // main layer
		0,                              // reserved
		0, 0, 0                         // layer masks ignored
	};

	// Once the pixel format has been set we try to get the closest pixel format
	// available by calling the 'ChoosePixelFormat()' function.
	size_t size  = sizeof(PIXELFORMATDESCRIPTOR);
	int nPixelFormat = ::ChoosePixelFormat(m_hDC, &pfd);
	if (0 == nPixelFormat)
	{// then we give it a try to get the current system default pixel format.
		nPixelFormat = 1;
		if (0 == ::DescribePixelFormat(m_hDC, nPixelFormat, size, &pfd))
		{
			MessageBox(_T("Can't get the pixel format description."));
			return FALSE;
		}
	}
	int nPixelIndices = ::DescribePixelFormat(m_hDC, nPixelFormat, size, &pfd);
	if (0 < nPixelIndices)
	{
		if (PFD_NEED_PALETTE & pfd.dwFlags || PFD_TYPE_COLORINDEX & pfd.iPixelType)
		{
			MessageBox(_T("Palette construction is needed for the current device context."));
		}
		m_bDoubleBuffered = (0 != (PFD_DOUBLEBUFFER & pfd.dwFlags));
	}

	// Once we obtain the closest match, also check to ensure that it worked properly.
	if (!::SetPixelFormat(m_hDC, nPixelFormat, &pfd))
	{
		MessageBox(_T("Error in setting the DC pixel format. "));
		return FALSE;
	}

	// If everything has worked properly the function returns true.
	return TRUE;
}

void COpenGL2dWnd::SetupDefaultScene()
{
	// Initialize the background color
	::glClearColor(m_red, m_green, m_blue, m_alpha);

	// The 'glClearDepth()' function specifies the depth value used by 'glClear()'
	// to clear the depth buffer. Values specified by glClearDepth are also clamped to
	// the range [0,1]. Here the current depth value has been set to 1.
	::glClearDepth(1);

	// Set default polygon mode as line drawing mode.
	::glPolygonMode(GL_FRONT, GL_FILL);
	::glPolygonMode(GL_BACK, GL_FILL);

	// Line smoothing (anti aliasing)
	EnableAntialiasing();

	return;
}

void COpenGL2dWnd::EnableAntialiasing(bool bEnable/* =true */)
{
	if (bEnable)
	{
		::glEnable(GL_LINE_SMOOTH);
		::glEnable(GL_BLEND);
		::glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
		::glHint(GL_LINE_SMOOTH_HINT, GL_NICEST);
	}
	else
	{
		::glDisable(GL_LINE_SMOOTH);
		::glDisable(GL_BLEND);
	}
}

void COpenGL2dWnd::OnSizeChanged(int cx, int cy)
{
	ASSERT(0 < cx && 0 < cy && m_hRC);

	// First, makes our OpenGL rendering context the calling thread's
	// current rendering context
	::wglMakeCurrent(m_hDC, m_hRC);

	//
	// The 'glViewport()' function sets the viewport.
	// Compute the aspect ratio, select the projection matrix and clear it.
	// This means that all further commands will affect the projection matrix.
	//
	// select the full client area
	::glViewport(0, 0, cx, cy);
	// compute the aspect ratio
	// this will keep all dimension scales equal
	m_cxViewport = cx;
	m_cyViewport = cy;

	// select the projection matrix and clear it
	::glMatrixMode(GL_PROJECTION);
	::glLoadIdentity();

	//
	// Set the Viewing Volume.
	// Select the Model view matrix and initialize it.
	// This means that all further commands will affect the Model view matrix.
	//
	// select the viewing rectangle
	::gluOrtho2D(m_xFrom, m_xTo+0.001F*m_xTo, m_yFrom, m_yTo+0.001F*m_yTo);

	// switch back to the model view matrix and clear it
	::glMatrixMode(GL_MODELVIEW);
	::glLoadIdentity();
	// changes the calling thread's current rendering context
	// so it's no longer current
	::wglMakeCurrent(m_hDC, NULL);
}

void COpenGL2dWnd::SetClippingRect(double x1, double x2, double y1, double y2)
{
	m_xFrom = x1, m_xTo = x2;
	m_yFrom = y1, m_yTo = y2;
	OnSizeChanged(m_cxViewport, m_cyViewport);
	Repaint();
}



// COpenGL2dWnd message handlers


BOOL COpenGL2dWnd::Create(const RECT& rect, CWnd* pParentWnd, UINT nID)
{
	DWORD wndStyle = WS_VISIBLE | WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS;
	DWORD clsStyle = CS_HREDRAW | CS_VREDRAW | CS_OWNDC;
	DWORD dwStyle = wndStyle | clsStyle;
	return CWnd::Create(NULL, NULL, dwStyle, rect, pParentWnd, nID);
}


int COpenGL2dWnd::OnCreate(LPCREATESTRUCT lpCreateStruct)
{
	if (CWnd::OnCreate(lpCreateStruct) == -1)
		return -1;

	InitOpenGLContext();

	return 0;
}


void COpenGL2dWnd::OnDestroy()
{
	if (m_hRC)
	{
		BOOL bDetached = ::wglMakeCurrent(m_hDC, NULL);
		if (!bDetached)
		{
			DWORD errorCode = ::GetLastError();
			TRACE1("wglMakeCurrent() error, error code = %u\n", errorCode);
		}
		::wglDeleteContext(m_hRC);
		::ReleaseDC(m_hWnd, m_hDC);
	}

	CWnd::OnDestroy();
}


BOOL COpenGL2dWnd::OnEraseBkgnd(CDC* pDC)
{
	return TRUE;
}


void COpenGL2dWnd::OnPaint()
{
	if (!m_hRC)
	{
		CPaintDC dc(this);
		return;
	}

	// First, makes our OpenGL rendering context the calling thread's
	// current rendering context
	::wglMakeCurrent(m_hDC, m_hRC);

	// Clear out the color & depth buffers
	::glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

	//
	// Call the homemade 'RenderScene()' function
	// which performs any rendering operation specific to the application.
	//
	OnDoRender();

	//
	// Swap the contents of the back buffer if double buffering is being used.
	//
	if (m_bDoubleBuffered)
		::SwapBuffers(m_hDC);
	else
		::glFlush();

	// changes the calling thread's current rendering context
	// so it's no longer current
	::wglMakeCurrent(m_hDC, NULL);

	ValidateRect(NULL);	// don't call the default OnPaint function.
}


void COpenGL2dWnd::OnSize(UINT nType, int cx, int cy)
{
	CWnd::OnSize(nType, cx, cy);

	if (m_hRC && 0 < cx && 0 < cy)
		OnSizeChanged(cx, cy);

	return;
}
