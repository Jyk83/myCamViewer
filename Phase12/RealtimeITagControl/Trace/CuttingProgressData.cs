using System;

namespace RealtimeITagControl.Trace
{
    /// <summary>
    /// Phase 8.2: 실시간 절단 진행 상황 데이터
    /// </summary>
    public class CuttingProgressData
    {
        /// <summary>
        /// 현재 절단 중인 Part 번호 (1-based)
        /// </summary>
        public int CurrentPart { get; set; }

        /// <summary>
        /// 현재 절단 중인 Contour 번호 (1-based)
        /// </summary>
        public int CurrentContour { get; set; }

        /// <summary>
        /// 진행률 (0.0 ~ 1.0)
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// 레이저 헤드 X 위치 (WCS)
        /// </summary>
        public double PositionX { get; set; }

        /// <summary>
        /// 레이저 헤드 Y 위치 (WCS)
        /// </summary>
        public double PositionY { get; set; }

        /// <summary>
        /// 현재 실행 중인 G-code 블록
        /// </summary>
        public string CurrentBlock { get; set; }

        /// <summary>
        /// 추적 시작 Part 번호 (1-based)
        /// </summary>
        public int StartPart { get; set; }

        /// <summary>
        /// 추적 시작 Contour 번호 (1-based)
        /// </summary>
        public int StartContour { get; set; }

        /// <summary>
        /// 절단 방향 (역방향 여부)
        /// </summary>
        public bool IsReverse { get; set; }

        /// <summary>
        /// 추적 활성화 상태
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 마지막 업데이트 시간
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        public CuttingProgressData()
        {
            CurrentBlock = string.Empty;
            LastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 진행률을 백분율로 반환
        /// </summary>
        public double ProgressPercent => Progress * 100.0;

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            CurrentPart = 0;
            CurrentContour = 0;
            Progress = 0.0;
            PositionX = 0.0;
            PositionY = 0.0;
            CurrentBlock = string.Empty;
            StartPart = 0;
            StartContour = 0;
            IsReverse = false;
            IsActive = false;
            LastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 디버깅용 문자열 표현
        /// </summary>
        public override string ToString()
        {
            if (!IsActive)
                return "Trace inactive";

            return $"Part {CurrentPart}, Contour {CurrentContour}, " +
                   $"Progress: {ProgressPercent:F1}%, " +
                   $"Position: ({PositionX:F3}, {PositionY:F3})";
        }
    }
}
