#include "stdafx.h"
#include "OpenGLNumber.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif // _DEBUG

#define byteFIRST_DIGIT	48

//////////////////////////////////////////////////////////////////////////

CGLNumber::CGLNumber() : m_hOpenGLRC(0), m_hGdiDC(0), m_uiBase(0)
{
}

CGLNumber::~CGLNumber()
{
	Delete();
}

bool CGLNumber::Create(LPCTSTR pszFaceName, int nHeight, bool bIsBold/* =false */, bool bIsItalic/* =false */)
{
	if (NULL == pszFaceName)
	{
		ASSERT(FALSE);
		return false;
	}
	m_hGdiDC = ::wglGetCurrentDC();
	m_hOpenGLRC = ::wglGetCurrentContext();
	if (!m_hGdiDC || !m_hOpenGLRC)
	{
		ASSERT(FALSE);	// Drawing contexts are not ready yet. Get them ready first
		return false;
	}
	Delete();
	ASSERT(NULL == m_font.m_hObject);

	m_uiBase = ::glGenLists(1);
	if (!m_uiBase)
	{
		ASSERT(FALSE);
		return false;
	}

	LOGFONT lf ={ 0 };
	lf.lfHeight = nHeight;
	lf.lfWeight = bIsBold? 700: 400;
	lf.lfItalic = bIsItalic? 1: 0;
 	lf.lfQuality = ANTIALIASED_QUALITY;
	_tcscpy_s(lf.lfFaceName, pszFaceName);
	if (!m_font.CreateFontIndirect(&lf))
	{
		ASSERT(FALSE);
		Delete();
		return false;
	}

	CDC* pDC = CDC::FromHandle(m_hGdiDC);
	CFont* pDefFont = pDC->SelectObject(&m_font);
	BOOL bOk = ::wglUseFontOutlines(m_hGdiDC, byteFIRST_DIGIT, 10, m_uiBase, 0.0f, 0.2f, WGL_FONT_POLYGONS, m_GMF);
	pDC->SelectObject(pDefFont);

	if (!bOk)
 	{
		ASSERT(FALSE);
		Delete();
		return false;
	}

	return true;
}


void CGLNumber::Delete()
{
	if (m_uiBase)
	{
		::glDeleteLists(m_uiBase, 10);
		m_uiBase =0;
	}
	if (m_font.m_hObject)
		m_font.DeleteObject();

	return;
}


bool CGLNumber::Print(const Point2d& pos, UINT number, double scale)
{
	if (0 == m_uiBase)
		return false;

	char szNumber[32];	// 64bit max integer is 9,223,372,036,854,775,807, which is 19-digit long
	sprintf_s(szNumber, sizeof(szNumber)-1, "%u", number);
	int cblen = strlen(szNumber);

	::glPushMatrix();

	::glTranslated(pos.x, pos.y, 0);
	::glScaled(scale, scale, 1);

	::glListBase(m_uiBase - byteFIRST_DIGIT);
	::glCallLists(cblen, GL_UNSIGNED_BYTE, szNumber);
	
	::glPopMatrix();

	return true;
}
