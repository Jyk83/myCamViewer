using System;
using System.Collections.Generic;
using CamViewerPOC.MPF;

namespace CamViewerPOC.Trace
{
    /// <summary>
    /// Manages real-time cutting progress tracking
    /// Based on HKCamInterface OpenGLCAMViewWnd cutting progress system
    /// </summary>
    public class CuttingProgressManager
    {
        // Cutting progress state
        private bool isUnderCuttingProgress = false;
        private bool hasCuttingProgress = false;
        private bool isReverse = false;

        // Position tracking (0-based indices internally, 1-based for external API)
        private int startPartIndex = -1;
        private int startContourIndex = -1;
        private int lastPartIndex = -1;
        private int lastContourIndex = -1;
        private int currentPartIndex = -1;
        private int currentContourIndex = -1;
        private int currentElementIndex = -1;

        // Progress tracking
        private double elementProgress = 0.0;
        private double totalCutDistance = 0.0;

        // Reference to MPF program data
        private MPFProgram program = null;

        /// <summary>
        /// Current cutting progress state
        /// </summary>
        public enum CuttingState
        {
            Idle,           // No cutting in progress
            InProgress,     // Cutting in progress
            Paused,         // Paused (future extension)
            Completed       // Cutting completed
        }

        public CuttingState State
        {
            get
            {
                if (!isUnderCuttingProgress && !hasCuttingProgress)
                    return CuttingState.Idle;
                else if (isUnderCuttingProgress)
                    return CuttingState.InProgress;
                else if (hasCuttingProgress)
                    return CuttingState.Completed;
                return CuttingState.Idle;
            }
        }

        // Properties (read-only access to state)
        public bool IsUnderCuttingProgress => isUnderCuttingProgress;
        public bool HasCuttingProgress => hasCuttingProgress;
        public int CurrentPartIndex => currentPartIndex;
        public int CurrentContourIndex => currentContourIndex;
        public int CurrentElementIndex => currentElementIndex;
        public double ElementProgress => elementProgress;
        public double TotalCutDistance => totalCutDistance;

        /// <summary>
        /// Event triggered when cutting progress is updated
        /// </summary>
        public event EventHandler<CuttingProgressEventArgs> ProgressUpdated;

        public CuttingProgressManager()
        {
        }

        /// <summary>
        /// Set MPF program reference
        /// </summary>
        public void SetProgram(MPFProgram mpfProgram)
        {
            program = mpfProgram;
        }

        /// <summary>
        /// Start cutting progress tracking
        /// </summary>
        /// <param name="partNumber">Part number (1-based)</param>
        /// <param name="contourNumber">Contour number (1-based)</param>
        /// <param name="reverse">Reverse cutting order</param>
        /// <returns>True if started successfully</returns>
        public bool StartCuttingProgress(int partNumber, int contourNumber, bool reverse = false)
        {
            if (program == null || program.Parts == null)
            {
                System.Diagnostics.Debug.WriteLine("[CuttingProgress] No program loaded");
                return false;
            }

            // Convert to 0-based indices
            int partIdx = partNumber - 1;
            int contourIdx = contourNumber - 1;

            // Validate indices
            if (partIdx < 0 || partIdx >= program.Parts.Count)
            {
                System.Diagnostics.Debug.WriteLine($"[CuttingProgress] Invalid part number: {partNumber}");
                return false;
            }

            if (contourIdx < 0 || contourIdx >= program.Parts[partIdx].Contours.Count)
            {
                System.Diagnostics.Debug.WriteLine($"[CuttingProgress] Invalid contour number: {contourNumber}");
                return false;
            }

            // Initialize state
            startPartIndex = partIdx;
            startContourIndex = contourIdx;
            currentPartIndex = partIdx;
            currentContourIndex = contourIdx;
            currentElementIndex = 0;
            lastPartIndex = -1;
            lastContourIndex = -1;
            elementProgress = 0.0;
            totalCutDistance = 0.0;
            isReverse = reverse;
            isUnderCuttingProgress = true;
            hasCuttingProgress = false;

            System.Diagnostics.Debug.WriteLine($"[CuttingProgress] Started: Part {partNumber}, Contour {contourNumber}, Reverse={reverse}");
            
            OnProgressUpdated(new CuttingProgressEventArgs
            {
                PartIndex = currentPartIndex,
                ContourIndex = currentContourIndex,
                ElementIndex = currentElementIndex,
                Progress = elementProgress,
                State = CuttingState.InProgress
            });

            return true;
        }

        /// <summary>
        /// Update cutting progress with part, contour, element, and progress
        /// </summary>
        public bool UpdateProgress(int partNumber, int contourNumber, int elementIndex, double progress)
        {
            if (!isUnderCuttingProgress)
            {
                System.Diagnostics.Debug.WriteLine("[CuttingProgress] Not under cutting progress");
                return false;
            }

            // Convert to 0-based indices
            int partIdx = partNumber - 1;
            int contourIdx = contourNumber - 1;

            // Validate
            if (partIdx < 0 || partIdx >= program.Parts.Count ||
                contourIdx < 0 || contourIdx >= program.Parts[partIdx].Contours.Count)
            {
                return false;
            }

            Contour contour = program.Parts[partIdx].Contours[contourIdx];
            if (elementIndex < 0 || elementIndex >= contour.AllSegments.Count)
            {
                return false;
            }

            // Update state
            bool partChanged = (currentPartIndex != partIdx);
            bool contourChanged = (currentContourIndex != contourIdx);

            if (partChanged || contourChanged)
            {
                lastPartIndex = currentPartIndex;
                lastContourIndex = currentContourIndex;
            }

            currentPartIndex = partIdx;
            currentContourIndex = contourIdx;
            currentElementIndex = elementIndex;
            elementProgress = Math.Max(0.0, Math.Min(1.0, progress));
            hasCuttingProgress = true;

            // Calculate total cut distance (simplified)
            // TODO: Implement precise distance calculation
            totalCutDistance += GetElementLength(partIdx, contourIdx, elementIndex) * progress;

            System.Diagnostics.Debug.WriteLine($"[CuttingProgress] Update: P{partNumber} C{contourNumber} E{elementIndex} Prog={progress:F2}");

            OnProgressUpdated(new CuttingProgressEventArgs
            {
                PartIndex = currentPartIndex,
                ContourIndex = currentContourIndex,
                ElementIndex = currentElementIndex,
                Progress = elementProgress,
                State = CuttingState.InProgress,
                PartChanged = partChanged,
                ContourChanged = contourChanged
            });

            return true;
        }

        /// <summary>
        /// Update cutting progress with WCS coordinates
        /// Find matching element by coordinate proximity
        /// </summary>
        public bool UpdateProgressByCoordinate(int partNumber, int contourNumber, double x, double y)
        {
            // Convert to 0-based
            int partIdx = partNumber - 1;
            int contourIdx = contourNumber - 1;

            if (partIdx < 0 || partIdx >= program.Parts.Count ||
                contourIdx < 0 || contourIdx >= program.Parts[partIdx].Contours.Count)
            {
                return false;
            }

            Contour contour = program.Parts[partIdx].Contours[contourIdx];
            
            // Find closest element
            int closestElementIndex = -1;
            double closestDistance = double.MaxValue;
            double progressOnElement = 0.0;

            for (int i = 0; i < contour.AllSegments.Count; i++)
            {
                PathSegment segment = contour.AllSegments[i];
                double dist = DistanceToSegment(segment, x, y, out double t);

                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestElementIndex = i;
                    progressOnElement = t;
                }
            }

            if (closestElementIndex >= 0)
            {
                return UpdateProgress(partNumber, contourNumber, closestElementIndex, progressOnElement);
            }

            return false;
        }

        /// <summary>
        /// Stop cutting progress (pause)
        /// </summary>
        public void StopCuttingProgress()
        {
            isUnderCuttingProgress = false;
            System.Diagnostics.Debug.WriteLine("[CuttingProgress] Stopped");
            
            OnProgressUpdated(new CuttingProgressEventArgs
            {
                PartIndex = currentPartIndex,
                ContourIndex = currentContourIndex,
                ElementIndex = currentElementIndex,
                Progress = elementProgress,
                State = CuttingState.Paused
            });
        }

        /// <summary>
        /// Reset all cutting progress
        /// </summary>
        public void ResetCuttingProgress()
        {
            isUnderCuttingProgress = false;
            hasCuttingProgress = false;
            startPartIndex = -1;
            startContourIndex = -1;
            lastPartIndex = -1;
            lastContourIndex = -1;
            currentPartIndex = -1;
            currentContourIndex = -1;
            currentElementIndex = -1;
            elementProgress = 0.0;
            totalCutDistance = 0.0;

            System.Diagnostics.Debug.WriteLine("[CuttingProgress] Reset");
            
            OnProgressUpdated(new CuttingProgressEventArgs
            {
                State = CuttingState.Idle
            });
        }

        /// <summary>
        /// Complete last element (set progress to 1.0)
        /// </summary>
        public void CompleteLastElement()
        {
            if (!hasCuttingProgress || currentElementIndex < 0)
                return;

            elementProgress = 1.0;
            System.Diagnostics.Debug.WriteLine($"[CuttingProgress] Completed element: P{currentPartIndex+1} C{currentContourIndex+1} E{currentElementIndex}");
            
            OnProgressUpdated(new CuttingProgressEventArgs
            {
                PartIndex = currentPartIndex,
                ContourIndex = currentContourIndex,
                ElementIndex = currentElementIndex,
                Progress = 1.0,
                State = CuttingState.InProgress
            });
        }

        /// <summary>
        /// Complete entire contour
        /// </summary>
        public void CompleteLastContour()
        {
            if (!hasCuttingProgress || currentContourIndex < 0)
                return;

            Contour contour = program.Parts[currentPartIndex].Contours[currentContourIndex];
            currentElementIndex = contour.AllSegments.Count - 1;
            elementProgress = 1.0;

            System.Diagnostics.Debug.WriteLine($"[CuttingProgress] Completed contour: P{currentPartIndex+1} C{currentContourIndex+1}");
            
            OnProgressUpdated(new CuttingProgressEventArgs
            {
                PartIndex = currentPartIndex,
                ContourIndex = currentContourIndex,
                ElementIndex = currentElementIndex,
                Progress = 1.0,
                State = CuttingState.InProgress
            });
        }

        /// <summary>
        /// Get element length (simplified)
        /// </summary>
        private double GetElementLength(int partIdx, int contourIdx, int elementIdx)
        {
            try
            {
                Contour contour = program.Parts[partIdx].Contours[contourIdx];
                PathSegment segment = contour.AllSegments[elementIdx];

                if (segment is LineSegment line)
                {
                    double dx = line.End.X - line.Start.X;
                    double dy = line.End.Y - line.Start.Y;
                    return Math.Sqrt(dx * dx + dy * dy);
                }
                else if (segment is ArcSegment arc)
                {
                    // Approximate arc length
                    // Calculate sweep angle from StartAngle and EndAngle
                    double sweepAngle = arc.EndAngle - arc.StartAngle;
                    // Handle angle wrapping
                    if (arc.Clockwise)
                    {
                        if (sweepAngle > 0) sweepAngle -= 360.0;
                    }
                    else
                    {
                        if (sweepAngle < 0) sweepAngle += 360.0;
                    }
                    return Math.Abs(sweepAngle) * arc.Radius * Math.PI / 180.0;
                }
            }
            catch
            {
                return 0.0;
            }
            return 0.0;
        }

        /// <summary>
        /// Calculate distance from point to segment
        /// </summary>
        private double DistanceToSegment(PathSegment segment, double px, double py, out double t)
        {
            t = 0.0;

            if (segment is LineSegment line)
            {
                double x1 = line.Start.X;
                double y1 = line.Start.Y;
                double x2 = line.End.X;
                double y2 = line.End.Y;

                double dx = x2 - x1;
                double dy = y2 - y1;
                double len2 = dx * dx + dy * dy;

                if (len2 < 1e-10)
                {
                    t = 0.0;
                    return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
                }

                t = ((px - x1) * dx + (py - y1) * dy) / len2;
                t = Math.Max(0.0, Math.Min(1.0, t));

                double projX = x1 + t * dx;
                double projY = y1 + t * dy;

                return Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
            }
            else if (segment is ArcSegment arc)
            {
                // Simplified: distance to arc center
                double dx = px - arc.Center.X;
                double dy = py - arc.Center.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                return Math.Abs(dist - arc.Radius);
            }

            return double.MaxValue;
        }

        /// <summary>
        /// Check if a specific element is completed
        /// </summary>
        public bool IsElementCompleted(int partIdx, int contourIdx, int elementIdx)
        {
            if (!hasCuttingProgress)
                return false;

            // Before current position = completed
            if (partIdx < currentPartIndex)
                return true;
            if (partIdx == currentPartIndex && contourIdx < currentContourIndex)
                return true;
            if (partIdx == currentPartIndex && contourIdx == currentContourIndex && elementIdx < currentElementIndex)
                return true;

            // Current element with progress = 1.0 = completed
            if (partIdx == currentPartIndex && contourIdx == currentContourIndex && 
                elementIdx == currentElementIndex && elementProgress >= 1.0)
                return true;

            return false;
        }

        /// <summary>
        /// Check if a specific element is in progress
        /// </summary>
        public bool IsElementInProgress(int partIdx, int contourIdx, int elementIdx)
        {
            if (!isUnderCuttingProgress)
                return false;

            return (partIdx == currentPartIndex && contourIdx == currentContourIndex && 
                    elementIdx == currentElementIndex && elementProgress < 1.0);
        }

        /// <summary>
        /// Check if a specific element is pending (not started yet)
        /// </summary>
        public bool IsElementPending(int partIdx, int contourIdx, int elementIdx)
        {
            if (!hasCuttingProgress)
                return true; // Everything is pending if no progress

            // After current position = pending
            if (partIdx > currentPartIndex)
                return true;
            if (partIdx == currentPartIndex && contourIdx > currentContourIndex)
                return true;
            if (partIdx == currentPartIndex && contourIdx == currentContourIndex && elementIdx > currentElementIndex)
                return true;

            return false;
        }

        /// <summary>
        /// Trigger progress updated event
        /// </summary>
        protected virtual void OnProgressUpdated(CuttingProgressEventArgs e)
        {
            ProgressUpdated?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Event arguments for cutting progress updates
    /// </summary>
    public class CuttingProgressEventArgs : EventArgs
    {
        public int PartIndex { get; set; } = -1;
        public int ContourIndex { get; set; } = -1;
        public int ElementIndex { get; set; } = -1;
        public double Progress { get; set; } = 0.0;
        public CuttingProgressManager.CuttingState State { get; set; }
        public bool PartChanged { get; set; } = false;
        public bool ContourChanged { get; set; } = false;
    }
}
