using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        private bool debug;

        public MPFParser(bool enableDebug = true)
        {
            lines = new List<string>();
            commands = new List<Command>();
            debug = enableDebug;
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

            Log("=== MPF 파서 시작 ===");
            Log("총 라인 수: " + lines.Count);

            // 모든 라인을 커맨드로 파싱
            ParseAllCommands();
            Log("파싱된 커맨드 수: " + commands.Count);

            // 구조화된 데이터로 변환
            MPFProgram program = BuildMPFProgram();
            Log("=== MPF 파서 완료 ===");
            return program;
        }

        private void Log(string message)
        {
            if (debug)
            {
                Console.WriteLine("[MPFParser] " + message);
            }
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

            Log("HK 함수 파싱: " + funcName + ", 인자: " + args.Length + "개");

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
            Log("프로그램 구조 생성 시작");

            string version = ExtractVersion();
            Log("버전: " + version);

            HKLDBCommand hkldb = ExtractHKLDB();
            Log("HKLDB: " + (hkldb != null ? "발견" : "없음"));

            HKINICommand hkini = ExtractHKINI();
            Log("HKINI: " + (hkini != null ? "발견" : "없음"));

            List<NestingInfo> nesting = ExtractNesting();
            Log("네스팅 정보: " + nesting.Count + "개");

            List<Part> parts = ExtractParts(nesting);
            Log("파트: " + parts.Count + "개");

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
                Log("WARNING: HKLDB command not found");
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
                Log("WARNING: HKINI command not found");
            }
            return hkini;
        }

        /// <summary>
        /// 네스팅 정보 추출
        /// </summary>
        private List<NestingInfo> ExtractNesting()
        {
            Log("=== 네스팅 정보 추출 시작 ===");
            List<NestingInfo> nesting = new List<NestingInfo>();
            int currentBlockNumber = 0;
            int hkostCount = 0;

            for (int i = 0; i < commands.Count; i++)
            {
                Command cmd = commands[i];

                // N 블록 번호 확인
                if (cmd is NBlockCommand)
                {
                    currentBlockNumber = ((NBlockCommand)cmd).BlockNumber;
                    Log("  N블록: " + currentBlockNumber);
                }

                // HKOST 발견
                if (cmd is HKOSTCommand)
                {
                    HKOSTCommand hkost = (HKOSTCommand)cmd;
                    hkostCount++;
                    Log(string.Format("  HKOST #{0}: 블록={1}, 파트코드={2}", hkostCount, currentBlockNumber, hkost.PartNumber));
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
                    Log("  HKPPP 발견");
                }

                // HKEND 발견 시 네스팅 정보 종료
                if (cmd is HKENDCommand)
                {
                    Log("  HKEND 발견 - 네스팅 종료");
                    break;
                }
            }

            Log("=== 네스팅 정보 추출 완료: " + nesting.Count + "개 ===");
            return nesting;
        }

        /// <summary>
        /// 파트 정보 추출
        /// </summary>
        private List<Part> ExtractParts(List<NestingInfo> nesting)
        {
            List<Part> parts = new List<Part>();

            Log(nesting.Count + "개 파트 추출 시작");

            foreach (NestingInfo nest in nesting)
            {
                Log("파트 " + nest.PartCodeBlockNumber + " 추출 중...");
                Part part = ExtractPart(nest);
                if (part != null)
                {
                    Log("  -> " + part.Contours.Count + "개 컨투어 발견");
                    parts.Add(part);
                }
                else
                {
                    Log("  -> 파트를 찾을 수 없음!");
                }
            }

            return parts;
        }

        /// <summary>
        /// 개별 파트 추출
        /// </summary>
        private Part ExtractPart(NestingInfo nest)
        {
            Log("파트 추출: 블록 " + nest.PartCodeBlockNumber);

            // 파트 시작 블록 찾기
            int startIdx = -1;
            for (int i = 0; i < commands.Count; i++)
            {
                Command cmd = commands[i];
                if (cmd is NBlockCommand && ((NBlockCommand)cmd).BlockNumber == nest.PartCodeBlockNumber)
                {
                    startIdx = i;
                    Log("  시작 인덱스: " + startIdx);
                    break;
                }
            }

            if (startIdx == -1)
            {
                Log("  ERROR: 블록 번호 " + nest.PartCodeBlockNumber + "를 찾을 수 없음!");
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
                    Log(string.Format("  컨투어 시작: 블록 {0}, piercingType={1}, cuttingType={2}", 
                        currentBlockNumber, hkstr.PiercingType, hkstr.CuttingType));
                    
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
                    Log("    피어싱 실행");
                }

                // HKLEA: Lead-in 정보
                if (cmd is HKLEACommand && currentContour != null)
                {
                    HKLEACommand hklea = (HKLEACommand)cmd;
                    Log(string.Format("    HKLEA: gCode={0}, x={1}, y={2}", hklea.GCode, hklea.X, hklea.Y));
                    
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
                            Log("    Lead-in 세그먼트 추가");
                        }
                    }
                    
                    // Mark that we're in lead-in mode (not cutting yet)
                    inCutting = false;
                }

                // HKCUT: 절단 시작 (리드인 끝)
                if (cmd is HKCUTCommand && currentContour != null)
                {
                    Log("    HKCUT - 절단 시작 (리드인 종료)");
                    inCutting = true;
                }

                // G-code: HKCUT 이후는 절단 경로, HKLEA 이후 HKCUT 전은 리드인 경로
                if (cmd is GCodeCommand && currentContour != null)
                {
                    GCodeCommand gcode = (GCodeCommand)cmd;
                    if (gcode.X.HasValue && gcode.Y.HasValue)
                    {
                        PathSegment segment = CreateSegment(
                            GetGCodeNumber(gcode.CommandCode),
                            currentPosition,
                            new Point2D(gcode.X.Value, gcode.Y.Value),
                            gcode.I ?? 0,
                            gcode.J ?? 0
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
                                Log("    Lead-in 경로 세그먼트 추가");
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

                    Log(string.Format("    HKSTO: gCode={0}, {1}개 기존 세그먼트", hksto.GCode, currentContour.CuttingPath.Count));

                    // 마지막 세그먼트 추가
                    if (hksto.GCode > 0 && (hksto.X != currentPosition.X || hksto.Y != currentPosition.Y))
                    {
                        string segmentType = hksto.GCode == 1 ? "직선" : hksto.GCode == 2 ? "G2원호" : "G3원호";
                        Log(string.Format("    마지막 세그먼트 추가: {0} ({1:F3},{2:F3}) → ({3:F3},{4:F3})",
                            segmentType, currentPosition.X, currentPosition.Y, hksto.X, hksto.Y));
                        
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
                            Log("      세그먼트 추가 성공! 타입: " + segment.Type);
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
                    Log("  컨투어 완료: " + contours.Count + "번째");
                    currentContour = null;
                    inCutting = false;
                }

                // HKPED: 파트 종료
                if (cmd is HKPEDCommand)
                {
                    Log("  파트 종료");
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

            Log(string.Format("파트 추출 완료: {0}개 컨투어, 크기: {1:F2}x{2:F2}", 
                contours.Count, partWidth, partHeight));
            return part;
        }

        /// <summary>
        /// 경로 세그먼트 생성
        /// </summary>
        private PathSegment CreateSegment(int gCode, Point2D start, Point2D end, double i, double j)
        {
            if (gCode == 1 || gCode == 0)
            {
                // G0, G1: 직선
                return new LineSegment(start, end);
            }
            else if (gCode == 2 || gCode == 3)
            {
                // G2, G3: 원호
                Point2D center = new Point2D(start.X + i, start.Y + j);
                double radius = Math.Sqrt(i * i + j * j);
                // Phase 7 FIX: 각도를 라디안에서 도(degree)로 변환
                double startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X) * 180.0 / Math.PI;
                double endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X) * 180.0 / Math.PI;

                return new ArcSegment(start, end, center, radius, gCode == 2, startAngle, endAngle, i, j);
            }

            return null;
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
