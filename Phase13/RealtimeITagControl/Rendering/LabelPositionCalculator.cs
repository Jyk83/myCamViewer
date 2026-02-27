using System;
using System.Drawing;
using RealtimeITagControl.MPF;

namespace RealtimeITagControl.Rendering
{
    /// <summary>
    /// 파트 및 컨투어 번호 라벨의 위치를 계산하는 클래스
    /// HKCamInterface의 OpenGLCAMViewWnd를 참조하여 구현
    /// </summary>
    public class LabelPositionCalculator
    {
        // 라벨 오프셋 (mm 단위)
        private const double PART_LABEL_OFFSET_X = 0.0;   // 중앙
        private const double PART_LABEL_OFFSET_Y = 0.0;   // 중앙
        private const double CONTOUR_LABEL_MARGIN = 0.5;  // 상단 마진 (축소)

        /// <summary>
        /// 파트 라벨 위치 계산
        /// </summary>
        /// <param name="part">파트 객체</param>
        /// <returns>라벨 중심 위치 (월드 좌표)</returns>
        public static Point2D CalculatePartLabelPosition(Part part)
        {
            if (part == null)
                throw new ArgumentNullException(nameof(part));

            // 파트의 네스팅 영역(점선 박스) 중앙 계산
            // Part.Width, Part.Height는 HKSTR에서 가져온 파트 네스팅 영역 크기
            double centerX = part.Width / 2.0 + PART_LABEL_OFFSET_X;
            double centerY = part.Height / 2.0 + PART_LABEL_OFFSET_Y;

            return new Point2D(centerX, centerY);
        }

        /// <summary>
        /// 컨투어 라벨 위치 계산
        /// </summary>
        /// <param name="contour">컨투어 객체</param>
        /// <param name="part">소속된 파트 (마지막 컨투어 판정용, optional)</param>
        /// <param name="isLastContour">파트의 마지막 컨투어 여부</param>
        /// <returns>라벨 중심 위치 (월드 좌표)</returns>
        public static Point2D CalculateContourLabelPosition(Contour contour, Part part = null, bool isLastContour = false)
        {
            if (contour == null)
                throw new ArgumentNullException(nameof(contour));

            // 마지막 컨투어인 경우: 파트 네스팅 영역의 상단 중앙
            if (isLastContour && part != null)
            {
                double centerX = part.Width / 2.0;
                double topY = part.Height + CONTOUR_LABEL_MARGIN;
                return new Point2D(centerX, topY);
            }

            // 일반 컨투어: 컨투어 바운딩 박스의 상단 중앙
            RectangleF bbox = CalculateContourBoundingBox(contour);
            double bboxCenterX = bbox.X + bbox.Width / 2.0;
            double bboxTopY = bbox.Y + bbox.Height + CONTOUR_LABEL_MARGIN;
            return new Point2D(bboxCenterX, bboxTopY);
        }

        /// <summary>
        /// 파트의 바운딩 박스 계산
        /// </summary>
        private static RectangleF CalculatePartBoundingBox(Part part)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            // 모든 컨투어의 점들을 순회하며 최소/최대 찾기
            foreach (var contour in part.Contours)
            {
                foreach (var segment in contour.AllSegments)
                {
                    // 시작점
                    minX = Math.Min(minX, (float)segment.Start.X);
                    minY = Math.Min(minY, (float)segment.Start.Y);
                    maxX = Math.Max(maxX, (float)segment.Start.X);
                    maxY = Math.Max(maxY, (float)segment.Start.Y);

                    // 끝점
                    minX = Math.Min(minX, (float)segment.End.X);
                    minY = Math.Min(minY, (float)segment.End.Y);
                    maxX = Math.Max(maxX, (float)segment.End.X);
                    maxY = Math.Max(maxY, (float)segment.End.Y);

                    // 원호인 경우 중심점 ± 반지름 포함
                    if (segment is ArcSegment arc)
                    {
                        double centerX = arc.Center.X;
                        double centerY = arc.Center.Y;
                        double radius = arc.Radius;

                        minX = Math.Min(minX, (float)(centerX - radius));
                        minY = Math.Min(minY, (float)(centerY - radius));
                        maxX = Math.Max(maxX, (float)(centerX + radius));
                        maxY = Math.Max(maxY, (float)(centerY + radius));
                    }
                }
            }

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 컨투어의 바운딩 박스 계산
        /// </summary>
        private static RectangleF CalculateContourBoundingBox(Contour contour)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (var segment in contour.AllSegments)
            {
                // 시작점
                minX = Math.Min(minX, (float)segment.Start.X);
                minY = Math.Min(minY, (float)segment.Start.Y);
                maxX = Math.Max(maxX, (float)segment.Start.X);
                maxY = Math.Max(maxY, (float)segment.Start.Y);

                // 끝점
                minX = Math.Min(minX, (float)segment.End.X);
                minY = Math.Min(minY, (float)segment.End.Y);
                maxX = Math.Max(maxX, (float)segment.End.X);
                maxY = Math.Max(maxY, (float)segment.End.Y);

                // 원호인 경우 중심점 ± 반지름 포함
                if (segment is ArcSegment arc)
                {
                    double centerX = arc.Center.X;
                    double centerY = arc.Center.Y;
                    double radius = arc.Radius;

                    minX = Math.Min(minX, (float)(centerX - radius));
                    minY = Math.Min(minY, (float)(centerY - radius));
                    maxX = Math.Max(maxX, (float)(centerX + radius));
                    maxY = Math.Max(maxY, (float)(centerY + radius));
                }
            }

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 줌 레벨에 따른 라벨 스케일 계산
        /// </summary>
        /// <param name="zoom">현재 줌 레벨 (1.0 = 100%)</param>
        /// <param name="baseFontSize">기본 폰트 크기 (픽셀)</param>
        /// <returns>적용할 스케일 값</returns>
        public static double CalculateLabelScale(double zoom, int baseFontSize)
        {
            // 줌이 클수록 라벨이 작아져야 함 (화면에서 일정한 크기 유지)
            // 기본 폰트 크기를 월드 단위로 변환
            // 픽셀을 mm로 변환: 작은 폰트에 맞게 조정
            const double PIXEL_TO_MM = 0.01;  // Reduced for finer control
            
            double worldFontSize = baseFontSize * PIXEL_TO_MM;
            
            // 줌 레벨에 반비례하는 스케일
            return worldFontSize / zoom;
        }
    }
}
