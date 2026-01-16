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
    /// Phase 15.5: PathSegment object pool
    /// PathSegment 전용 객체 풀
    /// </summary>
    public static class PathSegmentPool
    {
        private static MPFObjectPool<PathSegment> pool = new MPFObjectPool<PathSegment>(5000, segment =>
        {
            // Reset segment
            segment.Type = SegmentType.Line;
            segment.StartPoint = null;
            segment.EndPoint = null;
            segment.CenterPoint = null;
            segment.Radius = 0;
            segment.IsClockwise = false;
        });

        public static PathSegment Rent() => pool.Rent();
        public static void Return(PathSegment segment) => pool.Return(segment);
        public static void ReturnRange(IEnumerable<PathSegment> segments) => pool.ReturnRange(segments);
        public static int AvailableCount => pool.AvailableCount;
        public static double ReuseRate => pool.ReuseRate;
    }

    /// <summary>
    /// Phase 15.5: Point2D object pool
    /// Point2D 전용 객체 풀
    /// </summary>
    public static class Point2DPool
    {
        private static MPFObjectPool<Point2D> pool = new MPFObjectPool<Point2D>(10000, point =>
        {
            // Reset point
            point.X = 0;
            point.Y = 0;
        });

        public static Point2D Rent() => pool.Rent();
        public static void Return(Point2D point) => pool.Return(point);
        public static void ReturnRange(IEnumerable<Point2D> points) => pool.ReturnRange(points);
        public static int AvailableCount => pool.AvailableCount;
        public static double ReuseRate => pool.ReuseRate;
    }
}
