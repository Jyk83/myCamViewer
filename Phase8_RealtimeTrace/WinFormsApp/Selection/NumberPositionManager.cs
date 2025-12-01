using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CamViewerPOC.Selection
{
    /// <summary>
    /// 번호 위치 관리 클래스
    /// 파트/컨투어 번호의 사용자 정의 위치를 관리
    /// </summary>
    public class NumberPositionManager
    {
        #region Enums

        /// <summary>
        /// 번호 타입
        /// </summary>
        public enum NumberType
        {
            Part,    // 파트 번호
            Contour  // 컨투어 번호
        }

        #endregion

        #region Properties

        /// <summary>
        /// 파트 번호의 사용자 정의 위치 (partIndex -> Point2D)
        /// </summary>
        public Dictionary<int, GeometryUtils.Point2D> CustomPartPositions { get; private set; }

        /// <summary>
        /// 컨투어 번호의 사용자 정의 위치 ((partIndex, contourIndex) -> Point2D)
        /// </summary>
        public Dictionary<(int, int), GeometryUtils.Point2D> CustomContourPositions { get; private set; }

        /// <summary>
        /// 위치 설정 모드 활성화 여부
        /// </summary>
        public bool IsPositioningMode { get; private set; } = false;

        /// <summary>
        /// 현재 위치 설정 대상 번호 타입
        /// </summary>
        public NumberType PositioningTarget { get; private set; } = NumberType.Part;

        #endregion

        #region Events

        /// <summary>
        /// 위치 설정 모드가 변경되었을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler PositioningModeChanged;

        /// <summary>
        /// 위치가 변경되었을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler PositionChanged;

        #endregion

        #region Constructor

        public NumberPositionManager()
        {
            CustomPartPositions = new Dictionary<int, GeometryUtils.Point2D>();
            CustomContourPositions = new Dictionary<(int, int), GeometryUtils.Point2D>();
        }

        #endregion

        #region Positioning Mode

        /// <summary>
        /// 위치 설정 모드 활성화
        /// </summary>
        /// <param name="type">설정할 번호 타입</param>
        public void EnablePositioningMode(NumberType type)
        {
            IsPositioningMode = true;
            PositioningTarget = type;
            OnPositioningModeChanged();
        }

        /// <summary>
        /// 위치 설정 모드 비활성화
        /// </summary>
        public void DisablePositioningMode()
        {
            IsPositioningMode = false;
            OnPositioningModeChanged();
        }

        /// <summary>
        /// 위치 설정 모드 토글
        /// </summary>
        public void TogglePositioningMode(NumberType type)
        {
            if (IsPositioningMode && PositioningTarget == type)
            {
                DisablePositioningMode();
            }
            else
            {
                EnablePositioningMode(type);
            }
        }

        #endregion

        #region Position Management

        /// <summary>
        /// 번호 위치 설정
        /// </summary>
        /// <param name="partIndex">파트 인덱스</param>
        /// <param name="contourIndex">컨투어 인덱스 (파트 번호인 경우 null)</param>
        /// <param name="worldPos">월드 좌표 위치</param>
        public void SetPosition(int partIndex, int? contourIndex, GeometryUtils.Point2D worldPos)
        {
            if (contourIndex.HasValue)
            {
                // 컨투어 번호 위치
                CustomContourPositions[(partIndex, contourIndex.Value)] = worldPos;
            }
            else
            {
                // 파트 번호 위치
                CustomPartPositions[partIndex] = worldPos;
            }

            OnPositionChanged();
        }

        /// <summary>
        /// 파트 번호 위치 가져오기
        /// </summary>
        /// <param name="partIndex">파트 인덱스</param>
        /// <returns>사용자 정의 위치, 없으면 null</returns>
        public GeometryUtils.Point2D? GetPartPosition(int partIndex)
        {
            if (CustomPartPositions.TryGetValue(partIndex, out GeometryUtils.Point2D pos))
            {
                return pos;
            }
            return null;
        }

        /// <summary>
        /// 컨투어 번호 위치 가져오기
        /// </summary>
        /// <param name="partIndex">파트 인덱스</param>
        /// <param name="contourIndex">컨투어 인덱스</param>
        /// <returns>사용자 정의 위치, 없으면 null</returns>
        public GeometryUtils.Point2D? GetContourPosition(int partIndex, int contourIndex)
        {
            if (CustomContourPositions.TryGetValue((partIndex, contourIndex), out GeometryUtils.Point2D pos))
            {
                return pos;
            }
            return null;
        }

        /// <summary>
        /// 파트 번호 위치 제거
        /// </summary>
        public void RemovePartPosition(int partIndex)
        {
            if (CustomPartPositions.Remove(partIndex))
            {
                OnPositionChanged();
            }
        }

        /// <summary>
        /// 컨투어 번호 위치 제거
        /// </summary>
        public void RemoveContourPosition(int partIndex, int contourIndex)
        {
            if (CustomContourPositions.Remove((partIndex, contourIndex)))
            {
                OnPositionChanged();
            }
        }

        /// <summary>
        /// 모든 위치 초기화
        /// </summary>
        public void ClearAllPositions()
        {
            bool changed = CustomPartPositions.Count > 0 || CustomContourPositions.Count > 0;

            CustomPartPositions.Clear();
            CustomContourPositions.Clear();

            if (changed)
            {
                OnPositionChanged();
            }
        }

        #endregion

        #region Persistence (JSON)

        /// <summary>
        /// 위치 정보를 JSON 파일로 저장
        /// </summary>
        /// <param name="filePath">저장 경로</param>
        public void SavePositions(string filePath)
        {
            try
            {
                StringBuilder json = new StringBuilder();
                json.AppendLine("{");

                // 파트 번호 위치
                json.AppendLine("  \"partPositions\": {");
                bool firstPart = true;
                foreach (var kvp in CustomPartPositions)
                {
                    if (!firstPart) json.AppendLine(",");
                    json.Append($"    \"{kvp.Key}\": {{ \"x\": {kvp.Value.X}, \"y\": {kvp.Value.Y} }}");
                    firstPart = false;
                }
                json.AppendLine();
                json.AppendLine("  },");

                // 컨투어 번호 위치
                json.AppendLine("  \"contourPositions\": {");
                bool firstContour = true;
                foreach (var kvp in CustomContourPositions)
                {
                    if (!firstContour) json.AppendLine(",");
                    json.Append($"    \"{kvp.Key.Item1}_{kvp.Key.Item2}\": {{ \"x\": {kvp.Value.X}, \"y\": {kvp.Value.Y} }}");
                    firstContour = false;
                }
                json.AppendLine();
                json.AppendLine("  }");

                json.AppendLine("}");

                File.WriteAllText(filePath, json.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save positions: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// JSON 파일에서 위치 정보 로드
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        public void LoadPositions(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Position file not found: {filePath}");
                }

                string content = File.ReadAllText(filePath);

                // 간단한 JSON 파싱 (System.Text.Json 또는 Newtonsoft.Json 없이)
                CustomPartPositions.Clear();
                CustomContourPositions.Clear();

                // "partPositions" 섹션 파싱
                int partStart = content.IndexOf("\"partPositions\"");
                if (partStart >= 0)
                {
                    int braceStart = content.IndexOf('{', partStart + 15);
                    int braceEnd = FindMatchingBrace(content, braceStart);
                    string partSection = content.Substring(braceStart + 1, braceEnd - braceStart - 1);

                    ParsePositionSection(partSection, true);
                }

                // "contourPositions" 섹션 파싱
                int contourStart = content.IndexOf("\"contourPositions\"");
                if (contourStart >= 0)
                {
                    int braceStart = content.IndexOf('{', contourStart + 18);
                    int braceEnd = FindMatchingBrace(content, braceStart);
                    string contourSection = content.Substring(braceStart + 1, braceEnd - braceStart - 1);

                    ParsePositionSection(contourSection, false);
                }

                OnPositionChanged();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load positions: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// JSON 섹션 파싱 헬퍼
        /// </summary>
        private void ParsePositionSection(string section, bool isPartSection)
        {
            // "key": { "x": value, "y": value } 형태 파싱
            string[] entries = section.Split(new[] { "},", "}" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                // 키 추출
                int keyStart = entry.IndexOf('\"');
                int keyEnd = entry.IndexOf('\"', keyStart + 1);
                if (keyStart < 0 || keyEnd < 0)
                    continue;

                string key = entry.Substring(keyStart + 1, keyEnd - keyStart - 1);

                // X, Y 추출
                double x = ExtractJsonNumber(entry, "\"x\"");
                double y = ExtractJsonNumber(entry, "\"y\"");

                // 저장
                if (isPartSection)
                {
                    if (int.TryParse(key, out int partIndex))
                    {
                        CustomPartPositions[partIndex] = new GeometryUtils.Point2D(x, y);
                    }
                }
                else
                {
                    // "partIndex_contourIndex" 형태
                    string[] parts = key.Split('_');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int partIndex) &&
                        int.TryParse(parts[1], out int contourIndex))
                    {
                        CustomContourPositions[(partIndex, contourIndex)] = new GeometryUtils.Point2D(x, y);
                    }
                }
            }
        }

        /// <summary>
        /// JSON에서 숫자 추출
        /// </summary>
        private double ExtractJsonNumber(string json, string key)
        {
            int keyIndex = json.IndexOf(key);
            if (keyIndex < 0)
                return 0;

            int colonIndex = json.IndexOf(':', keyIndex);
            if (colonIndex < 0)
                return 0;

            int valueStart = colonIndex + 1;
            int valueEnd = json.IndexOfAny(new[] { ',', '}' }, valueStart);
            if (valueEnd < 0)
                valueEnd = json.Length;

            string valueStr = json.Substring(valueStart, valueEnd - valueStart).Trim();

            if (double.TryParse(valueStr, out double value))
                return value;

            return 0;
        }

        /// <summary>
        /// 매칭되는 중괄호 찾기
        /// </summary>
        private int FindMatchingBrace(string content, int startIndex)
        {
            int depth = 1;
            for (int i = startIndex + 1; i < content.Length; i++)
            {
                if (content[i] == '{')
                    depth++;
                else if (content[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }
            return content.Length - 1;
        }

        #endregion

        #region Event Raising

        /// <summary>
        /// PositioningModeChanged 이벤트 발생
        /// </summary>
        protected virtual void OnPositioningModeChanged()
        {
            PositioningModeChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// PositionChanged 이벤트 발생
        /// </summary>
        protected virtual void OnPositionChanged()
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
