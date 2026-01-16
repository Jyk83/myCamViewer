using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RealtimeITagControl.MPF
{
    /// <summary>
    /// Phase 15.4: Asynchronous MPF file loader
    /// 대용량 MPF 파일을 백그라운드에서 비동기 로딩
    /// </summary>
    public class AsyncMPFLoader
    {
        private CancellationTokenSource cancellationTokenSource;

        public event EventHandler<LoadProgressEventArgs> ProgressChanged;
        public event EventHandler<LoadCompletedEventArgs> LoadCompleted;
        public event EventHandler<Exception> LoadError;

        /// <summary>
        /// 진행률 이벤트 인자
        /// </summary>
        public class LoadProgressEventArgs : EventArgs
        {
            public int ProgressPercentage { get; set; }
            public long BytesRead { get; set; }
            public long TotalBytes { get; set; }
            public string CurrentStatus { get; set; }
        }

        /// <summary>
        /// 로딩 완료 이벤트 인자
        /// </summary>
        public class LoadCompletedEventArgs : EventArgs
        {
            public MPFProgram Program { get; set; }
            public TimeSpan LoadTime { get; set; }
            public bool Cancelled { get; set; }
        }

        /// <summary>
        /// MPF 파일 비동기 로딩
        /// </summary>
        public async Task<MPFProgram> LoadAsync(string filePath)
        {
            cancellationTokenSource = new CancellationTokenSource();
            var startTime = DateTime.Now;

            try
            {
                // 파일 크기 확인
                FileInfo fileInfo = new FileInfo(filePath);
                long totalBytes = fileInfo.Length;

                // 진행률 보고
                ReportProgress(0, 0, totalBytes, "파일 읽기 시작...");

                // Task.Run으로 백그라운드 스레드에서 실행
                var program = await Task.Run(() =>
                {
                    return LoadMPFInternal(filePath, totalBytes, cancellationTokenSource.Token);
                }, cancellationTokenSource.Token);

                var loadTime = DateTime.Now - startTime;

                // 완료 이벤트 발생
                LoadCompleted?.Invoke(this, new LoadCompletedEventArgs
                {
                    Program = program,
                    LoadTime = loadTime,
                    Cancelled = false
                });

                return program;
            }
            catch (OperationCanceledException)
            {
                // 취소됨
                LoadCompleted?.Invoke(this, new LoadCompletedEventArgs
                {
                    Program = null,
                    LoadTime = DateTime.Now - startTime,
                    Cancelled = true
                });

                return null;
            }
            catch (Exception ex)
            {
                LoadError?.Invoke(this, ex);
                throw;
            }
        }

        /// <summary>
        /// 내부 MPF 로딩 로직
        /// </summary>
        private MPFProgram LoadMPFInternal(string filePath, long totalBytes, CancellationToken cancellationToken)
        {
            long bytesRead = 0;
            int lastReportedProgress = 0;

            // 전체 파일 읽기
            string content = File.ReadAllText(filePath);
            bytesRead = content.Length;

            // 진행률 업데이트
            ReportProgress(50, bytesRead, totalBytes, "파일 읽기 완료, 파싱 시작...");

            // 취소 확인
            cancellationToken.ThrowIfCancellationRequested();

            // MPFParser 사용하여 파싱
            var parser = new MPFParser(true);
            var program = parser.Parse(content);

            // 파싱 완료
            ReportProgress(100, totalBytes, totalBytes, "로딩 완료");

            return program;
        }

        /// <summary>
        /// 진행률 보고
        /// </summary>
        private void ReportProgress(int percentage, long bytesRead, long totalBytes, string status)
        {
            ProgressChanged?.Invoke(this, new LoadProgressEventArgs
            {
                ProgressPercentage = percentage,
                BytesRead = bytesRead,
                TotalBytes = totalBytes,
                CurrentStatus = status
            });
        }

        /// <summary>
        /// 로딩 취소
        /// </summary>
        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// 로딩 중 여부
        /// </summary>
        public bool IsLoading => cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested;
    }
}
