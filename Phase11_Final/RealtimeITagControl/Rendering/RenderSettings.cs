using System;
using System.Drawing;

namespace RealtimeITagControl.Rendering
{
    /// <summary>
    /// Phase 5.3: Dash pattern types for part boundaries
    /// </summary>
    public enum DashPattern
    {
        Solid = 0,      // ________ (solid line)
        Dash1 = 1,      // ---- ---- (long dash)
        Dash2 = 2,      // --- --- (medium dash)
        Dash3 = 3,      // -- -- (short dash)
        DotDash = 4,    // .-.-.-. (dot-dash)
        DotDotDash = 5  // ..-..-..- (dot-dot-dash)
    }

    /// <summary>
    /// Phase 5.5: Canvas orientation types
    /// </summary>
    public enum CanvasOrientation
    {
        Normal = 0,        // 0° (no rotation)
        Rotate90CW = 1,    // 90° clockwise
        Rotate180 = 2,     // 180°
        Rotate270CW = 3    // 270° clockwise (90° counter-clockwise)
    }

    /// <summary>
    /// Global rendering settings for colors and sizes
    /// </summary>
    public class RenderSettings
    {
        private static RenderSettings instance = null;
        private static readonly object lockObj = new object();

        // Singleton pattern
        public static RenderSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObj)
                    {
                        if (instance == null)
                        {
                            instance = new RenderSettings();
                        }
                    }
                }
                return instance;
            }
        }

        private RenderSettings()
        {
            // Initialize with default values
            ResetToDefaults();
        }

        // Phase 8.1: 라벨 렌더링 설정은 RenderSettings에 통합됨
        // 폰트 사이즈: PartNumberSize, ContourNumberSize 사용

        // ========================================
        // Color Settings
        // ========================================

        /// <summary>Workpiece boundary color (external line)</summary>
        public Color WorkpieceBoundaryColor { get; set; }

        /// <summary>Workpiece interior area color (inside boundary)</summary>
        public Color WorkpieceInteriorColor { get; set; }

        /// <summary>Workpiece exterior area color (outside boundary)</summary>
        public Color WorkpieceExteriorColor { get; set; }

        /// <summary>Part origin point color (currently disabled)</summary>
        public Color PartOriginColor { get; set; }

        /// <summary>Piercing point color</summary>
        public Color PiercingPointColor { get; set; }

        /// <summary>Lead-in path color</summary>
        public Color LeadInColor { get; set; }

        /// <summary>Cutting path color (completed)</summary>
        public Color CuttingCompletedColor { get; set; }

        /// <summary>Cutting path color (in progress - simulation)</summary>
        public Color CuttingInProgressColor { get; set; }

        /// <summary>Cutting path color (pending - preview / uncut)</summary>
        public Color CuttingPendingColor { get; set; }

        /// <summary>Part number text color</summary>
        public Color PartNumberColor { get; set; }

        /// <summary>Contour number text color</summary>
        public Color ContourNumberColor { get; set; }

        /// <summary>Marking path color (CuttingType == 10)</summary>
        public Color MarkingColor { get; set; }

        // ========================================
        // Size Settings (in pixels)
        // ========================================

        /// <summary>Workpiece boundary line width</summary>
        public float WorkpieceBoundaryWidth { get; set; }

        /// <summary>Part origin point size</summary>
        public float PartOriginSize { get; set; }

        /// <summary>Piercing point size</summary>
        public float PiercingPointSize { get; set; }

        /// <summary>Lead-in path line width</summary>
        public float LeadInWidth { get; set; }

        /// <summary>Cutting path line width (completed)</summary>
        public float CuttingCompletedWidth { get; set; }

        /// <summary>Cutting path line width (in progress)</summary>
        public float CuttingInProgressWidth { get; set; }

        /// <summary>Cutting path line width (pending)</summary>
        public float CuttingPendingWidth { get; set; }

        /// <summary>Part number text size (font size in points)</summary>
        public float PartNumberSize { get; set; }

        /// <summary>Contour number text size (font size in points)</summary>
        public float ContourNumberSize { get; set; }

        // ========================================
        // Visibility Settings
        // ========================================

        /// <summary>Show/hide part origin points</summary>
        public bool ShowPartOrigin { get; set; }

        /// <summary>Show/hide workpiece boundary</summary>
        public bool ShowWorkpieceBoundary { get; set; }

        /// <summary>Show/hide part numbers</summary>
        public bool ShowPartNumbers { get; set; }

        /// <summary>Show/hide contour numbers</summary>
        public bool ShowContourNumbers { get; set; }

        // Phase 5.3: Part boundary settings
        /// <summary>Show/hide part boundaries (dashed rectangles)</summary>
        public bool ShowPartBoundaries { get; set; }

        /// <summary>Part boundary color</summary>
        public Color PartBoundaryColor { get; set; }

        /// <summary>Part boundary line width</summary>
        public float PartBoundaryWidth { get; set; }

        /// <summary>Part boundary dash pattern (0-5)</summary>
        public DashPattern PartBoundaryDashPattern { get; set; }

        // Phase 5.5: Canvas orientation
        /// <summary>Canvas orientation (rotation)</summary>
        public CanvasOrientation Orientation { get; set; }

        // ========================================
        // View Settings
        // ========================================

        /// <summary>Initial zoom multiplier when loading MPF (0.001 ~ 0.1)</summary>
        public float InitialZoomMultiplier { get; set; }

        // ========================================
        // Methods
        // ========================================

        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        public void ResetToDefaults()
        {
            // Default colors
            WorkpieceBoundaryColor = Color.FromArgb(100, 100, 100); // Dark Gray (hidden by default)
            WorkpieceInteriorColor = Color.FromArgb(0, 64, 64);     // Dark Cyan (HKCamInterface style)
            WorkpieceExteriorColor = Color.FromArgb(0, 0, 0);       // Pure Black
            PartOriginColor = Color.FromArgb(0, 255, 0);            // Green
            PiercingPointColor = Color.FromArgb(255, 0, 0);         // Red
            LeadInColor = Color.FromArgb(255, 255, 0);              // Yellow
            CuttingCompletedColor = Color.FromArgb(0, 255, 255);    // Cyan
            CuttingInProgressColor = Color.FromArgb(255, 0, 0);     // Red
            CuttingPendingColor = Color.FromArgb(80, 80, 80);       // Gray (uncut state)
            PartNumberColor = Color.FromArgb(255, 255, 255);        // White
            ContourNumberColor = Color.FromArgb(200, 200, 200);     // Light Gray
            MarkingColor = Color.FromArgb(255, 255, 0);             // Yellow (for marking paths)

            // Default sizes
            WorkpieceBoundaryWidth = 2.0f;
            PartOriginSize = 8.0f;
            PiercingPointSize = 2.0f;  // Changed to 2
            LeadInWidth = 1.0f;        // Changed to 1
            CuttingCompletedWidth = 1.0f;  // Changed to 1
            CuttingInProgressWidth = 1.0f; // Changed to 1
            CuttingPendingWidth = 1.0f;
            PartNumberSize = 12.0f;    // Default font size for part numbers
            ContourNumberSize = 10.0f; // Default font size for contour numbers

            // Default visibility
            ShowPartOrigin = false; // Disabled by default (Phase4 requirement)
            ShowWorkpieceBoundary = false; // Hide workpiece boundary (external line)
            ShowPartNumbers = false; // Hide part numbers by default
            ShowContourNumbers = false; // Hide contour numbers by default

            // Phase 5.3: Part boundary defaults
            ShowPartBoundaries = false; // Hide by default (shown when part numbers are visible)
            PartBoundaryColor = Color.FromArgb(100, 200, 255); // Light Blue
            PartBoundaryWidth = 1.5f;
            PartBoundaryDashPattern = DashPattern.Dash3; // Short dash (fixed)

            // Phase 5.5: Canvas orientation default
            Orientation = CanvasOrientation.Normal; // No rotation by default

            // Default view settings
            InitialZoomMultiplier = 0.005f; // Moderate initial zoom (adjustable: 0.001 ~ 0.1)
        }

        /// <summary>
        /// Get color as normalized float array (R, G, B)
        /// </summary>
        public float[] GetColorAsFloat(Color color)
        {
            return new float[] { color.R / 255.0f, color.G / 255.0f, color.B / 255.0f };
        }
        
        /// <summary>
        /// Save settings to file (simple JSON format)
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"Colors\": {");
                sb.AppendLine(string.Format("    \"WorkpieceBoundaryColor\": \"{0}\",", ColorToHex(WorkpieceBoundaryColor)));
                sb.AppendLine(string.Format("    \"WorkpieceInteriorColor\": \"{0}\",", ColorToHex(WorkpieceInteriorColor)));
                sb.AppendLine(string.Format("    \"WorkpieceExteriorColor\": \"{0}\",", ColorToHex(WorkpieceExteriorColor)));
                sb.AppendLine(string.Format("    \"PartOriginColor\": \"{0}\",", ColorToHex(PartOriginColor)));
                sb.AppendLine(string.Format("    \"PiercingPointColor\": \"{0}\",", ColorToHex(PiercingPointColor)));
                sb.AppendLine(string.Format("    \"LeadInColor\": \"{0}\",", ColorToHex(LeadInColor)));
                sb.AppendLine(string.Format("    \"CuttingCompletedColor\": \"{0}\",", ColorToHex(CuttingCompletedColor)));
                sb.AppendLine(string.Format("    \"CuttingInProgressColor\": \"{0}\",", ColorToHex(CuttingInProgressColor)));
                sb.AppendLine(string.Format("    \"CuttingPendingColor\": \"{0}\",", ColorToHex(CuttingPendingColor)));
                sb.AppendLine(string.Format("    \"PartNumberColor\": \"{0}\",", ColorToHex(PartNumberColor)));
                sb.AppendLine(string.Format("    \"ContourNumberColor\": \"{0}\",", ColorToHex(ContourNumberColor)));
                sb.AppendLine(string.Format("    \"MarkingColor\": \"{0}\"", ColorToHex(MarkingColor)));
                sb.AppendLine("  },");
                sb.AppendLine("  \"Sizes\": {");
                sb.AppendLine(string.Format("    \"WorkpieceBoundaryWidth\": {0},", WorkpieceBoundaryWidth));
                sb.AppendLine(string.Format("    \"PartOriginSize\": {0},", PartOriginSize));
                sb.AppendLine(string.Format("    \"PiercingPointSize\": {0},", PiercingPointSize));
                sb.AppendLine(string.Format("    \"LeadInWidth\": {0},", LeadInWidth));
                sb.AppendLine(string.Format("    \"CuttingCompletedWidth\": {0},", CuttingCompletedWidth));
                sb.AppendLine(string.Format("    \"CuttingInProgressWidth\": {0},", CuttingInProgressWidth));
                sb.AppendLine(string.Format("    \"CuttingPendingWidth\": {0},", CuttingPendingWidth));
                sb.AppendLine(string.Format("    \"PartNumberSize\": {0},", PartNumberSize));
                sb.AppendLine(string.Format("    \"ContourNumberSize\": {0}", ContourNumberSize));
                sb.AppendLine("  },");
                sb.AppendLine("  \"Visibility\": {");
                sb.AppendLine(string.Format("    \"ShowPartOrigin\": {0},", ShowPartOrigin.ToString().ToLower()));
                sb.AppendLine(string.Format("    \"ShowWorkpieceBoundary\": {0},", ShowWorkpieceBoundary.ToString().ToLower()));
                sb.AppendLine(string.Format("    \"ShowPartNumbers\": {0},", ShowPartNumbers.ToString().ToLower()));
                sb.AppendLine(string.Format("    \"ShowContourNumbers\": {0}", ShowContourNumbers.ToString().ToLower()));
                sb.AppendLine("  },");
                sb.AppendLine("  \"View\": {");
                sb.AppendLine(string.Format("    \"InitialZoomMultiplier\": {0}", InitialZoomMultiplier));
                sb.AppendLine("  }");
                sb.AppendLine("}");
                
                System.IO.File.WriteAllText(filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to save settings: " + ex.Message);
            }
        }
        
        /// <summary>
        /// Load settings from file (simple JSON parsing)
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return;
                
                string json = System.IO.File.ReadAllText(filePath);
                string[] lines = json.Split('\n');
                
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Contains(":"))
                    {
                        string[] parts = trimmed.Split(':');
                        if (parts.Length >= 2)
                        {
                            string key = parts[0].Trim().Replace("\"", "").Replace(",", "");
                            string value = parts[1].Trim().Replace("\"", "").Replace(",", "");
                            
                            // Parse colors
                            if (key == "WorkpieceBoundaryColor") WorkpieceBoundaryColor = HexToColor(value);
                            else if (key == "WorkpieceInteriorColor") WorkpieceInteriorColor = HexToColor(value);
                            else if (key == "WorkpieceExteriorColor") WorkpieceExteriorColor = HexToColor(value);
                            else if (key == "PartOriginColor") PartOriginColor = HexToColor(value);
                            else if (key == "PiercingPointColor") PiercingPointColor = HexToColor(value);
                            else if (key == "LeadInColor") LeadInColor = HexToColor(value);
                            else if (key == "CuttingCompletedColor") CuttingCompletedColor = HexToColor(value);
                            else if (key == "CuttingInProgressColor") CuttingInProgressColor = HexToColor(value);
                            else if (key == "CuttingPendingColor") CuttingPendingColor = HexToColor(value);
                            else if (key == "PartNumberColor") PartNumberColor = HexToColor(value);
                            else if (key == "ContourNumberColor") ContourNumberColor = HexToColor(value);
                            else if (key == "MarkingColor") MarkingColor = HexToColor(value);
                            // Parse sizes
                            else if (key == "WorkpieceBoundaryWidth") WorkpieceBoundaryWidth = float.Parse(value);
                            else if (key == "PartOriginSize") PartOriginSize = float.Parse(value);
                            else if (key == "PiercingPointSize") PiercingPointSize = float.Parse(value);
                            else if (key == "LeadInWidth") LeadInWidth = float.Parse(value);
                            else if (key == "CuttingCompletedWidth") CuttingCompletedWidth = float.Parse(value);
                            else if (key == "CuttingInProgressWidth") CuttingInProgressWidth = float.Parse(value);
                            else if (key == "CuttingPendingWidth") CuttingPendingWidth = float.Parse(value);
                            else if (key == "PartNumberSize") PartNumberSize = float.Parse(value);
                            else if (key == "ContourNumberSize") ContourNumberSize = float.Parse(value);
                            // Parse visibility
                            else if (key == "ShowPartOrigin") ShowPartOrigin = bool.Parse(value);
                            else if (key == "ShowWorkpieceBoundary") ShowWorkpieceBoundary = bool.Parse(value);
                            else if (key == "ShowPartNumbers") ShowPartNumbers = bool.Parse(value);
                            else if (key == "ShowContourNumbers") ShowContourNumbers = bool.Parse(value);
                            // Parse view settings
                            else if (key == "InitialZoomMultiplier") InitialZoomMultiplier = float.Parse(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load settings: " + ex.Message);
            }
        }
        
        private string ColorToHex(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }
        
        private Color HexToColor(string hex)
        {
            hex = hex.Replace("#", "");
            int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return Color.FromArgb(r, g, b);
        }
    }
}
