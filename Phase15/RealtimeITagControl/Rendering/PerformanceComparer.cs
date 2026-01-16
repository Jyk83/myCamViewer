using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RealtimeITagControl.Rendering
{
    /// <summary>
    /// Phase 15.3: Automated performance comparison system
    /// TYPE=1 (Default) vs TYPE=2 (DirtyRegion) 성능 비교 및 CSV 리포트 생성
    /// </summary>
    public class PerformanceComparer
    {
        private List<PerformanceSnapshot> snapshots = new List<PerformanceSnapshot>();
        private DateTime testStartTime;
        private bool isRecording = false;
        private System.Windows.Forms.Timer snapshotTimer;
        private int snapshotIntervalMs = 500; // 기본 500ms 간격

        /// <summary>
        /// 성능 스냅샷 데이터
        /// </summary>
        public class PerformanceSnapshot
        {
            public DateTime Timestamp { get; set; }
            public OpenGLRenderMode Mode { get; set; }
            public double FPS { get; set; }
            public double RenderTimeMs { get; set; }
            public double MinRenderTimeMs { get; set; }
            public double MaxRenderTimeMs { get; set; }
            public long PixelsPerFrame { get; set; }
            public double PixelPercentage { get; set; }
            public double CPUUsage { get; set; }
            public int InvalidateCount { get; set; }
            public int DirtyRegionCount { get; set; }
            public double EfficiencyIndex { get; set; }
            public int ProgressValue { get; set; } // PROGRESS 값
            
            public override string ToString()
            {
                return $"[{Mode}] FPS:{FPS:F1} | Render:{RenderTimeMs:F2}ms | Pixels:{PixelsPerFrame} ({PixelPercentage:F1}%) | CPU:{CPUUsage:F1}% | Efficiency:{EfficiencyIndex:F1}x";
            }
        }

        /// <summary>
        /// 성능 기록 시작 (주기적 스냅샷 활성화)
        /// </summary>
        /// <param name="intervalMs">스냅샷 간격 (ms), 기본 500ms</param>
        public void StartRecording(int intervalMs = 500)
        {
            snapshots.Clear();
            testStartTime = DateTime.Now;
            isRecording = true;
            snapshotIntervalMs = intervalMs;
            
            LogHelper.Log("PerformanceComparer", $"📊 Performance recording started (interval: {intervalMs}ms)");
        }

        /// <summary>
        /// 성능 기록 중지
        /// </summary>
        public void StopRecording()
        {
            isRecording = false;
            
            if (snapshotTimer != null)
            {
                snapshotTimer.Stop();
                snapshotTimer.Dispose();
                snapshotTimer = null;
            }
            
            LogHelper.Log("PerformanceComparer", $"📊 Performance recording stopped. Total snapshots: {snapshots.Count}");
        }

        /// <summary>
        /// 현재 성능 스냅샷 기록 (내부용)
        /// </summary>
        private void RecordSnapshotInternal(PerformanceMonitor monitor, OpenGLRenderMode mode, int progressValue)
        {
            if (!isRecording || monitor == null) return;

            var snapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.Now,
                Mode = mode,
                FPS = monitor.GetAverageFPS(),
                RenderTimeMs = monitor.GetAverageRenderTime(),
                MinRenderTimeMs = monitor.GetMinRenderTime(),
                MaxRenderTimeMs = monitor.GetMaxRenderTime(),
                PixelsPerFrame = monitor.GetAveragePixelsPerFrame(),
                PixelPercentage = monitor.GetPixelPercentage(),
                CPUUsage = monitor.GetCPUUsage(),
                InvalidateCount = 0, // 누적 카운트는 외부에서 주입
                DirtyRegionCount = 0,
                EfficiencyIndex = monitor.GetEfficiencyIndex(),
                ProgressValue = progressValue
            };

            snapshots.Add(snapshot);
        }
        
        /// <summary>
        /// 주기적 스냅샷 기록 시작 (타이머 기반)
        /// </summary>
        public void StartPeriodicSnapshot(PerformanceMonitor monitor, Func<OpenGLRenderMode> getModeFunc, Func<int> getProgressFunc)
        {
            if (snapshotTimer != null)
            {
                snapshotTimer.Stop();
                snapshotTimer.Dispose();
            }

            snapshotTimer = new System.Windows.Forms.Timer();
            snapshotTimer.Interval = snapshotIntervalMs;
            snapshotTimer.Tick += (sender, e) =>
            {
                if (isRecording && monitor != null)
                {
                    RecordSnapshotInternal(monitor, getModeFunc(), getProgressFunc());
                }
            };
            snapshotTimer.Start();
        }

        /// <summary>
        /// CSV 리포트 생성 (날짜별 파일, append 모드)
        /// </summary>
        public string GenerateCSVReport(string filePath = null)
        {
            if (snapshots.Count == 0)
            {
                LogHelper.Log("PerformanceComparer", "⚠️ No snapshots to export");
                return null;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                // 기본 경로: Desktop/CamViewer_Logs/
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string logDir = Path.Combine(desktopPath, "CamViewer_Logs");
                
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                // 날짜만 포함 (시간 제외)
                string dateString = DateTime.Now.ToString("yyyyMMdd");
                filePath = Path.Combine(logDir, $"Performance_{dateString}.csv");
            }

            try
            {
                bool fileExists = File.Exists(filePath);
                
                using (StreamWriter sw = new StreamWriter(filePath, append: true, Encoding.UTF8))
                {
                    // 파일이 새로 생성되는 경우에만 헤더 작성
                    if (!fileExists)
                    {
                        sw.WriteLine("Timestamp,Mode,FPS,RenderTimeMs,MinRenderTimeMs,MaxRenderTimeMs,PixelsPerFrame,PixelPercentage,CPUUsage,InvalidateCount,DirtyRegionCount,EfficiencyIndex,ProgressValue");
                    }

                    // 데이터 행 추가
                    foreach (var snapshot in snapshots)
                    {
                        sw.WriteLine($"{snapshot.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                                     $"{snapshot.Mode}," +
                                     $"{snapshot.FPS:F2}," +
                                     $"{snapshot.RenderTimeMs:F2}," +
                                     $"{snapshot.MinRenderTimeMs:F2}," +
                                     $"{snapshot.MaxRenderTimeMs:F2}," +
                                     $"{snapshot.PixelsPerFrame}," +
                                     $"{snapshot.PixelPercentage:F2}," +
                                     $"{snapshot.CPUUsage:F2}," +
                                     $"{snapshot.InvalidateCount}," +
                                     $"{snapshot.DirtyRegionCount}," +
                                     $"{snapshot.EfficiencyIndex:F2}," +
                                     $"{snapshot.ProgressValue}");
                    }
                }

                LogHelper.Log("PerformanceComparer", $"✅ CSV appended: {filePath} ({snapshots.Count} records)");
                return filePath;
            }
            catch (Exception ex)
            {
                LogHelper.Log("PerformanceComparer", $"❌ CSV export failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 통계 분석 리포트 생성
        /// </summary>
        public string GenerateStatisticsReport()
        {
            if (snapshots.Count == 0)
                return "No data available.";

            var defaultSnapshots = snapshots.Where(s => s.Mode == OpenGLRenderMode.Default).ToList();
            var dirtyRegionSnapshots = snapshots.Where(s => s.Mode == OpenGLRenderMode.DirtyRegion).ToList();

            var report = new StringBuilder();
            report.AppendLine("╔══════════════════════════════════════════════════════════════╗");
            report.AppendLine("║          Phase 15.3: Performance Comparison Report          ║");
            report.AppendLine("╚══════════════════════════════════════════════════════════════╝");
            report.AppendLine();
            report.AppendLine($"Test Duration: {(DateTime.Now - testStartTime).TotalSeconds:F1}s");
            report.AppendLine($"Total Snapshots: {snapshots.Count}");
            report.AppendLine($"  - TYPE=1 (Default): {defaultSnapshots.Count}");
            report.AppendLine($"  - TYPE=2 (DirtyRegion): {dirtyRegionSnapshots.Count}");
            report.AppendLine();

            if (defaultSnapshots.Count > 0)
            {
                report.AppendLine("─────────────────────────────────────────────────────────────");
                report.AppendLine("TYPE=1 (Default) Statistics:");
                report.AppendLine("─────────────────────────────────────────────────────────────");
                AppendModeStatistics(report, defaultSnapshots);
                report.AppendLine();
            }

            if (dirtyRegionSnapshots.Count > 0)
            {
                report.AppendLine("─────────────────────────────────────────────────────────────");
                report.AppendLine("TYPE=2 (DirtyRegion) Statistics:");
                report.AppendLine("─────────────────────────────────────────────────────────────");
                AppendModeStatistics(report, dirtyRegionSnapshots);
                report.AppendLine();
            }

            if (defaultSnapshots.Count > 0 && dirtyRegionSnapshots.Count > 0)
            {
                report.AppendLine("═════════════════════════════════════════════════════════════");
                report.AppendLine("Comparison (TYPE=2 vs TYPE=1):");
                report.AppendLine("═════════════════════════════════════════════════════════════");
                AppendComparison(report, defaultSnapshots, dirtyRegionSnapshots);
            }

            return report.ToString();
        }

        private void AppendModeStatistics(StringBuilder sb, List<PerformanceSnapshot> data)
        {
            if (data.Count == 0) return;

            sb.AppendLine($"  FPS:              Avg {data.Average(s => s.FPS):F1}  |  Min {data.Min(s => s.FPS):F1}  |  Max {data.Max(s => s.FPS):F1}");
            sb.AppendLine($"  Render Time (ms): Avg {data.Average(s => s.RenderTimeMs):F2}  |  Min {data.Min(s => s.RenderTimeMs):F2}  |  Max {data.Max(s => s.RenderTimeMs):F2}");
            sb.AppendLine($"  Pixels/Frame:     Avg {FormatPixels((long)data.Average(s => s.PixelsPerFrame))}  ({data.Average(s => s.PixelPercentage):F1}%)");
            sb.AppendLine($"  CPU Usage:        Avg {data.Average(s => s.CPUUsage):F1}%  |  Min {data.Min(s => s.CPUUsage):F1}%  |  Max {data.Max(s => s.CPUUsage):F1}%");
            sb.AppendLine($"  Efficiency Index: Avg {data.Average(s => s.EfficiencyIndex):F1}x  |  Max {data.Max(s => s.EfficiencyIndex):F1}x");
        }

        private void AppendComparison(StringBuilder sb, List<PerformanceSnapshot> type1, List<PerformanceSnapshot> type2)
        {
            double avgFPS1 = type1.Average(s => s.FPS);
            double avgFPS2 = type2.Average(s => s.FPS);
            double fpsImprovement = ((avgFPS2 - avgFPS1) / avgFPS1) * 100.0;

            long avgPixels1 = (long)type1.Average(s => s.PixelsPerFrame);
            long avgPixels2 = (long)type2.Average(s => s.PixelsPerFrame);
            double pixelReduction = ((double)(avgPixels1 - avgPixels2) / avgPixels1) * 100.0;

            double avgCPU1 = type1.Average(s => s.CPUUsage);
            double avgCPU2 = type2.Average(s => s.CPUUsage);
            double cpuReduction = ((avgCPU1 - avgCPU2) / avgCPU1) * 100.0;

            double avgEfficiency1 = type1.Average(s => s.EfficiencyIndex);
            double avgEfficiency2 = type2.Average(s => s.EfficiencyIndex);
            double efficiencyGain = ((avgEfficiency2 - avgEfficiency1) / avgEfficiency1) * 100.0;

            sb.AppendLine($"  FPS:              {avgFPS1:F1} → {avgFPS2:F1}  ({fpsImprovement:+0.0;-0.0}%)");
            sb.AppendLine($"  Pixels/Frame:     {FormatPixels(avgPixels1)} → {FormatPixels(avgPixels2)}  ({pixelReduction:+0.0;-0.0}% reduction)");
            sb.AppendLine($"  CPU Usage:        {avgCPU1:F1}% → {avgCPU2:F1}%  ({cpuReduction:+0.0;-0.0}% reduction)");
            sb.AppendLine($"  Efficiency Index: {avgEfficiency1:F1}x → {avgEfficiency2:F1}x  ({efficiencyGain:+0.0;-0.0}% gain)");
            sb.AppendLine();
            sb.AppendLine($"  💡 TYPE=2 is {avgEfficiency2 / avgEfficiency1:F1}x more efficient than TYPE=1");
        }

        private string FormatPixels(long pixels)
        {
            if (pixels >= 1000000)
                return $"{pixels / 1000000.0:F2}M";
            else if (pixels >= 1000)
                return $"{pixels / 1000.0:F1}K";
            else
                return pixels.ToString();
        }

        /// <summary>
        /// 기록된 스냅샷 개수
        /// </summary>
        public int SnapshotCount => snapshots.Count;

        /// <summary>
        /// 기록 중 여부
        /// </summary>
        public bool IsRecording => isRecording;

        /// <summary>
        /// 모든 스냅샷 가져오기
        /// </summary>
        public List<PerformanceSnapshot> GetSnapshots() => new List<PerformanceSnapshot>(snapshots);
    }
}
