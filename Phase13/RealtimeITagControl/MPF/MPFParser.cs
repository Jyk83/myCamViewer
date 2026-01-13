using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RealtimeITagControl.MPF
{
    /// <summary>
    /// HK MPF 파일 파서 (TypeScript 버전에서 포팅)
    /// HK 레이저 절단 프로그램 파일 파서
    /// </summary>
    public class MPFParser
    {
        private List<string> lines;
        private List<Command> commands;
        // debug 및 logFilePath 변수 삭제됨

        public MPFParser(bool enableDebug = true)
        {
            lines = new List<string>();
            commands = new List<Command>();
            // debug = enableDebug; // 삭제됨
        }

        /// <summary>
        /// MPF 파일 파싱
        /// </summary>
        public MPFProgram Parse(string content)
        {
            // 라인 분리 및 정리
            lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(line => line.Trim())
                          .Where(line => line.Length > 0)
                          .ToList();

            commands = new List<Command>();

            // 모든 라인을 커맨드로 파싱
            ParseAllCommands();

            // 구조화된 데이터로 변환
            MPFProgram program = BuildMPFProgram();
            return program;
        }

        /// <summary>
        /// 모든 커맨드 파싱
        /// </summary>
        private void ParseAllCommands()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                Command command = ParseLine(line);
                if (command != null)
                {
                    commands.Add(command);
                }
            }
        }

        /// <summary>
        /// 한 줄 파싱 (N 블록 번호와 명령어가 한 줄에 있는 경우도 처리)
        /// </summary>
        private Command ParseLine(string line)
        {
            // 주석 처리
            if (line.StartsWith(";"))
            {
                return new CommentCommand { Text = line.Substring(1).Trim() };
            }

            // N 블록 번호 처리
            if (Regex.IsMatch(line, @"^N\d+"))
            {
                Match match = Regex.Match(line, @"^N(\d+)\s*(.*)");
                if (match.Success)
                {
                    int blockNumber = int.Parse(match.Groups[1].Value);
                    string remainder = match.Groups[2].Value.Trim();

                    // N 블록 커맨드 추가
                    commands.Add(new NBlockCommand { BlockNumber = blockNumber });

                    // 남은 부분이 있으면 재귀적으로 파싱
                    if (remainder.Length > 0)
                    {
                        return ParseLine(remainder);
                    }

                    return null;
                }
            }

            // HK 함수
            if (line.StartsWith("HK"))
            {
                return ParseHKCommand(line);
            }

            // G-code
            if (Regex.IsMatch(line, @"^[GM]\d+") || Regex.IsMatch(line, @"^[XYZ]"))
            {
                return ParseGCode(line);
            }

            return null;
        }

        /// <summary>
        /// HK 함수 파싱
        /// </summary>
        private HKCommand ParseHKCommand(string line)
        {
            Match funcMatch = Regex.Match(line, @"^(HK[A-Z]+)");
            if (!funcMatch.Success) return null;

            string funcName = funcMatch.Groups[1].Value;

            // 괄호가 없는 경우도 처리
            Match argsMatch = Regex.Match(line, @"\(([^)]*)\)");
            string[] args = argsMatch.Success 
                ? argsMatch.Groups[1].Value.Split(',').Select(a => a.Trim()).ToArray() 
                : new string[0];

            switch (funcName)
            {
                case "HKLDB":
                    return new HKLDBCommand
                    {
                        Material = ParseNumber(GetArg(args, 0)),
                        DbName = GetArg(args, 1).Replace("\"", ""),
                        AssistGas = ParseNumber(GetArg(args, 2))
                    };

                case "HKINI":
                    return new HKINICommand
                    {
                        TotalParts = (int)ParseNumber(GetArg(args, 0)),
                        Width = ParseNumber(GetArg(args, 1)),
                        Height = ParseNumber(GetArg(args, 2))
                    };

                case "HKOST":
                    return new HKOSTCommand
                    {
                        X = ParseNumber(GetArg(args, 0)),
                        Y = ParseNumber(GetArg(args, 1)),
                        Rotation = ParseNumber(GetArg(args, 2)),
                        PartNumber = (int)ParseNumber(GetArg(args, 3)),
                        ContourCount = (int)ParseNumber(GetArg(args, 4))
                    };

                case "HKPPP":
                    return new HKPPPCommand();

                case "HKSTR":
                    return new HKSTRCommand
                    {
                        PiercingType = (int)ParseNumber(GetArg(args, 0)),
                        CuttingType = (int)ParseNumber(GetArg(args, 1)),
                        X = ParseNumber(GetArg(args, 2)),
                        Y = ParseNumber(GetArg(args, 3)),
                        ToolCompensation = (int)ParseNumber(GetArg(args, 4)),
                        ContourWidth = ParseNumber(GetArg(args, 5)),
                        ContourHeight = ParseNumber(GetArg(args, 6))
                    };

                case "HKPIE":
                    return new HKPIECommand();

                case "HKLEA":
                    return new HKLEACommand
                    {
                        GCode = (int)ParseNumber(GetArg(args, 0)),
                        X = ParseNumber(GetArg(args, 1)),
                        Y = ParseNumber(GetArg(args, 2)),
                        I = ParseNumber(GetArg(args, 3)),
                        J = ParseNumber(GetArg(args, 4))
                    };

                case "HKCUT":
                    return new HKCUTCommand();

                case "HKSTO":
                    return new HKSTOCommand
                    {
                        GCode = (int)ParseNumber(GetArg(args, 0)),
                        X = ParseNumber(GetArg(args, 1)),
                        Y = ParseNumber(GetArg(args, 2)),
                        I = ParseNumber(GetArg(args, 3)),
                        J = ParseNumber(GetArg(args, 4)),
                        WebOnOff = (int)ParseNumber(GetArg(args, 5))
                    };

                case "HKPED":
                    return new HKPEDCommand();

                case "HKEND":
                    return new HKENDCommand();

                case "HKSCRC":
                    if (args.Length >= 4)
                    {
                        return new HKSCRCCommand
                        {
                            CuttingType = (int)ParseNumber(GetArg(args, 0)),
                            CuttingKind = (int)ParseNumber(GetArg(args, 1)),
                            X = ParseNumber(GetArg(args, 2)),
                            Y = ParseNumber(GetArg(args, 3))
                        };
                    }
                    else if (args.Length == 1)
                    {
                        return new HKSCRCCommand
                        {
                            Params = (int)ParseNumber(GetArg(args, 0))
                        };
                    }
                    return new HKSCRCCommand();

                default:
                    return null;
            }
        }

        /// <summary>
        /// G-code 파싱
        /// </summary>
        private GCodeCommand ParseGCode(string line)
        {
            GCodeCommand gcode = new GCodeCommand();

            // G 또는 M 코드 추출
            Match cmdMatch = Regex.Match(line, @"^([GM]\d+)");
            if (cmdMatch.Success)
            {
                gcode.CommandCode = cmdMatch.Groups[1].Value;
            }

            // X, Y, Z, I, J, F 파라미터 추출
            Match xMatch = Regex.Match(line, @"X([-+]?[\d.]+)");
            Match yMatch = Regex.Match(line, @"Y([-+]?[\d.]+)");
            Match zMatch = Regex.Match(line, @"Z([-+]?[\d.]+)");
            Match iMatch = Regex.Match(line, @"I([-+]?[\d.]+)");
            Match jMatch = Regex.Match(line, @"J([-+]?[\d.]+)");
            Match fMatch = Regex.Match(line, @"F([-+]?[\d.]+)");

            if (xMatch.Success) gcode.X = double.Parse(xMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            if (yMatch.Success) gcode.Y = double.Parse(yMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            if (zMatch.Success) gcode.Z = double.Parse(zMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            if (iMatch.Success) gcode.I = double.Parse(iMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            if (jMatch.Success) gcode.J = double.Parse(jMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            if (fMatch.Success) gcode.F = double.Parse(fMatch.Groups[1].Value, CultureInfo.InvariantCulture);

            return gcode;
        }

        /// <summary>
        /// MPF 프로그램 구조 생성
        /// </summary>
        private MPFProgram BuildMPFProgram()
        {
            string version = ExtractVersion();

            HKLDBCommand hkldb = ExtractHKLDB();

            HKINICommand hkini = ExtractHKINI();

            List<NestingInfo> nesting = ExtractNesting();

            List<Part> parts = ExtractParts(nesting);

            return new MPFProgram
            {
                Version = version,
                HKLDB = hkldb,
                HKINI = hkini,
                Nesting = nesting,
                Parts = parts,
                Workpiece = new Workpiece
                {
                    Width = hkini != null ? hkini.Width : 0,
                    Height = hkini != null ? hkini.Height : 0
                },
                RawCommands = commands
            };
        }

        /// <summary>
        /// 버전 추출
        /// </summary>
        private string ExtractVersion()
        {
            CommentCommand versionCmd = commands
                .OfType<CommentCommand>()
                .FirstOrDefault(cmd => cmd.Text.StartsWith("!V"));
            return versionCmd != null ? versionCmd.Text : "Unknown";
        }

        /// <summary>
        /// HKLDB 추출
        /// </summary>
        private HKLDBCommand ExtractHKLDB()
        {
            HKLDBCommand hkldb = commands.OfType<HKLDBCommand>().FirstOrDefault();
            if (hkldb == null)
            {
                LogHelper.Log("MPFParser", "WARNING: HKLDB command not found");
            }
            return hkldb;
        }

        /// <summary>
        /// HKINI 추출
        /// </summary>
        private HKINICommand ExtractHKINI()
        {
            HKINICommand hkini = commands.OfType<HKINICommand>().FirstOrDefault();
            if (hkini == null)
            {
                LogHelper.Log("MPFParser", "WARNING: HKINI command not found");
            }
            return hkini;
        }

        /// <summary>
        /// 네스팅 정보 추출
        /// </summary>
        private List<NestingInfo> ExtractNesting()
        {
            // 커맨드 타입 분포 출력
            int nblockCount = commands.OfType<NBlockCommand>().Count();
            int hkostCount = commands.OfType<HKOSTCommand>().Count();
            int hkendCount = commands.OfType<HKENDCommand>().Count();
            
            List<NestingInfo> nesting = new List<NestingInfo>();
            int currentBlockNumber = 0;
            int hkostIndex = 0;

            for (int i = 0; i < commands.Count; i++)
            {
                Command cmd = commands[i];

                // N 블록 번호 확인
                if (cmd is NBlockCommand)
                {
                    currentBlockNumber = ((NBlockCommand)cmd).BlockNumber;
                }

                // HKOST 발견
                if (cmd is HKOSTCommand)
                {
                    HKOSTCommand hkost = (HKOSTCommand)cmd;
                    hkostIndex++;
                    nesting.Add(new NestingInfo
                    {
                        PartOriginBlockNumber = currentBlockNumber,
                        Origin = new Point2D(hkost.X, hkost.Y),
                        Rotation = hkost.Rotation,
                        PartCodeBlockNumber = hkost.PartNumber,
                        ContourCount = hkost.ContourCount
                    });
                }

                // HKPPP 확인
                if (cmd is HKPPPCommand)
                {
                }

                // HKEND 발견 시 네스팅 정보 종료 (하지만 파트 정의는 계속 읽음)
                if (cmd is HKENDCommand)
                {
                    // ⚠️ break 제거: HKEND 이후에도 파트 블록(N10001~)을 읽어야 함
                    break;
                }
            }

            
            if (nesting.Count == 0)
            {
                LogHelper.Log("MPFParser", "⚠️ WARNING: 네스팅 정보가 없습니다! HKOST 커맨드가 HKEND 이전에 존재하는지 확인하세요.");
            }
            
            return nesting;
        }

        /// <summary>
        /// 파트 정보 추출
        /// </summary>
        private List<Part> ExtractParts(List<NestingInfo> nesting)
        {
            List<Part> parts = new List<Part>();

            foreach (NestingInfo nest in nesting)
            {
                Part part = ExtractPart(nest);
                if (part != null)
                {
                    parts.Add(part);
                }
                else
                {
                    LogHelper.Log("MPFParser", "  -> 파트를 찾을 수 없음!");
                }
            }

            return parts;
        }

        /// <summary>
        /// 개별 파트 추출
        /// </summary>
        private Part ExtractPart(NestingInfo nest)
        {
            // 전체 N블록 번호 출력 (디버깅용)
            var allNBlocks = commands.OfType<NBlockCommand>().Select(cmd => cmd.BlockNumber).ToList();

            // 파트 시작 블록 찾기
            int startIdx = -1;
            for (int i = 0; i < commands.Count; i++)
            {
                Command cmd = commands[i];
                if (cmd is NBlockCommand && ((NBlockCommand)cmd).BlockNumber == nest.PartCodeBlockNumber)
                {
                    startIdx = i;
                    break;
                }
            }

            if (startIdx == -1)
            {
                LogHelper.Log("MPFParser", string.Format("  ⚠️ ERROR: 블록 번호 {0}를 찾을 수 없음! commands에 해당 NBlock이 없습니다.", nest.PartCodeBlockNumber));
                return null;
            }

            // 컨투어 추출
            List<Contour> contours = new List<Contour>();
            Contour currentContour = null;
            int currentBlockNumber = nest.PartCodeBlockNumber;
            Point2D currentPosition = new Point2D(0, 0);
            bool inCutting = false;

            for (int i = startIdx; i < commands.Count; i++)
            {
                Command cmd = commands[i];

                // 블록 번호 업데이트
                if (cmd is NBlockCommand)
                {
                    currentBlockNumber = ((NBlockCommand)cmd).BlockNumber;
                }

                // HKSTR: 컨투어 시작
                if (cmd is HKSTRCommand)
                {
                    HKSTRCommand hkstr = (HKSTRCommand)cmd;
                    
                    currentContour = new Contour
                    {
                        Id = "contour-" + currentBlockNumber,
                        BlockNumber = currentBlockNumber,
                        PiercingType = hkstr.PiercingType,
                        CuttingType = hkstr.CuttingType,
                        PiercingPosition = new Point2D(hkstr.X, hkstr.Y),
                        ToolCompensation = hkstr.ToolCompensation,
                        BoundingBox = new BoundingBox
                        {
                            Width = hkstr.ContourWidth,
                            Height = hkstr.ContourHeight
                        }
                    };
                    currentPosition = new Point2D(hkstr.X, hkstr.Y);
                    inCutting = false;
                }

                // HKPIE: 피어싱
                if (cmd is HKPIECommand && currentContour != null)
                {
                }

                // HKLEA: Lead-in 정보
                if (cmd is HKLEACommand && currentContour != null)
                {
                    HKLEACommand hklea = (HKLEACommand)cmd;
                    
                    if (hklea.GCode > 0 && (hklea.X != 0 || hklea.Y != 0))
                    {
                        PathSegment segment = CreateSegment(
                            hklea.GCode,
                            currentPosition,
                            new Point2D(hklea.X, hklea.Y),
                            hklea.I,
                            hklea.J
                        );
                        if (segment != null)
                        {
                            currentContour.LeadIn = new LeadInInfo
                            {
                                GCode = hklea.GCode,
                                Path = new List<PathSegment> { segment }
                            };
                            currentPosition = new Point2D(hklea.X, hklea.Y);
                        }
                    }
                    
                    // Mark that we're in lead-in mode (not cutting yet)
                    inCutting = false;
                }

                // HKCUT: 절단 시작 (리드인 끝)
                if (cmd is HKCUTCommand && currentContour != null)
                {
                    inCutting = true;
                }

                // G-code: HKCUT 이후는 절단 경로, HKLEA 이후 HKCUT 전은 리드인 경로
                if (cmd is GCodeCommand && currentContour != null)
                {
                    GCodeCommand gcode = (GCodeCommand)cmd;
                    if (gcode.X.HasValue && gcode.Y.HasValue)
                    {
                        // Phase 13: 원본 G-code 재구성
                        string originalGCode = ReconstructGCode(gcode);

                        PathSegment segment = CreateSegment(
                            GetGCodeNumber(gcode.CommandCode),
                            currentPosition,
                            new Point2D(gcode.X.Value, gcode.Y.Value),
                            gcode.I ?? 0,
                            gcode.J ?? 0,
                            originalGCode  // Phase 13: 원본 G-code 전달
                        );
                        if (segment != null)
                        {
                            if (inCutting)
                            {
                                // HKCUT 이후: 절단 경로
                                currentContour.CuttingPath.Add(segment);
                            }
                            else if (currentContour.LeadIn != null)
                            {
                                // HKLEA 이후 HKCUT 전: 리드인 경로
                                currentContour.LeadIn.Path.Add(segment);
                            }
                        }
                        currentPosition = new Point2D(gcode.X.Value, gcode.Y.Value);
                    }
                }

                // HKSTO: 컨투어 종료
                if (cmd is HKSTOCommand && currentContour != null)
                {
                    HKSTOCommand hksto = (HKSTOCommand)cmd;
                    currentContour.EndGCode = hksto.GCode;
                    currentContour.EndPosition = new Point2D(hksto.X, hksto.Y);

                    // 마지막 세그먼트 추가
                    if (hksto.GCode > 0 && (hksto.X != currentPosition.X || hksto.Y != currentPosition.Y))
                    {
                        string segmentType = hksto.GCode == 1 ? "직선" : hksto.GCode == 2 ? "G2원호" : "G3원호";
                        
                        PathSegment segment = CreateSegment(
                            hksto.GCode,
                            currentPosition,
                            new Point2D(hksto.X, hksto.Y),
                            hksto.I,
                            hksto.J
                        );
                        if (segment != null)
                        {
                            currentContour.CuttingPath.Add(segment);
                            currentPosition = new Point2D(hksto.X, hksto.Y);
                        }
                    }

                    // 전체 세그먼트 통합
                    currentContour.AllSegments = new List<PathSegment>();
                    if (currentContour.LeadIn != null)
                    {
                        currentContour.AllSegments.AddRange(currentContour.LeadIn.Path);
                    }
                    currentContour.AllSegments.AddRange(currentContour.ApproachPath);
                    currentContour.AllSegments.AddRange(currentContour.CuttingPath);

                    contours.Add(currentContour);
                    currentContour = null;
                    inCutting = false;
                }

                // HKPED: 파트 종료
                if (cmd is HKPEDCommand)
                {
                    break;
                }
            }

            // Phase 5: 마지막 컨투어의 크기를 파트 크기로 사용 (HKSTR의 ContourWidth/Height)
            double partWidth = 0;
            double partHeight = 0;
            if (contours.Count > 0)
            {
                Contour lastContour = contours[contours.Count - 1];
                if (lastContour.BoundingBox != null)
                {
                    partWidth = lastContour.BoundingBox.Width;
                    partHeight = lastContour.BoundingBox.Height;
                }
            }

            Part part = new Part
            {
                Id = "part-" + nest.PartOriginBlockNumber,
                BlockNumber = nest.PartCodeBlockNumber,
                Origin = nest.Origin,
                Rotation = nest.Rotation,
                Contours = contours,
                Width = partWidth,
                Height = partHeight
            };

            LogHelper.Log("MPFParser", string.Format("파트 추출 완료: {0}개 컨투어, 크기: {1:F2}x{2:F2}", 
                contours.Count, partWidth, partHeight));
            return part;
        }

        /// <summary>
        /// 경로 세그먼트 생성
        /// </summary>
        /// <summary>
        /// Phase 13: PathSegment 생성 (원본 G-code 포함)
        /// </summary>
        private PathSegment CreateSegment(int gCode, Point2D start, Point2D end, double i, double j, string originalGCode = null)
        {
            PathSegment segment = null;

            if (gCode == 1 || gCode == 0)
            {
                // G0, G1: 직선
                segment = new LineSegment(start, end);
            }
            else if (gCode == 2 || gCode == 3)
            {
                // G2, G3: 원호
                Point2D center = new Point2D(start.X + i, start.Y + j);
                double radius = Math.Sqrt(i * i + j * j);
                // Phase 7 FIX: 각도를 라디안에서 도(degree)로 변환
                double startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X) * 180.0 / Math.PI;
                double endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X) * 180.0 / Math.PI;

                segment = new ArcSegment(start, end, center, radius, gCode == 2, startAngle, endAngle, i, j);
            }

            // Phase 13: 원본 G-code 저장
            if (segment != null && !string.IsNullOrEmpty(originalGCode))
            {
                segment.OriginalGCode = originalGCode;
            }

            return segment;
        }

        /// <summary>
        /// G-code 문자열에서 숫자 추출
        /// </summary>
        private int GetGCodeNumber(string command)
        {
            if (string.IsNullOrEmpty(command)) return 0;
            Match match = Regex.Match(command, @"\d+");
            return match.Success ? int.Parse(match.Value) : 0;
        }

        /// <summary>
        /// 문자열을 숫자로 변환
        /// </summary>
        private double ParseNumber(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            double num;
            if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
            {
                return num;
            }
            return 0;
        }

        /// <summary>
        /// 배열에서 안전하게 인자 가져오기
        /// </summary>
        private string GetArg(string[] args, int index)
        {
            return index < args.Length ? args[index] : string.Empty;
        }

        /// <summary>
        /// Phase 13: G-code 재구성 (원본 포맷)
        /// </summary>
        private string ReconstructGCode(GCodeCommand gcode)
        {
            StringBuilder sb = new StringBuilder();
            
            // G-code 번호
            int gcodeNum = GetGCodeNumber(gcode.CommandCode);
            sb.Append($"G{gcodeNum}");

            // X 좌표
            if (gcode.X.HasValue)
            {
                sb.Append($" X{gcode.X.Value}");
            }

            // Y 좌표
            if (gcode.Y.HasValue)
            {
                sb.Append($" Y{gcode.Y.Value}");
            }

            // I (원호 중심 X offset)
            if (gcode.I.HasValue && gcode.I.Value != 0)
            {
                sb.Append($" I{gcode.I.Value}");
            }

            // J (원호 중심 Y offset)
            if (gcode.J.HasValue && gcode.J.Value != 0)
            {
                sb.Append($" J{gcode.J.Value}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// MPF 파일 파싱 헬퍼 클래스
    /// </summary>
    public static class MPFParserHelper
    {
        /// <summary>
        /// MPF 파일 파싱 (편의 함수)
        /// </summary>
        public static MPFProgram ParseMPFFile(string content, bool enableDebug = true)
        {
            MPFParser parser = new MPFParser(enableDebug);
            return parser.Parse(content);
        }
    }
}