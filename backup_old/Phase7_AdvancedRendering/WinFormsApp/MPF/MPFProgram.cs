using System;
using System.Collections.Generic;

namespace CamViewerPOC.MPF
{
    /// <summary>
    /// 워크피스 정보
    /// </summary>
    public class Workpiece
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    /// <summary>
    /// MPF 프로그램 데이터
    /// </summary>
    public class MPFProgram
    {
        public string Version { get; set; }
        public HKLDBCommand HKLDB { get; set; }
        public HKINICommand HKINI { get; set; }
        public List<NestingInfo> Nesting { get; set; }
        public List<Part> Parts { get; set; }
        public Workpiece Workpiece { get; set; }
        public List<Command> RawCommands { get; set; }

        public MPFProgram()
        {
            Nesting = new List<NestingInfo>();
            Parts = new List<Part>();
            RawCommands = new List<Command>();
        }
    }
}
