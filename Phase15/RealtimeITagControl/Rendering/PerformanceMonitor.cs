using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RealtimeITagControl.Rendering
{
    /// <summary>
    /// Phase 14.2: Performance monitoring for rendering optimization
    /// Phase 15.1: Enhanced with pixel count and CPU usage tracking
    /// </summary>
    public class PerformanceMonitor
    {
        private Stopwatch frameTimer = new Stopwatch();
        private Queue<double> frameTimes = new Queue<double>(60); // 최근 60 프레임
        private int frameCount = 0;
        private double totalTime = 0.0;
        
        // Phase 14.2: Invalidate 통계
        private int invalidateCount = 0;
        private int dirtyRegionCount = 0;
        private DateTime lastResetTime = DateTime.Now;
        
        // Phase 15.1: Pixel count 통계
        private long totalPixelsRefreshed = 0;
        private int fullScreenWidth = 1920;
        private int fullScreenHeight = 1080;
        private Queue<long> recentPixelCounts = new Queue<long>(60); // 최근 60 프레임
        
        // Phase 15.1: CPU 사용률 측정
        private Process currentProcess;
        private DateTime cpuStartTime;
        private TimeSpan cpuStartTotal;

        /// <summary>
        /// 프레임 시작
        /// </summary>
        public void StartFrame()
        {
            frameTimer.Restart();
        }

        /// <summary>
        /// 프레임 종료 및 시간 기록
        /// </summary>
        public void EndFrame()
        {
            frameTimer.Stop();
            double ms = frameTimer.Elapsed.TotalMilliseconds;

            frameTimes.Enqueue(ms);
            if (frameTimes.Count > 60)
            {
                frameTimes.Dequeue();
            }

            frameCount++;
            totalTime += ms;
        }

        /// <summary>
        /// 평균 FPS 계산 (최근 60 프레임)
        /// </summary>
        public double GetAverageFPS()
        {
            if (frameTimes.Count == 0) return 0.0;
            
            double avgMs = frameTimes.Average();
            if (avgMs <= 0) return 0.0;
            
            return 1000.0 / avgMs;
        }

        /// <summary>
        /// 평균 렌더링 시간 (최근 60 프레임)
        /// </summary>
        public double GetAverageRenderTime()
        {
            if (frameTimes.Count == 0) return 0.0;
            return frameTimes.Average();
        }

        /// <summary>
        /// 최소 렌더링 시간
        /// </summary>
        public double GetMinRenderTime()
        {
            if (frameTimes.Count == 0) return 0.0;
            return frameTimes.Min();
        }

        /// <summary>
        /// 최대 렌더링 시간
        /// </summary>
        public double GetMaxRenderTime()
        {
            if (frameTimes.Count == 0) return 0.0;
            return frameTimes.Max();
        }

        /// <summary>
        /// Phase 15.1: 성능 요약 문자열 (3줄 형식)
        /// </summary>
        public string GetPerformanceSummary()
        {
            double elapsed = (DateTime.Now - lastResetTime).TotalSeconds;
            double invalidateRate = elapsed > 0 ? (invalidateCount + dirtyRegionCount) / elapsed : 0;
            long avgPixels = GetAveragePixelsPerFrame();
            double pixelPct = GetPixelPercentage();
            double efficiency = GetEfficiencyIndex();
            
            // Line 1: FPS and Efficiency
            string line1 = $"FPS: {GetAverageFPS():F1}";
            if (efficiency > 1.0)
                line1 += $" | Efficiency: {efficiency:F1}x";
            
            // Line 2: Render time and Pixels
            string line2 = $"Render: {GetAverageRenderTime():F2}ms | Pixels: {FormatPixels(avgPixels)} ({pixelPct:F1}%)";
            
            // Line 3: CPU and Calls
            string line3 = $"CPU: {GetCPUUsage():F1}% | Calls: {invalidateCount + dirtyRegionCount} ({invalidateRate:F1}/s)";
            
            return $"{line1}\n{line2}\n{line3}";
        }

        /// <summary>
        /// Phase 15.1: 픽셀 수 포맷팅 (K, M 단위)
        /// </summary>
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
        /// Phase 15.1: 화면 해상도 설정
        /// </summary>
        public void SetScreenResolution(int width, int height)
        {
            fullScreenWidth = width;
            fullScreenHeight = height;
        }

        /// <summary>
        /// Phase 15.1: 픽셀 수 기록
        /// </summary>
        public void RecordPixels(int width, int height)
        {
            long pixels = (long)width * height;
            totalPixelsRefreshed += pixels;
            
            recentPixelCounts.Enqueue(pixels);
            if (recentPixelCounts.Count > 60)
            {
                recentPixelCounts.Dequeue();
            }
        }

        /// <summary>
        /// Phase 15.1: 프레임당 평균 픽셀 수
        /// </summary>
        public long GetAveragePixelsPerFrame()
        {
            if (recentPixelCounts.Count == 0) return 0;
            return (long)recentPixelCounts.Average();
        }

        /// <summary>
        /// Phase 15.1: 전체 화면 대비 픽셀 비율 (%)
        /// </summary>
        public double GetPixelPercentage()
        {
            long fullScreen = (long)fullScreenWidth * fullScreenHeight;
            if (fullScreen == 0) return 0.0;
            
            long avgPixels = GetAveragePixelsPerFrame();
            return (double)avgPixels / fullScreen * 100.0;
        }

        /// <summary>
        /// Phase 15.1: CPU 사용률 측정 시작
        /// </summary>
        public void StartCPUMeasurement()
        {
            currentProcess = Process.GetCurrentProcess();
            cpuStartTime = DateTime.Now;
            cpuStartTotal = currentProcess.TotalProcessorTime;
        }

        /// <summary>
        /// Phase 15.1: CPU 사용률 계산 (%)
        /// </summary>
        public double GetCPUUsage()
        {
            if (currentProcess == null) return 0.0;
            
            TimeSpan cpuNow = currentProcess.TotalProcessorTime;
            TimeSpan cpuUsed = cpuNow - cpuStartTotal;
            TimeSpan timeElapsed = DateTime.Now - cpuStartTime;
            
            if (timeElapsed.TotalMilliseconds == 0) return 0.0;
            
            int processorCount = Environment.ProcessorCount;
            double usage = (cpuUsed.TotalMilliseconds / (timeElapsed.TotalMilliseconds * processorCount)) * 100.0;
            
            return Math.Max(0.0, Math.Min(100.0, usage)); // Clamp [0, 100]
        }

        /// <summary>
        /// Phase 15.1: 성능 효율 지수 계산
        /// </summary>
        public double GetEfficiencyIndex()
        {
            double fps = GetAverageFPS();
            double cpuUsage = GetCPUUsage();
            double pixelPct = GetPixelPercentage();
            
            if (cpuUsage <= 0 || pixelPct <= 0) return 0.0;
            
            // Efficiency = (FPS / CPU%) × (100% / Pixel%)
            return (fps / cpuUsage) * (100.0 / pixelPct);
        }

        /// <summary>
        /// Phase 14.2: Full Invalidate 호출 기록
        /// </summary>
        public void RecordInvalidate()
        {
            invalidateCount++;
        }

        /// <summary>
        /// Phase 14.2: Dirty Region Invalidate 호출 기록
        /// </summary>
        public void RecordDirtyRegion()
        {
            dirtyRegionCount++;
        }

        /// <summary>
        /// 통계 초기화
        /// </summary>
        public void Reset()
        {
            frameTimes.Clear();
            frameCount = 0;
            totalTime = 0.0;
            invalidateCount = 0;
            dirtyRegionCount = 0;
            totalPixelsRefreshed = 0;
            recentPixelCounts.Clear();
            lastResetTime = DateTime.Now;
            
            // CPU 측정 재시작
            StartCPUMeasurement();
        }
    }
}
