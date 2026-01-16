using System;
using System.Collections.Generic;

namespace RealtimeITagControl.Rendering
{
    /// <summary>
    /// Phase 15.5: Render data cache manager
    /// 렌더링 데이터 캐싱으로 중복 계산 방지
    /// </summary>
    public class RenderDataCache
    {
        private Dictionary<string, CachedRenderData> cache = new Dictionary<string, CachedRenderData>();
        private int maxCacheSize = 500;
        private long cacheHits = 0;
        private long cacheMisses = 0;

        /// <summary>
        /// 캐시된 렌더링 데이터
        /// </summary>
        public class CachedRenderData
        {
            public DateTime CachedTime { get; set; }
            public object Data { get; set; }
            public int AccessCount { get; set; }
        }

        /// <summary>
        /// 캐시에서 데이터 가져오기
        /// </summary>
        public T Get<T>(string key) where T : class
        {
            if (cache.TryGetValue(key, out CachedRenderData cached))
            {
                cached.AccessCount++;
                cacheHits++;
                return cached.Data as T;
            }

            cacheMisses++;
            return null;
        }

        /// <summary>
        /// 캐시에 데이터 저장
        /// </summary>
        public void Set(string key, object data)
        {
            if (cache.ContainsKey(key))
            {
                cache[key].Data = data;
                cache[key].CachedTime = DateTime.Now;
            }
            else
            {
                // 캐시 크기 제한
                if (cache.Count >= maxCacheSize)
                {
                    EvictLeastUsed();
                }

                cache[key] = new CachedRenderData
                {
                    Data = data,
                    CachedTime = DateTime.Now,
                    AccessCount = 0
                };
            }
        }

        /// <summary>
        /// 가장 적게 사용된 항목 제거 (LRU)
        /// </summary>
        private void EvictLeastUsed()
        {
            string leastUsedKey = null;
            int minAccessCount = int.MaxValue;

            foreach (var kvp in cache)
            {
                if (kvp.Value.AccessCount < minAccessCount)
                {
                    minAccessCount = kvp.Value.AccessCount;
                    leastUsedKey = kvp.Key;
                }
            }

            if (leastUsedKey != null)
            {
                cache.Remove(leastUsedKey);
            }
        }

        /// <summary>
        /// 캐시 무효화
        /// </summary>
        public void Invalidate(string key)
        {
            cache.Remove(key);
        }

        /// <summary>
        /// 패턴으로 캐시 무효화
        /// </summary>
        public void InvalidateByPattern(string pattern)
        {
            var keysToRemove = new List<string>();

            foreach (var key in cache.Keys)
            {
                if (key.Contains(pattern))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
            }
        }

        /// <summary>
        /// 캐시 전체 초기화
        /// </summary>
        public void Clear()
        {
            cache.Clear();
            cacheHits = 0;
            cacheMisses = 0;
        }

        /// <summary>
        /// 캐시 적중률 (%)
        /// </summary>
        public double HitRate
        {
            get
            {
                long total = cacheHits + cacheMisses;
                return total > 0 ? (double)cacheHits / total * 100.0 : 0.0;
            }
        }

        /// <summary>
        /// 캐시 크기
        /// </summary>
        public int Count => cache.Count;

        /// <summary>
        /// 캐시 통계
        /// </summary>
        public string GetCacheStatistics()
        {
            return $"캐시 크기: {cache.Count}/{maxCacheSize} | " +
                   $"적중률: {HitRate:F1}% | " +
                   $"Hits: {cacheHits} | " +
                   $"Misses: {cacheMisses}";
        }
    }
}
