using System;

namespace CamViewerPOC.MPF
{
    /// <summary>
    /// 명령어 타입
    /// </summary>
    public enum CommandType
    {
        Comment,
        NBlock,
        HK,
        GCode
    }

    /// <summary>
    /// 명령어 베이스 클래스
    /// </summary>
    public abstract class Command
    {
        public CommandType Type { get; set; }
    }

    /// <summary>
    /// 주석 명령
    /// </summary>
    public class CommentCommand : Command
    {
        public string Text { get; set; }

        public CommentCommand()
        {
            Type = CommandType.Comment;
        }
    }

    /// <summary>
    /// N 블록 번호 명령
    /// </summary>
    public class NBlockCommand : Command
    {
        public int BlockNumber { get; set; }

        public NBlockCommand()
        {
            Type = CommandType.NBlock;
        }
    }

    /// <summary>
    /// HK 함수 타입
    /// </summary>
    public enum HKCommandType
    {
        HKLDB,
        HKINI,
        HKOST,
        HKPPP,
        HKSTR,
        HKPIE,
        HKLEA,
        HKCUT,
        HKSTO,
        HKPED,
        HKEND,
        HKSCRC,
        Unknown
    }

    /// <summary>
    /// HK 명령 베이스 클래스
    /// </summary>
    public class HKCommand : Command
    {
        public HKCommandType HKType { get; set; }

        public HKCommand()
        {
            Type = CommandType.HK;
        }
    }

    /// <summary>
    /// HKLDB: 절단 데이터베이스 로드
    /// </summary>
    public class HKLDBCommand : HKCommand
    {
        public double Material { get; set; }
        public string DbName { get; set; }
        public double AssistGas { get; set; }

        public HKLDBCommand()
        {
            HKType = HKCommandType.HKLDB;
        }
    }

    /// <summary>
    /// HKINI: 초기화
    /// </summary>
    public class HKINICommand : HKCommand
    {
        public int TotalParts { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public HKINICommand()
        {
            HKType = HKCommandType.HKINI;
        }
    }

    /// <summary>
    /// HKOST: 파트 원점 설정
    /// </summary>
    public class HKOSTCommand : HKCommand
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Rotation { get; set; }
        public int PartNumber { get; set; }
        public int ContourCount { get; set; }

        public HKOSTCommand()
        {
            HKType = HKCommandType.HKOST;
        }
    }

    /// <summary>
    /// HKPPP: 파트 프로그램 포인터
    /// </summary>
    public class HKPPPCommand : HKCommand
    {
        public HKPPPCommand()
        {
            HKType = HKCommandType.HKPPP;
        }
    }

    /// <summary>
    /// HKSTR: 컨투어 시작
    /// </summary>
    public class HKSTRCommand : HKCommand
    {
        public int PiercingType { get; set; }
        public int CuttingType { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int ToolCompensation { get; set; }
        public double ContourWidth { get; set; }
        public double ContourHeight { get; set; }

        public HKSTRCommand()
        {
            HKType = HKCommandType.HKSTR;
        }
    }

    /// <summary>
    /// HKPIE: 피어싱
    /// </summary>
    public class HKPIECommand : HKCommand
    {
        public HKPIECommand()
        {
            HKType = HKCommandType.HKPIE;
        }
    }

    /// <summary>
    /// HKLEA: Lead-in
    /// </summary>
    public class HKLEACommand : HKCommand
    {
        public int GCode { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double I { get; set; }
        public double J { get; set; }

        public HKLEACommand()
        {
            HKType = HKCommandType.HKLEA;
        }
    }

    /// <summary>
    /// HKCUT: 절단 시작
    /// </summary>
    public class HKCUTCommand : HKCommand
    {
        public HKCUTCommand()
        {
            HKType = HKCommandType.HKCUT;
        }
    }

    /// <summary>
    /// HKSTO: 컨투어 종료
    /// </summary>
    public class HKSTOCommand : HKCommand
    {
        public int GCode { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double I { get; set; }
        public double J { get; set; }
        public int WebOnOff { get; set; }

        public HKSTOCommand()
        {
            HKType = HKCommandType.HKSTO;
        }
    }

    /// <summary>
    /// HKPED: 파트 종료
    /// </summary>
    public class HKPEDCommand : HKCommand
    {
        public HKPEDCommand()
        {
            HKType = HKCommandType.HKPED;
        }
    }

    /// <summary>
    /// HKEND: 프로그램 종료
    /// </summary>
    public class HKENDCommand : HKCommand
    {
        public HKENDCommand()
        {
            HKType = HKCommandType.HKEND;
        }
    }

    /// <summary>
    /// HKSCRC: 잔재 절단
    /// </summary>
    public class HKSCRCCommand : HKCommand
    {
        public int CuttingType { get; set; }
        public int CuttingKind { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int Params { get; set; }

        public HKSCRCCommand()
        {
            HKType = HKCommandType.HKSCRC;
        }
    }

    /// <summary>
    /// G-code 명령
    /// </summary>
    public class GCodeCommand : Command
    {
        public string CommandCode { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Z { get; set; }
        public double? I { get; set; }
        public double? J { get; set; }
        public double? F { get; set; }

        public GCodeCommand()
        {
            Type = CommandType.GCode;
        }
    }
}
