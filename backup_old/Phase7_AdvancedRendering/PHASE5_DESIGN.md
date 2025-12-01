# Phase 5 설계 문서: 대화형 선택 및 실시간 통신

## 📋 목차
1. [개요](#1-개요)
2. [Siemens WinCC ITag 통신](#2-siemens-wincc-itag-통신)
3. [기능 1: 파트/컨투어 번호 위치 지정](#3-기능-1-파트컨투어-번호-위치-지정)
4. [기능 2: 파트 구분 외곽 점선 표시](#4-기능-2-파트-구분-외곽-점선-표시)
5. [기능 3: 컨투어 선택](#5-기능-3-컨투어-선택)
6. [기능 4: 엘리먼트 선택](#6-기능-4-엘리먼트-선택)
7. [기능 5: 캔버스 방향 전환](#7-기능-5-캔버스-방향-전환)
8. [구현 우선순위](#8-구현-우선순위)
9. [데이터 구조](#9-데이터-구조)
10. [UI 변경사항](#10-ui-변경사항)

---

## 1. 개요

### 1.1 Phase 5 목표
- **대화형 기능**: 마우스 클릭으로 파트/컨투어/엘리먼트 선택
- **시각화 개선**: 번호 위치 지정, 파트 외곽선, 방향 전환
- **실시간 통신**: Siemens WinCC ITag 프로토콜 연동 준비

### 1.2 기술 스택
- **C# WinForms**: UI 및 로직
- **OpenGL 2.1**: 렌더링
- **.NET Framework 4.7.2**
- **Siemens.Runtime.ControlDev.dll**: ITag 인터페이스 (VB.NET 호환)

---

## 2. Siemens WinCC ITag 통신

### 2.1 ITag 인터페이스 분석

**발견된 메서드 (Siemens.Runtime.ControlDev.dll):**
```
ITagDeviceType          // 장치 타입
ITagSink                // 이벤트 싱크
ReadTag                 // 동기 읽기
ReadTagAsync            // 비동기 읽기
ReadTagCyclic           // 주기적 읽기
TagNames                // 태그 목록
WriteTag                // 동기 쓰기
WriteTagAsync           // 비동기 쓰기
```

### 2.2 통신 아키텍처

```
[WinCC Runtime] ─────> [Siemens.Runtime.ControlDev.dll] ─────> [C# CAM Viewer]
   (PLC 데이터)         (.NET DLL, ITag 인터페이스)         (RealTimeDataReceiver)
        │                           │                                │
        │ itag://tag_name          │ ReadTagCyclic()                │
        v                           v                                v
    X 좌표 태그              double ReadTag("X_Position")      RealTimeData
    Y 좌표 태그              double ReadTag("Y_Position")      { X, Y, ... }
    Part 인덱스              int ReadTag("Part_Index")
    Contour 인덱스           int ReadTag("Contour_Index")
```

### 2.3 RealTimeDataReceiver 구현

#### Phase 5: 구조 정의 (통신 스텁)
```csharp
// RealTimeData.cs
public class RealTimeData
{
    public double X { get; set; }              // X 좌표 (mm)
    public double Y { get; set; }              // Y 좌표 (mm)
    public int PartIndex { get; set; }         // 현재 파트 인덱스
    public int ContourIndex { get; set; }      // 현재 컨투어 인덱스
    public int ElementIndex { get; set; }      // 현재 엘리먼트 인덱스
    public double FeedRate { get; set; }       // 이송 속도 (mm/min)
    public double LaserPower { get; set; }     // 레이저 출력 (%)
    public DateTime Timestamp { get; set; }    // 수신 시각
}

// RealTimeDataReceiver.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class RealTimeDataReceiver
{
    // 데이터 버퍼 (스레드 안전)
    private ConcurrentQueue<RealTimeData> dataBuffer;
    private const int MaxBufferSize = 10000;
    
    // 통신 스레드
    private Task receiverTask;
    private CancellationTokenSource cts;
    private bool isRunning = false;
    
    // 이벤트
    public event EventHandler<RealTimeData> DataReceived;
    
    // ITag DLL 참조 (Phase 5에서는 주석 처리)
    // [DllImport("Siemens.Runtime.ControlDev.dll", CallingConvention = CallingConvention.StdCall)]
    // private static extern double ReadTag([MarshalAs(UnmanagedType.LPWStr)] string tagName);
    
    public RealTimeDataReceiver()
    {
        dataBuffer = new ConcurrentQueue<RealTimeData>();
    }
    
    /// <summary>
    /// 실시간 데이터 수신 시작
    /// </summary>
    public void Start()
    {
        if (isRunning) return;
        
        isRunning = true;
        cts = new CancellationTokenSource();
        
        receiverTask = Task.Run(() => ReceiveLoop(cts.Token), cts.Token);
    }
    
    /// <summary>
    /// 실시간 데이터 수신 중지
    /// </summary>
    public void Stop()
    {
        if (!isRunning) return;
        
        isRunning = false;
        cts?.Cancel();
        receiverTask?.Wait(1000);
    }
    
    /// <summary>
    /// 버퍼에서 모든 데이터 가져오기
    /// </summary>
    public List<RealTimeData> GetBufferedData()
    {
        List<RealTimeData> result = new List<RealTimeData>();
        
        while (dataBuffer.TryDequeue(out RealTimeData data))
        {
            result.Add(data);
        }
        
        return result;
    }
    
    /// <summary>
    /// 데이터 수신 루프 (백그라운드 스레드)
    /// </summary>
    private void ReceiveLoop(CancellationToken token)
    {
        // Phase 5: 스텁 구현 (테스트용 더미 데이터)
        // Phase 6+: ITag API 호출로 교체
        
        while (isRunning && !token.IsCancellationRequested)
        {
            try
            {
                // TODO: ITag API 호출
                // double x = ReadTag("X_Position");
                // double y = ReadTag("Y_Position");
                // int partIndex = (int)ReadTag("Part_Index");
                
                // Phase 5: 더미 데이터 생성 (테스트용)
                RealTimeData data = new RealTimeData
                {
                    X = 0, // TODO: ReadTag("X_Position")
                    Y = 0, // TODO: ReadTag("Y_Position")
                    PartIndex = 0,
                    ContourIndex = 0,
                    ElementIndex = 0,
                    Timestamp = DateTime.Now
                };
                
                // 버퍼에 추가
                if (dataBuffer.Count < MaxBufferSize)
                {
                    dataBuffer.Enqueue(data);
                    
                    // 이벤트 발생
                    RaiseDataReceived(data);
                }
                
                // 주기: 10ms (100 Hz)
                Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RealTimeDataReceiver] Error: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 데이터 수신 이벤트 발생
    /// </summary>
    private void RaiseDataReceived(RealTimeData data)
    {
        DataReceived?.Invoke(this, data);
    }
}
```

**Phase 6+ ITag 연동 예시:**
```csharp
// VB.NET 없이 직접 .NET DLL 참조 가능 (확인 필요)
// Reference: Siemens.Runtime.ControlDev.dll

using Siemens.Runtime;

private ITag itagInterface;

public void InitializeITag()
{
    // ITag 인터페이스 초기화
    itagInterface = /* WinCC Runtime에서 제공 */;
    
    // 주기적 읽기 설정
    itagInterface.ReadTagCyclic("X_Position", 10); // 10ms마다
    itagInterface.ReadTagCyclic("Y_Position", 10);
}

private double ReadTag(string tagName)
{
    return itagInterface.ReadTag(tagName);
}
```

---

## 3. 기능 1: 파트/컨투어 번호 위치 지정

### 3.1 요구사항
- 사용자가 번호 표시 위치를 **마우스로 지정** 가능
- 위치는 **고정값으로 저장** (MPF 파일과 연동)
- 확대/축소 시 **비율에 따라 위치 자동 조정**
- 최적 위치 찾기 위한 **렌더링 설정값** 제공

### 3.2 데이터 구조
```csharp
// NumberPosition.cs
public class NumberPosition
{
    public int PartIndex { get; set; }
    public int? ContourIndex { get; set; }  // null이면 파트 번호
    public Point2D Position { get; set; }   // 월드 좌표 (mm)
    public Point2D Offset { get; set; }     // 기본 위치에서 오프셋 (mm)
}

// NumberPositionManager.cs
public class NumberPositionManager
{
    // 파트 번호 위치 (커스텀)
    private Dictionary<int, Point2D> customPartPositions;
    
    // 컨투어 번호 위치 (커스텀)
    private Dictionary<(int part, int contour), Point2D> customContourPositions;
    
    // 위치 지정 모드
    private bool isPositioningMode = false;
    private NumberType positioningTarget;  // Part or Contour
    
    public enum NumberType
    {
        Part,
        Contour
    }
    
    /// <summary>
    /// 위치 지정 모드 활성화
    /// </summary>
    public void EnablePositioningMode(NumberType type)
    {
        isPositioningMode = true;
        positioningTarget = type;
    }
    
    /// <summary>
    /// 마우스 클릭으로 위치 설정
    /// </summary>
    public void SetPosition(int partIndex, int? contourIndex, Point2D worldPos)
    {
        if (contourIndex.HasValue)
        {
            customContourPositions[(partIndex, contourIndex.Value)] = worldPos;
        }
        else
        {
            customPartPositions[partIndex] = worldPos;
        }
    }
    
    /// <summary>
    /// 번호 표시 위치 가져오기
    /// </summary>
    public Point2D GetPosition(int partIndex, int? contourIndex, Point2D defaultPos)
    {
        if (contourIndex.HasValue)
        {
            if (customContourPositions.TryGetValue((partIndex, contourIndex.Value), out Point2D pos))
                return pos;
        }
        else
        {
            if (customPartPositions.TryGetValue(partIndex, out Point2D pos))
                return pos;
        }
        
        return defaultPos;  // 기본 위치 (파트 원점 또는 피어싱 포인트)
    }
    
    /// <summary>
    /// 위치 저장/로드 (JSON)
    /// </summary>
    public void SavePositions(string filePath) { /* JSON 직렬화 */ }
    public void LoadPositions(string filePath) { /* JSON 역직렬화 */ }
}
```

### 3.3 UI 추가
```csharp
// MainForm.cs
private Button btnSetPartNumberPos;
private Button btnSetContourNumberPos;

private void InitializeNumberPositioningUI()
{
    btnSetPartNumberPos = new Button
    {
        Text = "파트 번호 위치 설정",
        Location = new Point(10, 500),
        Size = new Size(150, 30)
    };
    btnSetPartNumberPos.Click += BtnSetPartNumberPos_Click;
    
    btnSetContourNumberPos = new Button
    {
        Text = "컨투어 번호 위치 설정",
        Location = new Point(170, 500),
        Size = new Size(150, 30)
    };
    btnSetContourNumberPos.Click += BtnSetContourNumberPos_Click;
}

private void BtnSetPartNumberPos_Click(object sender, EventArgs e)
{
    numberPositionManager.EnablePositioningMode(NumberType.Part);
    lblStatus.Text = "파트 번호를 클릭하여 위치를 설정하세요.";
}
```

### 3.4 마우스 이벤트 처리
```csharp
// CamViewerControl.cs
private void RenderPanel_MouseClick(object sender, MouseEventArgs e)
{
    if (numberPositionManager.IsPositioningMode)
    {
        // 스크린 좌표 → 월드 좌표
        Point2D worldPos = ScreenToWorld(e.X, e.Y);
        
        // 가장 가까운 파트/컨투어 찾기
        (int partIndex, int? contourIndex) = FindNearestNumber(worldPos);
        
        // 위치 설정
        numberPositionManager.SetPosition(partIndex, contourIndex, worldPos);
        
        // 모드 종료
        numberPositionManager.DisablePositioningMode();
        
        // 재렌더링
        Invalidate();
    }
}
```

---

## 4. 기능 2: 파트 구분 외곽 점선 표시

### 4.1 요구사항
- 파트 번호 표시 활성화 시 → **파트 외곽선을 점선으로 표시**
- 외곽선은 **직사각형 Bounding Box**
- 점선 패턴: 여러 스타일 제공 (설정에서 선택)

### 4.2 점선 패턴 정의
```csharp
// DashPattern.cs
public enum DashPatternType
{
    Solid,              // ───────────
    Dash1,              // ─────  ─────
    Dash2,              // ───  ───  ───
    Dash3,              // ── ── ── ──
    DotDash,            // ─ ─ ─ ─ ─
    DotDotDash          // ─ · · ─ · ·
}

public class DashPattern
{
    public DashPatternType Type { get; set; }
    public float[] Pattern { get; set; }  // OpenGL stipple pattern
    
    public static DashPattern[] AllPatterns = new DashPattern[]
    {
        new DashPattern { Type = DashPatternType.Solid, Pattern = null },
        new DashPattern { Type = DashPatternType.Dash1, Pattern = new float[] {10.0f, 5.0f} },
        new DashPattern { Type = DashPatternType.Dash2, Pattern = new float[] {6.0f, 3.0f} },
        new DashPattern { Type = DashPatternType.Dash3, Pattern = new float[] {4.0f, 2.0f} },
        new DashPattern { Type = DashPatternType.DotDash, Pattern = new float[] {2.0f, 2.0f} },
        new DashPattern { Type = DashPatternType.DotDotDash, Pattern = new float[] {2.0f, 2.0f, 6.0f, 2.0f} }
    };
}
```

### 4.3 Bounding Box 계산
```csharp
// CamViewerControl.cs
private Rectangle2D CalculatePartBoundingBox(Part part, float originX, float originY)
{
    if (part.Contours.Count == 0)
        return new Rectangle2D(originX, originY, 0, 0);
    
    double minX = double.MaxValue, minY = double.MaxValue;
    double maxX = double.MinValue, maxY = double.MinValue;
    
    foreach (var contour in part.Contours)
    {
        foreach (var segment in contour.AllSegments)
        {
            if (segment is LineSegment line)
            {
                UpdateBounds(line.Start, ref minX, ref minY, ref maxX, ref maxY);
                UpdateBounds(line.End, ref minX, ref minY, ref maxX, ref maxY);
            }
            else if (segment is ArcSegment arc)
            {
                // 호의 시작/끝/중심점 모두 고려
                UpdateBounds(arc.Start, ref minX, ref minY, ref maxX, ref maxY);
                UpdateBounds(arc.End, ref minX, ref minY, ref maxX, ref maxY);
                
                // 호의 경계 박스 (반지름 고려)
                UpdateBounds(new Point2D(arc.Center.X - arc.Radius, arc.Center.Y - arc.Radius), 
                            ref minX, ref minY, ref maxX, ref maxY);
                UpdateBounds(new Point2D(arc.Center.X + arc.Radius, arc.Center.Y + arc.Radius), 
                            ref minX, ref minY, ref maxX, ref maxY);
            }
        }
    }
    
    // 월드 좌표 → 스크린 좌표
    float x = (float)(minX * workpieceScale) + originX;
    float y = (float)(minY * workpieceScale) + originY;
    float width = (float)((maxX - minX) * workpieceScale);
    float height = (float)((maxY - minY) * workpieceScale);
    
    return new Rectangle2D(x, y, width, height);
}

private void UpdateBounds(Point2D point, ref double minX, ref double minY, ref double maxX, ref double maxY)
{
    if (point.X < minX) minX = point.X;
    if (point.Y < minY) minY = point.Y;
    if (point.X > maxX) maxX = point.X;
    if (point.Y > maxY) maxY = point.Y;
}
```

### 4.4 점선 렌더링 (OpenGL)
```cpp
// NativeRenderer/renderer.cpp
RENDERER_API void DrawDashedRectangle(float x, float y, float width, float height, 
                                     float r, float g, float b, float lineWidth,
                                     float* dashPattern, int patternLength)
{
    if (!g_hRC) return;
    
    glColor3f(r, g, b);
    glLineWidth(lineWidth);
    
    // Enable line stipple
    if (dashPattern != nullptr && patternLength > 0)
    {
        glEnable(GL_LINE_STIPPLE);
        
        // Convert pattern to OpenGL stipple
        // OpenGL stipple: 16-bit pattern, factor
        GLint factor = (GLint)dashPattern[0];
        GLushort pattern = 0xFFFF;  // Solid by default
        
        if (patternLength == 2)
        {
            // dashPattern[0] = dash length, dashPattern[1] = gap length
            int dashBits = (int)dashPattern[0];
            int gapBits = (int)dashPattern[1];
            
            // Create stipple pattern
            pattern = 0;
            for (int i = 0; i < dashBits && i < 16; i++)
                pattern |= (1 << i);
        }
        
        glLineStipple(factor, pattern);
    }
    
    // Draw rectangle
    glBegin(GL_LINE_LOOP);
    glVertex2f(x, y);
    glVertex2f(x + width, y);
    glVertex2f(x + width, y + height);
    glVertex2f(x, y + height);
    glEnd();
    
    glDisable(GL_LINE_STIPPLE);
}
```

### 4.5 렌더링 통합
```csharp
// CamViewerControl.cs - RenderMPFScene()
private void RenderMPFScene()
{
    // ... 기존 렌더링 ...
    
    // 파트 번호 표시 활성화 시 외곽선 그리기
    if (RenderSettings.Instance.ShowPartNumbers)
    {
        foreach (Part part in currentProgram.Parts)
        {
            float originX = (float)(part.Origin.X * workpieceScale);
            float originY = (float)(part.Origin.Y * workpieceScale);
            
            // Bounding Box 계산
            Rectangle2D bbox = CalculatePartBoundingBox(part, originX, originY);
            
            // 점선 그리기
            float[] color = RenderSettings.Instance.GetColorAsFloat(RenderSettings.Instance.PartBoundaryColor);
            float[] dashPattern = RenderSettings.Instance.PartBoundaryDashPattern;
            
            NativeRenderer.DrawDashedRectangle(
                bbox.X, bbox.Y, bbox.Width, bbox.Height,
                color[0], color[1], color[2],
                RenderSettings.Instance.PartBoundaryWidth,
                dashPattern, dashPattern.Length
            );
        }
    }
}
```

### 4.6 RenderSettings 추가
```csharp
// RenderSettings.cs
public Color PartBoundaryColor { get; set; } = Color.FromArgb(255, 200, 200, 200);
public float PartBoundaryWidth { get; set; } = 1.0f;
public DashPatternType PartBoundaryDashType { get; set; } = DashPatternType.Dash2;
public float[] PartBoundaryDashPattern 
{ 
    get 
    {
        return DashPattern.AllPatterns[(int)PartBoundaryDashType].Pattern;
    }
}
```

---

## 5. 기능 3: 컨투어 선택

### 5.1 요구사항
- 컨투어 내부를 **마우스로 클릭** → 선택
- 선택된 컨투어: **색상 변경 + 두께 굵게**
- **한 번에 하나씩만** 선택 가능
- 선택 정보: **파트 인덱스 + 컨투어 인덱스** 리턴

### 5.2 선택 감지 알고리즘 (Point-in-Polygon)
```csharp
// GeometryUtils.cs
public static class GeometryUtils
{
    /// <summary>
    /// Point-in-Polygon 테스트 (Ray Casting Algorithm)
    /// </summary>
    public static bool IsPointInsideContour(Point2D point, Contour contour, float originX, float originY, float scale)
    {
        if (contour.AllSegments.Count == 0)
            return false;
        
        // 컨투어를 폴리곤 점들로 변환
        List<Point2D> polygon = ConvertContourToPolygon(contour, originX, originY, scale);
        
        if (polygon.Count < 3)
            return false;
        
        // Ray Casting: 점에서 오른쪽으로 수평선을 그어 교차 횟수 계산
        int intersections = 0;
        
        for (int i = 0; i < polygon.Count; i++)
        {
            Point2D p1 = polygon[i];
            Point2D p2 = polygon[(i + 1) % polygon.Count];
            
            // 수평선과 선분 교차 여부 확인
            if (RayIntersectsSegment(point, p1, p2))
            {
                intersections++;
            }
        }
        
        // 홀수 번 교차하면 내부
        return (intersections % 2) == 1;
    }
    
    /// <summary>
    /// 컨투어를 폴리곤 점들로 변환
    /// </summary>
    private static List<Point2D> ConvertContourToPolygon(Contour contour, float originX, float originY, float scale)
    {
        List<Point2D> points = new List<Point2D>();
        
        foreach (var segment in contour.AllSegments)
        {
            if (segment is LineSegment line)
            {
                points.Add(new Point2D(
                    line.Start.X * scale + originX,
                    line.Start.Y * scale + originY
                ));
                points.Add(new Point2D(
                    line.End.X * scale + originX,
                    line.End.Y * scale + originY
                ));
            }
            else if (segment is ArcSegment arc)
            {
                // 호를 직선 세그먼트들로 근사
                List<Point2D> arcPoints = ApproximateArc(arc, 20); // 20개 점으로 근사
                
                foreach (var p in arcPoints)
                {
                    points.Add(new Point2D(
                        p.X * scale + originX,
                        p.Y * scale + originY
                    ));
                }
            }
        }
        
        return points;
    }
    
    /// <summary>
    /// Ray가 선분과 교차하는지 확인
    /// </summary>
    private static bool RayIntersectsSegment(Point2D point, Point2D segStart, Point2D segEnd)
    {
        // 점의 Y가 선분의 Y 범위 밖이면 교차 없음
        if (point.Y < Math.Min(segStart.Y, segEnd.Y) || point.Y > Math.Max(segStart.Y, segEnd.Y))
            return false;
        
        // 점의 X가 선분의 최대 X보다 오른쪽이면 교차 없음
        if (point.X > Math.Max(segStart.X, segEnd.X))
            return false;
        
        // 점의 X가 선분의 최소 X보다 왼쪽이면 교차
        if (point.X < Math.Min(segStart.X, segEnd.X))
            return true;
        
        // 선분과 수평선의 교차점 X 좌표 계산
        double xIntersection = segStart.X + (point.Y - segStart.Y) / (segEnd.Y - segStart.Y) * (segEnd.X - segStart.X);
        
        return point.X < xIntersection;
    }
    
    /// <summary>
    /// 호를 직선 세그먼트들로 근사
    /// </summary>
    private static List<Point2D> ApproximateArc(ArcSegment arc, int segments)
    {
        List<Point2D> points = new List<Point2D>();
        
        double startAngle = arc.StartAngle * Math.PI / 180.0;
        double endAngle = arc.EndAngle * Math.PI / 180.0;
        double angleStep = (endAngle - startAngle) / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            double angle = startAngle + angleStep * i;
            double x = arc.Center.X + arc.Radius * Math.Cos(angle);
            double y = arc.Center.Y + arc.Radius * Math.Sin(angle);
            
            points.Add(new Point2D(x, y));
        }
        
        return points;
    }
}
```

### 5.3 선택 관리자
```csharp
// SelectionManager.cs
public class SelectionManager
{
    // 현재 선택된 항목
    private (int partIndex, int contourIndex)? selectedContour = null;
    private HashSet<(int part, int contour, int element)> selectedElements = new HashSet<(int, int, int)>();
    
    // 선택 모드
    public enum SelectionMode
    {
        None,
        Contour,
        Element
    }
    
    public SelectionMode CurrentMode { get; set; } = SelectionMode.None;
    
    // 이벤트
    public event EventHandler<(int part, int contour)> ContourSelected;
    public event EventHandler<(int part, int contour, int element)> ElementSelected;
    
    /// <summary>
    /// 컨투어 선택
    /// </summary>
    public void SelectContour(int partIndex, int contourIndex)
    {
        selectedContour = (partIndex, contourIndex);
        ContourSelected?.Invoke(this, (partIndex, contourIndex));
    }
    
    /// <summary>
    /// 선택 해제
    /// </summary>
    public void ClearContourSelection()
    {
        selectedContour = null;
    }
    
    /// <summary>
    /// 선택 여부 확인
    /// </summary>
    public bool IsContourSelected(int partIndex, int contourIndex)
    {
        return selectedContour.HasValue && 
               selectedContour.Value.partIndex == partIndex && 
               selectedContour.Value.contourIndex == contourIndex;
    }
    
    /// <summary>
    /// 선택된 컨투어 가져오기
    /// </summary>
    public (int part, int contour)? GetSelectedContour()
    {
        return selectedContour;
    }
}
```

### 5.4 마우스 클릭 처리
```csharp
// CamViewerControl.cs
private SelectionManager selectionManager = new SelectionManager();

private void RenderPanel_MouseClick(object sender, MouseEventArgs e)
{
    // 왼쪽 클릭만 처리
    if (e.Button != MouseButtons.Left)
        return;
    
    // 선택 모드에 따라 처리
    switch (selectionManager.CurrentMode)
    {
        case SelectionMode.Contour:
            HandleContourSelection(e.X, e.Y);
            break;
        
        case SelectionMode.Element:
            HandleElementSelection(e.X, e.Y);
            break;
    }
}

private void HandleContourSelection(int screenX, int screenY)
{
    // 스크린 좌표 → 월드 좌표
    Point2D worldPos = ScreenToWorld(screenX, screenY);
    
    // 클릭 위치에 있는 컨투어 찾기
    for (int pi = 0; pi < currentProgram.Parts.Count; pi++)
    {
        Part part = currentProgram.Parts[pi];
        float originX = (float)(part.Origin.X * workpieceScale);
        float originY = (float)(part.Origin.Y * workpieceScale);
        
        for (int ci = 0; ci < part.Contours.Count; ci++)
        {
            Contour contour = part.Contours[ci];
            
            if (GeometryUtils.IsPointInsideContour(worldPos, contour, originX, originY, (float)workpieceScale))
            {
                // 컨투어 선택
                selectionManager.SelectContour(pi, ci);
                
                // 재렌더링
                Invalidate();
                
                return;
            }
        }
    }
    
    // 빈 공간 클릭 → 선택 해제
    selectionManager.ClearContourSelection();
    Invalidate();
}
```

### 5.5 선택 강조 렌더링
```csharp
// CamViewerControl.cs - RenderMPFScene()
private void RenderMPFScene()
{
    // ... 기존 렌더링 ...
    
    // 컨투어 그리기
    for (int ci = 0; ci < part.Contours.Count; ci++)
    {
        Contour contour = part.Contours[ci];
        
        // 선택 여부 확인
        bool isSelected = selectionManager.IsContourSelected(pi, ci);
        
        // 색상 및 두께 결정
        Color pathColor;
        float lineWidth;
        
        if (isSelected)
        {
            // 선택된 컨투어: 강조 표시
            pathColor = settings.SelectedContourColor; // 노란색
            lineWidth = settings.SelectedContourWidth; // 3.0f
        }
        else
        {
            // 일반 컨투어
            pathColor = isMarking ? settings.MarkingColor : settings.CuttingPendingColor;
            lineWidth = settings.CuttingPendingWidth;
        }
        
        float[] colorArray = settings.GetColorAsFloat(pathColor);
        
        // 세그먼트 그리기
        foreach (PathSegment segment in contour.AllSegments)
        {
            DrawPathSegment(segment, originX, originY, colorArray[0], colorArray[1], colorArray[2], lineWidth);
        }
    }
}
```

### 5.6 RenderSettings 추가
```csharp
// RenderSettings.cs
public Color SelectedContourColor { get; set; } = Color.FromArgb(255, 255, 255, 0); // 노란색
public float SelectedContourWidth { get; set; } = 3.0f;
```

---

## 6. 기능 4: 엘리먼트 선택

### 6.1 요구사항
- 컨투어 선택의 **확장 버전**
- G-code **선분/호 단위로 선택** (엘리먼트)
- **다중 선택** 가능 (Ctrl+클릭)
- 선택 옵션: **단일 선택 / 다중 선택** 체크박스

### 6.2 엘리먼트 선택 감지
```csharp
// GeometryUtils.cs
/// <summary>
/// 점과 선분 사이의 최단 거리 계산
/// </summary>
public static double DistancePointToSegment(Point2D point, PathSegment segment, float originX, float originY, float scale)
{
    if (segment is LineSegment line)
    {
        Point2D p1 = new Point2D(line.Start.X * scale + originX, line.Start.Y * scale + originY);
        Point2D p2 = new Point2D(line.End.X * scale + originX, line.End.Y * scale + originY);
        
        return DistancePointToLine(point, p1, p2);
    }
    else if (segment is ArcSegment arc)
    {
        Point2D center = new Point2D(arc.Center.X * scale + originX, arc.Center.Y * scale + originY);
        double radius = arc.Radius * scale;
        
        // 점과 호 중심 사이의 거리
        double distToCenter = Distance(point, center);
        
        // 호까지의 거리 = |distToCenter - radius|
        double distToArc = Math.Abs(distToCenter - radius);
        
        // 호의 각도 범위 내인지 확인
        double angleToPoint = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180.0 / Math.PI;
        
        if (IsAngleInRange(angleToPoint, arc.StartAngle, arc.EndAngle, arc.Clockwise))
        {
            return distToArc;
        }
        else
        {
            // 호의 끝점까지의 거리
            Point2D arcStart = new Point2D(arc.Start.X * scale + originX, arc.Start.Y * scale + originY);
            Point2D arcEnd = new Point2D(arc.End.X * scale + originX, arc.End.Y * scale + originY);
            
            return Math.Min(Distance(point, arcStart), Distance(point, arcEnd));
        }
    }
    
    return double.MaxValue;
}

/// <summary>
/// 점과 선분 사이의 최단 거리
/// </summary>
private static double DistancePointToLine(Point2D point, Point2D lineStart, Point2D lineEnd)
{
    double dx = lineEnd.X - lineStart.X;
    double dy = lineEnd.Y - lineStart.Y;
    
    if (dx == 0 && dy == 0)
    {
        // 선분이 점인 경우
        return Distance(point, lineStart);
    }
    
    // 선분 위의 가장 가까운 점 찾기
    double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
    
    t = Math.Max(0, Math.Min(1, t));  // [0, 1] 범위로 클램핑
    
    Point2D closest = new Point2D(
        lineStart.X + t * dx,
        lineStart.Y + t * dy
    );
    
    return Distance(point, closest);
}

private static double Distance(Point2D p1, Point2D p2)
{
    double dx = p2.X - p1.X;
    double dy = p2.Y - p1.Y;
    return Math.Sqrt(dx * dx + dy * dy);
}
```

### 6.3 엘리먼트 선택 처리
```csharp
// SelectionManager.cs
private HashSet<(int part, int contour, int element)> selectedElements = new HashSet<(int, int, int)>();

public bool IsMultiSelectEnabled { get; set; } = false;

/// <summary>
/// 엘리먼트 선택 (단일 또는 다중)
/// </summary>
public void SelectElement(int partIndex, int contourIndex, int elementIndex, bool addToSelection)
{
    var key = (partIndex, contourIndex, elementIndex);
    
    if (addToSelection && IsMultiSelectEnabled)
    {
        // 다중 선택: 토글
        if (selectedElements.Contains(key))
            selectedElements.Remove(key);
        else
            selectedElements.Add(key);
    }
    else
    {
        // 단일 선택: 기존 선택 해제 후 선택
        selectedElements.Clear();
        selectedElements.Add(key);
    }
    
    ElementSelected?.Invoke(this, key);
}

/// <summary>
/// 엘리먼트 선택 여부 확인
/// </summary>
public bool IsElementSelected(int partIndex, int contourIndex, int elementIndex)
{
    return selectedElements.Contains((partIndex, contourIndex, elementIndex));
}

/// <summary>
/// 선택된 엘리먼트 목록 가져오기
/// </summary>
public List<(int part, int contour, int element)> GetSelectedElements()
{
    return selectedElements.ToList();
}

/// <summary>
/// 엘리먼트 선택 초기화
/// </summary>
public void ClearElementSelection()
{
    selectedElements.Clear();
}
```

### 6.4 마우스 클릭 처리
```csharp
// CamViewerControl.cs
private void HandleElementSelection(int screenX, int screenY)
{
    Point2D worldPos = ScreenToWorld(screenX, screenY);
    
    // 클릭 허용 오차 (픽셀 단위)
    float clickTolerancePixels = 5.0f;
    float clickToleranceWorld = clickTolerancePixels / zoom;
    
    // 가장 가까운 엘리먼트 찾기
    double minDistance = double.MaxValue;
    (int part, int contour, int element)? nearest = null;
    
    for (int pi = 0; pi < currentProgram.Parts.Count; pi++)
    {
        Part part = currentProgram.Parts[pi];
        float originX = (float)(part.Origin.X * workpieceScale);
        float originY = (float)(part.Origin.Y * workpieceScale);
        
        for (int ci = 0; ci < part.Contours.Count; ci++)
        {
            Contour contour = part.Contours[ci];
            
            for (int ei = 0; ei < contour.AllSegments.Count; ei++)
            {
                PathSegment segment = contour.AllSegments[ei];
                
                double distance = GeometryUtils.DistancePointToSegment(worldPos, segment, originX, originY, (float)workpieceScale);
                
                if (distance < minDistance && distance < clickToleranceWorld)
                {
                    minDistance = distance;
                    nearest = (pi, ci, ei);
                }
            }
        }
    }
    
    if (nearest.HasValue)
    {
        // Ctrl 키 확인 (다중 선택)
        bool isCtrlPressed = (ModifierKeys & Keys.Control) == Keys.Control;
        
        // 엘리먼트 선택
        selectionManager.SelectElement(nearest.Value.part, nearest.Value.contour, nearest.Value.element, isCtrlPressed);
        
        // 재렌더링
        Invalidate();
    }
    else
    {
        // 빈 공간 클릭 → 선택 해제
        if ((ModifierKeys & Keys.Control) != Keys.Control)
        {
            selectionManager.ClearElementSelection();
            Invalidate();
        }
    }
}
```

### 6.5 선택 강조 렌더링
```csharp
// CamViewerControl.cs - RenderMPFScene()
foreach (PathSegment segment in contour.AllSegments)
{
    // 엘리먼트 선택 여부 확인
    bool isElementSelected = selectionManager.IsElementSelected(pi, ci, ei);
    
    // 색상 및 두께 결정
    Color segmentColor;
    float segmentWidth;
    
    if (isElementSelected)
    {
        // 선택된 엘리먼트: 강조 표시
        segmentColor = settings.SelectedElementColor; // 주황색
        segmentWidth = settings.SelectedElementWidth; // 4.0f
    }
    else if (isContourSelected)
    {
        // 컨투어 선택 (엘리먼트는 비선택)
        segmentColor = settings.SelectedContourColor;
        segmentWidth = settings.SelectedContourWidth;
    }
    else
    {
        // 일반 세그먼트
        segmentColor = pathColor;
        segmentWidth = lineWidth;
    }
    
    float[] colorArray = settings.GetColorAsFloat(segmentColor);
    DrawPathSegment(segment, originX, originY, colorArray[0], colorArray[1], colorArray[2], segmentWidth);
    
    ei++;
}
```

### 6.6 UI 추가
```csharp
// MainForm.cs
private RadioButton rbSelectContour;
private RadioButton rbSelectElement;
private CheckBox chkMultiSelect;

private void InitializeSelectionUI()
{
    // 선택 모드 라디오 버튼
    rbSelectContour = new RadioButton
    {
        Text = "컨투어 선택",
        Location = new Point(10, 550),
        Size = new Size(120, 20),
        Checked = false
    };
    rbSelectContour.CheckedChanged += RbSelectContour_CheckedChanged;
    
    rbSelectElement = new RadioButton
    {
        Text = "엘리먼트 선택",
        Location = new Point(140, 550),
        Size = new Size(120, 20)
    };
    rbSelectElement.CheckedChanged += RbSelectElement_CheckedChanged;
    
    // 다중 선택 체크박스
    chkMultiSelect = new CheckBox
    {
        Text = "다중 선택",
        Location = new Point(270, 550),
        Size = new Size(100, 20),
        Enabled = false
    };
    chkMultiSelect.CheckedChanged += ChkMultiSelect_CheckedChanged;
}

private void RbSelectContour_CheckedChanged(object sender, EventArgs e)
{
    if (rbSelectContour.Checked)
    {
        camViewer.SelectionManager.CurrentMode = SelectionMode.Contour;
        chkMultiSelect.Enabled = false;
        chkMultiSelect.Checked = false;
    }
}

private void RbSelectElement_CheckedChanged(object sender, EventArgs e)
{
    if (rbSelectElement.Checked)
    {
        camViewer.SelectionManager.CurrentMode = SelectionMode.Element;
        chkMultiSelect.Enabled = true;
    }
}

private void ChkMultiSelect_CheckedChanged(object sender, EventArgs e)
{
    camViewer.SelectionManager.IsMultiSelectEnabled = chkMultiSelect.Checked;
}
```

---

## 7. 기능 5: 캔버스 방향 전환

### 7.1 요구사항
- **90도 회전** (시계방향)
- 설정값에 따라 회전 선택:
  - 0: 기본 (회전 없음)
  - 1: 시계방향 90도
  - 2: 180도
  - 3: 반시계방향 90도 (270도)

### 7.2 회전 열거형
```csharp
// CanvasOrientation.cs
public enum CanvasOrientation
{
    Normal = 0,     // 0도
    Rotate90CW = 1, // 시계방향 90도
    Rotate180 = 2,  // 180도
    Rotate270CW = 3 // 시계방향 270도 (= 반시계 90도)
}
```

### 7.3 OpenGL 변환 적용
```cpp
// NativeRenderer/renderer.cpp
RENDERER_API void SetCanvasOrientation(int orientation)
{
    if (!g_hRC) return;
    
    g_canvasOrientation = orientation;
}

// BeginMPFRender()에서 변환 적용
RENDERER_API void BeginMPFRenderWithBackground(float bgR, float bgG, float bgB)
{
    if (!g_hRC) return;
    
    glClearColor(bgR, bgG, bgB, 1.0f);
    glClear(GL_COLOR_BUFFER_BIT);
    
    glMatrixMode(GL_MODELVIEW);
    glLoadIdentity();
    
    // 캔버스 회전 적용
    ApplyCanvasOrientation();
}

void ApplyCanvasOrientation()
{
    switch (g_canvasOrientation)
    {
        case 0: // Normal
            // No transformation
            break;
        
        case 1: // Rotate 90 CW
            // 시계방향 90도 = 반시계방향 -90도
            glRotatef(-90.0f, 0.0f, 0.0f, 1.0f);
            // Y축 뒤집기 (좌표계 보정)
            glScalef(1.0f, -1.0f, 1.0f);
            break;
        
        case 2: // Rotate 180
            glRotatef(180.0f, 0.0f, 0.0f, 1.0f);
            break;
        
        case 3: // Rotate 270 CW = 90 CCW
            glRotatef(90.0f, 0.0f, 0.0f, 1.0f);
            glScalef(1.0f, -1.0f, 1.0f);
            break;
    }
}
```

### 7.4 C# 인터페이스
```csharp
// NativeRenderer P/Invoke
[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
public static extern void SetCanvasOrientation(int orientation);

// CamViewerControl.cs
private CanvasOrientation canvasOrientation = CanvasOrientation.Normal;

public void SetOrientation(CanvasOrientation orientation)
{
    canvasOrientation = orientation;
    NativeRenderer.SetCanvasOrientation((int)orientation);
    Invalidate();
}
```

### 7.5 UI 추가
```csharp
// MainForm.cs
private ComboBox cboOrientation;

private void InitializeOrientationUI()
{
    Label lblOrientation = new Label
    {
        Text = "캔버스 방향:",
        Location = new Point(10, 580),
        Size = new Size(80, 20)
    };
    
    cboOrientation = new ComboBox
    {
        Location = new Point(100, 580),
        Size = new Size(150, 20),
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    
    cboOrientation.Items.Add("0: 기본 (0도)");
    cboOrientation.Items.Add("1: 시계방향 90도");
    cboOrientation.Items.Add("2: 180도");
    cboOrientation.Items.Add("3: 반시계 90도");
    
    cboOrientation.SelectedIndex = 0;
    cboOrientation.SelectedIndexChanged += CboOrientation_SelectedIndexChanged;
}

private void CboOrientation_SelectedIndexChanged(object sender, EventArgs e)
{
    CanvasOrientation orientation = (CanvasOrientation)cboOrientation.SelectedIndex;
    camViewer.SetOrientation(orientation);
}
```

### 7.6 RenderSettings 통합
```csharp
// RenderSettings.cs
public CanvasOrientation Orientation { get; set; } = CanvasOrientation.Normal;

// 설정 저장/로드 시 포함
```

---

## 8. 구현 우선순위

### Phase 5.1: 기본 인프라 (1주)
1. ✅ RealTimeDataReceiver 구조 정의
2. ✅ SelectionManager 클래스 구현
3. ✅ NumberPositionManager 클래스 구현
4. ✅ GeometryUtils (Point-in-Polygon, 거리 계산)

### Phase 5.2: 파트/컨투어 번호 위치 (3일)
5. UI 추가 (위치 설정 버튼)
6. 마우스 이벤트 처리
7. 위치 저장/로드 (JSON)
8. 렌더링 통합

### Phase 5.3: 파트 외곽선 (3일)
9. Bounding Box 계산
10. 점선 패턴 정의
11. OpenGL 점선 렌더링
12. RenderSettings 통합

### Phase 5.4: 컨투어 선택 (5일)
13. Point-in-Polygon 알고리즘
14. 마우스 클릭 감지
15. 선택 강조 렌더링
16. UI 추가 (선택 모드 라디오 버튼)

### Phase 5.5: 엘리먼트 선택 (5일)
17. 점-선분 거리 계산
18. 다중 선택 로직
19. Ctrl+클릭 처리
20. UI 추가 (다중 선택 체크박스)

### Phase 5.6: 캔버스 방향 전환 (3일)
21. OpenGL 회전 변환
22. C# 인터페이스
23. UI 추가 (회전 콤보박스)
24. RenderSettings 통합

### Phase 5.7: 통합 테스트 (3일)
25. 전체 기능 테스트
26. 버그 수정
27. 문서화

**총 예상 기간: 3~4주**

---

## 9. 데이터 구조

### 9.1 새로운 클래스
```
Phase4_Graphics/
├── WinFormsApp/
│   ├── RealTime/
│   │   ├── RealTimeData.cs
│   │   └── RealTimeDataReceiver.cs
│   ├── Selection/
│   │   ├── SelectionManager.cs
│   │   ├── NumberPositionManager.cs
│   │   └── GeometryUtils.cs
│   ├── Rendering/
│   │   ├── CanvasOrientation.cs
│   │   ├── DashPattern.cs
│   │   └── Rectangle2D.cs
│   └── ...
└── ...
```

### 9.2 RenderSettings 확장
```csharp
// 파트 외곽선
public Color PartBoundaryColor { get; set; }
public float PartBoundaryWidth { get; set; }
public DashPatternType PartBoundaryDashType { get; set; }

// 선택 강조
public Color SelectedContourColor { get; set; }
public float SelectedContourWidth { get; set; }
public Color SelectedElementColor { get; set; }
public float SelectedElementWidth { get; set; }

// 캔버스 방향
public CanvasOrientation Orientation { get; set; }
```

---

## 10. UI 변경사항

### 10.1 MainForm 레이아웃
```
┌─────────────────────────────────────────────────────────┐
│ MenuStrip: File | Settings | Simulation | Help         │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────┐  ┌──────────────────────────────┐    │
│  │ File         │  │                               │    │
│  │ Explorer     │  │      CamViewerControl         │    │
│  │              │  │       (OpenGL Rendering)      │    │
│  │              │  │                               │    │
│  │              │  │                               │    │
│  └──────────────┘  └──────────────────────────────┘    │
│                                                          │
├─────────────────────────────────────────────────────────┤
│ Simulation Controls:                                    │
│  [Start] [Pause] [Resume] [Reset] Speed: [───────]     │
│                                                          │
│ Number Positioning:                                     │
│  [파트 번호 위치 설정] [컨투어 번호 위치 설정]        │
│                                                          │
│ Selection Mode:                                         │
│  ( ) 컨투어 선택  ( ) 엘리먼트 선택  [ ] 다중 선택    │
│                                                          │
│ Canvas Orientation:                                     │
│  방향: [0: 기본 (0도) ▼]                               │
│                                                          │
│ Status: Ready                                           │
└─────────────────────────────────────────────────────────┘
```

---

## 11. 다음 단계

### Phase 5 완료 후:
- **Phase 6**: ITag 실시간 통신 구현
- **Phase 7**: 고급 시각화 (히트맵, 속도 그래프)
- **Phase 8**: 데이터 로깅 및 분석

### 즉시 시작 가능:
1. **RealTimeDataReceiver 스텁 구현** (통신은 나중에)
2. **SelectionManager 구현** (컨투어/엘리먼트 선택 기반)
3. **GeometryUtils 구현** (기하학 알고리즘)

---

## 12. 참고 자료

### Siemens WinCC ITag
- **DLL**: Siemens.Runtime.ControlDev.dll
- **문서**: 109760182_ActiveXWCCPenUS.pdf
- **샘플**: 109760182_ControlDevelopment_samples.7z

### OpenGL 참고
- Line Stipple: `glLineStipple()`, `glEnable(GL_LINE_STIPPLE)`
- 2D Transformations: `glRotatef()`, `glScalef()`

### 알고리즘 참고
- **Point-in-Polygon**: Ray Casting Algorithm
- **Point-to-Segment Distance**: Parametric line equation

---

**Phase 5 설계 완료! 구현 준비되었습니다! 🚀**
