# WinCC Advanced v17 통합 가이드

이 문서는 CAM Viewer POC를 Siemens TIA Portal WinCC Advanced v17 프로젝트에 통합하는 방법을 설명합니다.

## WinCC Advanced 개요

### 지원 환경
- **TIA Portal**: v17 (V17 Update 1 이상 권장)
- **WinCC Advanced**: Runtime Advanced
- **운영체제**: Windows 10/11 (64-bit)
- **.NET Framework**: 4.7.2 이상

### UserControl 요구사항

WinCC Advanced에서 사용자 정의 컨트롤은:
1. **System.Windows.Forms.UserControl** 상속
2. **Serializable** 특성 필요
3. **Public** 생성자 필요
4. WinCC **디자인 타임**에 안전하게 로드 가능

## 통합 방법

### 방법 1: 소스 코드 직접 통합 (개발/테스트용)

#### 단계 1: 파일 준비

1. **CamViewerControl.cs** 복사:
   ```
   TIA_Project/ExternalControls/CamViewerControl.cs
   ```

2. **NativeRenderer.dll** 복사:
   ```
   TIA_Project/Runtime/NativeRenderer.dll
   ```

#### 단계 2: TIA Portal에서 설정

1. **TIA Portal 열기** → 프로젝트 선택

2. **HMI 장치** 선택 → **Graphics** 폴더

3. **화면 열기** 또는 새 화면 생성

4. **Toolbox** → 마우스 우클릭 → **Choose Items**

5. **.NET Framework Components** 탭 → **Browse**

6. 컴파일된 DLL 선택 또는 소스 참조

7. **CamViewerControl** 체크 → **OK**

#### 단계 3: 화면에 배치

1. Toolbox에서 **CamViewerControl** 드래그

2. 원하는 크기로 조정

3. 속성 창에서 설정 구성

### 방법 2: DLL 어셈블리 배포 (프로덕션)

#### 단계 1: Class Library 생성

**새 프로젝트 구조**:
```
CamViewerControl.Library/
├── CamViewerControl.cs        # 기존 코드
├── NativeRenderer.cs           # P/Invoke 래퍼
├── Properties/
│   └── AssemblyInfo.cs
└── CamViewerControl.Library.csproj
```

**프로젝트 설정**:
```xml
<PropertyGroup>
  <OutputType>Library</OutputType>
  <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
  <PlatformTarget>AnyCPU</PlatformTarget>
</PropertyGroup>
```

#### 단계 2: 어셈블리 빌드

```cmd
msbuild CamViewerControl.Library.csproj /p:Configuration=Release
```

**출력**:
```
bin/Release/
├── CamViewerControl.Library.dll
├── CamViewerControl.Library.pdb (디버깅용)
└── NativeRenderer.dll (함께 배포)
```

#### 단계 3: 서명 (선택사항, 보안 강화)

```cmd
REM 강력한 이름 키 생성
sn -k CamViewer.snk

REM 프로젝트에 키 설정
REM Project Properties → Signing → "Sign the assembly" 체크
```

#### 단계 4: GAC 등록 (선택사항)

```cmd
REM 관리자 권한 명령 프롬프트
gacutil /i CamViewerControl.Library.dll
```

**장점**:
- 중앙 집중식 관리
- 버전 관리 용이
- DLL Hell 방지

**단점**:
- 설치 권한 필요
- 업데이트 시 재등록 필요

#### 단계 5: WinCC 프로젝트에 추가

**Option A: 직접 참조**
```
TIA_Project/
└── ExternalAssemblies/
    ├── CamViewerControl.Library.dll
    └── NativeRenderer.dll
```

**Option B: 프로젝트 참조**
1. Solution Explorer → References → Add Reference
2. Browse → DLL 선택
3. Copy Local = True

## WinCC 태그 바인딩

### 속성 노출

```csharp
using System.ComponentModel;

public partial class CamViewerControl : UserControl
{
    // WinCC 태그 바인딩을 위한 속성
    [Category("WinCC Data")]
    [Description("데이터 소스 태그 이름")]
    [Browsable(true)]
    public string DataSourceTag { get; set; }

    [Category("WinCC Data")]
    [Description("줌 레벨 태그")]
    [Browsable(true)]
    public string ZoomTag { get; set; }

    [Category("WinCC Appearance")]
    [Description("배경 색상")]
    [Browsable(true)]
    public Color ViewerBackColor
    {
        get => renderPanel.BackColor;
        set => renderPanel.BackColor = value;
    }
}
```

### 런타임 태그 연결

```csharp
// WinCC API 사용 예제 (의사 코드)
public void ConnectToWinCC()
{
    if (DesignMode) return;

    try
    {
        // HMI Runtime API를 통한 태그 접근
        // var tagService = HMIRuntime.Tags;
        
        // 태그 변경 이벤트 구독
        // tagService[DataSourceTag].ValueChanged += OnDataChanged;
        // tagService[ZoomTag].ValueChanged += OnZoomChanged;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Trace.WriteLine($"WinCC connection error: {ex.Message}");
    }
}

private void OnDataChanged(object sender, TagEventArgs e)
{
    // 태그 값 변경 시 렌더링 업데이트
    if (InvokeRequired)
    {
        Invoke(new Action(() => UpdateVisualization(e.NewValue)));
    }
    else
    {
        UpdateVisualization(e.NewValue);
    }
}
```

## 런타임 배포

### 파일 구조

```
C:\Program Files\Siemens\Automation\WinCC_Runtime\
└── YourProject/
    ├── Runtime/
    │   ├── CamViewerControl.Library.dll
    │   ├── NativeRenderer.dll           # ⚠️ 필수!
    │   └── (WinCC 생성 파일들)
    └── Graphics/
        └── MainScreen.pdl (CamViewerControl 포함)
```

### 배포 체크리스트

- [ ] CamViewerControl.Library.dll 복사
- [ ] NativeRenderer.dll 복사 (같은 폴더)
- [ ] Visual C++ Redistributable 설치 확인
- [ ] .NET Framework 4.7.2+ 설치 확인
- [ ] OpenGL 드라이버 확인
- [ ] 관리자 권한으로 WinCC Runtime 시작 (첫 실행)

### 자동 배포 스크립트

```batch
@echo off
REM deploy_to_wincc.bat

set WINCC_RUNTIME=C:\Program Files\Siemens\Automation\WinCC_Runtime\YourProject\Runtime
set SOURCE_DIR=%~dp0WinFormsApp\bin\Release

echo Deploying CAM Viewer to WinCC Runtime...

REM DLL 복사
copy /Y "%SOURCE_DIR%\CamViewerControl.Library.dll" "%WINCC_RUNTIME%\"
copy /Y "%SOURCE_DIR%\NativeRenderer.dll" "%WINCC_RUNTIME%\"

echo Deployment completed!
pause
```

## 디자인 타임 고려사항

### DesignMode 처리

```csharp
public CamViewerControl()
{
    InitializeComponent();
    
    if (!DesignMode)
    {
        // 런타임에만 OpenGL 초기화
        SetupRenderPanel();
    }
    else
    {
        // 디자인 타임: 간단한 플레이스홀더
        var label = new Label
        {
            Text = "CAM Viewer\n(Runtime Only)",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.DarkGray,
            ForeColor = Color.White
        };
        this.Controls.Add(label);
    }
}
```

### TypeConverter 및 Serialization

```csharp
[TypeConverter(typeof(ExpandableObjectConverter))]
[Serializable]
public class ViewerSettings
{
    public float DefaultZoom { get; set; } = 1.0f;
    public bool EnablePanning { get; set; } = true;
    public bool EnableZoom { get; set; } = true;
}

public partial class CamViewerControl : UserControl
{
    [Category("WinCC Settings")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public ViewerSettings Settings { get; set; } = new ViewerSettings();
}
```

## 성능 최적화

### 1. 렌더링 빈도 제어

```csharp
private System.Windows.Forms.Timer renderTimer;
private const int TARGET_FPS = 30;

private void InitializeTimer()
{
    renderTimer = new System.Windows.Forms.Timer();
    renderTimer.Interval = 1000 / TARGET_FPS;
    renderTimer.Tick += (s, e) => renderPanel.Invalidate();
    renderTimer.Start();
}
```

### 2. 더블 버퍼링 비활성화 (OpenGL 사용 시)

```csharp
public CamViewerControl()
{
    // OpenGL이 자체 버퍼링을 하므로 GDI 버퍼링 비활성화
    SetStyle(ControlStyles.AllPaintingInWmPaint, true);
    SetStyle(ControlStyles.Opaque, true);
    SetStyle(ControlStyles.UserPaint, true);
}
```

### 3. 백그라운드 데이터 로딩

```csharp
public async Task LoadDataAsync(string filePath)
{
    // 백그라운드 스레드에서 데이터 로드
    var data = await Task.Run(() => LoadDataFromFile(filePath));
    
    // UI 스레드에서 렌더링 업데이트
    Invoke(new Action(() => {
        UpdateRenderer(data);
    }));
}
```

## 문제 해결

### 문제 1: "Type could not be loaded" 오류

**원인**: 어셈블리 또는 종속성을 찾을 수 없음

**해결책**:
1. DLL 위치 확인
2. .NET Framework 버전 확인
3. 이벤트 뷰어에서 자세한 오류 확인

### 문제 2: 디자인 타임에 컨트롤이 표시되지 않음

**원인**: 디자인 타임에 NativeRenderer.dll 로드 시도

**해결책**:
```csharp
if (!DesignMode && !LicenseManager.UsageMode == LicenseUsageMode.Designtime)
{
    InitializeOpenGL();
}
```

### 문제 3: WinCC Runtime에서 충돌

**원인**: OpenGL 컨텍스트 충돌

**해결책**:
1. 전용 스레드에서 OpenGL 컨텍스트 생성
2. WinCC Runtime 설정에서 그래픽 가속 확인
3. 최신 그래픽 드라이버 설치

### 문제 4: 태그 바인딩 동작 안 함

**디버깅**:
```csharp
protected override void OnLoad(EventArgs e)
{
    base.OnLoad(e);
    
    if (!DesignMode)
    {
        System.Diagnostics.Trace.WriteLine($"DataSourceTag: {DataSourceTag}");
        ConnectToWinCC();
    }
}
```

**WinCC Output 창 확인**:
- TIA Portal → Tools → Settings → HMI → Engineering
- "Show diagnostic window" 체크

## 보안 고려사항

### 1. 코드 서명

```cmd
REM 인증서로 어셈블리 서명
signtool sign /f MyCert.pfx /p password /t http://timestamp.digicert.com CamViewerControl.Library.dll
```

### 2. WinCC 보안 정책

**allowedAssemblies.config** 편집:
```xml
<configuration>
  <allowedAssemblies>
    <add name="CamViewerControl.Library" 
         version="1.0.0.0" 
         publicKeyToken="..." />
  </allowedAssemblies>
</configuration>
```

### 3. 런타임 권한

- WinCC Runtime 서비스 계정에 DLL 읽기 권한 부여
- OpenGL 컨텍스트 생성 권한 확인

## 버전 관리 및 업데이트

### 어셈블리 버전 정책

```csharp
// AssemblyInfo.cs
[assembly: AssemblyVersion("1.0.0.0")]          // 메이저 변경
[assembly: AssemblyFileVersion("1.0.0.0")]      // 빌드 버전
[assembly: AssemblyInformationalVersion("1.0.0-beta")]  // 표시 버전
```

### 업데이트 절차

1. **개발**:
   - 새 버전 빌드
   - 테스트 환경에서 검증

2. **배포**:
   - WinCC Runtime 중지
   - 기존 DLL 백업
   - 새 DLL 복사
   - WinCC Runtime 시작

3. **롤백** (문제 발생 시):
   - WinCC Runtime 중지
   - 백업 DLL 복원
   - WinCC Runtime 시작

## 참고 자료

### Siemens 문서
- [TIA Portal WinCC Advanced User Manual](https://support.industry.siemens.com/)
- [WinCC Advanced Scripting Guide](https://cache.industry.siemens.com/)
- [Custom Controls Development Guide](https://support.industry.siemens.com/)

### Microsoft 문서
- [Windows Forms Custom Controls](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/controls/custom-controls)
- [P/Invoke Tutorial](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)

---

**문서 버전**: 1.0  
**호환 WinCC 버전**: Advanced v17+  
**최종 업데이트**: 2024
