using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RealtimeITagControl.MPF;

namespace RealtimeITagControl.Simulation
{
    /// <summary>
    /// 절단 시뮬레이션 상태
    /// </summary>
    public enum SimulationState
    {
        Idle,       // 시뮬레이션 시작 전 (MPF 로드 후 초기 상태)
        Stopped,    // 시뮬레이션 중단됨
        Running,    // 실행 중
        Paused,     // 일시정지
        Completed   // 완료
    }

    /// <summary>
    /// 시뮬레이션 진행 이벤트 인자
    /// </summary>
    public class SimulationProgressEventArgs : EventArgs
    {
        public int PartIndex { get; set; }
        public int ContourIndex { get; set; }
        public int ElementIndex { get; set; }
        public int TotalParts { get; set; }
        public int TotalContours { get; set; }
        public int TotalElements { get; set; }
        public double ProgressPercentage { get; set; }
        public PathSegment CurrentSegment { get; set; }
        public string GCodeBlock { get; set; }
    }

    /// <summary>
    /// 시뮬레이션 엔진 (V20 로직을 Task 기반으로 현대화)
    /// </summary>
    public class SimulationEngine
    {
        private MPFProgram program;
        private CancellationTokenSource cts;
        private Task simulationTask;
        private SimulationState state;
        private int responseTime = 50; // ms

        // 현재 위치 추적
        private int currentPartIndex = 0;
        private int currentContourIndex = 0;
        private int currentElementIndex = 0;

        // 이벤트
        public event EventHandler<SimulationProgressEventArgs> ProgressUpdated;
        public event EventHandler SimulationCompleted;
        public event EventHandler<string> LogMessage;

        public SimulationState State
        {
            get { return state; }
            private set { state = value; }
        }

        public int ResponseTime
        {
            get { return responseTime; }
            set { responseTime = Math.Max(10, Math.Min(1000, value)); }
        }

        public SimulationEngine()
        {
            state = SimulationState.Idle;
        }

        /// <summary>
        /// MPF 프로그램 설정
        /// </summary>
        public void SetProgram(MPFProgram mpfProgram)
        {
            if (state == SimulationState.Running)
            {
                throw new InvalidOperationException("Cannot set program while simulation is running");
            }

            program = mpfProgram;
            ResetPosition();
            state = SimulationState.Idle; // 프로그램 설정 후 Idle 상태로
        }

        /// <summary>
        /// 시뮬레이션 시작
        /// </summary>
        public void Start()
        {
            if (program == null)
            {
                throw new InvalidOperationException("Program not set");
            }

            if (state == SimulationState.Running)
            {
                return;
            }

            if (state == SimulationState.Paused)
            {
                Resume();
                return;
            }

            ResetPosition();
            state = SimulationState.Running;
            cts = new CancellationTokenSource();

            simulationTask = Task.Run(() => RunSimulation(cts.Token), cts.Token);
        }

        /// <summary>
        /// 시뮬레이션 일시정지
        /// </summary>
        public void Pause()
        {
            if (state == SimulationState.Running)
            {
                state = SimulationState.Paused;
            }
        }

        /// <summary>
        /// 시뮬레이션 재개
        /// </summary>
        public void Resume()
        {
            if (state == SimulationState.Paused)
            {
                state = SimulationState.Running;
            }
        }

        /// <summary>
        /// 시뮬레이션 중지
        /// </summary>
        public void Stop()
        {
            if (state == SimulationState.Running || state == SimulationState.Paused)
            {
                if (cts != null)
                {
                    cts.Cancel();
                }

                state = SimulationState.Stopped; // 중단 상태
                // ResetPosition()은 호출하지 않음 - 중단된 위치 유지
            }
        }

        /// <summary>
        /// 위치 초기화
        /// </summary>
        private void ResetPosition()
        {
            currentPartIndex = 0;
            currentContourIndex = 0;
            currentElementIndex = 0;
        }

        /// <summary>
        /// 시뮬레이션 리셋 (처음부터 다시 시작 가능하도록)
        /// </summary>
        public void Reset()
        {
            Stop();
            ResetPosition();
            if (program != null)
            {
                state = SimulationState.Idle;
            }
        }

        /// <summary>
        /// 시뮬레이션 메인 루프
        /// </summary>
        private void RunSimulation(CancellationToken token)
        {
            try
            {
                Stopwatch timer = new Stopwatch();

                // Triple nested loop (V20 스타일)
                for (int pi = currentPartIndex; pi < program.Parts.Count; pi++)
                {
                    if (token.IsCancellationRequested) break;

                    Part part = program.Parts[pi];
                    currentPartIndex = pi;


                    for (int ci = currentContourIndex; ci < part.Contours.Count; ci++)
                    {
                        if (token.IsCancellationRequested) break;

                        Contour contour = part.Contours[ci];
                        currentContourIndex = ci;


                        // AllSegments: LeadIn + Approach + Cutting
                        List<PathSegment> segments = contour.AllSegments;

                        for (int ei = currentElementIndex; ei < segments.Count; ei++)
                        {
                            // 일시정지 처리
                            while (state == SimulationState.Paused)
                            {
                                if (token.IsCancellationRequested) break;
                                Thread.Sleep(100);
                            }

                            if (token.IsCancellationRequested) break;

                            // 타이밍 제어
                            timer.Restart();
                            while (timer.ElapsedMilliseconds < responseTime)
                            {
                                if (token.IsCancellationRequested) break;
                                Thread.Sleep(10);
                            }

                            currentElementIndex = ei;
                            PathSegment segment = segments[ei];

                            // 진행률 계산
                            int totalElements = program.Parts.Sum(p => p.Contours.Sum(c => c.AllSegments.Count));
                            int completedElements = 0;
                            for (int p = 0; p < pi; p++)
                            {
                                completedElements += program.Parts[p].Contours.Sum(c => c.AllSegments.Count);
                            }
                            for (int c = 0; c < ci; c++)
                            {
                                completedElements += part.Contours[c].AllSegments.Count;
                            }
                            completedElements += ei;

                            double progress = (double)completedElements / totalElements * 100.0;

                            // 이벤트 발생
                            RaiseProgressUpdated(new SimulationProgressEventArgs
                            {
                                PartIndex = pi,
                                ContourIndex = ci,
                                ElementIndex = ei,
                                TotalParts = program.Parts.Count,
                                TotalContours = part.Contours.Count,
                                TotalElements = segments.Count,
                                ProgressPercentage = progress,
                                CurrentSegment = segment,
                                GCodeBlock = FormatSegmentAsGCode(segment)
                            });
                        }

                        currentElementIndex = 0; // 다음 컨투어 시작
                    }

                    currentContourIndex = 0; // 다음 파트 시작
                }

                // 완료
                if (!token.IsCancellationRequested)
                {
                    state = SimulationState.Completed;
                    RaiseSimulationCompleted();
                }
            }
            catch (Exception ex)
            {
                state = SimulationState.Stopped;
            }
        }

        /// <summary>
        /// 세그먼트를 G-code 형식으로 변환
        /// </summary>
        private string FormatSegmentAsGCode(PathSegment segment)
        {
            if (segment is LineSegment)
            {
                LineSegment line = (LineSegment)segment;
                return string.Format("G1 X{0:F3} Y{1:F3}", line.End.X, line.End.Y);
            }
            else if (segment is ArcSegment)
            {
                ArcSegment arc = (ArcSegment)segment;
                string gcode = arc.Clockwise ? "G2" : "G3";
                return string.Format("{0} X{1:F3} Y{2:F3} I{3:F3} J{4:F3}",
                    gcode, arc.End.X, arc.End.Y, arc.I, arc.J);
            }

            return "Unknown";
        }

        /// <summary>
        /// 진행률 이벤트 발생
        /// </summary>
        private void RaiseProgressUpdated(SimulationProgressEventArgs e)
        {
            if (ProgressUpdated != null)
            {
                ProgressUpdated(this, e);
            }
        }

        /// <summary>
        /// 완료 이벤트 발생
        /// </summary>
        private void RaiseSimulationCompleted()
        {
            if (SimulationCompleted != null)
            {
                SimulationCompleted(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 로그 메시지 발생
        /// </summary>
        private void Log(string message)
        {
            if (LogMessage != null)
            {
                LogMessage(this, message);
            }
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            Stop();

            if (cts != null)
            {
                cts.Dispose();
                cts = null;
            }
        }
    }
}
