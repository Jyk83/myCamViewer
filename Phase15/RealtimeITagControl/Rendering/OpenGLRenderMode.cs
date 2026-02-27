using System;

namespace RealtimeITagControl.Rendering
{
    /// <summary>
    /// OpenGL 렌더링 모드
    /// 환경변수 HMI_OPENGL_TYPE로 제어
    /// </summary>
    public enum OpenGLRenderMode
    {
        /// <summary>
        /// Type 1: Dirty Region 최적화
        /// - 변경 영역만 갱신 (부분 Invalidate)
        /// - 성능 향상 기대: 1.5-2배
        /// - MPF 로딩 후 깜빡임 방지
        /// </summary>
        DirtyRegion = 0,

        /// <summary>
        /// Type 2: Default (기본 방식)
        /// - 전체 화면 갱신 (전체 Invalidate)
        /// - 안정적이나 MPF 로딩 후 깜빡임 발생 가능
        /// </summary>
        Default = 1
    }

    /// <summary>
    /// OpenGL 렌더링 설정 관리자
    /// </summary>
    public static class OpenGLSettings
    {
        // Phase 16.x: Changed default to DirtyRegion for production (prevents flickering on device)
        private static OpenGLRenderMode _currentMode = OpenGLRenderMode.DirtyRegion;

        /// <summary>
        /// 현재 렌더링 모드
        /// </summary>
        public static OpenGLRenderMode CurrentMode
        {
            get { return _currentMode; }
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                }
            }
        }

        /// <summary>
        /// ITag 값으로 모드 설정
        /// </summary>
        public static void SetModeFromITag(int openglType)
        {
            try
            {
                if (Enum.IsDefined(typeof(OpenGLRenderMode), openglType))
                {
                    CurrentMode = (OpenGLRenderMode)openglType;
                }
                // 잘못된 값은 무시 (기본값 유지)
            }
            catch (Exception ex)
            {
                LogHelper.Log("OpenGLSettings", $"❌ 모드 설정 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 모드 설명 가져오기
        /// </summary>
        public static string GetModeDescription(OpenGLRenderMode mode)
        {
            switch (mode)
            {
                case OpenGLRenderMode.Default:
                    return "Default 방식 (전체 화면 갱신, 기준 성능)";
                
                case OpenGLRenderMode.DirtyRegion:
                    return "Dirty Region 최적화 (화면 영역만 갱신)";
                
                default:
                    return "Unknown Mode";
            }
        }
    }
}
