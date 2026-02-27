# Phase 8.1 구현 요약 - 파트/컨투어 번호 표시

## ✅ 완료된 작업

### 1. OpenGL 텍스트 렌더링 시스템 구현

#### **TextRenderer.h / TextRenderer.cpp**
- ✅ `TextRenderer` C++ 클래스 구현
- ✅ OpenGL display list 기반 폰트 렌더링
- ✅ `wglUseFontOutlinesW()` 사용하여 고품질 폴리곤 폰트 생성
- ✅ 번호 및 텍스트 그리기 기능
- ✅ 중앙 정렬 및 스케일 조정

**주요 함수:**
```cpp
bool Create(const wchar_t* fontName, int height, bool bold, bool italic);
bool DrawNumber(double posX, double posY, unsigned int number, double scale);
bool DrawText(double posX, double posY, const char* text, double scale);
```

#### **renderer.h / renderer.cpp 업데이트**
- ✅ 텍스트 렌더링 함수 추가:
  - `InitializeTextRenderer()` - 폰트 초기화
  - `DrawPartNumber()` - 파트 번호 그리기
  - `DrawContourNumber()` - 컨투어 번호 그리기
  - `CleanupTextRenderer()` - 리소스 정리

### 2. C# P/Invoke 래퍼 구현

#### **NativeTextRenderer.cs**
- ✅ DLL 호출을 위한 P/Invoke 선언
- ✅ `LabelRenderSettings` 클래스 - 라벨 표시 설정
  - 파트/컨투어 번호 표시 on/off
  - 폰트 크기 설정 (픽셀)
  - 색상 설정 (RGB)
  - 폰트 이름 및 스타일
  - 표시 개수 제한 (성능 최적화)

### 3. 라벨 위치 계산 로직

#### **LabelPositionCalculator.cs**
- ✅ 바운딩 박스 기반 위치 계산
- ✅ 파트 라벨: 중앙 + 오프셋 (-5mm X, +5mm Y)
- ✅ 컨투어 라벨: 상단 중앙 + 마진 (2mm 위)
- ✅ 원호 세그먼트의 중심점 ± 반지름 포함
- ✅ 줌 레벨에 따른 스케일 계산

**주요 함수:**
```csharp
public static Point2D CalculatePartLabelPosition(Part part);
public static Point2D CalculateContourLabelPosition(Contour contour);
public static double CalculateLabelScale(double zoom, int baseFontSize);
```

### 4. RenderSettings 통합

#### **RenderSettings.cs 업데이트**
- ✅ `LabelSettings` 속성 추가
- ✅ 라벨 설정과 기존 렌더 설정 통합

---

## 📋 다음 작업 (남은 구현)

### 1. CamViewerControl에 라벨 렌더링 통합

**필요한 수정:**

```csharp
// CamViewerControl.cs

private bool textRendererInitialized = false;

// 초기화 시
private void InitializeTextRenderer()
{
    var settings = RenderSettings.Instance.LabelSettings;
    int result = NativeTextRenderer.InitializeTextRenderer(
        settings.FontName,
        settings.PartNumberFontSize,
        settings.Bold ? 1 : 0,
        0
    );
    textRendererInitialized = (result == 1);
}

// RenderMPFScene() 메서드 끝부분에 추가
private void RenderMPFScene()
{
    // ... 기존 렌더링 코드 ...
    
    NativeRenderer.EndMPFRender();
    
    // Phase 8.1: Draw part and contour numbers
    if (textRendererInitialized)
    {
        DrawPartAndContourNumbers();
    }
}

private void DrawPartAndContourNumbers()
{
    if (currentProgram == null) return;
    
    var settings = RenderSettings.Instance.LabelSettings;
    if (!settings.ShowPartNumbers && !settings.ShowContourNumbers)
        return;
    
    int displayCount = 0;
    
    // 파트 번호 표시
    if (settings.ShowPartNumbers)
    {
        var partColor = settings.GetPartNumberColorF();
        
        foreach (var part in currentProgram.Parts)
        {
            if (displayCount >= settings.MaxNumbersToDisplay) break;
            
            var pos = LabelPositionCalculator.CalculatePartLabelPosition(part);
            double scale = LabelPositionCalculator.CalculateLabelScale(
                zoom, settings.PartNumberFontSize);
            
            // 월드피스 스케일 적용
            double worldX = pos.X * workpieceScale;
            double worldY = pos.Y * workpieceScale;
            
            NativeTextRenderer.DrawPartNumber(
                worldX, worldY, (uint)part.Number,
                scale, partColor.r, partColor.g, partColor.b);
            
            displayCount++;
        }
    }
    
    // 컨투어 번호 표시
    if (settings.ShowContourNumbers)
    {
        var contourColor = settings.GetContourNumberColorF();
        
        foreach (var part in currentProgram.Parts)
        {
            float offsetX = (float)(part.Origin.X * workpieceScale);
            float offsetY = (float)(part.Origin.Y * workpieceScale);
            
            foreach (var contour in part.Contours)
            {
                if (displayCount >= settings.MaxNumbersToDisplay) break;
                
                var pos = LabelPositionCalculator.CalculateContourLabelPosition(contour);
                double scale = LabelPositionCalculator.CalculateLabelScale(
                    zoom, settings.ContourNumberFontSize);
                
                // 파트 오리진 오프셋 및 워크피스 스케일 적용
                double worldX = (pos.X * workpieceScale) + offsetX;
                double worldY = (pos.Y * workpieceScale) + offsetY;
                
                NativeTextRenderer.DrawContourNumber(
                    worldX, worldY, (uint)contour.Number,
                    scale, contourColor.r, contourColor.g, contourColor.b);
                
                displayCount++;
            }
        }
    }
}

// Dispose에 정리 추가
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        if (textRendererInitialized)
        {
            NativeTextRenderer.CleanupTextRenderer();
            textRendererInitialized = false;
        }
        
        if (isInitialized)
        {
            NativeRenderer.CleanupRenderer();
            isInitialized = false;
        }
    }
    base.Dispose(disposing);
}
```

### 2. UI 옵션 추가 (RenderSettingsForm)

**필요한 UI 컨트롤:**

```
┌─────────────────────────────────────┐
│ 라벨 표시 옵션                       │
├─────────────────────────────────────┤
│                                     │
│ ☑ 파트 번호 표시                     │
│   폰트 크기: [24] ▲▼ 픽셀           │
│   색상: [■ 노란색] [변경...]         │
│                                     │
│ ☑ 컨투어 번호 표시                   │
│   폰트 크기: [18] ▲▼ 픽셀           │
│   색상: [■ 흰색] [변경...]           │
│                                     │
│ 폰트: [Arial         ▼]            │
│ ☑ 굵게 표시                         │
│                                     │
│ 표시 제한: [100] 개                 │
│                                     │
│ [적용] [취소] [기본값으로]           │
└─────────────────────────────────────┘
```

**코드 추가 (RenderSettingsForm.cs):**

```csharp
private void InitializeLabelSettingsTab()
{
    // 파트 번호 그룹
    var partNumberGroup = new GroupBox
    {
        Text = "파트 번호",
        Location = new Point(10, 10),
        Size = new Size(350, 100)
    };
    
    var chkShowPartNumbers = new CheckBox
    {
        Text = "파트 번호 표시",
        Location = new Point(10, 20),
        Checked = RenderSettings.Instance.LabelSettings.ShowPartNumbers
    };
    chkShowPartNumbers.CheckedChanged += (s, e) => {
        RenderSettings.Instance.LabelSettings.ShowPartNumbers = chkShowPartNumbers.Checked;
    };
    
    var numPartFontSize = new NumericUpDown
    {
        Location = new Point(100, 45),
        Minimum = 6,
        Maximum = 48,
        Value = RenderSettings.Instance.LabelSettings.PartNumberFontSize
    };
    numPartFontSize.ValueChanged += (s, e) => {
        RenderSettings.Instance.LabelSettings.PartNumberFontSize = (int)numPartFontSize.Value;
    };
    
    // 컨투어 번호 그룹
    var contourNumberGroup = new GroupBox
    {
        Text = "컨투어 번호",
        Location = new Point(10, 120),
        Size = new Size(350, 100)
    };
    
    // ... (파트 번호와 유사하게 구현)
    
    // 공통 설정
    var commonGroup = new GroupBox
    {
        Text = "공통 설정",
        Location = new Point(10, 230),
        Size = new Size(350, 100)
    };
    
    var cmbFontName = new ComboBox
    {
        Location = new Point(100, 20),
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    cmbFontName.Items.AddRange(new[] { "Arial", "맑은 고딕", "굴림", "돋움" });
    cmbFontName.SelectedItem = RenderSettings.Instance.LabelSettings.FontName;
    
    var chkBold = new CheckBox
    {
        Text = "굵게",
        Location = new Point(10, 50),
        Checked = RenderSettings.Instance.LabelSettings.Bold
    };
}
```

### 3. 빌드 및 테스트

**빌드 명령:**
```bash
cd /home/user/webapp/Phase8_RealtimeTrace

# Native DLL 빌드
cd NativeRenderer
mkdir -p build && cd build
cmake .. -G "Visual Studio 15 2017" -A x64
cmake --build . --config Debug

# C# 프로젝트 빌드
cd ../../WinFormsApp
msbuild CamViewerPOC.csproj /p:Configuration=Debug /p:Platform=x64
```

**테스트 시나리오:**
1. MPF 파일 로드
2. 라벨 표시 옵션 활성화
3. 파트 번호가 각 파트 중앙에 표시되는지 확인
4. 컨투어 번호가 각 컨투어 상단에 표시되는지 확인
5. 줌 인/아웃 시 라벨 크기가 적절히 조정되는지 확인
6. 폰트 크기 변경 시 즉시 반영되는지 확인

---

## 🎯 성공 기준

- [x] OpenGL 텍스트 렌더링 시스템 구현
- [x] 바운딩 박스 기반 라벨 위치 계산
- [ ] CamViewerControl에 라벨 렌더링 통합
- [ ] UI 옵션 추가 (RenderSettingsForm)
- [ ] 줌 레벨 대응 동적 크기 조정 테스트
- [ ] 빌드 및 기능 테스트
- [ ] 성능 테스트 (100개 이상 라벨 표시 시)

---

## 📝 참고 사항

### 좌표계 변환
- MPF 좌표 (mm) → 월드 좌표 (workpieceScale 적용)
- 파트 오리진 오프셋 추가 (컨투어의 경우)
- 줌 레벨에 따른 스케일 조정

### 성능 최적화
- `MaxNumbersToDisplay` 제한 사용
- Display list 기반 렌더링 (재사용 가능)
- 화면에 보이는 라벨만 렌더링 (선택사항)

### 디버깅
- `TextRenderer_IsValid()` 함수로 초기화 상태 확인
- OpenGL 에러 체크: `glGetError()`
- 바운딩 박스 시각화 (디버그 모드)

---

**다음 단계**: CamViewerControl에 라벨 렌더링 통합 구현
