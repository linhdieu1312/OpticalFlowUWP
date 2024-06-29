using System.Collections.Generic;
using Windows.Foundation;

namespace Win2DCustomEffects
{
    public class MaskInfo
    {
        public MaskInfo(double x, double y, double w, double h)
        {
            PosX = x;
            PosY = y;
            UIWidth = w;
            UIHeight = h;
            RealPoint = new Point(-1, -1);

            StartTime = 0;
            StopTime = 0;

            BlurAmount = 10f;
            MaskShape = 1;
            MaskType = 0;

            WindowSize = 31;
            MaximumIterations = 20;
            Epsilon = 0.03;

            IsTrackingMask = true;
            FalseAlarmCount = 0;
        }

        public int FalseAlarmCount { get; set; }

        public Point RealPoint { get; set; }

        public double PosX { get; set; }
        public double PosY { get; set; }
        public double UIWidth { get; set; }
        public double UIHeight { get; set; }

        public double StartTime { get; set; }
        public double StopTime { get; set; }

        public double BlurAmount { get; set; }
        public int MaskShape { get; set; }  // 0:Rectangle or 1:Ellipse
        public int MaskType { get; set; }   // 0:Blur or 1:Pixelate

        public bool IsTrackingMask { get; set; }

        public int WindowSize { get; set; }
        public int MaximumIterations { get; set; }
        public double Epsilon { get; set; }

        public List<TrackingPoint> TrackingPath;
    }

    public class TrackingPoint
    {
        public TrackingPoint(Point realPoint, double relativeTime)
        {
            RealPoint = realPoint;
            RelativeTime = relativeTime;
        }

        public Point RealPoint { get; set; }
        public double RelativeTime { get; set; }
    }
}
