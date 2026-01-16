using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RealtimeITagControl.Trace
{
    /// <summary>
    /// Phase 15.4: Thread-safe ITag data queue
    /// ITag 업데이트를 스레드 안전하게 처리
    /// </summary>
    public class ThreadSafeTagQueue
    {
        private ConcurrentQueue<TagData> queue = new ConcurrentQueue<TagData>();
        private AutoResetEvent dataAvailableEvent = new AutoResetEvent(false);
        private int maxQueueSize = 1000;
        private long totalEnqueued = 0;
        private long totalDequeued = 0;

        /// <summary>
        /// ITag 데이터 추가
        /// </summary>
        public bool Enqueue(TagData data)
        {
            if (queue.Count >= maxQueueSize)
            {
                // 큐가 가득 차면 가장 오래된 항목 제거
                queue.TryDequeue(out _);
            }

            queue.Enqueue(data);
            Interlocked.Increment(ref totalEnqueued);
            dataAvailableEvent.Set();

            return true;
        }

        /// <summary>
        /// ITag 데이터 가져오기
        /// </summary>
        public bool TryDequeue(out TagData data)
        {
            bool result = queue.TryDequeue(out data);
            if (result)
            {
                Interlocked.Increment(ref totalDequeued);
            }
            return result;
        }

        /// <summary>
        /// 데이터 대기 (타임아웃)
        /// </summary>
        public bool WaitForData(int timeoutMs)
        {
            return dataAvailableEvent.WaitOne(timeoutMs);
        }

        /// <summary>
        /// 모든 대기 중인 데이터 처리
        /// </summary>
        public void ProcessAll(Action<TagData> processor)
        {
            TagData data;
            while (queue.TryDequeue(out data))
            {
                Interlocked.Increment(ref totalDequeued);
                processor?.Invoke(data);
            }
        }

        /// <summary>
        /// 큐 비우기
        /// </summary>
        public void Clear()
        {
            while (queue.TryDequeue(out _))
            {
                Interlocked.Increment(ref totalDequeued);
            }
        }

        /// <summary>
        /// 큐 크기
        /// </summary>
        public int Count => queue.Count;

        /// <summary>
        /// 최대 큐 크기 설정
        /// </summary>
        public int MaxQueueSize
        {
            get => maxQueueSize;
            set
            {
                if (value > 0)
                    maxQueueSize = value;
            }
        }

        /// <summary>
        /// 총 추가된 항목 수
        /// </summary>
        public long TotalEnqueued => totalEnqueued;

        /// <summary>
        /// 총 처리된 항목 수
        /// </summary>
        public long TotalDequeued => totalDequeued;

        /// <summary>
        /// 처리 지연 (대기 중인 항목)
        /// </summary>
        public int PendingCount => queue.Count;
    }
}
