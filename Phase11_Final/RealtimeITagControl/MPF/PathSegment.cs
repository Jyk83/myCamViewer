using System;

namespace RealtimeITagControl.MPF
{
    /// <summary>
    /// 경로 세그먼트 타입
    /// </summary>
    public enum PathSegmentType
    {
        Line,
        Arc
    }

    /// <summary>
    /// 경로 세그먼트 베이스 클래스
    /// </summary>
    public abstract class PathSegment
    {
        public PathSegmentType Type { get; set; }
        public Point2D Start { get; set; }
        public Point2D End { get; set; }
        public string OriginalGCode { get; set; } // 원본 G-Code 라인
        
        /// <summary>
        /// 세그먼트의 길이를 계산 (추상 메서드)
        /// </summary>
        public abstract double GetLength();
    }

    /// <summary>
    /// 직선 세그먼트 (G0, G1)
    /// </summary>
    public class LineSegment : PathSegment
    {
        public LineSegment()
        {
            Type = PathSegmentType.Line;
        }

        public LineSegment(Point2D start, Point2D end)
        {
            Type = PathSegmentType.Line;
            Start = start;
            End = end;
        }
        
        /// <summary>
        /// 직선 길이 계산: √((x2-x1)² + (y2-y1)²)
        /// </summary>
        public override double GetLength()
        {
            if (Start == null || End == null)
                return 0.0;
            
            double dx = End.X - Start.X;
            double dy = End.Y - Start.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    /// <summary>
    /// 원호 세그먼트 (G2, G3)
    /// </summary>
    public class ArcSegment : PathSegment
    {
        public Point2D Center { get; set; }
        public double Radius { get; set; }
        public bool Clockwise { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }
        public double I { get; set; }
        public double J { get; set; }

        public ArcSegment()
        {
            Type = PathSegmentType.Arc;
        }

        public ArcSegment(Point2D start, Point2D end, Point2D center, double radius, 
                         bool clockwise, double startAngle, double endAngle, double i, double j)
        {
            Type = PathSegmentType.Arc;
            Start = start;
            End = end;
            Center = center;
            Radius = radius;
            Clockwise = clockwise;
            StartAngle = startAngle;
            EndAngle = endAngle;
            I = i;
            J = j;
        }
        
        /// <summary>
        /// 원호 길이 계산: Arc Length = Radius × |ΔAngle|
        /// </summary>
        public override double GetLength()
        {
            // 각도 차이 계산 (라디안)
            double angleDiff = Math.Abs(EndAngle - StartAngle);
            
            // 360도(2π)를 넘는 경우 보정
            if (angleDiff > 2 * Math.PI)
            {
                angleDiff = 2 * Math.PI - (angleDiff % (2 * Math.PI));
            }
            
            // 호의 길이 = 반지름 × 각도(라디안)
            return Radius * angleDiff;
        }
    }
}
