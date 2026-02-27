using System;
using System.Runtime.InteropServices;

namespace CamViewerPOC.Rendering
{
    /// <summary>
    /// Native TextRenderer DLL에 대한 P/Invoke 래퍼 클래스
    /// </summary>
    public static class NativeTextRenderer
    {
        private const string DllName = "NativeRenderer.dll";

        /// <summary>
        /// 텍스트 렌더러 초기화
        /// </summary>
        /// <param name="fontName">폰트 이름 (예: "Arial", "맑은 고딕")</param>
        /// <param name="height">폰트 높이 (픽셀)</param>
        /// <param name="bold">굵게 표시 여부</param>
        /// <param name="italic">이탤릭 표시 여부</param>
        /// <returns>성공 시 1, 실패 시 0</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int InitializeTextRenderer(
            [MarshalAs(UnmanagedType.LPWStr)] string fontName,
            int height,
            int bold,
            int italic);

        /// <summary>
        /// 파트 번호 그리기
        /// </summary>
        /// <param name="posX">X 위치 (월드 좌표)</param>
        /// <param name="posY">Y 위치 (월드 좌표)</param>
        /// <param name="number">표시할 번호</param>
        /// <param name="scale">스케일 (1.0 = 100%)</param>
        /// <param name="r">Red (0.0 ~ 1.0)</param>
        /// <param name="g">Green (0.0 ~ 1.0)</param>
        /// <param name="b">Blue (0.0 ~ 1.0)</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DrawPartNumber(
            double posX,
            double posY,
            uint number,
            double scale,
            float r,
            float g,
            float b);

        /// <summary>
        /// 컨투어 번호 그리기
        /// </summary>
        /// <param name="posX">X 위치 (월드 좌표)</param>
        /// <param name="posY">Y 위치 (월드 좌표)</param>
        /// <param name="number">표시할 번호</param>
        /// <param name="scale">스케일 (1.0 = 100%)</param>
        /// <param name="r">Red (0.0 ~ 1.0)</param>
        /// <param name="g">Green (0.0 ~ 1.0)</param>
        /// <param name="b">Blue (0.0 ~ 1.0)</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DrawContourNumber(
            double posX,
            double posY,
            uint number,
            double scale,
            float r,
            float g,
            float b);

        /// <summary>
        /// 텍스트 렌더러 정리
        /// </summary>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CleanupTextRenderer();
    }

    /// <summary>
    /// 라벨 렌더링 설정
    /// LabelRenderSettings는 제거되고 RenderSettings로 통합됨
    /// JSON 파일(RenderSettings.json)에서 폰트 사이즈 설정:
    /// - PartNumberSize: 파트 번호 폰트 크기
    /// - ContourNumberSize: 컨투어 번호 폰트 크기
    /// </summary>
}
