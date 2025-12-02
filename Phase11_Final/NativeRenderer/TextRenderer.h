#ifndef TEXTRENDERER_H
#define TEXTRENDERER_H

#include <windows.h>
#include <gl/GL.h>

#ifdef _WIN32
    #ifdef RENDERER_EXPORTS
        #define RENDERER_API __declspec(dllexport)
    #else
        #define RENDERER_API __declspec(dllimport)
    #endif
#else
    #define RENDERER_API
#endif

// TextRenderer class for drawing numbers and text using OpenGL
// Based on HKCamInterface OpenGLNumber implementation
class TextRenderer
{
public:
    TextRenderer();
    ~TextRenderer();

    // Initialize text renderer with font settings
    bool Create(const wchar_t* fontName, int height, bool bold = false, bool italic = false);
    
    // Draw a number at specified position
    // posX, posY: center position in world coordinates
    // number: the number to display
    // scale: scaling factor (default 1.0)
    bool DrawNumber(double posX, double posY, unsigned int number, double scale = 1.0);
    
    // Draw text at specified position
    bool DrawText(double posX, double posY, const char* text, double scale = 1.0);
    
    // Check if renderer is ready
    bool IsValid() const { return m_base != 0; }
    
    // Clean up resources
    void Destroy();

private:
    HGLRC m_hRC;        // OpenGL rendering context
    HDC m_hDC;          // Device context
    GLuint m_base;      // Base display list
    HFONT m_hFont;      // Font handle
    GLYPHMETRICSFLOAT m_glyphMetrics[256];  // Glyph metrics for characters
    
    // Helper functions
    void CreateDisplayLists();
    void DeleteDisplayLists();
};

// C interface for text rendering
#ifdef __cplusplus
extern "C" {
#endif

// Initialize text renderer
RENDERER_API int TextRenderer_Create(const wchar_t* fontName, int height, int bold, int italic);

// Draw number at position
RENDERER_API void TextRenderer_DrawNumber(double posX, double posY, unsigned int number, 
                                          double scale, float r, float g, float b);

// Draw text at position
RENDERER_API void TextRenderer_DrawText(double posX, double posY, const char* text, 
                                        double scale, float r, float g, float b);

// Clean up text renderer
RENDERER_API void TextRenderer_Destroy();

// Check if text renderer is ready
RENDERER_API int TextRenderer_IsValid();

#ifdef __cplusplus
}
#endif

#endif // TEXTRENDERER_H
