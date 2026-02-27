using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CamViewerPOC.MPF;
using CamViewerPOC.Simulation;
using CamViewerPOC.Rendering;
using CamViewerPOC.Selection;
using CamViewerPOC.Trace;

namespace CamViewerPOC
{
    /// <summary>
    /// Native renderer P/Invoke declarations
    /// </summary>
    internal static class NativeRenderer
    {
        private const string DllName = "NativeRenderer.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int InitializeRenderer(IntPtr windowHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CleanupRenderer();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ResizeViewport(int width, int height);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RenderFrame();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DrawRectangle(float x, float y, float width, float height, 
                                                float r, float g, float b);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DrawCircle(float x, float y, float radius, 
                                             float r, float g, float b);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearShapes();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetViewTransform(float zoom, float panX, float panY);

        // MPF drawing functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void BeginMPFRender();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void BeginMPFRenderWithBackground(float bgR, float bgG, float bgB);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void EndMPFRender();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SwapBuffersNow();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DrawLine(float x1, float y1, float x2, float y2, 
                                          float r, float g, float b, float lineWidth);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DrawArc(float centerX, float centerY, float radius, 
                                         float startAngle, float endAngle, int clockwise, 
                                         float r, float g, float b, float lineWidth);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DrawPoint(float x, float y, float size, 
                                           float r, float g, float b);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DrawFilledRectangle(float x, float y, float width, float height,
                                                      float r, float g, float b);

        // Phase 5.3: Dashed rectangle for part boundaries
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DrawDashedRectangle(float x, float y, float width, float height,
                                                      float r, float g, float b, float lineWidth, int dashPattern);

        // Phase 5.5: Canvas orientation
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCanvasOrientation(int orientation);
    }

    /// <summary>
    /// WinCC Advanced compatible OpenGL viewer control
    /// </summary>
    public partial class CamViewerControl : UserControl
    {
        private Panel renderPanel;
        private bool isInitialized = false;
        private float zoom = 1.0f;
        private float panX = 0.0f;
        private float panY = 0.0f;
        private Point lastMousePos;
        private bool isPanning = false;

        // MPF data
        private MPFProgram currentProgram = null;
        private float workpieceScale = 0.001f; // mm to OpenGL units

        // Simulation engine
        private SimulationEngine simulationEngine = null;
        private int currentSimPartIndex = -1;
        private int currentSimContourIndex = -1;
        private int currentSimElementIndex = -1;
        
        // Throttle redraw to reduce flickering
        private System.Windows.Forms.Timer redrawTimer = null;
        private bool needsRedraw = false;
        private bool isRedrawing = false; // Prevent concurrent redraws

        // Phase 5: Selection management
        private SelectionManager selectionManager = null;
        private NumberPositionManager numberPositionManager = null;
        
        // Phase 7: Real-time cutting progress management
        private CuttingProgressManager cuttingProgressManager = null;
        
        // Phase 5 Debug: Show selection areas
        private bool showSelectionAreas = false;

        public CamViewerControl()
        {
            InitializeComponent();
            SetupRenderPanel();
            SetupSimulationEngine();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // CamViewerControl
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "CamViewerControl";
            this.Size = new System.Drawing.Size(800, 600);
            this.Load += new System.EventHandler(this.CamViewerControl_Load);
            
            this.ResumeLayout(false);
        }

        private void SetupRenderPanel()
        {
            renderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            
            // Enable double buffering to prevent flickering
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic,
                null, renderPanel, new object[] { true });

            renderPanel.Resize += RenderPanel_Resize;
            renderPanel.Paint += RenderPanel_Paint;
            renderPanel.MouseDown += RenderPanel_MouseDown;
            renderPanel.MouseMove += RenderPanel_MouseMove;
            renderPanel.MouseUp += RenderPanel_MouseUp;
            renderPanel.MouseWheel += RenderPanel_MouseWheel;

            this.Controls.Add(renderPanel);
        }

        private void CamViewerControl_Load(object sender, EventArgs e)
        {
            if (!DesignMode)
            {
                InitializeOpenGL();
            }
        }

        private void InitializeOpenGL()
        {
            try
            {
                // Check if panel handle is valid
                if (renderPanel.Handle == IntPtr.Zero)
                {
                    MessageBox.Show("Invalid window handle. Panel not created properly.", 
                                    "Initialization Error", 
                                    MessageBoxButtons.OK, 
                                    MessageBoxIcon.Error);
                    return;
                }

                int result = NativeRenderer.InitializeRenderer(renderPanel.Handle);
                if (result == 0)
                {
                    MessageBox.Show("Failed to initialize OpenGL renderer.\n\n" +
                                    "Possible causes:\n" +
                                    "- Graphics driver doesn't support OpenGL\n" +
                                    "- Failed to create OpenGL context\n" +
                                    "- Invalid pixel format\n\n" +
                                    "Please update your graphics drivers.", 
                                    "OpenGL Initialization Error", 
                                    MessageBoxButtons.OK, 
                                    MessageBoxIcon.Error);
                    return;
                }

                isInitialized = true;
                NativeRenderer.ResizeViewport(renderPanel.Width, renderPanel.Height);
                
                // Draw initial shapes for demo
                DrawSampleShapes();
                
                renderPanel.Invalidate();
            }
            catch (DllNotFoundException dllEx)
            {
                MessageBox.Show("NativeRenderer.dll not found!\n\n" +
                                "Please ensure the DLL is in the application directory.\n\n" +
                                "Expected location: " + System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\NativeRenderer.dll\n\n" +
                                "Error: " + dllEx.Message, 
                                "DLL Not Found", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Error);
            }
            catch (BadImageFormatException imgEx)
            {
                MessageBox.Show("Platform mismatch error!\n\n" +
                                "The NativeRenderer.dll architecture doesn't match this application.\n\n" +
                                "- DLL is x64 (64-bit)\n" +
                                "- Application must be x64 or AnyCPU\n\n" +
                                "Error: " + imgEx.Message, 
                                "Architecture Mismatch", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing renderer!\n\n" +
                                "Exception Type: " + ex.GetType().Name + "\n" +
                                "Message: " + ex.Message + "\n\n" +
                                "Stack Trace:\n" + ex.StackTrace, 
                                "Initialization Error", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Draw sample shapes for demonstration
        /// </summary>
        public void DrawSampleShapes()
        {
            if (!isInitialized) return;

            NativeRenderer.ClearShapes();

            // Draw rectangles
            NativeRenderer.DrawRectangle(-0.5f, -0.3f, 0.4f, 0.3f, 1.0f, 0.3f, 0.3f); // Red
            NativeRenderer.DrawRectangle(0.1f, 0.1f, 0.3f, 0.4f, 0.3f, 1.0f, 0.3f);   // Green
            
            // Draw circles
            NativeRenderer.DrawCircle(-0.3f, 0.3f, 0.2f, 0.3f, 0.3f, 1.0f);  // Blue
            NativeRenderer.DrawCircle(0.4f, -0.2f, 0.15f, 1.0f, 1.0f, 0.3f); // Yellow
            NativeRenderer.DrawCircle(0.0f, 0.0f, 0.1f, 1.0f, 0.5f, 0.0f);   // Orange

            renderPanel.Invalidate();
        }

        /// <summary>
        /// Add a rectangle to the scene
        /// </summary>
        public void AddRectangle(float x, float y, float width, float height, Color color)
        {
            if (!isInitialized) return;
            
            NativeRenderer.DrawRectangle(x, y, width, height, 
                                        color.R / 255.0f, 
                                        color.G / 255.0f, 
                                        color.B / 255.0f);
            renderPanel.Invalidate();
        }

        /// <summary>
        /// Add a circle to the scene
        /// </summary>
        public void AddCircle(float x, float y, float radius, Color color)
        {
            if (!isInitialized) return;
            
            NativeRenderer.DrawCircle(x, y, radius,
                                     color.R / 255.0f,
                                     color.G / 255.0f,
                                     color.B / 255.0f);
            renderPanel.Invalidate();
        }

        /// <summary>
        /// Clear all shapes from the scene
        /// </summary>
        public void ClearScene()
        {
            if (!isInitialized) return;
            
            NativeRenderer.ClearShapes();
            currentProgram = null;
            renderPanel.Invalidate();
        }

        /// <summary>
        /// Load and display MPF file
        /// </summary>
        public void LoadMPFFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("MPF file not found: " + filePath, 
                                    "File Error", 
                                    MessageBoxButtons.OK, 
                                    MessageBoxIcon.Error);
                    return;
                }

                // Read file content
                string content = File.ReadAllText(filePath);

                // Reset simulation state when loading new file
                if (simulationEngine != null)
                {
                    simulationEngine.Reset(); // Stop, ResetPosition, and set state to Idle
                }
                
                // Reset simulation tracking indices
                currentSimPartIndex = -1;
                currentSimContourIndex = -1;
                currentSimElementIndex = -1;

                // Parse MPF file
                MPFParser parser = new MPFParser(true);
                currentProgram = parser.Parse(content);

                // Clear all selections when loading new file
                selectionManager.ClearAllSelections();
                
                // Phase 7: Initialize cutting progress manager with new program
                if (cuttingProgressManager == null)
                {
                    cuttingProgressManager = new CuttingProgressManager();
                    cuttingProgressManager.ProgressUpdated += CuttingProgressManager_ProgressUpdated;
                }
                cuttingProgressManager.SetProgram(currentProgram);

                // Display the program (fresh initial state, no simulation history)
                DisplayMPFProgram();

                // Raise MPF loaded event instead of showing MessageBox
                if (MPFLoaded != null)
                {
                    MPFLoaded(this, new MPFLoadedEventArgs
                    {
                        Version = currentProgram.Version,
                        WorkpieceWidth = currentProgram.Workpiece.Width,
                        WorkpieceHeight = currentProgram.Workpiece.Height,
                        PartCount = currentProgram.Parts.Count,
                        ContourCount = currentProgram.Parts.Sum(p => p.Contours.Count)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("MPF 파일 로드 실패!\n\n" +
                                "오류: " + ex.Message + "\n\n" +
                                "스택 트레이스:\n" + ex.StackTrace,
                                "파싱 오류",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Display the loaded MPF program (initial load)
        /// </summary>
        private void DisplayMPFProgram()
        {
            if (!isInitialized || currentProgram == null) return;

            // Auto-fit to view first
            AutoFitView();

            // Render the scene
            RenderMPFScene();
        }

        /// <summary>
        /// Render the MPF scene (can be called from Paint event)
        /// </summary>
        private void RenderMPFScene()
        {
            if (!isInitialized || currentProgram == null) return;

            RenderSettings settings = RenderSettings.Instance;

            // Phase 5.5: Set canvas orientation before rendering
            NativeRenderer.SetCanvasOrientation((int)settings.Orientation);

            // Begin MPF rendering with exterior background color
            float[] bgColor = settings.GetColorAsFloat(settings.WorkpieceExteriorColor);
            NativeRenderer.BeginMPFRenderWithBackground(bgColor[0], bgColor[1], bgColor[2]);

            // Draw workpiece interior (filled rectangle)
            float width = (float)(currentProgram.Workpiece.Width * workpieceScale);
            float height = (float)(currentProgram.Workpiece.Height * workpieceScale);
            float[] interiorColor = settings.GetColorAsFloat(settings.WorkpieceInteriorColor);
            NativeRenderer.DrawFilledRectangle(0, 0, width, height, interiorColor[0], interiorColor[1], interiorColor[2]);

            // Draw workpiece boundary (optional - controlled by settings)
            if (settings.ShowWorkpieceBoundary)
            {
                DrawWorkpieceBoundary();
            }

            // Draw all parts
            for (int partIndex = 0; partIndex < currentProgram.Parts.Count; partIndex++)
            {
                DrawPart(currentProgram.Parts[partIndex], partIndex);
            }

            // Phase 5 Debug: Draw selection areas if enabled
            if (showSelectionAreas)
            {
                DrawSelectionAreas();
            }

            // End MPF rendering (finish OpenGL commands, but don't swap buffers yet)
            // SwapBuffers will be called after text overlays are drawn
            NativeRenderer.EndMPFRender();
        }

        /// <summary>
        /// Phase 5 Debug: Draw selection areas for all contours
        /// </summary>
        private void DrawSelectionAreas()
        {
            if (currentProgram == null) return;

            // Rainbow colors for different contours
            Color[] colors = new Color[]
            {
                Color.FromArgb(255, 0, 0),      // Red
                Color.FromArgb(255, 165, 0),    // Orange
                Color.FromArgb(255, 255, 0),    // Yellow
                Color.FromArgb(0, 255, 0),      // Green
                Color.FromArgb(0, 0, 255),      // Blue
                Color.FromArgb(75, 0, 130),     // Indigo
                Color.FromArgb(238, 130, 238),  // Violet
                Color.FromArgb(255, 192, 203),  // Pink
                Color.FromArgb(0, 255, 255),    // Cyan
                Color.FromArgb(255, 0, 255)     // Magenta
            };

            int colorIndex = 0;

            for (int partIndex = 0; partIndex < currentProgram.Parts.Count; partIndex++)
            {
                MPF.Part part = currentProgram.Parts[partIndex];
                if (part == null) continue;

                float offsetX = (float)(part.Origin.X * workpieceScale);
                float offsetY = (float)(part.Origin.Y * workpieceScale);

                for (int contourIndex = 0; contourIndex < part.Contours.Count; contourIndex++)
                {
                    Contour contour = part.Contours[contourIndex];
                    if (contour == null) continue;

                    // Select color (cycle through rainbow)
                    Color fillColor = colors[colorIndex % colors.Length];
                    colorIndex++;

                    float r = fillColor.R / 255.0f;
                    float g = fillColor.G / 255.0f;
                    float b = fillColor.B / 255.0f;

                    // Draw contour exactly as point-in-polygon test
                    // MUST use AllSegments (includes LeadIn for inside approach)
                    List<PathSegment> contourPath = contour.AllSegments ?? contour.CuttingPath;
                    
                    if (contourPath != null && contourPath.Count > 0)
                    {


                        // Track first and last points for gap detection
                        GeometryUtils.Point2D? firstPoint = null;
                        GeometryUtils.Point2D? lastPoint = null;

                        // Draw each segment with thick colored line
                        foreach (PathSegment segment in contourPath)
                        {
                            if (segment is LineSegment line)
                            {
                                float x1 = (float)(line.Start.X * workpieceScale) + offsetX;
                                float y1 = (float)(line.Start.Y * workpieceScale) + offsetY;
                                float x2 = (float)(line.End.X * workpieceScale) + offsetX;
                                float y2 = (float)(line.End.Y * workpieceScale) + offsetY;

                                if (!firstPoint.HasValue)
                                    firstPoint = new GeometryUtils.Point2D(x1, y1);
                                lastPoint = new GeometryUtils.Point2D(x2, y2);

                                // Draw thick line (5x normal width for visibility)
                                NativeRenderer.DrawLine(x1, y1, x2, y2, r, g, b, 5.0f);
                            }
                            else if (segment is ArcSegment arc)
                            {
                                float centerX = (float)(arc.Center.X * workpieceScale) + offsetX;
                                float centerY = (float)(arc.Center.Y * workpieceScale) + offsetY;
                                float radius = (float)(arc.Radius * workpieceScale);

                                // Calculate start/end points for gap detection
                                float x1 = centerX + radius * (float)Math.Cos(arc.StartAngle * Math.PI / 180.0);
                                float y1 = centerY + radius * (float)Math.Sin(arc.StartAngle * Math.PI / 180.0);
                                float x2 = centerX + radius * (float)Math.Cos(arc.EndAngle * Math.PI / 180.0);
                                float y2 = centerY + radius * (float)Math.Sin(arc.EndAngle * Math.PI / 180.0);

                                if (!firstPoint.HasValue)
                                    firstPoint = new GeometryUtils.Point2D(x1, y1);
                                lastPoint = new GeometryUtils.Point2D(x2, y2);

                                // Draw thick arc (Convert degrees to radians)
                                NativeRenderer.DrawArc(centerX, centerY, radius,
                                                     (float)(arc.StartAngle * Math.PI / 180.0), 
                                                     (float)(arc.EndAngle * Math.PI / 180.0),
                                                     arc.Clockwise ? 1 : 0,
                                                     r, g, b, 5.0f);
                            }
                        }

                        // Draw closing gap line if exists (CRITICAL for open contours)
                        if (firstPoint.HasValue && lastPoint.HasValue)
                        {
                            double gapDistance = GeometryUtils.Distance(firstPoint.Value, lastPoint.Value);
                            
                            if (gapDistance > 0.001) // If gap exists (> 1mm)
                            {

                                
                                // Draw gap closing line with dashed pattern for visibility
                                // Use white color to distinguish from normal contour
                                float gapX1 = (float)lastPoint.Value.X;
                                float gapY1 = (float)lastPoint.Value.Y;
                                float gapX2 = (float)firstPoint.Value.X;
                                float gapY2 = (float)firstPoint.Value.Y;

                                // Draw dashed line for gap (alternating segments)
                                int dashSegments = 10;
                                for (int i = 0; i < dashSegments; i++)
                                {
                                    if (i % 2 == 0) // Draw every other segment
                                    {
                                        float t1 = (float)i / dashSegments;
                                        float t2 = (float)(i + 1) / dashSegments;
                                        float dashX1 = gapX1 + t1 * (gapX2 - gapX1);
                                        float dashY1 = gapY1 + t1 * (gapY2 - gapY1);
                                        float dashX2 = gapX1 + t2 * (gapX2 - gapX1);
                                        float dashY2 = gapY1 + t2 * (gapY2 - gapY1);
                                        
                                        // White dashed line for gap
                                        NativeRenderer.DrawLine(dashX1, dashY1, dashX2, dashY2, 1.0f, 1.0f, 1.0f, 5.0f);
                                    }
                                }
                            }
                            else
                            {

                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draw workpiece boundary
        /// </summary>
        private void DrawWorkpieceBoundary()
        {
            if (currentProgram == null) return;

            RenderSettings settings = RenderSettings.Instance;
            float[] color = settings.GetColorAsFloat(settings.WorkpieceBoundaryColor);
            float lineWidth = settings.WorkpieceBoundaryWidth;

            float width = (float)(currentProgram.Workpiece.Width * workpieceScale);
            float height = (float)(currentProgram.Workpiece.Height * workpieceScale);

            // Draw boundary rectangle
            NativeRenderer.DrawLine(0, 0, width, 0, color[0], color[1], color[2], lineWidth);
            NativeRenderer.DrawLine(width, 0, width, height, color[0], color[1], color[2], lineWidth);
            NativeRenderer.DrawLine(width, height, 0, height, color[0], color[1], color[2], lineWidth);
            NativeRenderer.DrawLine(0, height, 0, 0, color[0], color[1], color[2], lineWidth);
        }

        /// <summary>
        /// Draw a single part with all its contours
        /// </summary>
        private void DrawPart(MPF.Part part, int partIndex)
        {
            if (part == null) return;

            RenderSettings settings = RenderSettings.Instance;

            // Apply part origin transformation
            float originX = (float)(part.Origin.X * workpieceScale);
            float originY = (float)(part.Origin.Y * workpieceScale);

            // Phase 5.3: Draw part boundary (dashed rectangle)
            // Use HKSTR's ContourWidth/Height from Part.Width/Height
            if (settings.ShowPartNumbers) // Part boundaries are now tied to part numbers
            {
                if (part.Width > 0 && part.Height > 0)
                {
                    float partWidth = (float)(part.Width * workpieceScale);
                    float partHeight = (float)(part.Height * workpieceScale);
                    
                    float[] boundaryColor = settings.GetColorAsFloat(settings.PartBoundaryColor);
                    NativeRenderer.DrawDashedRectangle(
                        originX, originY, 
                        partWidth, partHeight,
                        boundaryColor[0], boundaryColor[1], boundaryColor[2],
                        settings.PartBoundaryWidth,
                        (int)settings.PartBoundaryDashPattern
                    );
                }
            }

            // Draw part origin point (optional - Phase4)
            if (settings.ShowPartOrigin)
            {
                float[] color = settings.GetColorAsFloat(settings.PartOriginColor);
                NativeRenderer.DrawPoint(originX, originY, settings.PartOriginSize, color[0], color[1], color[2]);
            }

            // Draw all contours
            for (int contourIndex = 0; contourIndex < part.Contours.Count; contourIndex++)
            {
                DrawContour(part.Contours[contourIndex], originX, originY, partIndex, contourIndex);
            }
        }

        /// <summary>
        /// Draw a single contour
        /// </summary>
        private void DrawContour(Contour contour, float offsetX, float offsetY, int partIndex, int contourIndex)
        {
            if (contour == null) return;

            RenderSettings settings = RenderSettings.Instance;

            // Check if this is a marking contour (CuttingType == 10)
            bool isMarking = (contour.CuttingType == 10);

            // Phase 5: Check if this contour is selected
            bool isContourSelected = selectionManager != null && 
                                    selectionManager.IsContourSelected(partIndex, contourIndex);

            // Draw piercing point (skip if PiercingType == 0, e.g., marking has no piercing)
            if (contour.PiercingType != 0)
            {
                float pierceX = (float)(contour.PiercingPosition.X * workpieceScale) + offsetX;
                float pierceY = (float)(contour.PiercingPosition.Y * workpieceScale) + offsetY;
                float[] piercingColor = settings.GetColorAsFloat(settings.PiercingPointColor);
                NativeRenderer.DrawPoint(pierceX, pierceY, settings.PiercingPointSize, piercingColor[0], piercingColor[1], piercingColor[2]);
            }

            // Determine path color based on contour type and selection state
            Color pathColor;
            float lineWidth;

            if (isContourSelected)
            {
                // Phase 5: Highlight selected contour (yellow color + thicker line)
                pathColor = Color.DarkOrange;
                lineWidth = settings.CuttingPendingWidth * 1.1f; // 2.5x thicker
            }
            else
            {
                pathColor = isMarking ? settings.MarkingColor : settings.CuttingPendingColor;
                lineWidth = settings.CuttingPendingWidth;
            }

            float[] colorArray = settings.GetColorAsFloat(pathColor);

            // Draw all segments (using AllSegments for unified indexing)
            if (contour.AllSegments != null)
            {
                int elementIndex = 0;
                foreach (PathSegment segment in contour.AllSegments)
                {
                    // Phase 5: Check element selection
                    bool isElementSelected = selectionManager != null &&
                                            selectionManager.IsElementSelected(partIndex, contourIndex, elementIndex);

                    // Check if this segment is part of lead-in
                    bool isLeadInSegment = (contour.LeadIn != null && contour.LeadIn.Path.Contains(segment));
                    
                    // Phase 7: Check cutting progress state
                    bool isCompleted = (cuttingProgressManager != null && 
                                       cuttingProgressManager.IsElementCompleted(partIndex, contourIndex, elementIndex));
                    bool isInProgress = (cuttingProgressManager != null && 
                                        cuttingProgressManager.IsElementInProgress(partIndex, contourIndex, elementIndex));

                    // Determine color and width based on state priority:
                    // 0. Contour selection (HIGHEST priority - Phase 7 fix)
                    // 1. Element selection
                    // 2. Cutting progress state
                    // 3. Lead-in
                    // 4. Default (pending/marking)
                    
                    Color segmentColor;
                    float segmentWidth;

                    if (isContourSelected)
                    {
                        // Phase 7 FIX: 컨투어 선택 우선순위를 최상위로
                        segmentColor = Color.DarkOrange;
                        segmentWidth = lineWidth * 1.1f;
                    }
                    else if (isElementSelected)
                    {
                        // Highlight selected element (magenta color + thicker)
                        segmentColor = Color.Magenta;
                        segmentWidth = isLeadInSegment ? settings.LeadInWidth * 1.5f : lineWidth * 1.5f;
                    }
                    else if (isInProgress)
                    {
                        // Phase 7: Element in progress (red)
                        segmentColor = settings.CuttingInProgressColor;
                        segmentWidth = isLeadInSegment ? settings.LeadInWidth : settings.CuttingInProgressWidth;
                    }
                    else if (isCompleted)
                    {
                        // Phase 7: Element completed (white/light gray)
                        segmentColor = settings.CuttingCompletedColor;
                        segmentWidth = isLeadInSegment ? settings.LeadInWidth : settings.CuttingCompletedWidth;
                    }
                    else if (isLeadInSegment)
                    {
                        // Lead-in color (cyan)
                        segmentColor = settings.LeadInColor;
                        segmentWidth = settings.LeadInWidth;
                    }
                    else
                    {
                        // Default: Pending or marking
                        segmentColor = isMarking ? settings.MarkingColor : settings.CuttingPendingColor;
                        segmentWidth = settings.CuttingPendingWidth;
                    }

                    float[] finalColor = settings.GetColorAsFloat(segmentColor);
                    DrawPathSegment(segment, offsetX, offsetY, finalColor[0], finalColor[1], finalColor[2], segmentWidth);

                    elementIndex++;
                }
            }
        }

        /// <summary>
        /// Draw a path segment (line or arc)
        /// </summary>
        private void DrawPathSegment(PathSegment segment, float offsetX, float offsetY, 
                                    float r, float g, float b, float lineWidth)
        {
            if (segment == null) return;

            if (segment is LineSegment)
            {
                LineSegment line = (LineSegment)segment;
                float x1 = (float)(line.Start.X * workpieceScale) + offsetX;
                float y1 = (float)(line.Start.Y * workpieceScale) + offsetY;
                float x2 = (float)(line.End.X * workpieceScale) + offsetX;
                float y2 = (float)(line.End.Y * workpieceScale) + offsetY;

                NativeRenderer.DrawLine(x1, y1, x2, y2, r, g, b, lineWidth);
            }
            else if (segment is ArcSegment)
            {
                ArcSegment arc = (ArcSegment)segment;
                float centerX = (float)(arc.Center.X * workpieceScale) + offsetX;
                float centerY = (float)(arc.Center.Y * workpieceScale) + offsetY;
                float radius = (float)(arc.Radius * workpieceScale);

                NativeRenderer.DrawArc(centerX, centerY, radius, 
                                      (float)(arc.StartAngle * Math.PI / 180.0), 
                                      (float)(arc.EndAngle * Math.PI / 180.0), 
                                      arc.Clockwise ? 1 : 0, 
                                      r, g, b, lineWidth);
            }
        }

        /// <summary>
        /// Auto-fit view to show entire workpiece
        /// </summary>
        private void AutoFitView()
        {
            if (currentProgram == null) return;

            float width = (float)(currentProgram.Workpiece.Width * workpieceScale);
            float height = (float)(currentProgram.Workpiece.Height * workpieceScale);

            // Calculate zoom to fit workpiece at 50% of screen (1/2)
            // This provides comfortable viewing with space around workpiece
            float aspect = (float)renderPanel.Width / (float)renderPanel.Height;
            float wpAspect = width / height;

            // Calculate zoom using configurable multiplier from RenderSettings
            // User can adjust InitialZoomMultiplier in Render Settings (0.001 ~ 0.1)
            float multiplier = RenderSettings.Instance.InitialZoomMultiplier;
            float zoomByWidth = (float)renderPanel.Width / width * multiplier;
            float zoomByHeight = (float)renderPanel.Height / height * multiplier;
            
            // Use the smaller zoom to ensure entire workpiece fits
            zoom = Math.Min(zoomByWidth, zoomByHeight);

            // Center the view
            panX = width / 2.0f;
            panY = height / 2.0f;

            UpdateViewTransform();
        }

        /// <summary>
        /// Reset view to default
        /// </summary>
        public void ResetView()
        {
            zoom = 1.0f;
            panX = 0.0f;
            panY = 0.0f;
            UpdateViewTransform();
        }

        // ========================================
        // Simulation Methods
        // ========================================

        /// <summary>
        /// Setup simulation engine
        /// </summary>
        private void SetupSimulationEngine()
        {
            simulationEngine = new SimulationEngine();
            simulationEngine.ResponseTime = 50; // 50ms per element

            // 이벤트 구독
            simulationEngine.ProgressUpdated += SimulationEngine_ProgressUpdated;
            simulationEngine.SimulationCompleted += SimulationEngine_SimulationCompleted;
            simulationEngine.LogMessage += SimulationEngine_LogMessage;
            
            // Setup redraw throttle timer to reduce flickering
            redrawTimer = new System.Windows.Forms.Timer();
            redrawTimer.Interval = 16; // Redraw at most every 16ms (60 FPS)
            redrawTimer.Tick += (s, e) =>
            {
                if (needsRedraw && !isRedrawing)
                {
                    needsRedraw = false;
                    renderPanel.Invalidate();
                }
            };
            redrawTimer.Start();

            // Phase 5: Setup selection managers
            selectionManager = new SelectionManager();
            selectionManager.CurrentMode = SelectionManager.SelectionMode.None;
            selectionManager.SelectionChanged += SelectionManager_SelectionChanged;

            numberPositionManager = new NumberPositionManager();
            numberPositionManager.PositionChanged += NumberPositionManager_PositionChanged;
        }

        /// <summary>
        /// Phase 5: Selection changed event handler
        /// </summary>
        private void SelectionManager_SelectionChanged(object sender, EventArgs e)
        {
            // Phase 7 FIX: 명시적으로 Invalidate() 호출하여 즉시 다시 그리기
            needsRedraw = true;
            Invalidate();

        }

        /// <summary>
        /// Phase 5: Number position changed event handler
        /// </summary>
        private void NumberPositionManager_PositionChanged(object sender, EventArgs e)
        {
            // Redraw to show updated number positions
            needsRedraw = true;
        }
        
        /// <summary>
        /// Phase 7: Cutting progress updated event handler
        /// </summary>
        private void CuttingProgressManager_ProgressUpdated(object sender, CuttingProgressEventArgs e)
        {
            // Redraw to show cutting progress
            needsRedraw = true;
            Log($"[CuttingProgress] P{e.PartIndex+1} C{e.ContourIndex+1} E{e.ElementIndex} = {e.Progress:F2}");
        }

        /// <summary>
        /// Start simulation
        /// </summary>
        public void StartSimulation()
        {
            if (currentProgram == null)
            {
                MessageBox.Show("MPF 파일을 먼저 로드하세요.", "시뮬레이션", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (simulationEngine.State == SimulationState.Running)
            {
                return;
            }

            try
            {
                // Clear all selections when starting simulation
                selectionManager.ClearAllSelections();
                
                simulationEngine.SetProgram(currentProgram);
                simulationEngine.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("시뮬레이션 시작 실패!\n\n" + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Pause simulation
        /// </summary>
        public void PauseSimulation()
        {
            if (simulationEngine.State == SimulationState.Running)
            {
                simulationEngine.Pause();
            }
        }

        /// <summary>
        /// Resume simulation
        /// </summary>
        public void ResumeSimulation()
        {
            if (simulationEngine.State == SimulationState.Paused)
            {
                simulationEngine.Resume();
            }
        }

        /// <summary>
        /// Stop simulation
        /// </summary>
        public void StopSimulation()
        {
            simulationEngine.Stop();
            
            // Keep current progress (do NOT reset indices)
            // User can see where simulation stopped
            
            // Redraw to show final state
            if (currentProgram != null)
            {
                RedrawSimulation();
            }
        }
        
        /// <summary>
        /// Reset simulation to beginning
        /// </summary>
        public void ResetSimulation()
        {
            simulationEngine.Reset();
            
            // Reset display to initial state
            currentSimPartIndex = -1;
            currentSimContourIndex = -1;
            currentSimElementIndex = -1;
            
            if (currentProgram != null)
            {
                DisplayMPFProgram();
            }
        }

        /// <summary>
        /// Set simulation speed (10-1000 ms)
        /// </summary>
        public void SetSimulationSpeed(int milliseconds)
        {
            simulationEngine.ResponseTime = milliseconds;
        }

        /// <summary>
        /// Get current simulation state
        /// </summary>
        public SimulationState GetSimulationState()
        {
            return simulationEngine.State;
        }

        /// <summary>
        /// Simulation progress event handler
        /// </summary>
        private void SimulationEngine_ProgressUpdated(object sender, SimulationProgressEventArgs e)
        {
            // UI 스레드에서 실행
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SimulationEngine_ProgressUpdated(sender, e)));
                return;
            }

            // 현재 위치 저장
            currentSimPartIndex = e.PartIndex;
            currentSimContourIndex = e.ContourIndex;
            currentSimElementIndex = e.ElementIndex;

            // 진행 중인 세그먼트만 다시 그리기 (throttle timer에서 처리)
            RedrawSimulation();

            // 이벤트 발생 (MainForm에서 처리)
            if (SimulationProgress != null)
            {
                SimulationProgress(this, e);
            }
        }

        /// <summary>
        /// Simulation completed event handler
        /// </summary>
        private void SimulationEngine_SimulationCompleted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SimulationEngine_SimulationCompleted(sender, e)));
                return;
            }

            // Don't redraw - keep final simulation state visible
            // DisplayMPFProgram() would reset to preview mode, which is not desired
            
            MessageBox.Show("시뮬레이션이 완료되었습니다!", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Simulation log message handler
        /// </summary>
        private void SimulationEngine_LogMessage(object sender, string message)
        {
            // 로그는 MainForm에 전달
            if (SimulationLog != null)
            {
                SimulationLog(this, message);
            }
        }

        /// <summary>
        /// Redraw simulation (trigger Paint event)
        /// </summary>
        private void RedrawSimulation()
        {
            if (!isInitialized || currentProgram == null) return;
            
            // Set flag for throttled redraw (reduces flickering)
            needsRedraw = true;
        }

        // Simulation events for MainForm
        public event EventHandler<SimulationProgressEventArgs> SimulationProgress;
        public event EventHandler<string> SimulationLog;
        
        // MPF loaded event
        public event EventHandler<MPFLoadedEventArgs> MPFLoaded;
        
        // General log event (for debug and selection logs)
        public event EventHandler<string> LogMessage;
        
        public class MPFLoadedEventArgs : EventArgs
        {
            public string Version { get; set; }
            public double WorkpieceWidth { get; set; }
            public double WorkpieceHeight { get; set; }
            public int PartCount { get; set; }
            public int ContourCount { get; set; }
        }

        private void UpdateViewTransform()
        {
            if (!isInitialized) return;
            
            NativeRenderer.SetViewTransform(zoom, panX, panY);
            
            // Always use Invalidate to trigger Paint event (for text overlay)
            renderPanel.Invalidate();
        }

        private void RenderPanel_Resize(object sender, EventArgs e)
        {
            if (!isInitialized) return;
            
            NativeRenderer.ResizeViewport(renderPanel.Width, renderPanel.Height);
            renderPanel.Invalidate();
        }

        private void RenderPanel_Paint(object sender, PaintEventArgs e)
        {
            if (!isInitialized) return;
            
            // Prevent concurrent rendering
            if (isRedrawing) return;
            
            try
            {
                isRedrawing = true;
                
                // If MPF program is loaded, render it
                if (currentProgram != null)
                {
                    // Check if simulation has been started (not Idle)
                    if (simulationEngine.State != SimulationState.Idle)
                    {
                        // Render with simulation progress (Running, Paused, Stopped, Completed)
                        RenderSimulationFrame();
                    }
                    else
                    {
                        // Normal rendering (initial load, before simulation starts)
                        RenderMPFScene();
                    }
                    
                    // Draw text overlays BEFORE swapping buffers
                    // EndMPFRender now calls glFinish() but NOT SwapBuffers
                    // We draw text using Paint event Graphics, then call SwapBuffersNow()
                    DrawTextOverlays(e.Graphics);
                    
                    // Now swap buffers to present both OpenGL and GDI+ content
                    NativeRenderer.SwapBuffersNow();
                }
                else
                {
                    // Otherwise render old shapes system
                    NativeRenderer.RenderFrame();
                }
            }
            finally
            {
                isRedrawing = false;
            }
        }
        

        
        /// <summary>
        /// Render frame with simulation progress
        /// </summary>
        private void RenderSimulationFrame()
        {
            if (!isInitialized || currentProgram == null) return;

            RenderSettings settings = RenderSettings.Instance;

            // Phase 5.5: Set canvas orientation before rendering
            NativeRenderer.SetCanvasOrientation((int)settings.Orientation);

            // Begin MPF rendering with exterior background color
            float[] bgColor = settings.GetColorAsFloat(settings.WorkpieceExteriorColor);
            NativeRenderer.BeginMPFRenderWithBackground(bgColor[0], bgColor[1], bgColor[2]);

            // Draw workpiece interior (filled rectangle)
            float width = (float)(currentProgram.Workpiece.Width * workpieceScale);
            float height = (float)(currentProgram.Workpiece.Height * workpieceScale);
            float[] interiorColor = settings.GetColorAsFloat(settings.WorkpieceInteriorColor);
            NativeRenderer.DrawFilledRectangle(0, 0, width, height, interiorColor[0], interiorColor[1], interiorColor[2]);

            // Draw workpiece boundary (optional - controlled by settings)
            if (settings.ShowWorkpieceBoundary)
            {
                DrawWorkpieceBoundary();
            }

            // Draw parts up to current position
            for (int pi = 0; pi < currentProgram.Parts.Count; pi++)
            {
                Part part = currentProgram.Parts[pi];
                float originX = (float)(part.Origin.X * workpieceScale);
                float originY = (float)(part.Origin.Y * workpieceScale);

                // Phase 5.3: Draw part boundary (dashed rectangle)
                // Phase 5 Fix: Use Part.Width/Height directly instead of calculating from contours
                if (settings.ShowPartNumbers) // Part boundaries are now tied to part numbers
                {
                    if (part.Width > 0 && part.Height > 0)
                    {
                        float partWidth = (float)(part.Width * workpieceScale);
                        float partHeight = (float)(part.Height * workpieceScale);
                        
                        float[] boundaryColor = settings.GetColorAsFloat(settings.PartBoundaryColor);
                        NativeRenderer.DrawDashedRectangle(
                            originX, originY,
                            partWidth, partHeight,
                            boundaryColor[0], boundaryColor[1], boundaryColor[2],
                            settings.PartBoundaryWidth,
                            (int)settings.PartBoundaryDashPattern
                        );
                    }
                }

                // Draw part origin (optional)
                if (settings.ShowPartOrigin)
                {
                    float[] originColor = settings.GetColorAsFloat(settings.PartOriginColor);
                    NativeRenderer.DrawPoint(originX, originY, settings.PartOriginSize, originColor[0], originColor[1], originColor[2]);
                }

                for (int ci = 0; ci < part.Contours.Count; ci++)
                {
                    Contour contour = part.Contours[ci];

                    // Check if this is a marking contour
                    bool isMarking = (contour.CuttingType == 10);

                    // Draw piercing point (skip if PiercingType == 0)
                    if (contour.PiercingType != 0)
                    {
                        float pierceX = (float)(contour.PiercingPosition.X * workpieceScale) + originX;
                        float pierceY = (float)(contour.PiercingPosition.Y * workpieceScale) + originY;
                        float[] piercingColor = settings.GetColorAsFloat(settings.PiercingPointColor);
                        NativeRenderer.DrawPoint(pierceX, pierceY, settings.PiercingPointSize, piercingColor[0], piercingColor[1], piercingColor[2]);
                    }

                    // Draw segments based on simulation progress
                    for (int ei = 0; ei < contour.AllSegments.Count; ei++)
                    {
                        PathSegment segment = contour.AllSegments[ei];
                        
                        // Determine if this segment is completed, in progress, or future
                        bool isCompleted = false;
                        bool isCurrent = false;
                        
                        if (pi < currentSimPartIndex ||
                            (pi == currentSimPartIndex && ci < currentSimContourIndex))
                        {
                            // Completed part/contour
                            isCompleted = true;
                        }
                        else if (pi == currentSimPartIndex && ci == currentSimContourIndex)
                        {
                            // Current contour
                            if (ei < currentSimElementIndex)
                            {
                                isCompleted = true;
                            }
                            else if (ei == currentSimElementIndex)
                            {
                                isCurrent = true;
                            }
                            // else: future segment (draw as gray preview)
                        }
                        // else: future part/contour (draw as gray preview)
                        
                        // Phase 5: Check selection state
                        bool isContourSelected = selectionManager != null &&
                                                selectionManager.IsContourSelected(pi, ci);
                        bool isElementSelected = selectionManager != null &&
                                                selectionManager.IsElementSelected(pi, ci, ei);

                        // Choose color based on state (selection overrides simulation state)
                        float r, g, b, lineWidth;
                        
                        if (isElementSelected)
                        {
                            // Element selection (highest priority)
                            float[] color = settings.GetColorAsFloat(Color.Magenta);
                            r = color[0]; g = color[1]; b = color[2];
                            lineWidth = settings.CuttingPendingWidth * 2.0f;
                        }
                        else if (isContourSelected)
                        {
                            // Contour selection
                            float[] color = settings.GetColorAsFloat(Color.Yellow);
                            r = color[0]; g = color[1]; b = color[2];
                            lineWidth = settings.CuttingPendingWidth * 1.8f;
                        }
                        else if (isCompleted)
                        {
                            // Completed segments (both lead-in and cutting)
                            float[] color = settings.GetColorAsFloat(settings.CuttingCompletedColor);
                            r = color[0]; g = color[1]; b = color[2];
                            
                            if (contour.LeadIn != null && contour.LeadIn.Path.Contains(segment))
                            {
                                lineWidth = settings.LeadInWidth;
                            }
                            else
                            {
                                lineWidth = settings.CuttingCompletedWidth;
                            }
                        }
                        else if (isCurrent)
                        {
                            // Current segment (both lead-in and cutting): use in-progress color (red)
                            float[] color = settings.GetColorAsFloat(settings.CuttingInProgressColor);
                            r = color[0]; g = color[1]; b = color[2];
                            
                            if (contour.LeadIn != null && contour.LeadIn.Path.Contains(segment))
                            {
                                lineWidth = settings.LeadInWidth;
                            }
                            else
                            {
                                lineWidth = settings.CuttingInProgressWidth;
                            }
                        }
                        else
                        {
                            // Future segments (미진행 세그먼트)
                            if (contour.LeadIn != null && contour.LeadIn.Path.Contains(segment))
                            {
                                // 리드인: LeadInColor 유지
                                float[] color = settings.GetColorAsFloat(settings.LeadInColor);
                                r = color[0]; g = color[1]; b = color[2];
                                lineWidth = settings.LeadInWidth;
                            }
                            else
                            {
                                // 절단 경로: marking color 또는 pending color
                                Color futureColor = isMarking ? settings.MarkingColor : settings.CuttingPendingColor;
                                float[] color = settings.GetColorAsFloat(futureColor);
                                r = color[0]; g = color[1]; b = color[2];
                                lineWidth = settings.CuttingPendingWidth;
                            }
                        }
                        
                        DrawPathSegment(segment, originX, originY, r, g, b, lineWidth);
                    }
                }
            }

            // End MPF rendering (finish OpenGL commands, but don't swap buffers yet)
            // SwapBuffers will be called after text overlays are drawn
            NativeRenderer.EndMPFRender();
        }
        
        /// <summary>
        /// Draw text overlays for part and contour numbers
        /// </summary>
        private void DrawTextOverlays(Graphics g)
        {
            if (currentProgram == null) return;
            
            RenderSettings settings = RenderSettings.Instance;
            
            // Calculate world-to-screen transformation
            float aspect = (float)renderPanel.Width / (float)renderPanel.Height;
            float viewWidth = 2.0f / zoom;
            float viewHeight = viewWidth / aspect;
            
            float worldLeft = -viewWidth / 2 + panX;
            float worldRight = viewWidth / 2 + panX;
            float worldBottom = -viewHeight / 2 + panY;
            float worldTop = viewHeight / 2 + panY;
            
            int partIndex = 0;
            foreach (Part part in currentProgram.Parts)
            {
                float originX = (float)(part.Origin.X * workpieceScale);
                float originY = (float)(part.Origin.Y * workpieceScale);
                
                // Draw part number
                if (settings.ShowPartNumbers)
                {
                    // Phase 5.4: Check for custom position
                    GeometryUtils.Point2D? customPos = numberPositionManager?.GetPartPosition(partIndex);
                    
                    float worldX, worldY;
                    if (customPos.HasValue)
                    {
                        // Use custom position
                        worldX = (float)customPos.Value.X;
                        worldY = (float)customPos.Value.Y;
                    }
                    else
                    {
                        // Use default position (part origin)
                        worldX = originX;
                        worldY = originY;
                    }
                    
                    // Convert world coordinates to screen coordinates
                    float screenX = (worldX - worldLeft) / (worldRight - worldLeft) * renderPanel.Width;
                    float screenY = (worldTop - worldY) / (worldTop - worldBottom) * renderPanel.Height;
                    
                    using (Font font = new Font("Arial", settings.PartNumberSize, FontStyle.Bold))
                    using (SolidBrush brush = new SolidBrush(settings.PartNumberColor))
                    {
                        string text = (partIndex + 1).ToString(); // 1-based index
                        SizeF textSize = g.MeasureString(text, font);
                        g.DrawString(text, font, brush, screenX - textSize.Width / 2, screenY - textSize.Height / 2);
                    }
                }
                
                // Draw contour numbers
                if (settings.ShowContourNumbers)
                {
                    int contourIndex = 0;
                    foreach (Contour contour in part.Contours)
                    {
                        // Phase 5.4: Check for custom position
                        GeometryUtils.Point2D? customPos = numberPositionManager?.GetContourPosition(partIndex, contourIndex);
                        
                        float worldX, worldY;
                        if (customPos.HasValue)
                        {
                            // Use custom position
                            worldX = (float)customPos.Value.X;
                            worldY = (float)customPos.Value.Y;
                        }
                        else
                        {
                            // Use default position (piercing position)
                            worldX = (float)(contour.PiercingPosition.X * workpieceScale) + originX;
                            worldY = (float)(contour.PiercingPosition.Y * workpieceScale) + originY;
                        }
                        
                        // Convert world coordinates to screen coordinates
                        float screenX = (worldX - worldLeft) / (worldRight - worldLeft) * renderPanel.Width;
                        float screenY = (worldTop - worldY) / (worldTop - worldBottom) * renderPanel.Height;
                        
                        using (Font font = new Font("Arial", settings.ContourNumberSize, FontStyle.Regular))
                        using (SolidBrush brush = new SolidBrush(settings.ContourNumberColor))
                        {
                            string text = (contourIndex + 1).ToString(); // 1-based index
                            SizeF textSize = g.MeasureString(text, font);
                            // Phase 5.4: Center text at position (custom positions are centered, default has small offset)
                            if (customPos.HasValue)
                            {
                                g.DrawString(text, font, brush, screenX - textSize.Width / 2, screenY - textSize.Height / 2);
                            }
                            else
                            {
                                // Default: offset slightly to avoid overlapping with piercing point
                                g.DrawString(text, font, brush, screenX + 5, screenY - textSize.Height / 2);
                            }
                        }
                        
                        contourIndex++;
                    }
                }
                
                partIndex++;
            }
        }
        
        /// <summary>
        /// Override Invalidate to update render panel
        /// </summary>
        public new void Invalidate()
        {
            base.Invalidate();
            if (renderPanel != null)
                renderPanel.Invalidate();
        }

        private void RenderPanel_MouseDown(object sender, MouseEventArgs e)
        {
            // Phase 5.4: Handle number positioning mode first (highest priority)
            if (e.Button == MouseButtons.Left && numberPositionManager != null && numberPositionManager.IsPositioningMode)
            {
                HandleNumberPositioning(e.Location);
                return; // Don't start panning or selection
            }

            // Phase 5: Handle selection modes
            if (e.Button == MouseButtons.Left && selectionManager != null)
            {
                if (selectionManager.CurrentMode == SelectionManager.SelectionMode.Contour)
                {
                    // Contour selection mode
                    HandleContourSelection(e.Location);
                    return; // Don't start panning
                }
                else if (selectionManager.CurrentMode == SelectionManager.SelectionMode.Element)
                {
                    // Element selection mode
                    bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                    HandleElementSelection(e.Location, isCtrlPressed);
                    return; // Don't start panning
                }
            }

            // Phase4: Left click also enables panning (only if not in selection mode)
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
            {
                isPanning = true;
                lastMousePos = e.Location;
                renderPanel.Cursor = Cursors.Hand;
            }
        }

        private void RenderPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                // Phase4: Inverted pan direction for intuitive mouse control
                float dx = -(e.X - lastMousePos.X) / (float)renderPanel.Width * 2.0f / zoom;
                float dy = (e.Y - lastMousePos.Y) / (float)renderPanel.Height * 2.0f / zoom;
                
                panX += dx;
                panY += dy;
                
                lastMousePos = e.Location;
                UpdateViewTransform();
            }
        }

        private void RenderPanel_MouseUp(object sender, MouseEventArgs e)
        {
            // Phase4: Left click also enables panning
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
            {
                isPanning = false;
                renderPanel.Cursor = Cursors.Default;
            }
        }

        private void RenderPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            // Reversed zoom direction: scroll up = zoom in, scroll down = zoom out
            float zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            zoom *= zoomFactor;
            
            // Phase 7: Increased max zoom limit to 30000.0 for extreme detail
            if (zoom < 0.1f) zoom = 0.1f;
            if (zoom > 30000.0f) zoom = 30000.0f;
            
            UpdateViewTransform();
        }

        #region Phase 5: Selection Methods

        /// <summary>
        /// Convert screen coordinates to object space coordinates (matches glOrtho projection)
        /// </summary>
        private GeometryUtils.Point2D ScreenToObject(Point screenPos)
        {
            if (renderPanel == null)
                return new GeometryUtils.Point2D(0, 0);

            // Normalize to [-1, 1] range (NDC)
            float ndcX = (screenPos.X / (float)renderPanel.Width) * 2.0f - 1.0f;
            float ndcY = -((screenPos.Y / (float)renderPanel.Height) * 2.0f - 1.0f); // Flip Y

            // Apply inverse projection (NDC → Object Space)
            // Matches glOrtho: objectX = ndcX * (viewWidth/2) + panX
            //                         = ndcX / zoom + panX
            float objectX = (float)(ndcX / zoom + panX);
            float objectY = (float)(ndcY / zoom + panY);

            // Debug log
            Log($"ScreenToObject: Screen({screenPos.X},{screenPos.Y}) → NDC({ndcX:F3},{ndcY:F3}) → Object({objectX:F3},{objectY:F3})");
            Log($"  Zoom: {zoom:F3}, Pan: ({panX:F3},{panY:F3}), Scale: {workpieceScale:F3}");

            return new GeometryUtils.Point2D(objectX, objectY);
        }



        /// <summary>
        /// Handle contour selection at screen position
        /// </summary>
        private void HandleContourSelection(Point screenPos)
        {
            if (currentProgram == null || currentProgram.Parts == null || currentProgram.Parts.Count == 0)
                return;

            // Convert screen → object space (direct conversion)
            GeometryUtils.Point2D objectPos = ScreenToObject(screenPos);

            // Calculate part offsets (in object space)
            (float X, float Y)[] partOffsets = CalculatePartOffsets();

            // Debug: log coordinates and part offsets
            Log($"Click position: Screen({screenPos.X},{screenPos.Y}) → Object({objectPos.X:F3},{objectPos.Y:F3})");
            for (int i = 0; i < partOffsets.Length; i++)
            {
                var part = currentProgram.Parts[i];
                Log($"  Part[{i}]: Origin({part.Origin.X:F2},{part.Origin.Y:F2}mm), Offset({partOffsets[i].X:F2},{partOffsets[i].Y:F2}), Size({part.Width:F2}x{part.Height:F2}mm)");
            }

            // Phase 6: Find all overlapping contours
            var allContours = selectionManager.FindAllContoursAtPoint(objectPos, currentProgram.Parts, partOffsets, workpieceScale);

            if (allContours.Count == 0)
            {
                selectionManager.ClearContourSelection();
                Log("✗ No contour found at click position");
            }
            else if (allContours.Count >= 1)
            {
                // Phase 7: 자동으로 첫 번째(정렬된) 컨투어 선택
                // 정렬 규칙: 1) 작은 면적 우선, 2) 같은 영역이면 큰 번호 우선
                selectionManager.SelectContour(allContours[0].partIndex, allContours[0].contourIndex);
                Log($"✓ Auto-selected: Part {allContours[0].partIndex + 1}, Contour {allContours[0].contourIndex + 1} (from {allContours.Count} overlapping)");
                
                // 명시적으로 다시 그리기 (색상 변경 적용)
                Invalidate();
            }
        }

        /// <summary>
        /// Show context menu for selecting from multiple overlapping contours
        /// </summary>
        private void ShowContourSelectionMenu(Point screenPos, List<(int partIndex, int contourIndex, double area)> contours)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripLabel($"겹치는 컨투어 {contours.Count}개 발견:") { Font = new Font(menu.Font, FontStyle.Bold) });
            menu.Items.Add(new ToolStripSeparator());

            // Add menu items for each contour
            for (int i = 0; i < contours.Count; i++)
            {
                var contour = contours[i];
                int displayPartIndex = contour.partIndex + 1;  // 1-based
                int displayContourIndex = contour.contourIndex + 1;  // 1-based
                
                string itemText = $"Part {displayPartIndex}, Contour {displayContourIndex} (면적: {contour.area:F2})";
                ToolStripMenuItem item = new ToolStripMenuItem(itemText);
                
                // Capture variables for closure
                int capturedPartIndex = contour.partIndex;
                int capturedContourIndex = contour.contourIndex;
                
                item.Click += (sender, e) =>
                {
                    selectionManager.SelectContour(capturedPartIndex, capturedContourIndex);
                    Log($"✓ Selected: Part {capturedPartIndex + 1}, Contour {capturedContourIndex + 1}");
                };
                
                menu.Items.Add(item);
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("취소") { Font = new Font(menu.Font, FontStyle.Italic) });

            // Show menu at click position
            menu.Show(renderPanel, screenPos);
        }

        /// <summary>
        /// Handle element selection at screen position
        /// </summary>
        private void HandleElementSelection(Point screenPos, bool addToSelection)
        {
            if (currentProgram == null || currentProgram.Parts == null || currentProgram.Parts.Count == 0)
                return;

            // Convert screen → object space (direct conversion)
            GeometryUtils.Point2D objectPos = ScreenToObject(screenPos);

            // Calculate part offsets (in object space)
            (float X, float Y)[] partOffsets = CalculatePartOffsets();

            // Find element at click position (using object space coordinates)
            var result = selectionManager.FindElementAtPoint(objectPos, currentProgram.Parts, partOffsets, zoom, workpieceScale);

            if (result.HasValue)
            {
                selectionManager.SelectElement(result.Value.Item1, result.Value.Item2, result.Value.Item3, addToSelection);
                Log($"✓ Selected: Part {result.Value.Item1}, Contour {result.Value.Item2}, Element {result.Value.Item3}");
            }
            else if (!addToSelection)
            {
                selectionManager.ClearElementSelection();
                Log("✗ No element found at click position");
            }
        }

        /// <summary>
        /// Calculate part offsets for rendering
        /// </summary>
        private (float X, float Y)[] CalculatePartOffsets()
        {
            if (currentProgram == null || currentProgram.Parts == null)
                return new (float, float)[0];

            int partCount = currentProgram.Parts.Count;
            (float X, float Y)[] offsets = new (float X, float Y)[partCount];

            // Phase 5: Part offsets should match DrawPart's originX/originY
            // DrawPart uses: originX = part.Origin.X * workpieceScale
            // So we need to return the same value for selection to work
            for (int i = 0; i < partCount; i++)
            {
                Part part = currentProgram.Parts[i];
                if (part != null)
                {
                    // Use part.Origin (from HKOST) scaled to OpenGL coordinates
                    float originX = (float)(part.Origin.X * workpieceScale);
                    float originY = (float)(part.Origin.Y * workpieceScale);
                    offsets[i] = (originX, originY);
                }
                else
                {
                    offsets[i] = (0, 0);
                }
            }

            return offsets;
        }

        /// <summary>
        /// Public method to set selection mode
        /// </summary>
        public void SetSelectionMode(SelectionManager.SelectionMode mode)
        {
            if (selectionManager != null)
            {
                selectionManager.CurrentMode = mode;
                
                // Clear selections when changing mode
                if (mode == SelectionManager.SelectionMode.None)
                {
                    selectionManager.ClearAllSelections();
                }
                
                // Update cursor
                UpdateCursor();
                Log($"Selection mode: {mode}");
            }
        }

        /// <summary>
        /// Public method to set multi-select mode
        /// </summary>
        public void SetMultiSelectEnabled(bool enabled)
        {
            if (selectionManager != null)
            {
                selectionManager.IsMultiSelectEnabled = enabled;
                Log($"Multi-select: {(enabled ? "Enabled" : "Disabled")}");
            }
        }

        /// <summary>
        /// Phase 6: Public method to set contour selection method
        /// </summary>
        public void SetContourSelectionMethod(SelectionManager.ContourSelectionMethod method)
        {
            if (selectionManager != null)
            {
                selectionManager.SelectionMethod = method;
                Log($"Contour selection method: {method}");
            }
        }

        /// <summary>
        /// Phase 5 Debug: Toggle selection area visualization
        /// </summary>
        public void SetShowSelectionAreas(bool show)
        {
            showSelectionAreas = show;
            Log($"Show Selection Areas: {(show ? "ON" : "OFF")}");
        }

        /// <summary>
        /// Get current selection manager (for external access)
        /// </summary>
        public SelectionManager GetSelectionManager()
        {
            return selectionManager;
        }

        /// <summary>
        /// Get current program (for external access)
        /// </summary>
        public MPFProgram GetCurrentProgram()
        {
            return currentProgram;
        }

        /// <summary>
        /// Get cutting progress manager (for external access)
        /// Phase 7: Real-time trace system integration
        /// </summary>
        public CuttingProgressManager GetCuttingProgressManager()
        {
            return cuttingProgressManager;
        }

        /// <summary>
        /// Update cursor based on current mode
        /// </summary>
        private void UpdateCursor()
        {
            if (renderPanel == null)
                return;

            if (!isPanning)
            {
                // Phase 5.4: Number positioning mode has highest priority
                if (numberPositionManager != null && numberPositionManager.IsPositioningMode)
                {
                    renderPanel.Cursor = Cursors.Cross;
                    return;
                }

                // Phase 5.2: Selection modes
                if (selectionManager != null)
                {
                    switch (selectionManager.CurrentMode)
                    {
                        case SelectionManager.SelectionMode.Contour:
                        case SelectionManager.SelectionMode.Element:
                            renderPanel.Cursor = Cursors.Cross;
                            break;
                        default:
                            renderPanel.Cursor = Cursors.Default;
                            break;
                    }
                }
                else
                {
                    renderPanel.Cursor = Cursors.Default;
                }
            }
        }

        #endregion

        #region Phase 5.4: Number Positioning Methods

        /// <summary>
        /// Enable number positioning mode
        /// </summary>
        public void EnableNumberPositioning(NumberPositionManager.NumberType type)
        {
            if (numberPositionManager != null)
            {
                numberPositionManager.EnablePositioningMode(type);
                UpdateCursor();
                Log($"Number positioning mode enabled: {type}");
            }
        }

        /// <summary>
        /// Disable number positioning mode
        /// </summary>
        public void DisableNumberPositioning()
        {
            if (numberPositionManager != null)
            {
                numberPositionManager.DisablePositioningMode();
                UpdateCursor();
                Log("Number positioning mode disabled");
            }
        }

        /// <summary>
        /// Handle number positioning click
        /// </summary>
        private void HandleNumberPositioning(Point screenPos)
        {
            if (currentProgram == null || currentProgram.Parts == null || currentProgram.Parts.Count == 0)
                return;

            if (numberPositionManager == null || !numberPositionManager.IsPositioningMode)
                return;

            GeometryUtils.Point2D objectPos = ScreenToObject(screenPos);
            (float X, float Y)[] partOffsets = CalculatePartOffsets();

            if (numberPositionManager.PositioningTarget == NumberPositionManager.NumberType.Part)
            {
                // Find which part was clicked (using bounding box)
                for (int pi = 0; pi < currentProgram.Parts.Count; pi++)
                {
                    Part part = currentProgram.Parts[pi];
                    float offsetX = partOffsets[pi].X;
                    float offsetY = partOffsets[pi].Y;

                    // Phase 5: Use Part.Width/Height from HKSTR
                    float partWidth = (float)(part.Width * workpieceScale);
                    float partHeight = (float)(part.Height * workpieceScale);

                    // Check if click is inside bounding box
                    if (objectPos.X >= offsetX && objectPos.X <= offsetX + partWidth &&
                        objectPos.Y >= offsetY && objectPos.Y <= offsetY + partHeight)
                    {
                        numberPositionManager.SetPosition(pi, null, objectPos);
                        Log($"Part {pi} number position set to: {objectPos}");
                        numberPositionManager.DisablePositioningMode();
                        needsRedraw = true;
                        return;
                    }
                }

                Log("No part found at click position");
            }
            else if (numberPositionManager.PositioningTarget == NumberPositionManager.NumberType.Contour)
            {
                // Find which contour was clicked (using Point-in-Polygon)
                var result = selectionManager?.FindContourAtPoint(objectPos, currentProgram.Parts, partOffsets);

                if (result.HasValue)
                {
                    numberPositionManager.SetPosition(result.Value.Item1, result.Value.Item2, objectPos);
                    Log($"Contour ({result.Value.Item1}, {result.Value.Item2}) number position set to: {objectPos}");
                    numberPositionManager.DisablePositioningMode();
                    needsRedraw = true;
                }
                else
                {
                    Log("No contour found at click position");
                }
            }
        }

        /// <summary>
        /// Save number positions to file
        /// </summary>
        public void SaveNumberPositions(string filePath)
        {
            if (numberPositionManager != null)
            {
                numberPositionManager.SavePositions(filePath);
            }
        }

        /// <summary>
        /// Load number positions from file
        /// </summary>
        public void LoadNumberPositions(string filePath)
        {
            if (numberPositionManager != null)
            {
                numberPositionManager.LoadPositions(filePath);
                needsRedraw = true;
            }
        }

        /// <summary>
        /// Get number position manager (for external access)
        /// </summary>
        public NumberPositionManager GetNumberPositionManager()
        {
            return numberPositionManager;
        }

        #endregion

        #region Debug Logging

        /// <summary>
        /// 디버그 로그 메시지 출력
        /// </summary>
        private void Log(string message)
        {
            // Output to MainForm's log window via event
            LogMessage?.Invoke(this, message);
            
            // Also keep Debug output for development/debugging
            System.Diagnostics.Debug.WriteLine($"[CamViewerControl] {message}");
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (isInitialized)
                {
                    NativeRenderer.CleanupRenderer();
                    isInitialized = false;
                }
            }
            base.Dispose(disposing);
        }
    }
}
