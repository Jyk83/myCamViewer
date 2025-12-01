using System;
using CamViewerPOC.MPF;

namespace CamViewerPOC.Trace
{
    /// <summary>
    /// Phase 8.2: 실시간 트레이스 관리자
    /// HKCamInterface의 StartCuttingProgress, UpdateCuttingProgress 참조
    /// </summary>
    public class TraceManager
    {
        private CuttingProgressData _currentProgress;
        private MPFProgram _mpfProgram;
        private bool _isActive;

        public TraceManager(MPFProgram mpfProgram)
        {
            _mpfProgram = mpfProgram;
            _currentProgress = new CuttingProgressData();
            _isActive = false;
        }

        /// <summary>
        /// MPF 프로그램 업데이트
        /// </summary>
        public void SetMPFProgram(MPFProgram mpfProgram)
        {
            _mpfProgram = mpfProgram;
        }

        /// <summary>
        /// 실시간 트레이스 시작
        /// HKCamInterface: CVStartCutting()
        /// </summary>
        /// <param name="startPart">시작 Part 번호 (1-based)</param>
        /// <param name="startContour">시작 Contour 번호 (1-based)</param>
        /// <param name="isReverse">역방향 절단 여부</param>
        /// <returns>성공 여부</returns>
        public bool StartTrace(int startPart, int startContour, bool isReverse = false)
        {
            // 유효성 검사
            if (_mpfProgram == null)
            {
                System.Diagnostics.Debug.WriteLine("TraceManager: MPF program not loaded");
                return false;
            }

            if (startPart < 1 || startPart > _mpfProgram.Parts.Count)
            {
                System.Diagnostics.Debug.WriteLine($"TraceManager: Invalid part number {startPart}");
                return false;
            }

            var part = _mpfProgram.Parts[startPart - 1];
            if (startContour < 1 || startContour > part.Contours.Count)
            {
                System.Diagnostics.Debug.WriteLine($"TraceManager: Invalid contour number {startContour}");
                return false;
            }

            // 상태 초기화
            _currentProgress.Reset();
            _currentProgress.StartPart = startPart;
            _currentProgress.StartContour = startContour;
            _currentProgress.CurrentPart = startPart;
            _currentProgress.CurrentContour = startContour;
            _currentProgress.Progress = 0.0;
            _currentProgress.IsReverse = isReverse;
            _currentProgress.IsActive = true;
            _currentProgress.LastUpdateTime = DateTime.Now;
            _isActive = true;

            // Native 렌더러 호출
            try
            {
                NativeRenderer.StartCuttingTrace(startPart, startContour, isReverse ? 1 : 0);
                System.Diagnostics.Debug.WriteLine($"TraceManager: Started trace at Part {startPart}, Contour {startContour}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TraceManager: Failed to start trace - {ex.Message}");
                _isActive = false;
                _currentProgress.IsActive = false;
                return false;
            }
        }

        /// <summary>
        /// 실시간 트레이스 진행 상황 업데이트
        /// HKCamInterface: CVUpdateCutting()
        /// </summary>
        /// <param name="part">현재 Part 번호 (1-based)</param>
        /// <param name="contour">현재 Contour 번호 (1-based)</param>
        /// <param name="progress">진행률 (0.0 ~ 1.0)</param>
        /// <param name="posX">레이저 헤드 X 위치 (WCS)</param>
        /// <param name="posY">레이저 헤드 Y 위치 (WCS)</param>
        /// <param name="currentBlock">현재 실행 중인 G-code 블록</param>
        /// <returns>성공 여부</returns>
        public bool UpdateProgress(int part, int contour, double progress,
                                    double posX, double posY, string currentBlock = "")
        {
            if (!_isActive)
            {
                System.Diagnostics.Debug.WriteLine("TraceManager: Update called but trace not active");
                return false;
            }

            // 유효성 검사
            if (_mpfProgram == null)
            {
                return false;
            }

            if (part < 1 || part > _mpfProgram.Parts.Count)
            {
                System.Diagnostics.Debug.WriteLine($"TraceManager: Invalid part number {part} in update");
                return false;
            }

            var currentPart = _mpfProgram.Parts[part - 1];
            if (contour < 1 || contour > currentPart.Contours.Count)
            {
                System.Diagnostics.Debug.WriteLine($"TraceManager: Invalid contour number {contour} in update");
                return false;
            }

            // 진행률 범위 제한 (0.0 ~ 1.0)
            progress = Math.Max(0.0, Math.Min(1.0, progress));

            // 상태 업데이트
            _currentProgress.CurrentPart = part;
            _currentProgress.CurrentContour = contour;
            _currentProgress.Progress = progress;
            _currentProgress.PositionX = posX;
            _currentProgress.PositionY = posY;
            _currentProgress.CurrentBlock = currentBlock ?? string.Empty;
            _currentProgress.LastUpdateTime = DateTime.Now;

            // Native 렌더러 업데이트
            try
            {
                NativeRenderer.UpdateCuttingTrace(part, contour, progress, (float)posX, (float)posY);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TraceManager: Failed to update trace - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 실시간 트레이스 중지
        /// HKCamInterface: CVStopCutting()
        /// </summary>
        public void StopTrace()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _currentProgress.IsActive = false;

            try
            {
                NativeRenderer.StopCuttingTrace();
                System.Diagnostics.Debug.WriteLine("TraceManager: Stopped trace");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TraceManager: Failed to stop trace - {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 진행 상황 데이터 가져오기
        /// </summary>
        public CuttingProgressData GetCurrentProgress()
        {
            return _currentProgress;
        }

        /// <summary>
        /// 트레이스 활성화 상태
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// 현재 Part 번호 (1-based)
        /// </summary>
        public int CurrentPart => _currentProgress.CurrentPart;

        /// <summary>
        /// 현재 Contour 번호 (1-based)
        /// </summary>
        public int CurrentContour => _currentProgress.CurrentContour;

        /// <summary>
        /// 현재 진행률 (0.0 ~ 1.0)
        /// </summary>
        public double CurrentProgress => _currentProgress.Progress;

        /// <summary>
        /// 레이저 헤드 마커 그리기
        /// </summary>
        /// <param name="scale">줌 레벨에 따른 스케일</param>
        public void DrawLaserHeadMarker(float scale = 1.0f)
        {
            if (!_isActive)
            {
                return;
            }

            try
            {
                // 주황색 레이저 헤드 마커
                NativeRenderer.DrawLaserHeadMarker(
                    (float)_currentProgress.PositionX,
                    (float)_currentProgress.PositionY,
                    scale,
                    1.0f, 0.4f, 0.0f  // RGB: Orange
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TraceManager: Failed to draw laser head marker - {ex.Message}");
            }
        }
    }
}
