#pragma once

#include "gl/gl.h"
#include "Graph2d.h"

class CGLNumber
{
public:
	CGLNumber();
	~CGLNumber();

	bool IsValid() const;
	bool Create(LPCTSTR pszFaceName, int nHeight, bool bIsBold=false, bool bIsItalic=false);
	bool Print(const Point2d& posCenter, UINT number, double scale=1);
	void Delete();

protected:
	HGLRC	m_hOpenGLRC;
	HDC		m_hGdiDC;
	GLuint	m_uiBase;
	CFont	m_font;
	GLYPHMETRICSFLOAT m_GMF[10];
};

inline bool CGLNumber::IsValid() const { return (0 != m_uiBase); }
