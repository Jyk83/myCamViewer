using System;
using System.Collections.Generic;
using RealtimeITagControl.MPF;

namespace RealtimeITagControl.Selection
{
    /// <summary>
    /// 선택 관리 클래스
    /// 컨투어 선택, 엘리먼트 선택을 관리
    /// </summary>
    public class SelectionManager
    {
        #region Enums

        /// <summary>
        /// 선택 모드
        /// </summary>
        public enum SelectionMode
        {
            None,     // 선택 비활성
            Contour,  // 컨투어 선택
            Element   // 엘리먼트 선택
        }

        #endregion

        #region Properties

        /// <summary>
        /// 현재 선택 모드
        /// </summary>
        public SelectionMode CurrentMode { get; set; } = SelectionMode.None;

        /// <summary>
        /// 엘리먼트 다중 선택 활성화 여부
        /// </summary>
        public bool IsMultiSelectEnabled { get; set; } = false;

        /// <summary>
        /// 선택된 컨투어 (partIndex, contourIndex)
        /// </summary>
        public (int partIndex, int contourIndex)? SelectedContour { get; private set; } = null;

        /// <summary>
        /// 선택된 엘리먼트 집합 (partIndex, contourIndex, elementIndex)
        /// </summary>
        public HashSet<(int, int, int)> SelectedElements { get; private set; } = new HashSet<(int, int, int)>();

        /// <summary>
        /// 엘리먼트 선택 시 클릭 허용 거리 (픽셀)
        /// </summary>
        public double ClickTolerance { get; set; } = 10.0;

        #endregion

        #region Events

        /// <summary>
        /// 선택이 변경되었을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler SelectionChanged;

        #endregion

        #region Contour Selection

        /// <summary>
        /// 컨투어 선택
        /// </summary>
        /// <param name="partIndex">파트 인덱스</param>
        /// <param name="contourIndex">컨투어 인덱스</param>
        public void SelectContour(int partIndex, int contourIndex)
        {
            SelectedContour = (partIndex, contourIndex);
            SelectedElements.Clear(); // 엘리먼트 선택 초기화
            OnSelectionChanged();
        }

        /// <summary>
        /// 컨투어 선택 해제
        /// </summary>
        public void ClearContourSelection()
        {
            if (SelectedContour.HasValue)
            {
                SelectedContour = null;
                OnSelectionChanged();
            }
        }

        /// <summary>
        /// 특정 컨투어가 선택되어 있는지 확인
        /// </summary>
        public bool IsContourSelected(int partIndex, int contourIndex)
        {
            return SelectedContour.HasValue &&
                   SelectedContour.Value.partIndex == partIndex &&
                   SelectedContour.Value.contourIndex == contourIndex;
        }

        /// <summary>
        /// 선택 방식
        /// </summary>
        public enum ContourSelectionMethod
        {
            BoundingBox,  // 바운딩 박스 (기본값)
            ConvexHull,   // Convex Hull (볼록 껍질)
            Polygon       // 정밀 폴리곤 (Ray-casting)
        }

        /// <summary>
        /// 현재 컨투어 선택 방식
        /// </summary>
        public ContourSelectionMethod SelectionMethod { get; set; } = ContourSelectionMethod.BoundingBox;

        /// <summary>
        /// 클릭 위치에서 컨투어 찾기 (Point-in-Polygon)
        /// </summary>
        /// <param name="clickPoint">클릭 위치 (월드 좌표)</param>
        /// <param name="parts">파트 리스트</param>
        /// <param name="partOffsets">파트 오프셋 배열</param>
        /// <returns>찾은 컨투어의 (partIndex, contourIndex), 없으면 null</returns>
        public (int, int)? FindContourAtPoint(GeometryUtils.Point2D clickPoint, List<Part> parts, (float X, float Y)[] partOffsets, float scale = 1.0f)
        {
            if (parts == null || partOffsets == null)
                return null;

            // Phase 5 Fix: Area-based priority - smaller contours have higher priority
            // Collect all contours that contain the click point, then select the smallest one
            
            List<(int partIndex, int contourIndex, double area)> matchingContours = new List<(int, int, double)>();

            for (int pi = 0; pi < parts.Count; pi++)
            {
                Part part = parts[pi];
                if (part == null || part.Contours == null)
                    continue;

                float offsetX = partOffsets[pi].X;
                float offsetY = partOffsets[pi].Y;

                for (int ci = 0; ci < part.Contours.Count; ci++)
                {
                    Contour contour = part.Contours[ci];
                    if (contour == null)
                        continue;

                    // Debug: Log contour 4 (0-based, which is contour 5 in 1-based)
                    if (pi == 0 && ci == 4)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FindContourAtPoint] Checking Part {pi}, Contour {ci} (Contour 5 in 1-based)");
                        System.Diagnostics.Debug.WriteLine($"  Click point: ({clickPoint.X:F3}, {clickPoint.Y:F3})");
                        System.Diagnostics.Debug.WriteLine($"  Offset: ({offsetX:F3}, {offsetY:F3})");
                        System.Diagnostics.Debug.WriteLine($"  CuttingPath count: {contour.CuttingPath?.Count ?? 0}");
                        System.Diagnostics.Debug.WriteLine($"  AllSegments count: {contour.AllSegments?.Count ?? 0}");
                    }

                    // Point-in-Contour 검사 (선택 방식에 따라)
                    bool isInside = false;
                    try
                    {
                        switch (SelectionMethod)
                        {
                            case ContourSelectionMethod.BoundingBox:
                                isInside = GeometryUtils.IsPointInsideContour(clickPoint, contour, offsetX, offsetY, scale);
                                break;
                            case ContourSelectionMethod.ConvexHull:
                                isInside = GeometryUtils.IsPointInsideContourConvexHull(clickPoint, contour, offsetX, offsetY, scale);
                                break;
                            case ContourSelectionMethod.Polygon:
                                // Ray-casting 방식 (폴리곤 변환 후 체크)
                                var polygon = GeometryUtils.ConvertContourToPolygon(contour, offsetX, offsetY, scale);
                                isInside = GeometryUtils.IsPointInPolygon(clickPoint, polygon);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FindContourAtPoint] Exception in Point-in-Contour check for Part {pi}, Contour {ci}: {ex.Message}");
                        continue;
                    }
                    
                    if (pi == 0 && ci == 4)
                    {
                        System.Diagnostics.Debug.WriteLine($"  IsPointInsideContour result: {isInside}");
                    }
                    
                    if (isInside)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FindContourAtPoint] Point INSIDE Part {pi}, Contour {ci}");
                        
                        // Calculate contour area
                        double area = double.MaxValue;
                        try
                        {
                            area = GeometryUtils.CalculateContourArea(contour, scale);
                            System.Diagnostics.Debug.WriteLine($"[FindContourAtPoint] Contour area: {area:F3}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FindContourAtPoint] Exception in CalculateContourArea for Part {pi}, Contour {ci}: {ex.Message}");
                        }
                        
                        matchingContours.Add((pi, ci, area));
                        System.Diagnostics.Debug.WriteLine($"[FindContourAtPoint] Added Part {pi}, Contour {ci} to matching list (area: {area:F3})");
                        
                        if (pi == 0 && ci == 4)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Added to matching contours with area: {area:F3}");
                        }
                    }
                }
            }

            // If no contours found, return null
            if (matchingContours.Count == 0)
                return null;

            // If only one contour found, return it
            if (matchingContours.Count == 1)
                return (matchingContours[0].partIndex, matchingContours[0].contourIndex);

            // Multiple contours found - return the one with smallest area
            var smallest = matchingContours[0];
            for (int i = 1; i < matchingContours.Count; i++)
            {
                if (matchingContours[i].area < smallest.area)
                    smallest = matchingContours[i];
            }

            return (smallest.partIndex, smallest.contourIndex);
        }

        /// <summary>
        /// 클릭 위치에서 모든 겹치는 컨투어 찾기 (다중 선택 메뉴용)
        /// </summary>
        /// <param name="clickPoint">클릭 위치 (월드 좌표)</param>
        /// <param name="parts">파트 리스트</param>
        /// <param name="partOffsets">파트 오프셋 배열</param>
        /// <returns>찾은 모든 컨투어 리스트 (partIndex, contourIndex, area)</returns>
        public List<(int partIndex, int contourIndex, double area)> FindAllContoursAtPoint(
            GeometryUtils.Point2D clickPoint, List<Part> parts, (float X, float Y)[] partOffsets, float scale = 1.0f)
        {
            List<(int partIndex, int contourIndex, double area)> matchingContours = new List<(int, int, double)>();

            if (parts == null || partOffsets == null)
                return matchingContours;

            for (int pi = 0; pi < parts.Count; pi++)
            {
                Part part = parts[pi];
                if (part == null || part.Contours == null)
                    continue;

                float offsetX = partOffsets[pi].X;
                float offsetY = partOffsets[pi].Y;

                for (int ci = 0; ci < part.Contours.Count; ci++)
                {
                    Contour contour = part.Contours[ci];
                    if (contour == null)
                        continue;

                    // Point-in-Contour 검사 (선택 방식에 따라)
                    bool isInside = false;
                    try
                    {
                        switch (SelectionMethod)
                        {
                            case ContourSelectionMethod.BoundingBox:
                                isInside = GeometryUtils.IsPointInsideContour(clickPoint, contour, offsetX, offsetY, scale);
                                break;
                            case ContourSelectionMethod.ConvexHull:
                                isInside = GeometryUtils.IsPointInsideContourConvexHull(clickPoint, contour, offsetX, offsetY, scale);
                                break;
                            case ContourSelectionMethod.Polygon:
                                var polygon = GeometryUtils.ConvertContourToPolygon(contour, offsetX, offsetY, scale);
                                isInside = GeometryUtils.IsPointInPolygon(clickPoint, polygon);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FindAllContoursAtPoint] Exception for Part {pi}, Contour {ci}: {ex.Message}");
                        continue;
                    }

                    if (isInside)
                    {
                        double area = GeometryUtils.CalculateContourArea(contour, scale);
                        matchingContours.Add((pi, ci, area));
                    }
                }
            }

            // Phase 7: 컨투어 선택 우선순위 정렬 (외곽선 제거 전에 먼저 정렬)
            // 1순위: 면적 (작은 것 = 내부 컨투어 우선)
            // 2순위: 같은 영역이면 컨투어 번호가 큰 것 우선
            
            matchingContours.Sort((a, b) =>
            {
                // 면적 차이가 10% 이내면 "같은 영역"으로 간주
                double areaDiff = Math.Abs(a.area - b.area);
                double areaThreshold = Math.Min(a.area, b.area) * 0.1;
                
                if (areaDiff <= areaThreshold)
                {
                    // 같은 영역: 같은 파트면 큰 컨투어 번호 우선
                    if (a.partIndex == b.partIndex)
                        return b.contourIndex.CompareTo(a.contourIndex);
                    else
                        return a.partIndex.CompareTo(b.partIndex);
                }
                else
                {
                    // 다른 영역: 작은 면적(내부) 우선
                    return a.area.CompareTo(b.area);
                }
            });
            
            // 정렬 후 마지막 컨투어(외곽선) 제외 - 여러 컨투어가 겹칠 때만
            if (matchingContours.Count > 1)
            {
                for (int pi = 0; pi < parts.Count; pi++)
                {
                    if (parts[pi] == null || parts[pi].Contours == null)
                        continue;
                    
                    int lastContourIndex = parts[pi].Contours.Count - 1;
                    matchingContours.RemoveAll(c => c.partIndex == pi && c.contourIndex == lastContourIndex);
                }
            }

            return matchingContours;
        }

        #endregion

        #region Element Selection

        /// <summary>
        /// 엘리먼트 선택 (단일 또는 다중)
        /// </summary>
        /// <param name="partIndex">파트 인덱스</param>
        /// <param name="contourIndex">컨투어 인덱스</param>
        /// <param name="elementIndex">엘리먼트 인덱스</param>
        /// <param name="addToSelection">다중 선택 모드 (true면 기존 선택에 추가/제거)</param>
        public void SelectElement(int partIndex, int contourIndex, int elementIndex, bool addToSelection)
        {
            var key = (partIndex, contourIndex, elementIndex);

            if (addToSelection && IsMultiSelectEnabled)
            {
                // 다중 선택: 토글
                if (SelectedElements.Contains(key))
                {
                    SelectedElements.Remove(key);
                }
                else
                {
                    SelectedElements.Add(key);
                }
            }
            else
            {
                // 단일 선택
                SelectedElements.Clear();
                SelectedElements.Add(key);
            }

            SelectedContour = null; // 컨투어 선택 초기화
            OnSelectionChanged();
        }

        /// <summary>
        /// 엘리먼트 선택 해제
        /// </summary>
        public void ClearElementSelection()
        {
            if (SelectedElements.Count > 0)
            {
                SelectedElements.Clear();
                OnSelectionChanged();
            }
        }

        /// <summary>
        /// 특정 엘리먼트가 선택되어 있는지 확인
        /// </summary>
        public bool IsElementSelected(int partIndex, int contourIndex, int elementIndex)
        {
            return SelectedElements.Contains((partIndex, contourIndex, elementIndex));
        }

        /// <summary>
        /// 클릭 위치에서 엘리먼트 찾기 (최단 거리)
        /// </summary>
        /// <param name="clickPoint">클릭 위치 (월드 좌표)</param>
        /// <param name="parts">파트 리스트</param>
        /// <param name="partOffsets">파트 오프셋 배열</param>
        /// <param name="zoomFactor">줌 팩터 (픽셀 단위를 월드 단위로 변환)</param>
        /// <returns>찾은 엘리먼트의 (partIndex, contourIndex, elementIndex), 없으면 null</returns>
        public (int, int, int)? FindElementAtPoint(GeometryUtils.Point2D clickPoint, List<Part> parts, 
            (float X, float Y)[] partOffsets, float zoomFactor, float scale = 1.0f)
        {
            if (parts == null || partOffsets == null)
                return null;

            double minDistance = double.MaxValue;
            (int, int, int)? closestElement = null;

            // 월드 단위 허용 거리 계산
            double worldTolerance = ClickTolerance / zoomFactor;

            for (int pi = 0; pi < parts.Count; pi++)
            {
                Part part = parts[pi];
                if (part == null || part.Contours == null)
                    continue;

                float offsetX = partOffsets[pi].X;
                float offsetY = partOffsets[pi].Y;

                for (int ci = 0; ci < part.Contours.Count; ci++)
                {
                    Contour contour = part.Contours[ci];
                    if (contour == null)
                        continue;

                    // 리드인 경로
                    if (contour.LeadIn != null && contour.LeadIn.Path != null)
                    {
                        for (int ei = 0; ei < contour.LeadIn.Path.Count; ei++)
                        {
                            PathSegment segment = contour.LeadIn.Path[ei];
                            double distance = GeometryUtils.DistancePointToSegment(clickPoint, segment, offsetX, offsetY, scale);

                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestElement = (pi, ci, ei);
                            }
                        }
                    }

                    // 전체 경로 (AllSegments 사용)
                    if (contour.AllSegments != null)
                    {
                        for (int ei = 0; ei < contour.AllSegments.Count; ei++)
                        {
                            PathSegment segment = contour.AllSegments[ei];
                            double distance = GeometryUtils.DistancePointToSegment(clickPoint, segment, offsetX, offsetY, scale);

                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestElement = (pi, ci, ei);
                            }
                        }
                    }
                }
            }

            // 허용 거리 내에 있으면 반환
            if (minDistance <= worldTolerance)
            {
                return closestElement;
            }

            return null;
        }

        #endregion

        #region Clear All

        /// <summary>
        /// 모든 선택 해제
        /// </summary>
        public void ClearAllSelections()
        {
            bool changed = SelectedContour.HasValue || SelectedElements.Count > 0;

            SelectedContour = null;
            SelectedElements.Clear();

            if (changed)
            {
                OnSelectionChanged();
            }
        }

        #endregion

        #region Event Raising

        /// <summary>
        /// SelectionChanged 이벤트 발생
        /// </summary>
        protected virtual void OnSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
