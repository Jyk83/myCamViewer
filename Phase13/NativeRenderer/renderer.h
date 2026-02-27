#ifndef RENDERER_H
#define RENDERER_H

#ifdef _WIN32
    #ifdef RENDERER_EXPORTS
        #define RENDERER_API __declspec(dllexport)
    #else
        #define RENDERER_API __declspec(dllimport)
    #endif
#else
    #define RENDERER_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Renderer initialization and cleanup
RENDERER_API int InitializeRenderer(void* windowHandle);
RENDERER_API void CleanupRenderer();

// Rendering functions
RENDERER_API void ResizeViewport(int width, int height);
RENDERER_API void RenderFrame();

// Shape drawing functions
RENDERER_API void DrawRectangle(float x, float y, float width, float height, float r, float g, float b);
RENDERER_API void DrawCircle(float x, float y, float radius, float r, float g, float b);
RENDERER_API void ClearShapes();

// MPF drawing functions
RENDERER_API void BeginMPFRender();
RENDERER_API void BeginMPFRenderWithBackground(float bgR, float bgG, float bgB);
RENDERER_API void EndMPFRender();
RENDERER_API void SwapBuffersNow(); // Swap buffers after text drawing
RENDERER_API void DrawLine(float x1, float y1, float x2, float y2, float r, float g, float b, float lineWidth);
RENDERER_API void DrawArc(float centerX, float centerY, float radius, float startAngle, float endAngle, 
                           int clockwise, float r, float g, float b, float lineWidth);
RENDERER_API void DrawPoint(float x, float y, float size, float r, float g, float b);
RENDERER_API void DrawFilledRectangle(float x, float y, float width, float height, float r, float g, float b);

// Phase 5.3: Dashed line drawing
// dashPattern: 0=solid, 1=dash1, 2=dash2, 3=dash3, 4=dotdash, 5=dotdotdash
RENDERER_API void DrawDashedRectangle(float x, float y, float width, float height, 
                                       float r, float g, float b, float lineWidth, int dashPattern);

// Camera/View control
RENDERER_API void SetViewTransform(float zoom, float panX, float panY);

// Phase 5.5: Canvas orientation
// orientation: 0=0°, 1=90°CW, 2=180°, 3=270°CW
RENDERER_API void SetCanvasOrientation(int orientation);

// Phase 13: View direction (origin position)
// direction: 1=RightBottom, 2=LeftBottom(default)
RENDERER_API void SetViewDirection(int direction);

// Phase 8.1: Text rendering for part/contour numbers
RENDERER_API int InitializeTextRenderer(const wchar_t* fontName, int height, int bold, int italic);
RENDERER_API void DrawPartNumber(double posX, double posY, unsigned int number, double scale, float r, float g, float b);
RENDERER_API void DrawContourNumber(double posX, double posY, unsigned int number, double scale, float r, float g, float b);
RENDERER_API void CleanupTextRenderer();

// Phase 8.2: Realtime trace (cutting progress)
RENDERER_API void StartCuttingTrace(int startPart, int startContour, int isReverse);
RENDERER_API void UpdateCuttingTrace(int currentPart, int currentContour, double progress, float posX, float posY);
RENDERER_API void StopCuttingTrace();
RENDERER_API void DrawLaserHeadMarker(float posX, float posY, float scale, float r, float g, float b);

#ifdef __cplusplus
}
#endif

#endif // RENDERER_H
