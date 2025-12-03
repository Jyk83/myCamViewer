using System;
using System.Collections.Generic;
using RealtimeITagControl.MPF;

namespace RealtimeITagControl.Selection
{
    /// <summary>
    /// 기하학 유틸리티 클래스
    /// Point-in-Polygon 검사, Point-to-Segment 거리 계산 등 제공
    /// </summary>
    public static class GeometryUtils
    {
        /// <summary>
        /// 2D 점 구조체
        /// </summary>
        public struct Point2D
        {
            public double X;
            public double Y;

            public Point2D(double x, double y)
            {
                X = x;
                Y = y;
            }

            public override string ToString()
            {
                return $"({X:F3}, {Y:F3})";
            }
        }

        /// <summary>
        /// 2D 사각형 구조체
        /// </summary>
        public struct Rectangle2D
        {
            public double X;
            public double Y;
            public double Width;
            public double Height;

            public Rectangle2D(double x, double y, double width, double height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public double MinX => X;
            public double MinY => Y;
            public double MaxX => X + Width;
            public double MaxY => Y + Height;

            public Point2D Center => new Point2D(X + Width / 2, Y + Height / 2);

            public override string ToString()
            {
                return $"[({X:F3}, {Y:F3}) {Width:F3}x{Height:F3}]";
            }
        }

        #region Point-in-Polygon (Ray Casting Algorithm)

        /// <summary>
        /// 점이 컨투어 내부에 있는지 확인 (Bounding Box 방식)
        /// </summary>
        /// <param name="point">검사할 점 (월드 좌표)</param>
        /// <param name="contour">컨투어</param>
        /// <param name="offsetX">파트 X 오프셋</param>
        /// <param name="offsetY">파트 Y 오프셋</param>
        /// <returns>내부에 있으면 true</returns>
        public static bool IsPointInsideContour(Point2D point, Contour contour, float offsetX, float offsetY, float scale = 1.0f)
        {
            // 컨투어의 바운딩 박스 계산
            Rectangle2D boundingBox = CalculateContourBoundingBox(contour, offsetX, offsetY, scale);

            if (boundingBox.Width == 0 || boundingBox.Height == 0)
                return false; // 유효하지 않은 바운딩 박스

            // 점이 바운딩 박스 내부에 있는지 확인
            bool isInside = IsPointInBoundingBox(point, boundingBox);
            
            
            return isInside;
        }

        /// <summary>
        /// 컨투어의 모든 세그먼트를 폴리곤 점 리스트로 변환
        /// </summary>
        public static List<Point2D> ConvertContourToPolygon(Contour contour, float offsetX, float offsetY, float scale = 1.0f)
        {
            List<Point2D> polygon = new List<Point2D>();

            // CRITICAL FIX: Find ACTUAL cutting path by detecting closed loops
            // 
            // Problem: CuttingPath includes LeadIn approach segments!
            //   Example Contour 5 CuttingPath:
            //     Segment 0: Line (LeadIn end → Arc start)  ← Approach line
            //     Segment 1: Arc (small approach arc)        ← Approach arc  
            //     Segment 2-6: Arc (ACTUAL semi-circle)      ← Real cutting path!
            // 
            // Solution: Find where Start[i] ≈ End[last] (closed loop)
            //   Skip segments before this point (LeadIn approach)
            //   Use only the closed cutting geometry

            List<PathSegment> contourPath = null;
            int startIdx = 0;
            
            // ALWAYS prefer CuttingPath (excludes external LeadIn)
            if (contour.CuttingPath != null && contour.CuttingPath.Count > 0)
            {
                contourPath = contour.CuttingPath;
                
                // Find the closed loop (actual cutting path)
                // Strategy: Find first segment whose Start matches last segment's End
                // This identifies where the true cutting loop begins (skips LeadIn approach)
                if (contourPath.Count > 1)
                {
                    var lastSeg = contourPath[contourPath.Count - 1];
                    Point2D lastEnd = new Point2D(lastSeg.End.X * scale + offsetX, lastSeg.End.Y * scale + offsetY);
                    
                    
                    // CRITICAL: Use tight tolerance (0.001 = 1mm)
                    // Loose tolerance would incorrectly match LeadIn approach segments!
                    double bestDist = double.MaxValue;
                    int bestIdx = 0;
                    
                    for (int i = 0; i < contourPath.Count; i++)
                    {
                        var seg = contourPath[i];
                        Point2D segStart = new Point2D(seg.Start.X * scale + offsetX, seg.Start.Y * scale + offsetY);
                        Point2D segEnd = new Point2D(seg.End.X * scale + offsetX, seg.End.Y * scale + offsetY);
                        double dist = Distance(segStart, lastEnd);
                        
                        // Track best match
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestIdx = i;
                        }
                        
                    }
                    
                    // Only skip segments if we found a GOOD match (< 1mm) AND it's not the first segment
                    if (bestDist < 0.01 && bestIdx > 0)
                    {
                        startIdx = bestIdx;
                    }
                    else if (bestDist >= 0.001)
                    {
                    }
                }
            }
            // Fallback to AllSegments only if CuttingPath is unavailable
            else if (contour.AllSegments != null && contour.AllSegments.Count > 0)
            {
                contourPath = contour.AllSegments;
            }
            else
            {
            }

            if (contourPath != null)
            {
                // Add only segments from startIdx onwards (skip LeadIn approach)
                for (int i = startIdx; i < contourPath.Count; i++)
                {
                    var segment = contourPath[i];
                    AddSegmentPoints(segment, polygon, offsetX, offsetY, scale);
                }
            }

            // CRITICAL FIX: Handle both closed and open contours
            // For ray-casting algorithm:
            //   polygon[(i+1)%n] automatically connects last point back to first
            //   So we MUST NOT have duplicate first/last points!
            //   
            // Example: [A, B, C, D, A] is WRONG (5 points, last A is duplicate)
            //   - Loop creates: A→B, B→C, C→D, D→A, A→A (duplicate edge!)
            //   
            // Example: [A, B, C, D] is CORRECT (4 points)
            //   - Loop creates: A→B, B→C, C→D, D→A (closing edge via modulo)
            //
            // Strategy:
            //   1. If already closed (first ≈ last): Remove duplicate last point
            //   2. If open: Don't add first to end (modulo handles closing)
            
            if (polygon.Count > 1)
            {
                Point2D first = polygon[0];
                Point2D last = polygon[polygon.Count - 1];
                double distFirstLast = Distance(first, last);
                
                
                if (distFirstLast < 0.001)
                {
                    // Already closed - MUST remove duplicate to avoid double-counting
                    polygon.RemoveAt(polygon.Count - 1);
                }
                else
                {
                    // Open contour - ray-casting will auto-close via polygon[(i+1)%n]
                    // This is actually CORRECT behavior for ray-casting!
                    if (distFirstLast <= 0.1)
                    {
                    }
                    else
                    {
                    }
                }
            }

            if (polygon.Count > 0)
            {
                // Print first few and last few points for debugging
                int showCount = Math.Min(5, polygon.Count);
                for (int i = 0; i < showCount; i++)
                {
                }
                if (polygon.Count > showCount)
                {
                    for (int i = Math.Max(0, polygon.Count - showCount); i < polygon.Count; i++)
                    {
                    }
                }
            }
            if (polygon.Count > 1)
            {
                double finalDist = Distance(polygon[0], polygon[polygon.Count-1]);
            }

            return polygon;
        }

        /// <summary>
        /// 세그먼트의 점들을 폴리곤에 추가
        /// </summary>
        private static void AddSegmentPoints(PathSegment segment, List<Point2D> polygon, float offsetX, float offsetY, float scale = 1.0f)
        {
            if (segment is LineSegment line)
            {
                // 시작점만 추가 (끝점은 다음 세그먼트의 시작점)
                if (polygon.Count == 0 || Distance(polygon[polygon.Count - 1], 
                    new Point2D(line.Start.X * scale + offsetX, line.Start.Y * scale + offsetY)) > 0.001)
                {
                    polygon.Add(new Point2D(line.Start.X * scale + offsetX, line.Start.Y * scale + offsetY));
                }
                polygon.Add(new Point2D(line.End.X * scale + offsetX, line.End.Y * scale + offsetY));
            }
            else if (segment is ArcSegment arc)
            {
                // 호를 여러 선분으로 근사
                List<Point2D> arcPoints = ApproximateArc(arc, offsetX, offsetY, scale, 16); // 16 segments
                
                foreach (var pt in arcPoints)
                {
                    if (polygon.Count == 0 || Distance(polygon[polygon.Count - 1], pt) > 0.001)
                    {
                        polygon.Add(pt);
                    }
                }
            }
        }

        /// <summary>
        /// 호를 선분들로 근사화
        /// </summary>
        private static List<Point2D> ApproximateArc(ArcSegment arc, float offsetX, float offsetY, float scale, int segments)
        {
            List<Point2D> points = new List<Point2D>();

            double centerX = arc.Center.X * scale + offsetX;
            double centerY = arc.Center.Y * scale + offsetY;
            double radius = arc.Radius * scale;

            double startAngle = arc.StartAngle * Math.PI / 180.0;
            double endAngle = arc.EndAngle * Math.PI / 180.0;

            // 각도 범위 계산
            double angleRange;
            if (arc.Clockwise)
            {
                if (endAngle > startAngle)
                    angleRange = endAngle - startAngle - 2 * Math.PI;
                else
                    angleRange = endAngle - startAngle;
            }
            else
            {
                if (endAngle < startAngle)
                    angleRange = endAngle - startAngle + 2 * Math.PI;
                else
                    angleRange = endAngle - startAngle;
            }

            double angleStep = angleRange / segments;

            for (int i = 0; i <= segments; i++)
            {
                double angle = startAngle + angleStep * i;
                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + radius * Math.Sin(angle);
                points.Add(new Point2D(x, y));
            }

            return points;
        }

        /// <summary>
        /// Ray Casting 알고리즘으로 점이 폴리곤 내부에 있는지 확인
        /// </summary>
        public static bool IsPointInPolygon(Point2D point, List<Point2D> polygon)
        {
            int intersections = 0;
            int n = polygon.Count;


            for (int i = 0; i < n; i++)
            {
                Point2D p1 = polygon[i];
                Point2D p2 = polygon[(i + 1) % n];

                // 점에서 오른쪽으로 수평선을 그었을 때 선분과 교차하는지 확인
                bool intersects = RayIntersectsSegment(point, p1, p2);
                if (intersects)
                {
                    intersections++;
                }
            }

            bool isInside = (intersections % 2) == 1;
            
            // 교차 횟수가 홀수면 내부
            return isInside;
        }

        /// <summary>
        /// 점에서 오른쪽으로 그은 수평선이 선분과 교차하는지 확인
        /// </summary>
        private static bool RayIntersectsSegment(Point2D point, Point2D p1, Point2D p2)
        {
            
            // 선분이 점의 Y 범위 내에 있는지 확인 - Swap to ensure p1.Y <= p2.Y
            if (p1.Y > p2.Y)
            {
                Point2D temp = p1;
                p1 = p2;
                p2 = temp;
            }

            // Y-range check: point must be in [p1.Y, p2.Y) range
            bool yInRange = !(point.Y < p1.Y || point.Y >= p2.Y);
            if (!yInRange)
            {
                return false;
            }

            // Horizontal line check
            double deltaY = p2.Y - p1.Y;
            if (Math.Abs(deltaY) < 1e-10)
            {
                return false;
            }

            // X-intersection calculation
            double numerator = (point.Y - p1.Y) * (p2.X - p1.X);
            double xIntersect = p1.X + numerator / deltaY;
            bool intersects = xIntersect > point.X;
            
            if (intersects)
            {
            }
            else
            {
            }
            
            return intersects;
        }

        #endregion

        #region Point-to-Segment Distance

        /// <summary>
        /// 점과 세그먼트 사이의 최단 거리 계산
        /// </summary>
        /// <param name="point">점 (월드 좌표)</param>
        /// <param name="segment">세그먼트</param>
        /// <param name="offsetX">파트 X 오프셋</param>
        /// <param name="offsetY">파트 Y 오프셋</param>
        /// <returns>최단 거리</returns>
        public static double DistancePointToSegment(Point2D point, PathSegment segment, float offsetX, float offsetY, float scale = 1.0f)
        {
            if (segment is LineSegment line)
            {
                return DistancePointToLine(point, 
                    new Point2D(line.Start.X * scale + offsetX, line.Start.Y * scale + offsetY),
                    new Point2D(line.End.X * scale + offsetX, line.End.Y * scale + offsetY));
            }
            else if (segment is ArcSegment arc)
            {
                return DistancePointToArc(point, arc, offsetX, offsetY, scale);
            }

            return double.MaxValue;
        }

        /// <summary>
        /// 점과 선분 사이의 최단 거리
        /// </summary>
        private static double DistancePointToLine(Point2D point, Point2D lineStart, Point2D lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;
            double lengthSquared = dx * dx + dy * dy;

            if (lengthSquared < 1e-10)
            {
                // 선분이 점인 경우
                return Distance(point, lineStart);
            }

            // 선분 위의 가장 가까운 점의 매개변수 t (0 ≤ t ≤ 1)
            double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared;
            t = Math.Max(0, Math.Min(1, t)); // [0, 1] 범위로 제한

            // 가장 가까운 점
            Point2D closest = new Point2D(
                lineStart.X + t * dx,
                lineStart.Y + t * dy
            );

            return Distance(point, closest);
        }

        /// <summary>
        /// 점과 호 사이의 최단 거리
        /// </summary>
        private static double DistancePointToArc(Point2D point, ArcSegment arc, float offsetX, float offsetY, float scale = 1.0f)
        {
            Point2D center = new Point2D(arc.Center.X * scale + offsetX, arc.Center.Y * scale + offsetY);
            double radius = arc.Radius * scale;

            // 점에서 중심까지의 거리
            double distToCenter = Distance(point, center);

            // 점에서 중심으로의 각도
            double angleToPoint = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180.0 / Math.PI;
            if (angleToPoint < 0) angleToPoint += 360;

            // 점이 호의 각도 범위 내에 있는지 확인
            bool inAngleRange = IsAngleInRange(angleToPoint, arc.StartAngle, arc.EndAngle, arc.Clockwise);

            if (inAngleRange)
            {
                // 호의 원 위의 점까지의 거리
                return Math.Abs(distToCenter - radius);
            }
            else
            {
                // 호의 끝점까지의 거리 중 최소값
                Point2D arcStart = new Point2D(
                    center.X + radius * Math.Cos(arc.StartAngle * Math.PI / 180.0),
                    center.Y + radius * Math.Sin(arc.StartAngle * Math.PI / 180.0)
                );
                Point2D arcEnd = new Point2D(
                    center.X + radius * Math.Cos(arc.EndAngle * Math.PI / 180.0),
                    center.Y + radius * Math.Sin(arc.EndAngle * Math.PI / 180.0)
                );

                return Math.Min(Distance(point, arcStart), Distance(point, arcEnd));
            }
        }

        /// <summary>
        /// 각도가 호의 범위 내에 있는지 확인
        /// </summary>
        private static bool IsAngleInRange(double angle, double startAngle, double endAngle, bool clockwise)
        {
            // 각도를 [0, 360) 범위로 정규화
            angle = NormalizeAngle(angle);
            startAngle = NormalizeAngle(startAngle);
            endAngle = NormalizeAngle(endAngle);

            if (clockwise)
            {
                // 시계방향: startAngle에서 endAngle로 (감소)
                if (startAngle > endAngle)
                {
                    return angle <= startAngle && angle >= endAngle;
                }
                else
                {
                    return angle <= startAngle || angle >= endAngle;
                }
            }
            else
            {
                // 반시계방향: startAngle에서 endAngle로 (증가)
                if (startAngle < endAngle)
                {
                    return angle >= startAngle && angle <= endAngle;
                }
                else
                {
                    return angle >= startAngle || angle <= endAngle;
                }
            }
        }

        /// <summary>
        /// 각도를 [0, 360) 범위로 정규화
        /// </summary>
        private static double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 360;
            while (angle >= 360) angle -= 360;
            return angle;
        }

        #endregion

        #region Bounding Box Selection

        /// <summary>
        /// 컨투어의 바운딩 박스 계산
        /// </summary>
        /// <param name="contour">컨투어</param>
        /// <param name="offsetX">파트 X 오프셋</param>
        /// <param name="offsetY">파트 Y 오프셋</param>
        /// <param name="scale">스케일</param>
        /// <returns>바운딩 박스</returns>
        public static Rectangle2D CalculateContourBoundingBox(Contour contour, float offsetX, float offsetY, float scale = 1.0f)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            bool hasPoints = false;

            // Phase 7 FIX: CuttingPath에서 폐곡선(실제 절단 경로)만 사용
            // HKSTR부터 HKSTO까지의 모든 좌표를 포함하여 정확한 BoundingBox 계산
            List<PathSegment> contourPath = null;
            int startIdx = 0;
            
            // CuttingPath 우선 사용 (LeadIn 제외된 경로)
            if (contour.CuttingPath != null && contour.CuttingPath.Count > 0)
            {
                contourPath = contour.CuttingPath;
                
                // 폐곡선 감지: 마지막 세그먼트의 End와 가장 가까운 Start 찾기
                if (contourPath.Count > 1)
                {
                    var lastSeg = contourPath[contourPath.Count - 1];
                    Point2D lastEnd = new Point2D(lastSeg.End.X, lastSeg.End.Y);
                    
                    double bestDist = double.MaxValue;
                    int bestIdx = 0;
                    
                    for (int i = 0; i < contourPath.Count; i++)
                    {
                        Point2D segStart = new Point2D(contourPath[i].Start.X, contourPath[i].Start.Y);
                        double dist = Distance(segStart, lastEnd);
                        
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestIdx = i;
                        }
                    }
                    
                    // 폐곡선이 발견되면 그 시점부터 사용 (LeadIn 접근 세그먼트 제외)
                    if (bestDist < 0.01 && bestIdx > 0)
                    {
                        startIdx = bestIdx;
                    }
                }
            }
            else
            {
                // Fallback: AllSegments 사용
                contourPath = contour.AllSegments;
            }
            
            
            if (contourPath != null)
            {
                for (int i = startIdx; i < contourPath.Count; i++)
                {
                    var segment = contourPath[i];
                    UpdateBoundsFromSegment(segment, offsetX, offsetY, scale, ref minX, ref minY, ref maxX, ref maxY);
                    hasPoints = true;
                }
            }

            if (!hasPoints)
            {
                return new Rectangle2D(0, 0, 0, 0);
            }

            // Phase 7 FIX: Arc 극값점 및 부동소수점 오차를 고려하여 1픽셀(0.001) 여유 추가
            const double margin = 0.001;
            minX -= margin;
            minY -= margin;
            maxX += margin;
            maxY += margin;

            var bbox = new Rectangle2D(minX, minY, maxX - minX, maxY - minY);
            return bbox;
        }

        /// <summary>
        /// 점이 바운딩 박스 내부에 있는지 확인
        /// </summary>
        /// <param name="point">검사할 점</param>
        /// <param name="box">바운딩 박스</param>
        /// <returns>내부에 있으면 true</returns>
        public static bool IsPointInBoundingBox(Point2D point, Rectangle2D box)
        {
            return point.X >= box.MinX && point.X <= box.MaxX &&
                   point.Y >= box.MinY && point.Y <= box.MaxY;
        }

        /// <summary>
        /// 세그먼트로부터 바운딩 박스 업데이트 (스케일 포함)
        /// </summary>
        private static void UpdateBoundsFromSegment(PathSegment segment, float offsetX, float offsetY, float scale,
            ref double minX, ref double minY, ref double maxX, ref double maxY)
        {
            if (segment is LineSegment line)
            {
                UpdateBounds(new Point2D(line.Start.X * scale + offsetX, line.Start.Y * scale + offsetY),
                    ref minX, ref minY, ref maxX, ref maxY);
                UpdateBounds(new Point2D(line.End.X * scale + offsetX, line.End.Y * scale + offsetY),
                    ref minX, ref minY, ref maxX, ref maxY);
            }
            else if (segment is ArcSegment arc)
            {
                // Phase 7 FIX: Arc의 Start/End 포인트를 직접 사용 (각도 재계산으로 인한 부동소수점 오차 방지)
                UpdateBounds(new Point2D(arc.Start.X * scale + offsetX, arc.Start.Y * scale + offsetY),
                    ref minX, ref minY, ref maxX, ref maxY);
                UpdateBounds(new Point2D(arc.End.X * scale + offsetX, arc.End.Y * scale + offsetY),
                    ref minX, ref minY, ref maxX, ref maxY);

                // 극값점 검사를 위해 center와 radius 계산
                Point2D center = new Point2D(arc.Center.X * scale + offsetX, arc.Center.Y * scale + offsetY);
                double radius = arc.Radius * scale;

                // Phase 7 FIX: Arc 극값점이 범위에 포함되는지 체크
                // 디버그: Arc 정보 출력

                // 호가 0°, 90°, 180°, 270°를 포함하는지 확인 (극값)
                CheckArcExtreme(arc, center, radius, 0, ref minX, ref minY, ref maxX, ref maxY);   // +X
                CheckArcExtreme(arc, center, radius, 90, ref minX, ref minY, ref maxX, ref maxY);  // +Y
                CheckArcExtreme(arc, center, radius, 180, ref minX, ref minY, ref maxX, ref maxY); // -X
                CheckArcExtreme(arc, center, radius, 270, ref minX, ref minY, ref maxX, ref maxY); // -Y
            }
        }

        #endregion

        #region Convex Hull Selection

        /// <summary>
        /// 컨투어의 Convex Hull(볼록 껍질) 계산 - Jarvis March 알고리즘
        /// </summary>
        /// <param name="contour">컨투어</param>
        /// <param name="offsetX">파트 X 오프셋</param>
        /// <param name="offsetY">파트 Y 오프셋</param>
        /// <param name="scale">스케일</param>
        /// <returns>Convex Hull 점 리스트</returns>
        public static List<Point2D> CalculateContourConvexHull(Contour contour, float offsetX, float offsetY, float scale = 1.0f)
        {
            // 컨투어를 폴리곤으로 변환
            List<Point2D> polygon = ConvertContourToPolygon(contour, offsetX, offsetY, scale);
            
            if (polygon.Count < 3)
                return polygon; // 점이 3개 미만이면 그대로 반환

            // Jarvis March (Gift Wrapping) 알고리즘
            List<Point2D> hull = new List<Point2D>();

            // 가장 왼쪽 점 찾기 (시작점)
            int leftmost = 0;
            for (int i = 1; i < polygon.Count; i++)
            {
                if (polygon[i].X < polygon[leftmost].X)
                    leftmost = i;
                else if (polygon[i].X == polygon[leftmost].X && polygon[i].Y < polygon[leftmost].Y)
                    leftmost = i;
            }

            int p = leftmost;
            int q;
            do
            {
                hull.Add(polygon[p]);

                // 다음 점 찾기
                q = (p + 1) % polygon.Count;
                for (int i = 0; i < polygon.Count; i++)
                {
                    // i가 p-q 선분의 왼쪽에 있으면 q를 i로 업데이트
                    if (CrossProduct(polygon[p], polygon[i], polygon[q]) < 0)
                        q = i;
                }

                p = q;

            } while (p != leftmost); // 시작점으로 돌아올 때까지

            return hull;
        }

        /// <summary>
        /// 점이 Convex Hull 내부에 있는지 확인
        /// </summary>
        /// <param name="point">검사할 점</param>
        /// <param name="hull">Convex Hull 점 리스트</param>
        /// <returns>내부에 있으면 true</returns>
        public static bool IsPointInConvexHull(Point2D point, List<Point2D> hull)
        {
            if (hull == null || hull.Count < 3)
                return false;

            // Convex polygon은 모든 edge에 대해 점이 같은 방향에 있어야 함
            int n = hull.Count;
            bool? firstSign = null;

            for (int i = 0; i < n; i++)
            {
                Point2D p1 = hull[i];
                Point2D p2 = hull[(i + 1) % n];

                double cross = CrossProduct(p1, point, p2);

                if (Math.Abs(cross) < 1e-10)
                    continue; // 선 위에 있음

                bool sign = cross > 0;

                if (!firstSign.HasValue)
                    firstSign = sign;
                else if (firstSign.Value != sign)
                    return false; // 다른 방향 -> 외부
            }

            return true;
        }

        /// <summary>
        /// 외적(Cross Product) 계산 - CCW 테스트용
        /// </summary>
        /// <param name="o">기준점</param>
        /// <param name="a">점 A</param>
        /// <param name="b">점 B</param>
        /// <returns>양수: CCW, 음수: CW, 0: 일직선</returns>
        private static double CrossProduct(Point2D o, Point2D a, Point2D b)
        {
            return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        }

        /// <summary>
        /// 컨투어가 Convex Hull 내부에 있는지 확인 (IsPointInsideContour의 Convex Hull 버전)
        /// </summary>
        /// <param name="point">검사할 점</param>
        /// <param name="contour">컨투어</param>
        /// <param name="offsetX">파트 X 오프셋</param>
        /// <param name="offsetY">파트 Y 오프셋</param>
        /// <param name="scale">스케일</param>
        /// <returns>내부에 있으면 true</returns>
        public static bool IsPointInsideContourConvexHull(Point2D point, Contour contour, float offsetX, float offsetY, float scale = 1.0f)
        {
            List<Point2D> hull = CalculateContourConvexHull(contour, offsetX, offsetY, scale);
            
            if (hull.Count < 3)
                return false;

            bool isInside = IsPointInConvexHull(point, hull);
            
            
            return isInside;
        }

        #endregion

        #region Bounding Box Calculation

        /// <summary>
        /// 파트의 바운딩 박스 계산
        /// </summary>
        /// <param name="part">파트</param>
        /// <param name="offsetX">파트 X 오프셋</param>
        /// <param name="offsetY">파트 Y 오프셋</param>
        /// <returns>바운딩 박스</returns>
        public static Rectangle2D CalculatePartBoundingBox(Part part, float offsetX, float offsetY)
        {
            // Phase 5 Fix: Use Part.Width/Height from HKSTR instead of calculating from contours
            // This ensures the boundary rectangle matches the actual part size and doesn't overlap contours
            
            // Part.Width and Part.Height are in mm, need to convert to OpenGL units
            // offsetX/offsetY already include workpieceScale, so Width/Height need the same scale
            
            // Calculate workpieceScale from offsetX (offsetX = Part.Origin.X * workpieceScale)
            // Assuming Part.Origin.X is in mm, workpieceScale = offsetX / Part.Origin.X
            // But we don't have Origin here, so we need to pass scale as parameter or use a different approach
            
            // Alternative: Use actual contour bounds but add offset to avoid overlap
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            bool hasPoints = false;

            foreach (var contour in part.Contours)
            {
                // 리드인 경로
                if (contour.LeadIn != null && contour.LeadIn.Path != null)
                {
                    foreach (var segment in contour.LeadIn.Path)
                    {
                        UpdateBoundsFromSegment(segment, offsetX, offsetY, ref minX, ref minY, ref maxX, ref maxY);
                        hasPoints = true;
                    }
                }

                // 절단 경로 (AllSegments 사용)
                if (contour.AllSegments != null)
                {
                    foreach (var segment in contour.AllSegments)
                    {
                        UpdateBoundsFromSegment(segment, offsetX, offsetY, ref minX, ref minY, ref maxX, ref maxY);
                        hasPoints = true;
                    }
                }
            }

            if (!hasPoints)
            {
                return new Rectangle2D(0, 0, 0, 0);
            }

            // Add offset to ensure boundary doesn't overlap contours
            const double boundaryOffset = 0.002; // 2mm offset in OpenGL units (assuming workpieceScale = 0.001)
            
            return new Rectangle2D(
                minX - boundaryOffset, 
                minY - boundaryOffset, 
                (maxX - minX) + 2 * boundaryOffset, 
                (maxY - minY) + 2 * boundaryOffset
            );
        }

        /// <summary>
        /// 세그먼트로부터 바운딩 박스 업데이트 (파트용 - 스케일 미적용)
        /// </summary>
        private static void UpdateBoundsFromSegment(PathSegment segment, float offsetX, float offsetY,
            ref double minX, ref double minY, ref double maxX, ref double maxY)
        {
            if (segment is LineSegment line)
            {
                UpdateBounds(new Point2D(line.Start.X + offsetX, line.Start.Y + offsetY),
                    ref minX, ref minY, ref maxX, ref maxY);
                UpdateBounds(new Point2D(line.End.X + offsetX, line.End.Y + offsetY),
                    ref minX, ref minY, ref maxX, ref maxY);
            }
            else if (segment is ArcSegment arc)
            {
                Point2D center = new Point2D(arc.Center.X + offsetX, arc.Center.Y + offsetY);
                double radius = arc.Radius;

                // 호의 시작/끝점
                Point2D arcStart = new Point2D(
                    center.X + radius * Math.Cos(arc.StartAngle * Math.PI / 180.0),
                    center.Y + radius * Math.Sin(arc.StartAngle * Math.PI / 180.0)
                );
                Point2D arcEnd = new Point2D(
                    center.X + radius * Math.Cos(arc.EndAngle * Math.PI / 180.0),
                    center.Y + radius * Math.Sin(arc.EndAngle * Math.PI / 180.0)
                );

                UpdateBounds(arcStart, ref minX, ref minY, ref maxX, ref maxY);
                UpdateBounds(arcEnd, ref minX, ref minY, ref maxX, ref maxY);

                // 호가 0°, 90°, 180°, 270°를 포함하는지 확인 (극값)
                CheckArcExtreme(arc, center, radius, 0, ref minX, ref minY, ref maxX, ref maxY);   // +X
                CheckArcExtreme(arc, center, radius, 90, ref minX, ref minY, ref maxX, ref maxY);  // +Y
                CheckArcExtreme(arc, center, radius, 180, ref minX, ref minY, ref maxX, ref maxY); // -X
                CheckArcExtreme(arc, center, radius, 270, ref minX, ref minY, ref maxX, ref maxY); // -Y
            }
        }

        /// <summary>
        /// 호가 특정 각도를 포함하는지 확인하고 바운딩 박스 업데이트
        /// </summary>
        private static void CheckArcExtreme(ArcSegment arc, Point2D center, double radius, double extremeAngle,
            ref double minX, ref double minY, ref double maxX, ref double maxY)
        {
            if (IsAngleInRange(extremeAngle, arc.StartAngle, arc.EndAngle, arc.Clockwise))
            {
                Point2D extremePoint = new Point2D(
                    center.X + radius * Math.Cos(extremeAngle * Math.PI / 180.0),
                    center.Y + radius * Math.Sin(extremeAngle * Math.PI / 180.0)
                );
                UpdateBounds(extremePoint, ref minX, ref minY, ref maxX, ref maxY);
            }
        }

        /// <summary>
        /// 점으로 바운딩 박스 업데이트
        /// </summary>
        private static void UpdateBounds(Point2D point, ref double minX, ref double minY, ref double maxX, ref double maxY)
        {
            if (point.X < minX) minX = point.X;
            if (point.Y < minY) minY = point.Y;
            if (point.X > maxX) maxX = point.X;
            if (point.Y > maxY) maxY = point.Y;
        }

        #endregion


        #region Helper Methods

        /// <summary>
        /// 두 점 사이의 거리 계산
        /// </summary>
        public static double Distance(Point2D p1, Point2D p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 컨투어의 면적 계산 (Shoelace formula 사용)
        /// 작은 면적을 가진 컨투어가 선택 우선순위가 높음
        /// </summary>
        public static double CalculateContourArea(Contour contour, float scale)
        {
            if (contour == null || contour.AllSegments == null || contour.AllSegments.Count == 0)
                return double.MaxValue; // Invalid contour has maximum area (lowest priority)

            // Use bounding box area instead of shoelace formula
            // This is more robust for complex shapes with arcs
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            
            foreach (var segment in contour.AllSegments)
            {
                if (segment is LineSegment line)
                {
                    minX = Math.Min(minX, Math.Min(line.Start.X, line.End.X) * scale);
                    maxX = Math.Max(maxX, Math.Max(line.Start.X, line.End.X) * scale);
                    minY = Math.Min(minY, Math.Min(line.Start.Y, line.End.Y) * scale);
                    maxY = Math.Max(maxY, Math.Max(line.Start.Y, line.End.Y) * scale);
                }
                else if (segment is ArcSegment arc)
                {
                    // For arc, include center and endpoints in bounding box
                    minX = Math.Min(minX, Math.Min(arc.Start.X, arc.End.X) * scale);
                    maxX = Math.Max(maxX, Math.Max(arc.Start.X, arc.End.X) * scale);
                    minY = Math.Min(minY, Math.Min(arc.Start.Y, arc.End.Y) * scale);
                    maxY = Math.Max(maxY, Math.Max(arc.Start.Y, arc.End.Y) * scale);
                    
                    // Also consider arc extremes based on angle range
                    double radiusScaled = arc.Radius * scale;
                    minX = Math.Min(minX, arc.Center.X * scale - radiusScaled);
                    maxX = Math.Max(maxX, arc.Center.X * scale + radiusScaled);
                    minY = Math.Min(minY, arc.Center.Y * scale - radiusScaled);
                    maxY = Math.Max(maxY, arc.Center.Y * scale + radiusScaled);
                }
            }

            if (minX == double.MaxValue || maxX == double.MinValue)
                return double.MaxValue; // Invalid bounds

            // Return bounding box area
            double width = maxX - minX;
            double height = maxY - minY;
            return width * height;
        }

        #endregion
    }
}
