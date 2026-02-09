using System;
using System.Diagnostics;
using System.Runtime;

namespace RealtimeITagControl
{
    /// <summary>
    /// Phase 15.5: Memory optimization manager
    /// 메모리 사용량 모니터링 및 최적화
    /// </summary>
    public class MemoryOptimizer
    {
        private Process currentProcess;
        private long initialMemory = 0;
        private long peakMemory = 0;
        private int gcCollectionCount = 0;
        private bool aggressiveGC = false;

        public MemoryOptimizer()
        {
            currentProcess = Process.GetCurrentProcess();
            initialMemory = GetCurrentMemoryUsage();
            
            // Enable server GC for better performance
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        }

        /// <summary>
        /// 현재 메모리 사용량 (MB)
        /// </summary>
        public long GetCurrentMemoryUsage()
        {
            currentProcess.Refresh();
            long memoryUsage = currentProcess.WorkingSet64 / 1024 / 1024; // MB

            if (memoryUsage > peakMemory)
                peakMemory = memoryUsage;

            return memoryUsage;
        }

        /// <summary>
        /// 관리되는 메모리 (Managed Memory) (MB)
        /// </summary>
        public long GetManagedMemoryUsage()
        {
            return GC.GetTotalMemory(false) / 1024 / 1024; // MB
        }

        /// <summary>
        /// GC 강제 실행
        /// </summary>
        public void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            gcCollectionCount++;
        }

        /// <summary>
        /// 메모리 최적화 (메모리 부족 시 자동 호출)
        /// </summary>
        public void OptimizeMemory()
        {
            // GC 실행
            ForceGarbageCollection();

            // Large Object Heap 압축
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        /// <summary>
        /// 메모리 임계값 확인 (MB)
        /// </summary>
        public bool IsMemoryAboveThreshold(long thresholdMB)
        {
            return GetCurrentMemoryUsage() > thresholdMB;
        }

        /// <summary>
        /// 자동 메모리 관리 활성화
        /// </summary>
        public void EnableAutoMemoryManagement(long thresholdMB)
        {
            if (IsMemoryAboveThreshold(thresholdMB))
            {
                OptimizeMemory();
            }
        }

        /// <summary>
        /// Aggressive GC 모드 설정
        /// </summary>
        public void SetAggressiveGC(bool enabled)
        {
            aggressiveGC = enabled;
            
            if (enabled)
            {
                GCSettings.LatencyMode = GCLatencyMode.Batch;
            }
            else
            {
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            }
        }

        /// <summary>
        /// 메모리 통계 정보
        /// </summary>
        public string GetMemoryStatistics()
        {
            long current = GetCurrentMemoryUsage();
            long managed = GetManagedMemoryUsage();
            long unmanaged = current - managed;

            return $"총 메모리: {current}MB | " +
                   $"관리: {managed}MB | " +
                   $"비관리: {unmanaged}MB | " +
                   $"최대: {peakMemory}MB | " +
                   $"GC 횟수: {gcCollectionCount}";
        }

        /// <summary>
        /// 초기 메모리 대비 증가량 (MB)
        /// </summary>
        public long MemoryGrowth => GetCurrentMemoryUsage() - initialMemory;

        /// <summary>
        /// 최대 메모리 사용량 (MB)
        /// </summary>
        public long PeakMemory => peakMemory;

        /// <summary>
        /// GC 컬렉션 횟수
        /// </summary>
        public int GCCollectionCount => gcCollectionCount;

        /// <summary>
        /// Generation별 GC 횟수
        /// </summary>
        public string GetGCGenerationInfo()
        {
            return $"Gen0: {GC.CollectionCount(0)} | " +
                   $"Gen1: {GC.CollectionCount(1)} | " +
                   $"Gen2: {GC.CollectionCount(2)}";
        }
    }
}
