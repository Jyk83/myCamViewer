using System;
using System.IO;

namespace RealtimeITagControl
{
    /// <summary>
    /// 파일 로그 유틸리티 (WinCC DLL 디버깅용)
    /// DLL로 사용되므로 System.Diagnostics.Debug.WriteLine 대신 파일 로그 사용
    /// </summary>
    public static class LogHelper
    {
        private static readonly string baseLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
            "CamViewer_Logs");

        static LogHelper()
        {
            // 로그 디렉토리 생성
            try
            {
                if (!Directory.Exists(baseLogPath))
                {
                    Directory.CreateDirectory(baseLogPath);
                }
            }
            catch { }
        }

        /// <summary>
        /// 파일별 로그 작성
        /// </summary>
        public static void Log(string fileName, string message)
        {
            try
            {
                string logFile = Path.Combine(baseLogPath, $"{fileName}.txt");
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] {message}\n";
                File.AppendAllText(logFile, logMessage);
            }
            catch
            {
                // 로그 실패 시 무시 (DLL 안정성 우선)
            }
        }

        /// <summary>
        /// 로그 파일 초기화
        /// </summary>
        public static void ClearLog(string fileName)
        {
            try
            {
                string logFile = Path.Combine(baseLogPath, $"{fileName}.txt");
                if (File.Exists(logFile))
                {
                    File.Delete(logFile);
                }
            }
            catch { }
        }
    }
}
