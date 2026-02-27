using System;

namespace RealtimeITagControl
{
    /// <summary>
    /// WinCC ITag 정의 (14개)
    /// </summary>
    public static class TagDefinitions
    {
        // 머신 좌표
        public const string X_MCS = "HMI_VIEW_X_MCS";
        public const string Y_MCS = "HMI_VIEW_Y_MCS";

        // 워크 좌표
        public const string X_WCS = "HMI_VIEW_X_WCS";
        public const string Y_WCS = "HMI_VIEW_Y_WCS";

        // 진행 정보
        public const string PROGRESS_DISTANCE = "HMI_VIEW_PROGRESS_DISTANCE";

        // 파트/컨투어
        public const string CURRENT_PART = "HMI_VIEW_CURRENT_PART";
        public const string CURRENT_CONT = "HMI_VIEW_CURRENT_CONT";

        // 작업 정보
        public const string WORK_DIR = "HMI_VIEW_WORK_DIR";
        public const string WORK_MPF_NAME = "HMI_VIEW_WORK_MPF_NAME";
        public const string WORK_STATUS = "HMI_VIEW_WORK_STATUS";

        // 현재 실행 정보
        public const string ACT_LINE_CODE = "HMI_VIEW_ACT_LINE_CODE";
        public const string ACT_LINE_NUM = "HMI_VIEW_ACT_LINE_NUM";

        // 선택 정보
        public const string SEARCH_PART = "HMI_VIEW_SEARCH_PART";
        public const string SEARCH_CONT = "HMI_VIEW_SEARCH_CONT";

        // 장비 타입
        public const string DIR_TYPE = "HMI_VIEW_DIR_TYPE";

        /// <summary>
        /// 모든 Tag 이름 배열 (ReadTagCyclic용)
        /// </summary>
        public static readonly string[] AllTagNames = new string[]
        {
            X_MCS, Y_MCS,
            X_WCS, Y_WCS,
            PROGRESS_DISTANCE,
            CURRENT_PART, CURRENT_CONT,
            WORK_DIR, WORK_MPF_NAME, WORK_STATUS,
            ACT_LINE_CODE, ACT_LINE_NUM,
            SEARCH_PART, SEARCH_CONT,
            DIR_TYPE
        };

        /// <summary>
        /// 작업 상태 열거형
        /// </summary>
        public enum WorkStatus
        {
            End = 0,
            Start = 1,
            Reset = 2,
            FeedHold = 3
        }

        /// <summary>
        /// 장비 타입 열거형
        /// </summary>
        public enum DirectionType
        {
            Small = 1,
            Large = 2
        }
    }

    /// <summary>
    /// ITag 데이터 구조체
    /// </summary>
    public struct TagData
    {
        // 머신 좌표
        public double X_MCS;
        public double Y_MCS;

        // 워크 좌표
        public double X_WCS;
        public double Y_WCS;

        // 진행 정보
        public double ProgressDistance;

        // 파트/컨투어
        public int CurrentPart;
        public int CurrentContour;

        // 작업 정보
        public string WorkDir;
        public string WorkMpfName;
        public TagDefinitions.WorkStatus WorkStatus;

        // 현재 실행 정보
        public string ActLineCode;
        public int ActLineNum;

        // 선택 정보
        public int SearchPart;
        public int SearchContour;

        // 장비 타입
        public TagDefinitions.DirectionType DirType;

        /// <summary>
        /// MPF 파일 전체 경로
        /// </summary>
        public string FullMpfPath
        {
            get
            {
                if (string.IsNullOrEmpty(WorkDir) || string.IsNullOrEmpty(WorkMpfName))
                    return null;
                return System.IO.Path.Combine(WorkDir, WorkMpfName);
            }
        }
    }
}
