#include "TextRenderer.h"
#include <cstdio>
#include <cstring>

// Static global instance for C interface
static TextRenderer* g_textRenderer = nullptr;

//////////////////////////////////////////////////////////////////////////
// TextRenderer Class Implementation
//////////////////////////////////////////////////////////////////////////

TextRenderer::TextRenderer()
    : m_hRC(nullptr)
    , m_hDC(nullptr)
    , m_base(0)
    , m_hFont(nullptr)
{
    memset(m_glyphMetrics, 0, sizeof(m_glyphMetrics));
}

TextRenderer::~TextRenderer()
{
    Destroy();
}

bool TextRenderer::Create(const wchar_t* fontName, int height, bool bold, bool italic)
{
    if (!fontName)
        return false;

    // Get current OpenGL contexts
    m_hDC = wglGetCurrentDC();
    m_hRC = wglGetCurrentContext();
    
    if (!m_hDC || !m_hRC)
        return false;

    // Clean up any existing resources
    Destroy();

    // Generate display list base
    m_base = glGenLists(256);  // 256 characters (0-255)
    if (!m_base)
        return false;

    // Create font
    LOGFONTW lf = { 0 };
    lf.lfHeight = height;
    lf.lfWeight = bold ? FW_BOLD : FW_NORMAL;
    lf.lfItalic = italic ? TRUE : FALSE;
    lf.lfCharSet = DEFAULT_CHARSET;
    lf.lfOutPrecision = OUT_TT_PRECIS;
    lf.lfClipPrecision = CLIP_DEFAULT_PRECIS;
    lf.lfQuality = ANTIALIASED_QUALITY;
    lf.lfPitchAndFamily = FF_DONTCARE | DEFAULT_PITCH;
    wcscpy_s(lf.lfFaceName, fontName);

    m_hFont = CreateFontIndirectW(&lf);
    if (!m_hFont)
    {
        Destroy();
        return false;
    }

    // Select font into DC
    HFONT oldFont = (HFONT)SelectObject(m_hDC, m_hFont);

    // Create display lists for digits (0-9) using outlined fonts
    // Using WGL_FONT_POLYGONS for better quality
    BOOL result = wglUseFontOutlinesW(
        m_hDC,
        0,                      // First character
        256,                    // Number of characters
        m_base,                 // Base of display lists
        0.0f,                   // Deviation (0 = best quality)
        0.2f,                   // Extrusion (thickness)
        WGL_FONT_POLYGONS,      // Format
        m_glyphMetrics          // Glyph metrics
    );

    SelectObject(m_hDC, oldFont);

    if (!result)
    {
        Destroy();
        return false;
    }

    return true;
}

void TextRenderer::Destroy()
{
    if (m_base)
    {
        glDeleteLists(m_base, 256);
        m_base = 0;
    }

    if (m_hFont)
    {
        DeleteObject(m_hFont);
        m_hFont = nullptr;
    }

    memset(m_glyphMetrics, 0, sizeof(m_glyphMetrics));
}

bool TextRenderer::DrawNumber(double posX, double posY, unsigned int number, double scale)
{
    if (!m_base)
        return false;

    // Convert number to string
    char buffer[32];
    sprintf_s(buffer, sizeof(buffer), "%u", number);

    return DrawText(posX, posY, buffer, scale);
}

bool TextRenderer::DrawText(double posX, double posY, const char* text, double scale)
{
    if (!m_base || !text)
        return false;

    int length = (int)strlen(text);
    if (length == 0)
        return true;

    // Calculate text width for centering
    float totalWidth = 0.0f;
    for (int i = 0; i < length; i++)
    {
        unsigned char ch = (unsigned char)text[i];
        totalWidth += m_glyphMetrics[ch].gmfCellIncX;
    }

    // Save current matrix
    glPushMatrix();

    // Translate to position (centered)
    glTranslated(posX - (totalWidth * scale * 0.5), posY, 0.0);
    
    // Apply scale
    glScaled(scale, scale, 1.0);

    // Draw text using display lists
    glPushAttrib(GL_LIST_BIT);
    glListBase(m_base);
    glCallLists(length, GL_UNSIGNED_BYTE, text);
    glPopAttrib();

    // Restore matrix
    glPopMatrix();

    return true;
}

//////////////////////////////////////////////////////////////////////////
// C Interface Implementation
//////////////////////////////////////////////////////////////////////////

RENDERER_API int TextRenderer_Create(const wchar_t* fontName, int height, int bold, int italic)
{
    if (!g_textRenderer)
    {
        g_textRenderer = new TextRenderer();
    }

    bool result = g_textRenderer->Create(fontName, height, bold != 0, italic != 0);
    return result ? 1 : 0;
}

RENDERER_API void TextRenderer_DrawNumber(double posX, double posY, unsigned int number, 
                                          double scale, float r, float g, float b)
{
    if (!g_textRenderer || !g_textRenderer->IsValid())
        return;

    // Set color
    glColor3f(r, g, b);

    // Draw number
    g_textRenderer->DrawNumber(posX, posY, number, scale);
}

RENDERER_API void TextRenderer_DrawText(double posX, double posY, const char* text, 
                                        double scale, float r, float g, float b)
{
    if (!g_textRenderer || !g_textRenderer->IsValid())
        return;

    // Set color
    glColor3f(r, g, b);

    // Draw text
    g_textRenderer->DrawText(posX, posY, text, scale);
}

RENDERER_API void TextRenderer_Destroy()
{
    if (g_textRenderer)
    {
        delete g_textRenderer;
        g_textRenderer = nullptr;
    }
}

RENDERER_API int TextRenderer_IsValid()
{
    return (g_textRenderer && g_textRenderer->IsValid()) ? 1 : 0;
}
