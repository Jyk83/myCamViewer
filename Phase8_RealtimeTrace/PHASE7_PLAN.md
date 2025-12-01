# Phase 7: Advanced Rendering & Real-time Trace System

## 목표 (Goals)
HKCamInterface C++ DLL의 실시간 트레이스 시스템을 C# WinForms로 완전 구현

## 주요 기능 (Core Features)

### 1. OpenGL 기반 텍스트 렌더링 (OpenGL Text Rendering)
**참조**: `OpenGLNumber.cpp`, `OpenGLNumber.h`

**구현 내용**:
- ~~`wglUseFontOutlines()` 기반 숫자 렌더링~~ → **GDI+ 유지 (성능 충분)**
- Display List를 사용한 고속 렌더링
- 파트/컨투어 번호 위치 자동 계산 (중심점 기준)
- 스케일에 따른 폰트 크기 자동 조정

**현재 상태**: GDI+ 텍스트 렌더링으로 충분히 작동 중. OpenGL은 추후 최적화 시 고려.

---

### 2. 실시간 트레이스 데이터 처리 (Real-time Trace Data Processing)
**참조**: `OpenGLCAMViewWnd.cpp` - `UpdateCuttingProgress()` 메서드들

**필요한 데이터**:
```cpp
// 3가지 UpdateCuttingProgress 오버로드
BOOL UpdateCuttingProgress(int nPart, int nContour, const char* currentBlock, 
                          double progress, double xwcs, double ywcs, bool isBlockInMpf);

BOOL UpdateCuttingProgress(int nPart, int nContour, int mpfLineNo, 
                          double progress, double xwcs, double ywcs);

BOOL UpdateCuttingProgress(int nPart, int nContour, int iBlock, 
                          double xwcs, double ywcs);
```

**데이터 항목**:
- `nPart`: 파트 번호 (1-based)
- `nContour`: 컨투어 번호 (1-based)
- `currentBlock`: 현재 실행 중인 G코드 블록 (예: "G1 X100 Y50")
- `progress`: 현재 엘리먼트의 진행률 (0.0 ~ 1.0)
- `xwcs`, `ywcs`: 현재 WCS (Workpiece Coordinate System) 좌표
- `mpfLineNo`: MPF 파일의 라인 번호
- `iBlock`: 엘리먼트 인덱스
- `isBlockInMpf`: G코드가 MPF에 있는지 여부

---

### 3. 절단 진행 상태 관리 (Cutting Progress State Management)
**참조**: `OpenGLCAMViewWnd.cpp` - 멤버 변수들

**상태 변수**:
```cpp
int m_iStartPart, m_iStartContour;    // 시작 위치
int m_iLastPart, m_iLastContour;      // 마지막 완료 위치
int m_iCurrPart, m_iCurrContour;      // 현재 절단 위치
int m_iCurrElement;                    // 현재 엘리먼트 인덱스
double m_elemProgress;                 // 엘리먼트 진행률
double m_cutDistance;                  // 총 절단 거리
bool m_bUnderCuttingProgress;          // 절단 진행 중
bool m_bHasCuttingProgress;            // 절단 데이터 존재
```

**주요 메서드**:
- `StartCuttingProgress()`: 절단 시작
- `UpdateCuttingProgress()`: 실시간 위치 업데이트
- `StopCuttingProgress()`: 절단 중지
- `ResetCuttingProgress()`: 진행 상태 초기화
- `CompleteLastElement()`: 마지막 엘리먼트 완료 처리
- `CompleteLastContour()`: 마지막 컨투어 완료 처리

---

### 4. MPF 파일 경로 및 데이터 소스 관리

**현재 구현**:
- FileExplorerControl을 통한 파일 선택
- MPFParser를 통한 파일 파싱
- 메모리에 MPFProgram 데이터 보관

**추가 필요**:
- 실제 NC 제어기와의 통신 인터페이스 (향후)
- 현재는 **테스트용 데이터 입력 UI** 구현

---

### 5. 테스트 인터페이스 구현 (Test Data Input Interface)

**목적**: 실제 NC 통신 없이 트레이스 시스템 테스트

**UI 요소**:
```
┌─────────────────────────────────────────┐
│  실시간 트레이스 테스트 (Trace Test)    │
├─────────────────────────────────────────┤
│ MPF 파일: [현재 로드된 파일 표시]       │
├─────────────────────────────────────────┤
│ 파트 번호 (Part):     [1]  ▲▼          │
│ 컨투어 번호 (Contour): [1]  ▲▼          │
│ 엘리먼트 번호 (Element): [0]  ▲▼        │
│ 진행률 (Progress):    [0.0] (0-1)       │
├─────────────────────────────────────────┤
│ WCS 좌표:                                │
│   X: [0.00] mm                           │
│   Y: [0.00] mm                           │
├─────────────────────────────────────────┤
│ G코드 블록: [G1 X100 Y50 F3000]         │
│ MPF 라인 번호: [150]                     │
├─────────────────────────────────────────┤
│ [시작 (Start)]  [업데이트 (Update)]     │
│ [중지 (Stop)]   [리셋 (Reset)]          │
│ [완료 (Complete Last)]                   │
└─────────────────────────────────────────┘
```

**기능**:
- 현재 로드된 MPF 파일 정보 표시
- 파트/컨투어/엘리먼트 번호 입력 (NumericUpDown)
- 진행률 슬라이더 (0.0 ~ 1.0)
- WCS 좌표 입력
- G코드 블록 입력 (TextBox)
- 버튼:
  - **시작**: `StartCuttingProgress()` 호출
  - **업데이트**: `UpdateCuttingProgress()` 호출
  - **중지**: `StopCuttingProgress()` 호출
  - **리셋**: `ResetCuttingProgress()` 호출
  - **완료**: `CompleteLastElement()` 호출

---

## 구현 우선순위

### Priority 1: 트레이스 데이터 처리 강화
- [x] 리드인 색상 수정 완료 (Phase 6)
- [ ] `CuttingProgressManager` 클래스 생성
  - 절단 진행 상태 관리
  - 완료/진행중/대기 엘리먼트 추적
  - 절단 거리 계산
- [ ] `UpdateCuttingProgress()` 3가지 오버로드 구현
- [ ] 진행률에 따른 세그먼트 색상 동적 변경

### Priority 2: 테스트 인터페이스 구현
- [ ] `TraceTestForm.cs` 생성
- [ ] 데이터 입력 UI 구현
- [ ] MainForm과 통신 연결
- [ ] 실시간 업데이트 테스트

### Priority 3: 렌더링 최적화
- [ ] OpenGL Display List 활용
- [ ] 부분 렌더링 (변경된 영역만)
- [ ] 더블 버퍼링 개선

### Priority 4: 데이터 검증 및 로깅
- [ ] 입력 데이터 검증
- [ ] 진행 상태 로깅
- [ ] 에러 핸들링 강화

---

## HKCamInterface 참조 포인트

### 주요 참조 파일:
1. `OpenGLCAMViewWnd.cpp` (lines 579-753)
   - `StartCuttingProgress()`
   - `UpdateCuttingProgress()` 3가지 오버로드
   - 진행 상태 추적 로직

2. `OpenGLCAMViewWnd.cpp` (lines 1957-2300)
   - `UpdateProgress()` 내부 구현
   - 엘리먼트 매칭 로직
   - 진행률 계산

3. `OpenGLNumber.cpp`
   - OpenGL 텍스트 렌더링 (참조용)

4. `HKCAMInterfaceDLL.h`
   - 데이터 구조 정의
   - 에러 코드 정의
   - 콜백 함수 프로토타입

---

## 테스트 시나리오

### 시나리오 1: 단일 컨투어 트레이스
1. MPF 파일 로드
2. Part 1, Contour 1 선택
3. Start 버튼 클릭
4. 진행률 슬라이더를 0 → 1로 이동하면서 Update
5. 리드인 → 절단 경로가 순차적으로 색상 변경되는지 확인

### 시나리오 2: 다중 컨투어 이동
1. Part 1, Contour 1 완료
2. Part 1, Contour 2로 이동
3. 이전 컨투어는 완료 색상, 현재 컨투어는 진행 색상 확인

### 시나리오 3: G코드 블록 추적
1. G코드 텍스트 입력 (예: "G1 X100 Y50 F3000")
2. 해당 G코드에 해당하는 엘리먼트 하이라이트 확인

### 시나리오 4: WCS 좌표 추적
1. WCS 좌표 입력
2. 해당 좌표 근처의 엘리먼트 하이라이트
3. 진행률 자동 계산

---

## 다음 단계 (Next Phase - Phase 8)

Phase 7 완료 후:
- **Phase 8: iTAG 통신 연동**
  - 실제 NC 제어기와 TCP/IP 통신
  - 실시간 데이터 수신
  - 양방향 통신 (명령 전송)

---

## 참고 사항

- C++ DLL의 `wglUseFontOutlines()`는 Windows GDI 의존성이 있음
- C#에서는 GDI+를 사용한 텍스트 렌더링으로 충분
- OpenGL 텍스트는 성능 최적화가 필요할 때만 고려
- 현재 GDI+ 렌더링으로 60fps 이상 가능

---

**작성일**: 2025-11-21  
**작성자**: GenSpark AI Developer  
**Phase**: 7 (Advanced Rendering & Real-time Trace)
