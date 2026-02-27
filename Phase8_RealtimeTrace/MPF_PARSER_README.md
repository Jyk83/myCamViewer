# MPF Parser Implementation - Complete Guide

## 📋 Overview

MPF Parser를 TypeScript 버전에서 C#로 완전히 포팅했습니다. HK 레이저 절단 프로그램 파일을 파싱하고 OpenGL로 시각화할 수 있습니다.

---

## 🎯 구현 완료 사항

### ✅ Phase 1: MPF 데이터 구조 (완료)

**위치**: `/home/user/webapp/CamViewerPOC/WinFormsApp/MPF/`

#### 생성된 파일:
1. **Point2D.cs** - 2D 좌표 구조체
2. **PathSegment.cs** - 경로 세그먼트 (Line, Arc)
3. **Commands.cs** - 모든 HK 명령어 클래스
4. **Contour.cs** - 컨투어 및 Lead-in 정보
5. **Part.cs** - 파트 및 네스팅 정보
6. **MPFProgram.cs** - 전체 MPF 프로그램 데이터
7. **MPFParser.cs** - 파서 로직 (TypeScript 포팅)

#### 지원하는 HK 함수:
```csharp
HKLDB  - 절단 데이터베이스 로드
HKINI  - 초기화 (워크피스 크기)
HKOST  - 파트 원점 설정 (네스팅)
HKPPP  - 파트 프로그램 포인터
HKEND  - 네스팅 종료
HKSTR  - 컨투어 시작
HKPIE  - 피어싱
HKLEA  - Lead-in 경로
HKCUT  - 절단 시작
HKSTO  - 컨투어 종료
HKPED  - 파트 종료
HKSCRC - 잔재 절단
```

#### 지원하는 G-code:
```csharp
G0  - 급속 이동 (직선)
G1  - 직선 보간
G2  - 시계방향 원호
G3  - 반시계방향 원호
M30 - 프로그램 종료
```

---

### ✅ Phase 2: NativeRenderer 확장 (완료)

**위치**: `/home/user/webapp/CamViewerPOC/NativeRenderer/`

#### 추가된 C++ 함수:
```cpp
// renderer.h
void DrawLine(float x1, float y1, float x2, float y2, 
              float r, float g, float b, float lineWidth);

void DrawArc(float centerX, float centerY, float radius, 
             float startAngle, float endAngle, int clockwise, 
             float r, float g, float b, float lineWidth);

void DrawPoint(float x, float y, float size, 
               float r, float g, float b);
```

#### 구현 세부사항:
- **DrawLine**: OpenGL GL_LINES를 사용한 직선 그리기
- **DrawArc**: 64 segments로 부드러운 원호 렌더링
- **DrawPoint**: 피어싱 위치 및 파트 원점 표시
- **각도 처리**: 시계방향/반시계방향 원호 지원

---

### ✅ Phase 3: CamViewerControl 통합 (완료)

**위치**: `/home/user/webapp/CamViewerPOC/WinFormsApp/CamViewerControl.cs`

#### 추가된 C# P/Invoke:
```csharp
[DllImport("NativeRenderer.dll")]
public static extern void DrawLine(float x1, float y1, float x2, float y2, 
                                  float r, float g, float b, float lineWidth);

[DllImport("NativeRenderer.dll")]
public static extern void DrawArc(float centerX, float centerY, float radius, 
                                 float startAngle, float endAngle, int clockwise, 
                                 float r, float g, float b, float lineWidth);

[DllImport("NativeRenderer.dll")]
public static extern void DrawPoint(float x, float y, float size, 
                                   float r, float g, float b);
```

#### 추가된 메서드:
```csharp
// MPF 파일 로드
public void LoadMPFFile(string filePath)

// MPF 프로그램 표시
private void DisplayMPFProgram()

// 워크피스 경계선 그리기
private void DrawWorkpieceBoundary()

// 파트 그리기 (변환 적용)
private void DrawPart(MPF.Part part)

// 컨투어 그리기 (Lead-in, 절단 경로)
private void DrawContour(Contour contour, float offsetX, float offsetY)

// 경로 세그먼트 그리기 (Line/Arc)
private void DrawPathSegment(PathSegment segment, float offsetX, float offsetY, 
                            float r, float g, float b, float lineWidth)

// 자동 뷰 맞춤
private void AutoFitView()
```

#### 색상 구분:
- **흰색 (1.0, 1.0, 1.0)**: 워크피스 경계선
- **녹색 (0.0, 1.0, 0.0)**: 파트 원점 (8.0 픽셀)
- **빨강 (1.0, 0.0, 0.0)**: 피어싱 위치 (6.0 픽셀)
- **노랑 (1.0, 1.0, 0.0)**: Lead-in 경로 (1.5px 라인)
- **청록 (0.0, 1.0, 1.0)**: 절단 경로 (2.0px 라인)

---

### ✅ Phase 4: MainForm UI 추가 (완료)

**위치**: `/home/user/webapp/CamViewerPOC/WinFormsApp/MainForm.cs`

#### 추가된 버튼:
```csharp
Button btnLoadMPF = CreateButton("Load MPF File", 790, 10);
// 녹색 배경: Color.FromArgb(0, 204, 102)
```

#### OpenFileDialog 설정:
```csharp
Filter: "MPF Files (*.mpf;*.txt;*.nc)|*.mpf;*.txt;*.nc|All Files (*.*)|*.*"
Title: "Select MPF File"
```

---

## 📂 프로젝트 구조

```
CamViewerPOC/
├── WinFormsApp/
│   ├── CamViewerControl.cs       (✅ MPF 로드/렌더링 추가)
│   ├── MainForm.cs                (✅ Load MPF 버튼 추가)
│   ├── DiagnosticHelper.cs        (기존)
│   ├── Program.cs                 (기존)
│   └── MPF/                       (✅ 신규 폴더)
│       ├── Point2D.cs            (✅ 2D 좌표)
│       ├── PathSegment.cs        (✅ Line/Arc 세그먼트)
│       ├── Commands.cs           (✅ HK/GCode 명령어)
│       ├── Contour.cs            (✅ 컨투어 정보)
│       ├── Part.cs               (✅ 파트/네스팅)
│       ├── MPFProgram.cs         (✅ 전체 프로그램)
│       └── MPFParser.cs          (✅ 파서 로직)
│
├── NativeRenderer/
│   ├── renderer.h                (✅ DrawLine, DrawArc, DrawPoint 추가)
│   └── renderer.cpp              (✅ MPF 그리기 함수 구현)
│
├── SampleMPF/                    (✅ 신규 폴더)
│   └── simple_test.mpf           (✅ 테스트용 샘플 파일)
│
└── CamViewerPOC.csproj           (✅ MPF 클래스 추가)
```

---

## 🚀 사용 방법

### 1. 빌드

#### Windows에서:
```batch
REM C++ DLL 빌드
build_native.bat

REM C# 애플리케이션 빌드
build_csharp.bat
```

#### 또는 Visual Studio:
1. CamViewerPOC.sln 열기
2. F5 또는 Ctrl+F5로 실행

### 2. MPF 파일 로드

1. 애플리케이션 실행
2. **"Load MPF File"** 버튼 클릭 (녹색)
3. MPF 파일 선택 (*.mpf, *.txt, *.nc)
4. 자동으로 파싱 및 시각화

### 3. 뷰어 조작

- **마우스 휠**: 확대/축소
- **마우스 오른쪽 드래그**: 팬 (이동)
- **마우스 가운데 드래그**: 팬 (이동)
- **Reset View 버튼**: 초기 뷰로 복귀

---

## 📊 MPF 파일 형식 예시

```
;!V16A05
N1
HKLDB(1,"MS010",1,0,0,0)
HKINI(2,400.,400.,0,0,0)

; Nesting (파트 배치)
N10000 HKOST(50.,50.,0.,10001,2,0,0,0)
HKPPP
N30000 HKEND(0,0,0)

; Part Definition
N10001 HKSTR(1,1,10.,10.,0,100.,100.,0)  ; 컨투어 시작
HKPIE(0,0,0)                             ; 피어싱
HKLEA(1,5.,10.,0,0,0,0,0)                ; Lead-in
HKCUT(0,0,0)                             ; 절단 시작
G1 X10. Y100.                             ; 직선
G1 X100. Y100.
G1 X100. Y10.
HKSTO(1,10.,10.,0,0,0,0,0)               ; 컨투어 종료
N10003 HKPED(0,0,0)                       ; 파트 종료

N10 M30
```

---

## 🔍 디버깅 팁

### 파서 로그 활성화:
```csharp
MPFParser parser = new MPFParser(enableDebug: true);
```

Console 출력:
```
[MPFParser] === MPF 파서 시작 ===
[MPFParser] 총 라인 수: 25
[MPFParser] HK 함수 파싱: HKLDB, 인자: 6개
[MPFParser] HK 함수 파싱: HKINI, 인자: 6개
[MPFParser] === 네스팅 정보 추출 시작 ===
[MPFParser]   HKOST #1: 블록=10000, 파트코드=10001
[MPFParser] === 네스팅 정보 추출 완료: 1개 ===
[MPFParser] 파트 10001 추출 중...
[MPFParser]   컨투어 시작: 블록 10001
[MPFParser]     HKCUT - 절단 시작
[MPFParser]   컨투어 완료: 1번째
[MPFParser] 파트 추출 완료: 1개 컨투어
```

### 일반적인 오류:

#### 1. "HKLDB command not found"
- MPF 파일에 HKLDB가 누락됨
- 해결: HKLDB 명령어 추가

#### 2. "블록 번호 XXXX를 찾을 수 없음"
- HKOST의 PartNumber가 실제 N 블록과 일치하지 않음
- 해결: N 블록 번호 확인

#### 3. 아무것도 표시되지 않음
- 워크피스 스케일 문제 (mm vs OpenGL units)
- 해결: `workpieceScale` 조정 (기본값: 0.001f)

---

## 🎓 코드 설명

### MPF 파싱 흐름:

```
1. ParseLine()
   ↓
2. 주석, N블록, HK함수, G-code 구분
   ↓
3. ParseHKCommand() / ParseGCode()
   ↓
4. commands 리스트에 추가
   ↓
5. BuildMPFProgram()
   ↓
6. ExtractNesting() → HKOST 찾기
   ↓
7. ExtractParts() → 각 파트 추출
   ↓
8. ExtractPart() → 컨투어 추출
   ↓
9. HKSTR ~ HKSTO 사이 경로 파싱
   ↓
10. CreateSegment() → Line/Arc 생성
```

### 좌표 변환:

```csharp
// MPF 좌표 (mm) → OpenGL 좌표
float glX = (float)(mpfX * workpieceScale) + offsetX;
float glY = (float)(mpfY * workpieceScale) + offsetY;

// 파트 원점 적용
offsetX = part.Origin.X * workpieceScale;
offsetY = part.Origin.Y * workpieceScale;
```

### 원호 계산:

```csharp
// I, J는 시작점으로부터의 오프셋
Point2D center = new Point2D(start.X + i, start.Y + j);
double radius = Math.Sqrt(i * i + j * j);
double startAngle = Math.Atan2(start.Y - center.Y, start.X - center.X);
double endAngle = Math.Atan2(end.Y - center.Y, end.X - center.X);

// G2 = 시계방향, G3 = 반시계방향
bool clockwise = (gCode == 2);
```

---

## 📚 참고 자료

### V20 MPF Viewer와의 비교:

| 기능 | V20 (기존) | CamViewerPOC (현재) |
|------|-----------|---------------------|
| 렌더링 엔진 | HKCAMInterface.dll (독점) | OpenGL (오픈소스) |
| 파서 | DLL 내부 | C# MPFParser 클래스 |
| 시뮬레이션 | Triple nested loop | ⏳ 향후 구현 |
| 스레딩 | Thread.Abort() | ⏳ Task 기반 예정 |
| WinCC 통합 | ❌ | ✅ UserControl |

### TypeScript 버전과의 차이:

- **언어**: TypeScript → C# 3.0 호환
- **타입**: `interface` → `class`
- **배열**: `Array<T>` → `List<T>`
- **옵셔널**: `?` → `Nullable<T>` 또는 `null` 체크
- **정규식**: 동일한 패턴 사용
- **Math**: `Math.atan2()` → `Math.Atan2()`

---

## 🛠️ 향후 개선 사항

### Phase 2 (계획):
1. ✅ MPF 파싱 및 표시
2. ⏳ 절단 시뮬레이션 (V20 스타일)
3. ⏳ Part/Contour 번호 표시
4. ⏳ 진행률 표시 바
5. ⏳ 파일 탐색 TreeView (V20 스타일)

### Phase 3 (계획):
1. ⏳ VBO 렌더링 (성능 최적화)
2. ⏳ 셰이더 지원
3. ⏳ 3D 뷰 (Z축)
4. ⏳ 슬라이스 시각화

---

## ✅ 체크리스트

### 구현 완료:
- [x] MPF 데이터 구조 설계
- [x] HK 명령어 파싱
- [x] G-code 파싱
- [x] 네스팅 정보 추출
- [x] 파트/컨투어 추출
- [x] Line/Arc 세그먼트 생성
- [x] OpenGL DrawLine 함수
- [x] OpenGL DrawArc 함수 (G2/G3)
- [x] OpenGL DrawPoint 함수
- [x] C# P/Invoke 통합
- [x] MPF 로드 UI
- [x] 자동 뷰 맞춤
- [x] 색상 구분 렌더링

### 테스트 필요:
- [ ] 실제 Windows 환경에서 빌드
- [ ] 실제 MPF 파일 로드 테스트
- [ ] WinCC Advanced v17 통합 테스트
- [ ] 대용량 MPF 파일 성능 테스트

---

## 📞 문의

기술적 질문이나 버그 리포트는 프로젝트 문서를 참고하세요.

**개발 날짜**: 2025-11-18  
**개발자**: Claude Code  
**버전**: 1.0.0 (MPF Parser 초기 구현)
