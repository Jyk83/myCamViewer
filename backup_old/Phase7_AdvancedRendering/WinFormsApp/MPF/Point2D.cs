using System;

namespace CamViewerPOC.MPF
{
    /// <summary>
    /// 2D coordinate structure
    /// </summary>
    public struct Point2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point2D(double x, double y) : this()
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return string.Format("({0:F3}, {1:F3})", X, Y);
        }
    }
}
