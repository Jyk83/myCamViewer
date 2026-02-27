using System;
using System.Collections.Generic;

namespace CamViewerPOC.MPF
{
    /// <summary>
    /// 네스팅 정보 (워크피스 내 파트 배치)
    /// </summary>
    public class NestingInfo
    {
        public int PartOriginBlockNumber { get; set; }
        public Point2D Origin { get; set; }
        public double Rotation { get; set; }
        public int PartCodeBlockNumber { get; set; }
        public int ContourCount { get; set; }
    }

    /// <summary>
    /// 파트 (절단할 부품)
    /// </summary>
    public class Part
    {
        public string Id { get; set; }
        public int BlockNumber { get; set; }
        public Point2D Origin { get; set; }
        public double Rotation { get; set; }
        public List<Contour> Contours { get; set; }
        
        // Phase 5: 파트 크기 (HKSTR의 마지막 컨투어 크기)
        public double Width { get; set; }
        public double Height { get; set; }

        public Part()
        {
            Contours = new List<Contour>();
        }
    }
}
