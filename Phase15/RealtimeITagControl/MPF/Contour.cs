using System;
using System.Collections.Generic;

namespace RealtimeITagControl.MPF
{
    /// <summary>
    /// Lead-in 정보
    /// </summary>
    public class LeadInInfo
    {
        public int GCode { get; set; }
        public List<PathSegment> Path { get; set; }

        public LeadInInfo()
        {
            Path = new List<PathSegment>();
        }
    }

    /// <summary>
    /// 경계 상자
    /// </summary>
    public class BoundingBox
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    /// <summary>
    /// 컨투어 (절단 윤곽선)
    /// </summary>
    public class Contour
    {
        public string Id { get; set; }
        public int BlockNumber { get; set; }
        public int PiercingType { get; set; }
        public int CuttingType { get; set; }
        public Point2D PiercingPosition { get; set; }
        public int ToolCompensation { get; set; }
        public BoundingBox BoundingBox { get; set; }
        public LeadInInfo LeadIn { get; set; }
        public List<PathSegment> ApproachPath { get; set; }
        public List<PathSegment> CuttingPath { get; set; }
        public List<PathSegment> AllSegments { get; set; }
        public int EndGCode { get; set; }
        public Point2D EndPosition { get; set; }

        public Contour()
        {
            ApproachPath = new List<PathSegment>();
            CuttingPath = new List<PathSegment>();
            AllSegments = new List<PathSegment>();
        }
    }
}
