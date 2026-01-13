using System;
using Siemens.Runtime.ITag;

namespace RealtimeITagControl
{
    /// <summary>
    /// ITag 관리 인터페이스
    /// RealtimeITagControl이 구현하며, 팝업 등에서 ITag 인스턴스를 공유받기 위한 인터페이스
    /// </summary>
    public interface IITagManager
    {
        /// <summary>
        /// ITag 인스턴스 (외부에서 접근 가능)
        /// </summary>
        ITag ITag { get; }

        /// <summary>
        /// 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 주기적 읽기 실행 상태
        /// </summary>
        bool IsCyclicReading { get; }

        /// <summary>
        /// 마지막 오류 메시지
        /// </summary>
        string LastErrorMessage { get; }

        /// <summary>
        /// ITag 서버 연결
        /// </summary>
        bool Connect();

        /// <summary>
        /// ITag 서버 연결 해제
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 14개 Tag 주기적 읽기 시작
        /// </summary>
        bool StartCyclicRead(int cycleMs);

        /// <summary>
        /// 주기적 읽기 중지
        /// </summary>
        void StopCyclicRead();

        /// <summary>
        /// 단일 Tag 읽기
        /// </summary>
        bool ReadTag(string tagName, out object value);

        /// <summary>
        /// 여러 Tag 읽기
        /// </summary>
        bool ReadTags(string[] tagNames, out object[] values);

        /// <summary>
        /// 단일 Tag 쓰기
        /// </summary>
        bool WriteTag(string tagName, object value);

        /// <summary>
        /// Tag 데이터 변경 이벤트
        /// </summary>
        event EventHandler<TagDataEventArgs> DataChanged;

        /// <summary>
        /// 연결 상태 변경 이벤트
        /// </summary>
        event EventHandler<bool> ConnectionChanged;
    }
}
