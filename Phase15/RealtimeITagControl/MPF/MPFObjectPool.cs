using System;
using System.Collections.Generic;
using System.Linq;

namespace RealtimeITagControl.MPF
{
    /// <summary>
    /// Phase 15.5: Memory pool for MPF objects
    /// 객체 재사용을 통한 메모리 할당 최적화
    /// </summary>
    public class MPFObjectPool<T> where T : class, new()
    {
        private Stack<T> pool = new Stack<T>();
        private int maxPoolSize = 1000;
        private int totalCreated = 0;
        private int totalReused = 0;
        private Action<T> resetAction;

        public MPFObjectPool(int maxSize = 1000, Action<T> resetFunc = null)
        {
            maxPoolSize = maxSize;
            resetAction = resetFunc;
        }

        /// <summary>
        /// 객체 가져오기 (풀에서 재사용 또는 새로 생성)
        /// </summary>
        public T Rent()
        {
            lock (pool)
            {
                if (pool.Count > 0)
                {
                    totalReused++;
                    var obj = pool.Pop();
                    resetAction?.Invoke(obj);
                    return obj;
                }
            }

            totalCreated++;
            return new T();
        }

        /// <summary>
        /// 객체 반환 (풀에 추가)
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null)
                return;

            lock (pool)
            {
                if (pool.Count < maxPoolSize)
                {
                    resetAction?.Invoke(obj);
                    pool.Push(obj);
                }
            }
        }

        /// <summary>
        /// 여러 객체 반환
        /// </summary>
        public void ReturnRange(IEnumerable<T> objects)
        {
            if (objects == null)
                return;

            foreach (var obj in objects)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// 풀 비우기
        /// </summary>
        public void Clear()
        {
            lock (pool)
            {
                pool.Clear();
            }
        }

        /// <summary>
        /// 현재 풀 크기
        /// </summary>
        public int AvailableCount => pool.Count;

        /// <summary>
        /// 총 생성된 객체 수
        /// </summary>
        public int TotalCreated => totalCreated;

        /// <summary>
        /// 총 재사용된 객체 수
        /// </summary>
        public int TotalReused => totalReused;

        /// <summary>
        /// 재사용률 (%)
        /// </summary>
        public double ReuseRate
        {
            get
            {
                int total = totalCreated + totalReused;
                return total > 0 ? (double)totalReused / total * 100.0 : 0.0;
            }
        }
    }

    /// <summary>
    /// Phase 15.5: LineSegment object pool
    /// LineSegment 전용 객체 풀
    /// </summary>
    public static class LineSegmentPool
    {
        private static MPFObjectPool<LineSegment> pool = new MPFObjectPool<LineSegment>(5000, segment =>
        {
            // Reset segment
            segment.Start = new Point2D(0, 0);
            segment.End = new Point2D(0, 0);
            segment.OriginalGCode = null;
        });

        public static LineSegment Rent() => pool.Rent();
        public static void Return(LineSegment segment) => pool.Return(segment);
        public static void ReturnRange(IEnumerable<LineSegment> segments) => pool.ReturnRange(segments);
        public static int AvailableCount => pool.AvailableCount;
        public static double ReuseRate => pool.ReuseRate;
    }

    /// <summary>
    /// Phase 15.5: ArcSegment object pool
    /// ArcSegment 전용 객체 풀
    /// </summary>
    public static class ArcSegmentPool
    {
        private static MPFObjectPool<ArcSegment> pool = new MPFObjectPool<ArcSegment>(5000, segment =>
        {
            // Reset segment
            segment.Start = new Point2D(0, 0);
            segment.End = new Point2D(0, 0);
            segment.Center = new Point2D(0, 0);
            segment.Radius = 0;
            segment.Clockwise = false;
            segment.StartAngle = 0;
            segment.EndAngle = 0;
            segment.I = 0;
            segment.J = 0;
            segment.OriginalGCode = null;
        });

        public static ArcSegment Rent() => pool.Rent();
        public static void Return(ArcSegment segment) => pool.Return(segment);
        public static void ReturnRange(IEnumerable<ArcSegment> segments) => pool.ReturnRange(segments);
        public static int AvailableCount => pool.AvailableCount;
        public static double ReuseRate => pool.ReuseRate;
    }
}
