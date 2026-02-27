using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RealtimeITagControl.Rendering
{
    /// <summary>
    /// Phase 14.2: Performance monitoring for rendering optimization
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
        /// 성능 요약 문자열
        /// </summary>
        public string GetPerformanceSummary()
        {
            double elapsed = (DateTime.Now - lastResetTime).TotalSeconds;
            double invalidateRate = elapsed > 0 ? invalidateCount / elapsed : 0;
            
            return $"FPS: {GetAverageFPS():F1} | " +
                   $"Render: {GetAverageRenderTime():F2}ms " +
                   $"(Min: {GetMinRenderTime():F2}ms, Max: {GetMaxRenderTime():F2}ms)\n" +
                   $"Invalidate: {invalidateCount} ({invalidateRate:F1}/s) | DirtyRegion: {dirtyRegionCount}";
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
            lastResetTime = DateTime.Now;
        }
    }
}
