using System;
using System.Drawing;
using RealtimeITagControl.MPF;

namespace RealtimeITagControl.Rendering
{
    /// <summary>
    /// Dirty Region 계산기
    /// 변경된 화면 영역만 계산하여 부분 갱신
    /// </summary>
    public class DirtyRegionCalculator
    {
        private MPFProgram currentProgram;
        private float zoom = 1.0f;
        private float panX = 0.0f;
        private float panY = 0.0f;
        private int viewportWidth = 0;
        private int viewportHeight = 0;

        /// <summary>
        /// 뷰포트 설정 업데이트
        /// </summary>
        public void UpdateViewport(int width, int height, float zoom, float panX, float panY)
        {
            this.viewportWidth = width;
            this.viewportHeight = height;
            this.zoom = zoom;
            this.panX = panX;
            this.panY = panY;
        }

        /// <summary>
        /// MPF 프로그램 설정
        /// </summary>
        public void SetProgram(MPFProgram program)
        {
            this.currentProgram = program;
        }

        /// <summary>
        /// 컨투어 영역 계산 (Dirty Region)
        /// </summary>
        public Rectangle CalculateContourRegion(int partNo, int contourNo)
        {
            if (currentProgram == null || partNo < 0 || partNo >= currentProgram.Parts.Count)
                return Rectangle.Empty;

            var part = currentProgram.Parts[partNo];
            if (contourNo < 0 || contourNo >= part.Contours.Count)
                return Rectangle.Empty;

            var contour = part.Contours[contourNo];

            // 컨투어의 월드 좌표 바운딩 박스 계산
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            // AllSegments 사용 (LineSegment, ArcSegment 타입)
            foreach (var segment in contour.AllSegments)
            {
                if (segment is LineSegment line)
                {
                    minX = Math.Min(minX, (float)Math.Min(line.Start.X, line.End.X));
                    minY = Math.Min(minY, (float)Math.Min(line.Start.Y, line.End.Y));
                    maxX = Math.Max(maxX, (float)Math.Max(line.Start.X, line.End.X));
                    maxY = Math.Max(maxY, (float)Math.Max(line.Start.Y, line.End.Y));
                }
                else if (segment is ArcSegment arc)
                {
                    // Arc의 바운딩 박스 (중심 ± 반지름)
                    minX = Math.Min(minX, (float)(arc.Center.X - arc.Radius));
                    minY = Math.Min(minY, (float)(arc.Center.Y - arc.Radius));
                    maxX = Math.Max(maxX, (float)(arc.Center.X + arc.Radius));
                    maxY = Math.Max(maxY, (float)(arc.Center.Y + arc.Radius));
                }
            }

            if (minX == float.MaxValue || minY == float.MaxValue)
                return Rectangle.Empty;

            // 월드 좌표 → 화면 좌표 변환
            Point topLeft = WorldToScreen(minX, maxY);  // Y축 반전
            Point bottomRight = WorldToScreen(maxX, minY);

            // 마진 추가 (렌더링 두께 고려)
            int margin = (int)(10 * zoom);  // 10픽셀 마진

            int x = Math.Max(0, topLeft.X - margin);
            int y = Math.Max(0, topLeft.Y - margin);
            int width = Math.Min(viewportWidth, bottomRight.X + margin) - x;
            int height = Math.Min(viewportHeight, bottomRight.Y + margin) - y;

            return new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// 진행 중인 영역 계산 (현재 파트/컨투어)
        /// </summary>
        public Rectangle CalculateProgressRegion(int currentPartNo, int currentContourNo)
        {
            // 현재 컨투어와 이전 컨투어 영역 합치기
            Rectangle currentRegion = CalculateContourRegion(currentPartNo, currentContourNo);
            
            if (currentContourNo > 0)
            {
                Rectangle prevRegion = CalculateContourRegion(currentPartNo, currentContourNo - 1);
                currentRegion = Rectangle.Union(currentRegion, prevRegion);
            }

            return currentRegion;
        }

        /// <summary>
        /// 월드 좌표 → 화면 좌표 변환
        /// </summary>
        private Point WorldToScreen(float worldX, float worldY)
        {
            // OpenGL 변환 로직과 동일
            float screenX = (worldX * zoom) + panX + (viewportWidth / 2.0f);
            float screenY = (worldY * zoom) + panY + (viewportHeight / 2.0f);

            return new Point((int)screenX, (int)screenY);
        }

        /// <summary>
        /// 전체 화면 영역 반환
        /// </summary>
        public Rectangle GetFullScreenRegion()
        {
            return new Rectangle(0, 0, viewportWidth, viewportHeight);
        }

        /// <summary>
        /// 레이저 헤드 마커 영역 계산
        /// </summary>
        public Rectangle CalculateLaserMarkerRegion(float posX, float posY, float scale = 1.0f)
        {
            Point center = WorldToScreen(posX, posY);
            int size = (int)(20 * scale * zoom);  // 마커 크기
            int margin = 5;

            return new Rectangle(
                center.X - size - margin,
                center.Y - size - margin,
                (size + margin) * 2,
                (size + margin) * 2
            );
        }
    }
}
