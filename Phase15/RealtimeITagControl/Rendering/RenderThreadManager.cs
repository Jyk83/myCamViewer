using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Forms;

namespace RealtimeITagControl.Rendering
{
    /// <summary>
    /// Phase 15.4: Multi-threaded rendering manager
    /// 백그라운드 렌더링 스레드 관리 및 UI 스레드 분리
    /// </summary>
    public class RenderThreadManager : IDisposable
    {
        private Thread renderThread;
        private volatile bool isRunning = false;
        private volatile bool isPaused = false;
        private AutoResetEvent renderEvent = new AutoResetEvent(false);
        private ConcurrentQueue<RenderCommand> commandQueue = new ConcurrentQueue<RenderCommand>();
        private Control renderTarget;
        private int targetFPS = 60;
        private bool disposed = false;

        public event EventHandler<RenderEventArgs> RenderRequested;
        public event EventHandler<Exception> RenderError;

        /// <summary>
        /// 렌더링 명령
        /// </summary>
        public class RenderCommand
        {
            public RenderCommandType Type { get; set; }
            public object Data { get; set; }
        }

        public enum RenderCommandType
        {
            FullRefresh,
            PartialUpdate,
            DirtyRegion,
            ElementUpdate
        }

        public class RenderEventArgs : EventArgs
        {
            public RenderCommand Command { get; set; }
        }

        public RenderThreadManager(Control target, int fps = 60)
        {
            renderTarget = target;
            targetFPS = fps;
        }

        /// <summary>
        /// 렌더링 스레드 시작
        /// </summary>
        public void Start()
        {
            if (isRunning)
                return;

            isRunning = true;
            isPaused = false;

            renderThread = new Thread(RenderLoop)
            {
                Name = "RenderThread",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };

            renderThread.Start();
        }

        /// <summary>
        /// 렌더링 스레드 중지
        /// </summary>
        public void Stop()
        {
            if (!isRunning)
                return;

            isRunning = false;
            renderEvent.Set(); // Wake up thread to exit

            if (renderThread != null && renderThread.IsAlive)
            {
                if (!renderThread.Join(2000)) // Wait 2 seconds
                {
                    renderThread.Abort();
                }
            }
        }

        /// <summary>
        /// 렌더링 일시정지
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// 렌더링 재개
        /// </summary>
        public void Resume()
        {
            isPaused = false;
            renderEvent.Set();
        }

        /// <summary>
        /// 렌더링 명령 추가
        /// </summary>
        public void EnqueueCommand(RenderCommandType type, object data = null)
        {
            commandQueue.Enqueue(new RenderCommand
            {
                Type = type,
                Data = data
            });

            renderEvent.Set(); // Wake up render thread
        }

        /// <summary>
        /// 즉시 렌더링 요청
        /// </summary>
        public void RequestRender()
        {
            renderEvent.Set();
        }

        /// <summary>
        /// 렌더링 루프 (백그라운드 스레드)
        /// </summary>
        private void RenderLoop()
        {
            int frameTime = 1000 / targetFPS;

            while (isRunning)
            {
                try
                {
                    // Wait for render request or timeout
                    renderEvent.WaitOne(frameTime);

                    if (!isRunning)
                        break;

                    if (isPaused)
                        continue;

                    // Process all queued commands
                    while (commandQueue.TryDequeue(out RenderCommand command))
                    {
                        if (!isRunning)
                            break;

                        // Fire render event on UI thread
                        if (renderTarget != null && !renderTarget.IsDisposed)
                        {
                            renderTarget.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    RenderRequested?.Invoke(this, new RenderEventArgs { Command = command });
                                }
                                catch (Exception ex)
                                {
                                    RenderError?.Invoke(this, ex);
                                }
                            }));
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    // Thread is being aborted, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    RenderError?.Invoke(this, ex);
                }
            }
        }

        /// <summary>
        /// FPS 설정
        /// </summary>
        public void SetTargetFPS(int fps)
        {
            if (fps < 1) fps = 1;
            if (fps > 120) fps = 120;
            targetFPS = fps;
        }

        /// <summary>
        /// 대기 중인 명령 개수
        /// </summary>
        public int PendingCommands => commandQueue.Count;

        /// <summary>
        /// 실행 중 여부
        /// </summary>
        public bool IsRunning => isRunning;

        /// <summary>
        /// 일시정지 여부
        /// </summary>
        public bool IsPaused => isPaused;

        public void Dispose()
        {
            if (disposed)
                return;

            Stop();

            if (renderEvent != null)
            {
                renderEvent.Dispose();
                renderEvent = null;
            }

            disposed = true;
        }
    }
}
