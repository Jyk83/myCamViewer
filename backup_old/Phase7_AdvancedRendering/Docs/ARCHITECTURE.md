# CAM Viewer POC - 아키텍처 설계 문서

## 개요

이 문서는 WinCC Advanced v17에 삽입 가능한 C# + C++ 하이브리드 CAM 뷰어의 상세 아키텍처를 설명합니다.

## 설계 원칙

### 1. 관심사의 분리 (Separation of Concerns)

```
UI/데이터 레이어 (C#) ←→ 렌더링 레이어 (C++)
```

- **C# 레이어**: 비즈니스 로직, UI, 데이터 관리
- **C++ 레이어**: 고성능 그래픽 렌더링

### 2. 플랫폼 호환성

- Windows 10/11 64-bit
- .NET Framework 4.7.2+ (WinCC Advanced v17 요구사항)
- OpenGL 2.1+ 지원

### 3. WinCC 통합 우선

- UserControl 기반 설계
- WinCC Runtime과의 안정적인 상호운용
- 태그 시스템과의 원활한 연동

## 상세 컴포넌트 설계

### 1. C++ Native Renderer (NativeRenderer.dll)

#### 1.1 책임

- OpenGL 컨텍스트 생성 및 관리
- 2D/3D 기하학 렌더링
- 뷰포트 변환 (줌, 패닝)
- 렌더링 최적화

#### 1.2 API 설계

**초기화/정리**:
```cpp
int InitializeRenderer(void* windowHandle)
// 반환: 1=성공, 0=실패
// windowHandle: HWND 핸들

void CleanupRenderer()
// OpenGL 리소스 해제
```

**렌더링 제어**:
```cpp
void ResizeViewport(int width, int height)
// 뷰포트 크기 변경 시 호출

void RenderFrame()
// 현재 장면 렌더링 (SwapBuffers 포함)
```

**도형 관리**:
```cpp
void DrawRectangle(float x, float y, float width, float height, 
                   float r, float g, float b)
void DrawCircle(float x, float y, float radius, 
                float r, float g, float b)
void ClearShapes()
```

**뷰 변환**:
```cpp
void SetViewTransform(float zoom, float panX, float panY)
// zoom: 1.0 = 기본, >1.0 = 확대
// panX, panY: 화면 이동 오프셋
```

#### 1.3 내부 구조

```cpp
// 전역 상태
static HGLRC g_hRC;              // OpenGL 렌더링 컨텍스트
static HDC g_hDC;                // 디바이스 컨텍스트
static int g_viewportWidth;       // 뷰포트 너비
static int g_viewportHeight;      // 뷰포트 높이
static float g_zoom;              // 줌 레벨
static float g_panX, g_panY;      // 패닝 오프셋

// 도형 데이터
struct Shape {
    enum Type { RECTANGLE, CIRCLE };
    Type type;
    float x, y, width, height, radius;
    float r, g, b;
};
static std::vector<Shape> g_shapes;
```

#### 1.4 렌더링 파이프라인

```
1. wglMakeCurrent() - 컨텍스트 활성화
   ↓
2. glClear() - 화면 클리어
   ↓
3. 투영 행렬 설정 (glOrtho)
   ↓
4. 모델 행렬 설정 (줌/패닝 적용)
   ↓
5. 도형 렌더링 (glBegin/glEnd)
   ↓
6. SwapBuffers() - 버퍼 교체
```

### 2. C# WinForms Layer

#### 2.1 CamViewerControl (UserControl)

**책임**:
- Native DLL P/Invoke 래핑
- 사용자 인터랙션 처리
- WinCC 태그 연동 준비
- 고수준 API 제공

**주요 메서드**:
```csharp
// 공개 API
public void AddRectangle(float x, float y, float width, float height, Color color)
public void AddCircle(float x, float y, float radius, Color color)
public void ClearScene()
public void ResetView()
public void DrawSampleShapes()

// 내부 메서드
private void InitializeOpenGL()
private void UpdateViewTransform()
```

**이벤트 처리**:
```csharp
// 마우스 이벤트
- MouseDown: 패닝 시작
- MouseMove: 패닝 업데이트
- MouseUp: 패닝 종료
- MouseWheel: 줌 제어

// 컨트롤 이벤트
- Load: OpenGL 초기화
- Resize: 뷰포트 조정
- Paint: 렌더링 트리거
```

#### 2.2 P/Invoke 브리지

```csharp
internal static class NativeRenderer
{
    private const string DllName = "NativeRenderer.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int InitializeRenderer(IntPtr windowHandle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void CleanupRenderer();

    // ... 기타 함수
}
```

**P/Invoke 설계 고려사항**:
- **CallingConvention**: Cdecl 사용 (C 표준)
- **데이터 마샬링**: 단순 타입만 사용 (float, int, IntPtr)
- **에러 처리**: 반환값으로 성공/실패 전달
- **메모리 관리**: Native 측에서 관리

## 데이터 흐름

### 초기화 시퀀스

```
1. MainForm 생성
   ↓
2. CamViewerControl 생성
   ↓
3. Control.Load 이벤트
   ↓
4. InitializeOpenGL()
   ↓
5. NativeRenderer.InitializeRenderer(panel.Handle)
   ↓
6. OpenGL 컨텍스트 생성 (C++)
   ↓
7. 초기 도형 렌더링
```

### 렌더링 시퀀스

```
사용자 액션 (예: AddRectangle)
   ↓
C# 메서드 호출
   ↓
P/Invoke → NativeRenderer.DrawRectangle()
   ↓
C++ 도형 데이터 추가 (std::vector)
   ↓
renderPanel.Invalidate()
   ↓
Paint 이벤트 발생
   ↓
NativeRenderer.RenderFrame()
   ↓
OpenGL 렌더링 (C++)
   ↓
SwapBuffers() → 화면 갱신
```

### 인터랙션 시퀀스 (줌/패닝)

```
마우스 휠/드래그
   ↓
C# 이벤트 핸들러
   ↓
zoom/pan 변수 업데이트
   ↓
UpdateViewTransform()
   ↓
NativeRenderer.SetViewTransform(zoom, panX, panY)
   ↓
C++ 전역 변수 업데이트
   ↓
renderPanel.Invalidate()
   ↓
RenderFrame() → 변환 적용된 렌더링
```

## 성능 고려사항

### 1. 렌더링 최적화

**현재 구현** (POC):
- Immediate mode (glBegin/glEnd)
- CPU에서 도형 데이터 관리

**향후 최적화**:
```cpp
// VBO 사용
GLuint vbo, ibo;
glGenBuffers(1, &vbo);
glBindBuffer(GL_ARRAY_BUFFER, vbo);
glBufferData(GL_ARRAY_BUFFER, vertices.size() * sizeof(Vertex), 
             vertices.data(), GL_STATIC_DRAW);

// 셰이더 프로그램
GLuint shaderProgram = glCreateProgram();
// ... 셰이더 컴파일 및 링크
```

### 2. 메모리 관리

**C++ 측**:
- `std::vector`로 동적 메모리 관리
- RAII 패턴으로 리소스 자동 해제
- 명시적 `CleanupRenderer()` 호출

**C# 측**:
- `Dispose` 패턴 구현
- `CleanupRenderer()` 호출 보장

### 3. 스레딩

**현재**:
- 단일 UI 스레드에서 모든 작업 수행

**향후**:
```csharp
// 백그라운드 데이터 로드
Task.Run(() => {
    var data = LoadData();
    Invoke(() => UpdateRenderer(data));
});
```

## WinCC Advanced 통합 가이드

### 1. UserControl 등록

**Option A: 소스 코드 통합**
```csharp
// WinCC 프로젝트에 CamViewerControl.cs 추가
// Graphics Designer에서 직접 사용
```

**Option B: DLL 컴파일 및 등록**
```csharp
// 1. Class Library로 컴파일
// 2. GAC 등록 또는 프로젝트 참조
// 3. Toolbox에 추가
```

### 2. 태그 바인딩 (예제)

```csharp
public class CamViewerControl : UserControl
{
    // WinCC 태그 프로퍼티
    [Browsable(true)]
    [Category("WinCC")]
    public string DataSourceTag { get; set; }

    // 태그 변경 이벤트 구독
    public void ConnectToWinCC()
    {
        // WinCC API를 통한 태그 연결
        // HMIRuntime.Tags[DataSourceTag].Changed += OnTagChanged;
    }

    private void OnTagChanged(object sender, TagEventArgs e)
    {
        // 태그 값에 따라 렌더링 업데이트
        UpdateVisualization(e.NewValue);
    }
}
```

### 3. 배포 구성

```
WinCC_Project/
├── bin/
│   ├── CamViewerPOC.dll          # C# 어셈블리
│   ├── NativeRenderer.dll        # C++ DLL
│   └── ... (기타 종속성)
└── Graphics/
    └── MainScreen.pdl            # CamViewerControl 사용
```

## 확장 로드맵

### Phase 1: 기본 기능 (현재)
- ✅ 사각형/원형 렌더링
- ✅ 줌/패닝
- ✅ P/Invoke 브리지

### Phase 2: 고급 렌더링
- [ ] VBO/IBO 사용
- [ ] 셰이더 프로그램
- [ ] 텍스처 매핑
- [ ] 안티앨리어싱

### Phase 3: CAM 기능
- [ ] STL 파일 로드
- [ ] 슬라이스 시각화
- [ ] 레이어 관리
- [ ] 측정 도구

### Phase 4: WinCC 통합
- [ ] 태그 바인딩
- [ ] 알람 시각화
- [ ] 트렌드 표시
- [ ] 레포트 생성

## 참고 자료

### OpenGL
- [OpenGL 2.1 Reference](https://www.khronos.org/registry/OpenGL-Refpages/gl2.1/)
- [GLFW Documentation](https://www.glfw.org/docs/latest/)

### WinCC Advanced
- [TIA Portal WinCC Advanced Documentation](https://support.industry.siemens.com/)
- [WinCC User Controls Guide](https://cache.industry.siemens.com/)

### P/Invoke
- [Microsoft P/Invoke Tutorial](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
- [Marshaling Guide](https://docs.microsoft.com/en-us/dotnet/framework/interop/marshaling-data-with-platform-invoke)

---

**문서 버전**: 1.0  
**최종 업데이트**: 2024  
**작성자**: CAM Viewer Development Team
