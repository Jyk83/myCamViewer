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

            // MPFParser 사용
            var parser = new MPFParser();
            var program = new MPFProgram();

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                int lineNumber = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    // 취소 확인
                    cancellationToken.ThrowIfCancellationRequested();

                    lineNumber++;
                    bytesRead += line.Length + Environment.NewLine.Length;

                    // 진행률 계산 (10% 단위로 보고)
                    int progress = (int)((bytesRead * 100) / totalBytes);
                    if (progress >= lastReportedProgress + 10)
                    {
                        lastReportedProgress = progress;
                        ReportProgress(progress, bytesRead, totalBytes, $"라인 {lineNumber} 처리 중...");
                    }

                    // 라인 파싱 (기존 MPFParser 로직 활용)
                    parser.ParseLine(line, program);
                }
            }

            // 파싱 완료 후 추가 처리
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
