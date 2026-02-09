using System;
using System.Linq;
using System.Runtime.InteropServices;
using Siemens.Runtime;
using Siemens.Runtime.ITag;

namespace RealtimeITagControl
{
    /// <summary>
    /// ITag 통신 싱글톤 매니저 (전역 ITag 인스턴스 공유)
    /// WinCC에서 단 하나의 ITag 인스턴스만 생성하여 모든 클래스가 공유
    /// </summary>
    public sealed class ITagManager : ITagSink
    {
        #region Singleton 패턴

        private static readonly object _lock = new object();
        private static ITagManager _instance = null;

        /// <summary>
        /// ITagManager 싱글톤 인스턴스 (전역 공유)
        /// </summary>
        public static ITagManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ITagManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// ITagManager 초기화 (IServiceProvider 전달 필요 - WinCC 내부에서만)
        /// </summary>
        public static void Initialize(IServiceProvider siteProvider)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new ITagManager();
                }
                _instance.siteProvider = siteProvider;
            }
        }

        // Private 생성자 (외부에서 new 불가)
        private ITagManager()
        {
        }

        #endregion

        #region 이벤트

        /// <summary>
        /// Tag 데이터 변경 이벤트 (여러 구독자 가능)
        /// </summary>
        public event EventHandler<TagDataEventArgs> DataChanged;

        /// <summary>
        /// 연결 상태 변경 이벤트 (여러 구독자 가능)
        /// </summary>
        public event EventHandler<bool> ConnectionChanged;

        #endregion

        #region 멤버 변수

        private ITag m_ITag;
        private long m_RegisterCookie;
        private bool isConnected = false;
        private bool isCyclicReading = false;
        private IServiceProvider siteProvider;

        #endregion

        #region 속성

        public bool IsConnected => isConnected;
        public bool IsCyclicReading => isCyclicReading;

        /// <summary>
        /// 내부 ITag 인스턴스 (읽기 전용, 직접 접근용)
        /// </summary>
        public ITag ITagInstance => m_ITag;

        /// <summary>
        /// 마지막 오류 메시지 (UI 표시용)
        /// </summary>
        public string LastErrorMessage { get; private set; }

        #endregion

        #region 연결 관리

        /// <summary>
        /// ITag 서버 연결 (단 한 번만 생성)
        /// </summary>
        public bool Connect()
        {
            lock (_lock)
            {
                LastErrorMessage = null;

                // 이미 연결되어 있으면 성공 반환
                if (isConnected && m_ITag != null)
                {
                    return true;
                }

                try
                {
                    
                    // 1순위: Site.GetService (WinCC 내부)
                    if (siteProvider != null)
                    {
                        try
                        {
                            m_ITag = (ITag)siteProvider.GetService(typeof(ITag));
                            if (m_ITag != null)
                            {
                            }
                            else
                            {
                            }
                        }
                        catch (Exception ex)
                        {
                            LastErrorMessage = $"Site.GetService 실패: {ex.Message}";
                        }
                    }
                    else
                    {
                        LastErrorMessage = "IServiceProvider가 null입니다 (WinCC 외부 실행?)";
                    }

                    // 2순위: COM ProgID로 생성 (독립 실행)
                    if (m_ITag == null)
                    {
                        try
                        {
                            Type itagType = Type.GetTypeFromProgID("CCITagControl.ITagControl.1");
                            if (itagType != null)
                            {
                                m_ITag = (ITag)Activator.CreateInstance(itagType);
                                if (m_ITag != null)
                                {
                                }
                                else
                                {
                                    LastErrorMessage = "COM 인스턴스 생성 실패 (WinCC Runtime 미실행?)";
                                }
                            }
                            else
                            {
                                LastErrorMessage = "COM ProgID 'CCITagControl.ITagControl.1'을 찾을 수 없습니다";
                            }
                        }
                        catch (Exception ex)
                        {
                            LastErrorMessage = $"COM 생성 실패: {ex.Message}";
                        }
                    }

                    if (m_ITag == null)
                    {
                        string errorMsg = "ITag 인스턴스 생성 실패 - WinCC Runtime이 실행 중인지 확인하세요";
                        if (string.IsNullOrEmpty(LastErrorMessage))
                        {
                            LastErrorMessage = errorMsg;
                        }
                        ConnectionChanged?.Invoke(this, false);
                        return false;
                    }

                    // ITagSink 콜백 등록 (this = ITagManager 자신)
                    m_RegisterCookie = m_ITag.Register(this);

                    isConnected = true;
                    ConnectionChanged?.Invoke(this, true);
                    return true;
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Connect 예외 발생: {ex.Message}";
                    LastErrorMessage = errorMsg;
                    ConnectionChanged?.Invoke(this, false);
                    return false;
                }
            }
        }

        /// <summary>
        /// ITag 서버 연결 해제
        /// </summary>
        public void Disconnect()
        {
            lock (_lock)
            {
                try
                {
                    StopCyclicRead();

                    if (m_ITag != null && isConnected)
                    {
                        m_ITag.Unregister((int)m_RegisterCookie);
                    }

                    isConnected = false;
                    ConnectionChanged?.Invoke(this, false);
                    m_ITag = null;  // ITag 참조 완전 해제 (중요!)
                }
                catch (Exception ex)
                {
                    LogHelper.Log("ITagManager", "exception m_RegisterCookie");
                }
            }
        }

        #endregion

        #region Tag 읽기/쓰기

        /// <summary>
        /// 14개 Tag 주기적 읽기 시작 (ReadTagCyclic)
        /// </summary>
        public bool StartCyclicRead(int cycleMs)
        {
            lock (_lock)
            {
                if (!isConnected)
                {
                    return false;
                }

                if (isCyclicReading)
                {
                    return true;
                }

                try
                {
                    int tagCount = TagDefinitions.AllTagNames.Length;
                    int[] cycles = Enumerable.Repeat(cycleMs, tagCount).ToArray();
                    int[] cookies = Enumerable.Range(1, tagCount).ToArray();

                    object serverCookie = null;

                    m_ITag.ReadTagCyclic(
                        (int)m_RegisterCookie,
                        (object)TagDefinitions.AllTagNames,
                        (object)cycles,
                        (object)cookies,
                        out serverCookie
                    );

                    isCyclicReading = true;
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 주기적 읽기 중지
        /// </summary>
        public void StopCyclicRead()
        {
            lock (_lock)
            {
                if (!isConnected || !isCyclicReading)
                    return;

                try
                {
                    m_ITag.Cancel((int)m_RegisterCookie);
                    isCyclicReading = false;
                }
                catch (Exception ex)
                {
                }
            }
        }

        /// <summary>
        /// 단일 Tag 읽기
        /// </summary>
        public bool ReadTag(string tagName, out object value)
        {
            value = null;

            if (!isConnected)
            {
                return false;
            }

            try
            {
                object result = m_ITag.ReadTag((int)m_RegisterCookie, tagName);
                value = result;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 여러 Tag 읽기 (배열)
        /// </summary>
        public bool ReadTags(string[] tagNames, out object[] values)
        {
            values = null;

            if (!isConnected)
            {
                return false;
            }

            try
            {
                object result = m_ITag.ReadTag((int)m_RegisterCookie, (object)tagNames);

                if (result is Array resultArr)
                {
                    values = new object[resultArr.Length];
                    for (int i = 0; i < resultArr.Length; i++)
                    {
                        values[i] = resultArr.GetValue(i);
                    }
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 단일 Tag 쓰기
        /// </summary>
        public bool WriteTag(string tagName, object value)
        {
            if (!isConnected)
            {
                return false;
            }

            try
            {
                m_ITag.WriteTag((int)m_RegisterCookie, tagName, value);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion

        #region ITagSink 구현

        /// <summary>
        /// Tag 데이터 변경 콜백 (OnDataChanged)
        /// </summary>
        public void OnDataChanged(int RegisterCookie, object TagNames, object Values, 
            object Qualities, object VarStates, object TimeStamps, object Cookies)
        {
            try
            {
                if (!(TagNames is Array tagArr) || !(Values is Array valArr))
                    return;

                // TagData 구조체 생성
                var tagData = new TagData();

                for (int i = 0; i < tagArr.Length; i++)
                {
                    string tagName = tagArr.GetValue(i)?.ToString();
                    object value = valArr.GetValue(i);

                    if (string.IsNullOrEmpty(tagName))
                        continue;

                    // Tag 이름에 따라 값 할당
                    switch (tagName)
                    {
                        case TagDefinitions.X_MCS:
                            tagData.X_MCS = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.Y_MCS:
                            tagData.Y_MCS = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.X_WCS:
                            tagData.X_WCS = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.Y_WCS:
                            tagData.Y_WCS = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.PROGRESS_DISTANCE:
                            tagData.ProgressDistance = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.CURRENT_PART:
                            tagData.CurrentPart = Convert.ToInt32(value);
                            break;
                        case TagDefinitions.CURRENT_CONT:
                            tagData.CurrentContour = Convert.ToInt32(value);
                            break;
                        case TagDefinitions.WORK_DIR:
                            tagData.WorkDir = value?.ToString() ?? "";
                            break;
                        case TagDefinitions.WORK_MPF_NAME:
                            tagData.WorkMpfName = value?.ToString() ?? "";
                            break;
                        case TagDefinitions.WORK_STATUS:
                            tagData.WorkStatus = (TagDefinitions.WorkStatus)Convert.ToInt32(value);
                            break;
                        case TagDefinitions.ACT_LINE_CODE:
                            tagData.ActLineCode = value?.ToString() ?? "";
                            break;
                        case TagDefinitions.ACT_LINE_NUM:
                            tagData.ActLineNum = Convert.ToInt32(value);
                            break;
                        case TagDefinitions.SEARCH_PART:
                            tagData.SearchPart = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.SEARCH_CONT:
                            tagData.SearchContour = Convert.ToDouble(value);
                            break;
                        case TagDefinitions.DIR_TYPE:
                            tagData.DirType = (TagDefinitions.DirectionType)Convert.ToInt32(value);
                            break;
                        case TagDefinitions.OPENGL_TYPE:
                            tagData.OpenGLType = Convert.ToInt32(value);
                            break;
                    }
                }

                // 이벤트 발생 (모든 구독자에게 전달)
                DataChanged?.Invoke(this, new TagDataEventArgs(tagData));
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// 비동기 쓰기 완료 콜백
        /// </summary>
        public void OnWriteComplete(int RegisterCookie, object TagNames, object Cookies)
        {
            // 필요 시 구현
        }

        /// <summary>
        /// 에러 콜백
        /// </summary>
        public void OnError(int RegisterCookie, object TagNames, object Cookies, object Errors)
        {
        }

        /// <summary>
        /// 주기적 읽기 취소 콜백
        /// </summary>
        public void OnCanceled(int RegisterCookie)
        {
            isCyclicReading = false;
        }

        /// <summary>
        /// Tag 제거 콜백
        /// </summary>
        public void OnRemoved(int RegisterCookie, object Cookies)
        {
        }

        #endregion
    }

    /// <summary>
    /// Tag 데이터 이벤트 인자
    /// </summary>
    public class TagDataEventArgs : EventArgs
    {
        public TagData Data { get; }

        public TagDataEventArgs(TagData data)
        {
            Data = data;
        }
    }
}
