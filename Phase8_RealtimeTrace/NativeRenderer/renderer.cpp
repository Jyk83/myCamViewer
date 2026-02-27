#include "renderer.h"
#include "TextRenderer.h"
#include <windows.h>
#include <gl/GL.h>
#include <vector>
#include <cmath>

#pragma comment(lib, "opengl32.lib")

// Math constants
#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif
static const float PI = (float)M_PI;
static const float TWO_PI = 2.0f * PI;

// Global state
static HGLRC g_hRC = nullptr;
static HDC g_hDC = nullptr;
static int g_viewportWidth = 800;
static int g_viewportHeight = 600;
static float g_zoom = 1.0f;
static float g_panX = 0.0f;
static float g_panY = 0.0f;

// Phase 5.5: Canvas orientation
// 0 = 0°, 1 = 90°CW, 2 = 180°, 3 = 270°CW
static int g_canvasOrientation = 0;

// Shape data structures
struct Shape {
    enum Type { RECTANGLE, CIRCLE };
    Type type;
    float x, y, width, height, radius;
    float r, g, b;
};

static std::vector<Shape> g_shapes;

// Initialize OpenGL context
RENDERER_API int InitializeRenderer(void* windowHandle) {
    HWND hwnd = (HWND)windowHandle;
    g_hDC = GetDC(hwnd);
    
    if (!g_hDC) {
        return 0;
    }

    PIXELFORMATDESCRIPTOR pfd = {
        sizeof(PIXELFORMATDESCRIPTOR),
        1,
        PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER,
        PFD_TYPE_RGBA,
        32,
        0, 0, 0, 0, 0, 0,
        0,
        0,
        0,
        0, 0, 0, 0,
        24,
        8,
        0,
        PFD_MAIN_PLANE,
        0,
        0, 0, 0
    };

    int pixelFormat = ChoosePixelFormat(g_hDC, &pfd);
    if (!pixelFormat) {
        return 0;
    }

    if (!SetPixelFormat(g_hDC, pixelFormat, &pfd)) {
        return 0;
    }

    g_hRC = wglCreateContext(g_hDC);
    if (!g_hRC) {
        return 0;
    }

    if (!wglMakeCurrent(g_hDC, g_hRC)) {
        return 0;
    }

    // OpenGL initialization
    glClearColor(0.0f, 0.0f, 0.0f, 1.0f); // Black background
    glEnable(GL_BLEND);
    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    glEnable(GL_LINE_SMOOTH);
    glHint(GL_LINE_SMOOTH_HINT, GL_NICEST);

    return 1;
}

RENDERER_API void CleanupRenderer() {
    if (g_hRC) {
        wglMakeCurrent(nullptr, nullptr);
        wglDeleteContext(g_hRC);
        g_hRC = nullptr;
    }
    
    if (g_hDC) {
        ReleaseDC(WindowFromDC(g_hDC), g_hDC);
        g_hDC = nullptr;
    }
    
    g_shapes.clear();
}

RENDERER_API void ResizeViewport(int width, int height) {
    g_viewportWidth = width;
    g_viewportHeight = height;
    
    if (g_hRC) {
        wglMakeCurrent(g_hDC, g_hRC);
        glViewport(0, 0, width, height);
    }
}

// Helper function to draw filled circle
static void DrawFilledCircle(float cx, float cy, float r, int segments = 32) {
    glBegin(GL_TRIANGLE_FAN);
    glVertex2f(cx, cy);
    for (int i = 0; i <= segments; i++) {
        float theta = 2.0f * 3.14159f * float(i) / float(segments);
        float x = r * cosf(theta);
        float y = r * sinf(theta);
        glVertex2f(x + cx, y + cy);
    }
    glEnd();
}

RENDERER_API void RenderFrame() {
    if (!g_hRC) return;
    
    wglMakeCurrent(g_hDC, g_hRC);
    
    glClear(GL_COLOR_BUFFER_BIT);
    
    // Setup projection matrix
    glMatrixMode(GL_PROJECTION);
    glLoadIdentity();
    
    float aspect = (float)g_viewportWidth / (float)g_viewportHeight;
    float viewWidth = 2.0f / g_zoom;
    float viewHeight = viewWidth / aspect;
    
    glOrtho(-viewWidth / 2 + g_panX, viewWidth / 2 + g_panX,
            -viewHeight / 2 + g_panY, viewHeight / 2 + g_panY,
            -1.0, 1.0);
    
    glMatrixMode(GL_MODELVIEW);
    glLoadIdentity();
    
    // Draw all shapes
    for (const auto& shape : g_shapes) {
        glColor3f(shape.r, shape.g, shape.b);
        
        if (shape.type == Shape::RECTANGLE) {
            glBegin(GL_QUADS);
            glVertex2f(shape.x, shape.y);
            glVertex2f(shape.x + shape.width, shape.y);
            glVertex2f(shape.x + shape.width, shape.y + shape.height);
            glVertex2f(shape.x, shape.y + shape.height);
            glEnd();
            
            // Draw outline
            glColor3f(shape.r * 0.7f, shape.g * 0.7f, shape.b * 0.7f);
            glLineWidth(2.0f);
            glBegin(GL_LINE_LOOP);
            glVertex2f(shape.x, shape.y);
            glVertex2f(shape.x + shape.width, shape.y);
            glVertex2f(shape.x + shape.width, shape.y + shape.height);
            glVertex2f(shape.x, shape.y + shape.height);
            glEnd();
        }
        else if (shape.type == Shape::CIRCLE) {
            DrawFilledCircle(shape.x, shape.y, shape.radius);
            
            // Draw outline
            glColor3f(shape.r * 0.7f, shape.g * 0.7f, shape.b * 0.7f);
            glLineWidth(2.0f);
            glBegin(GL_LINE_LOOP);
            for (int i = 0; i < 32; i++) {
                float theta = 2.0f * 3.14159f * float(i) / 32.0f;
                float x = shape.radius * cosf(theta);
                float y = shape.radius * sinf(theta);
                glVertex2f(x + shape.x, y + shape.y);
            }
            glEnd();
        }
    }
    
    SwapBuffers(g_hDC);
}

RENDERER_API void DrawRectangle(float x, float y, float width, float height, float r, float g, float b) {
    Shape shape;
    shape.type = Shape::RECTANGLE;
    shape.x = x;
    shape.y = y;
    shape.width = width;
    shape.height = height;
    shape.r = r;
    shape.g = g;
    shape.b = b;
    g_shapes.push_back(shape);
}

RENDERER_API void DrawCircle(float x, float y, float radius, float r, float g, float b) {
    Shape shape;
    shape.type = Shape::CIRCLE;
    shape.x = x;
    shape.y = y;
    shape.radius = radius;
    shape.r = r;
    shape.g = g;
    shape.b = b;
    g_shapes.push_back(shape);
}

RENDERER_API void ClearShapes() {
    g_shapes.clear();
}

RENDERER_API void SetViewTransform(float zoom, float panX, float panY) {
    g_zoom = zoom;
    g_panX = panX;
    g_panY = panY;
}

// Phase 5.5: Set canvas orientation
RENDERER_API void SetCanvasOrientation(int orientation) {
    if (orientation >= 0 && orientation <= 3) {
        g_canvasOrientation = orientation;
    }
}

// MPF drawing functions

RENDERER_API void BeginMPFRender() {
    BeginMPFRenderWithBackground(0.0f, 0.0f, 0.0f);
}

RENDERER_API void BeginMPFRenderWithBackground(float bgR, float bgG, float bgB) {
    if (!g_hRC) return;
    
    wglMakeCurrent(g_hDC, g_hRC);
    
    // Set background color before clearing
    glClearColor(bgR, bgG, bgB, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT);
    
    // Setup projection matrix with current view transform
    glMatrixMode(GL_PROJECTION);
    glLoadIdentity();
    
    float aspect = (float)g_viewportWidth / (float)g_viewportHeight;
    float viewWidth = 2.0f / g_zoom;
    float viewHeight = viewWidth / aspect;
    
    glOrtho(-viewWidth / 2 + g_panX, viewWidth / 2 + g_panX,
            -viewHeight / 2 + g_panY, viewHeight / 2 + g_panY,
            -1.0, 1.0);
    
    glMatrixMode(GL_MODELVIEW);
    glLoadIdentity();
    
    // Phase 5.5: Apply canvas orientation rotation
    if (g_canvasOrientation != 0) {
        switch (g_canvasOrientation) {
            case 1: // 90° clockwise
                glRotatef(-90.0f, 0.0f, 0.0f, 1.0f);
                break;
            case 2: // 180°
                glRotatef(180.0f, 0.0f, 0.0f, 1.0f);
                break;
            case 3: // 270° clockwise (90° counter-clockwise)
                glRotatef(90.0f, 0.0f, 0.0f, 1.0f);
                break;
        }
    }
}

RENDERER_API void EndMPFRender() {
    if (!g_hRC) return;
    
    // Finish all OpenGL commands but DON'T swap buffers yet
    // This allows GDI+ to draw text overlays before the swap
    glFinish();
    
    // SwapBuffers will be called separately after text is drawn
    // SwapBuffers(g_hDC); // Commented out
}

// New function to swap buffers after text drawing
RENDERER_API void SwapBuffersNow() {
    if (!g_hDC) return;
    SwapBuffers(g_hDC);
}

RENDERER_API void DrawLine(float x1, float y1, float x2, float y2, float r, float g, float b, float lineWidth) {
    if (!g_hRC) return;
    
    glColor3f(r, g, b);
    glLineWidth(lineWidth);
    glBegin(GL_LINES);
    glVertex2f(x1, y1);
    glVertex2f(x2, y2);
    glEnd();
}

RENDERER_API void DrawArc(float centerX, float centerY, float radius, float startAngle, float endAngle, 
                           int clockwise, float r, float g, float b, float lineWidth) {
    if (!g_hRC) return;
    
    glColor3f(r, g, b);
    glLineWidth(lineWidth);
    
    const int segments = 64;
    float angleStep;
    float currentAngle = startAngle;
    float targetAngle = endAngle;
    
    // Normalize angles to 0~2PI range
    while (currentAngle < 0) currentAngle += TWO_PI;
    while (currentAngle >= TWO_PI) currentAngle -= TWO_PI;
    while (targetAngle < 0) targetAngle += TWO_PI;
    while (targetAngle >= TWO_PI) targetAngle -= TWO_PI;
    
    // Calculate arc angle range
    float arcAngle;
    if (clockwise) {
        // Clockwise (G2)
        if (targetAngle > currentAngle) {
            arcAngle = -(TWO_PI - (targetAngle - currentAngle));
        } else {
            arcAngle = -(currentAngle - targetAngle);
        }
    } else {
        // Counter-clockwise (G3)
        if (targetAngle < currentAngle) {
            arcAngle = TWO_PI - (currentAngle - targetAngle);
        } else {
            arcAngle = targetAngle - currentAngle;
        }
    }
    
    angleStep = arcAngle / segments;
    
    glBegin(GL_LINE_STRIP);
    for (int i = 0; i <= segments; i++) {
        float angle = currentAngle + angleStep * i;
        float x = centerX + radius * cosf(angle);
        float y = centerY + radius * sinf(angle);
        glVertex2f(x, y);
    }
    glEnd();
}

RENDERER_API void DrawPoint(float x, float y, float size, float r, float g, float b) {
    if (!g_hRC) return;
    
    glColor3f(r, g, b);
    glPointSize(size);
    glBegin(GL_POINTS);
    glVertex2f(x, y);
    glEnd();
}

RENDERER_API void DrawFilledRectangle(float x, float y, float width, float height, float r, float g, float b) {
    if (!g_hRC) return;
    
    glColor3f(r, g, b);
    glBegin(GL_QUADS);
    glVertex2f(x, y);
    glVertex2f(x + width, y);
    glVertex2f(x + width, y + height);
    glVertex2f(x, y + height);
    glEnd();
}

// Phase 5.3: Helper function to draw a dashed line segment
static void DrawDashedLine(float x1, float y1, float x2, float y2, 
                          float r, float g, float b, float lineWidth, int dashPattern) {
    if (!g_hRC) return;
    
    // Calculate line length and direction
    float dx = x2 - x1;
    float dy = y2 - y1;
    float length = sqrtf(dx * dx + dy * dy);
    
    if (length < 0.001f) return; // Too short to draw
    
    // Normalize direction
    float dirX = dx / length;
    float dirY = dy / length;
    
    glColor3f(r, g, b);
    glLineWidth(lineWidth);
    
    // Define dash patterns (in OpenGL units, scaled by zoom)
    // Pattern format: [dash_length, gap_length, ...]
    float dashLength = 0.0f;
    float gapLength = 0.0f;
    
    switch (dashPattern) {
        case 0: // Solid (no dashing)
            glBegin(GL_LINES);
            glVertex2f(x1, y1);
            glVertex2f(x2, y2);
            glEnd();
            return;
            
        case 1: // Dash1 (long dash)
            dashLength = 0.02f;
            gapLength = 0.01f;
            break;
            
        case 2: // Dash2 (medium dash)
            dashLength = 0.015f;
            gapLength = 0.01f;
            break;
            
        case 3: // Dash3 (very short dash - very tight spacing like ------)
            dashLength = 0.004f;  // Very short dash segments
            gapLength = 0.001f;   // Very tight gaps for dense appearance
            break;
            
        case 4: // DotDash (dot-dash pattern)
            // Will alternate between dots and dashes
            dashLength = 0.01f;
            gapLength = 0.005f;
            break;
            
        case 5: // DotDotDash (dot-dot-dash pattern)
            dashLength = 0.01f;
            gapLength = 0.005f;
            break;
            
        default:
            dashLength = 0.01f;
            gapLength = 0.01f;
            break;
    }
    
    // Draw dashed line
    float currentPos = 0.0f;
    bool isDash = true;
    int segmentCount = 0;
    
    glBegin(GL_LINES);
    
    while (currentPos < length) {
        float segmentLength = 0.0f;
        
        // Special handling for dotdash and dotdotdash patterns
        if (dashPattern == 4) { // DotDash: dot, gap, dash, gap, ...
            if (segmentCount % 4 == 0) segmentLength = 0.003f; // Dot
            else if (segmentCount % 4 == 1) segmentLength = gapLength; // Gap
            else if (segmentCount % 4 == 2) segmentLength = dashLength; // Dash
            else segmentLength = gapLength; // Gap
        }
        else if (dashPattern == 5) { // DotDotDash: dot, gap, dot, gap, dash, gap, ...
            if (segmentCount % 6 == 0 || segmentCount % 6 == 2) segmentLength = 0.003f; // Dot
            else if (segmentCount % 6 == 1 || segmentCount % 6 == 3) segmentLength = gapLength; // Gap
            else if (segmentCount % 6 == 4) segmentLength = dashLength; // Dash
            else segmentLength = gapLength; // Gap
        }
        else {
            segmentLength = isDash ? dashLength : gapLength;
        }
        
        float nextPos = currentPos + segmentLength;
        if (nextPos > length) nextPos = length;
        
        // Determine if this segment should be drawn (dash vs gap)
        bool shouldDraw = false;
        if (dashPattern == 4) {
            shouldDraw = (segmentCount % 4 == 0 || segmentCount % 4 == 2); // Dot or dash
        }
        else if (dashPattern == 5) {
            shouldDraw = (segmentCount % 6 == 0 || segmentCount % 6 == 2 || segmentCount % 6 == 4); // Dots or dash
        }
        else {
            shouldDraw = isDash;
        }
        
        if (shouldDraw) {
            float sx = x1 + dirX * currentPos;
            float sy = y1 + dirY * currentPos;
            float ex = x1 + dirX * nextPos;
            float ey = y1 + dirY * nextPos;
            
            glVertex2f(sx, sy);
            glVertex2f(ex, ey);
        }
        
        currentPos = nextPos;
        isDash = !isDash;
        segmentCount++;
    }
    
    glEnd();
}

// Phase 5.3: Draw dashed rectangle (part boundary)
RENDERER_API void DrawDashedRectangle(float x, float y, float width, float height, 
                                       float r, float g, float b, float lineWidth, int dashPattern) {
    if (!g_hRC) return;
    
    // Draw four sides of rectangle with dashed pattern
    DrawDashedLine(x, y, x + width, y, r, g, b, lineWidth, dashPattern); // Bottom
    DrawDashedLine(x + width, y, x + width, y + height, r, g, b, lineWidth, dashPattern); // Right
    DrawDashedLine(x + width, y + height, x, y + height, r, g, b, lineWidth, dashPattern); // Top
    DrawDashedLine(x, y + height, x, y, r, g, b, lineWidth, dashPattern); // Left
}

//////////////////////////////////////////////////////////////////////////
// Phase 8.1: Text Rendering Functions
//////////////////////////////////////////////////////////////////////////

RENDERER_API int InitializeTextRenderer(const wchar_t* fontName, int height, int bold, int italic) {
    if (!g_hRC) return 0;
    
    wglMakeCurrent(g_hDC, g_hRC);
    return TextRenderer_Create(fontName, height, bold, italic);
}

RENDERER_API void DrawPartNumber(double posX, double posY, unsigned int number, 
                                  double scale, float r, float g, float b) {
    if (!g_hRC) return;
    
    wglMakeCurrent(g_hDC, g_hRC);
    
    // Save current state
    glPushAttrib(GL_CURRENT_BIT | GL_ENABLE_BIT);
    
    // Disable depth test for text (always on top)
    glDisable(GL_DEPTH_TEST);
    
    // Draw the number
    TextRenderer_DrawNumber(posX, posY, number, scale, r, g, b);
    
    // Restore state
    glPopAttrib();
}

RENDERER_API void DrawContourNumber(double posX, double posY, unsigned int number, 
                                     double scale, float r, float g, float b) {
    if (!g_hRC) return;
    
    wglMakeCurrent(g_hDC, g_hRC);
    
    // Save current state
    glPushAttrib(GL_CURRENT_BIT | GL_ENABLE_BIT);
    
    // Disable depth test for text (always on top)
    glDisable(GL_DEPTH_TEST);
    
    // Draw the number
    TextRenderer_DrawNumber(posX, posY, number, scale, r, g, b);
    
    // Restore state
    glPopAttrib();
}

RENDERER_API void CleanupTextRenderer() {
    TextRenderer_Destroy();
}

//////////////////////////////////////////////////////////////////////////
// Phase 8.2: Realtime Trace (Cutting Progress)
//////////////////////////////////////////////////////////////////////////

// Global trace state
struct TraceState {
    bool isActive;
    int startPart;
    int startContour;
    int currentPart;
    int currentContour;
    double progress;
    float posX;
    float posY;
    int isReverse;
} g_traceState = { false, 0, 0, 0, 0, 0.0, 0.0f, 0.0f, 0 };

RENDERER_API void StartCuttingTrace(int startPart, int startContour, int isReverse) {
    g_traceState.isActive = true;
    g_traceState.startPart = startPart;
    g_traceState.startContour = startContour;
    g_traceState.currentPart = startPart;
    g_traceState.currentContour = startContour;
    g_traceState.progress = 0.0;
    g_traceState.posX = 0.0f;
    g_traceState.posY = 0.0f;
    g_traceState.isReverse = isReverse;
}

RENDERER_API void UpdateCuttingTrace(int currentPart, int currentContour, 
                                      double progress, float posX, float posY) {
    if (!g_traceState.isActive) return;
    
    g_traceState.currentPart = currentPart;
    g_traceState.currentContour = currentContour;
    g_traceState.progress = progress;
    g_traceState.posX = posX;
    g_traceState.posY = posY;
}

RENDERER_API void StopCuttingTrace() {
    g_traceState.isActive = false;
    g_traceState.progress = 0.0;
}

RENDERER_API void DrawLaserHeadMarker(float posX, float posY, float scale, 
                                       float r, float g, float b) {
    if (!g_hRC) return;
    
    wglMakeCurrent(g_hDC, g_hRC);
    
    // Save current state
    glPushAttrib(GL_CURRENT_BIT | GL_LINE_BIT | GL_ENABLE_BIT);
    
    // Disable depth test (always on top)
    glDisable(GL_DEPTH_TEST);
    
    // Set color
    glColor3f(r, g, b);
    
    // Set line width
    glLineWidth(2.0f);
    
    // Calculate marker size based on scale
    float size = 5.0f * scale;
    
    // Draw crosshair (+)
    glBegin(GL_LINES);
    
    // Horizontal line
    glVertex2f(posX - size, posY);
    glVertex2f(posX + size, posY);
    
    // Vertical line
    glVertex2f(posX, posY - size);
    glVertex2f(posX, posY + size);
    
    glEnd();
    
    // Draw center circle
    int segments = 16;
    float circleRadius = size * 0.3f;
    
    glBegin(GL_LINE_LOOP);
    for (int i = 0; i < segments; i++) {
        float angle = TWO_PI * i / segments;
        float x = posX + circleRadius * cosf(angle);
        float y = posY + circleRadius * sinf(angle);
        glVertex2f(x, y);
    }
    glEnd();
    
    // Restore state
    glPopAttrib();
}
