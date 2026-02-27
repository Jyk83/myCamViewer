# Phase10: Realtime ITag Viewer (통합 UserControl DLL)

Phase8 실시간 뷰어 + Phase9 ITag 통신을 통합한 WinCC 임포트 가능 UserControl DLL

## 📋 프로젝트 개요

### 목적
- Phase8의 실시간 렌더링 기능과 Phase9의 ITag 통신 기능을 하나의 UserControl로 통합
- WinCC Graphics Designer에 임포트하여 사용 가능한 DLL 생성
- 14개 ITag를 실시간으로 읽어서 MPF 프로그램 진행 상황 시각화

### 주요 기능
1. ✅ **실시간 MPF 렌더링** (Phase8 기반)
2. ✅ **ITag 통신** (Phase9 기반, 14개 Tag 동시 읽기)
3. ✅ **프로그램 Info 패널** (300x630, 좌측)
   - ✅ **ITag 서버 상태 실시간 표시** (연결/주기읽기 상태)
   - ✅ 좌표 정보 (X, Y 통합 표시)
   - ✅ 진행/작업/실행 정보
4. ✅ **파트/컨투어 체크박스**
5. ✅ **시뮬레이션 모드**
6. ✅ **엘리먼트 선택**
7. ✅ **ITag 연결 테스트 팝업** (독립적 ITag 통신)

---

## 🏗️ 프로젝트 구조

```
Phase10_RealtimeITag/
├── RealtimeITagControl/          # UserControl 프로젝트
│   ├── RealtimeITagControl.cs    # ⭐ 메인 UserControl (IITagManager, ITagSink 구현)
│   ├── RealtimeITagControl.Designer.cs
│   ├── RealtimeITagControl.resx
│   ├── RealtimeITagControl.csproj
│   │
│   ├── IITagManager.cs           # ⭐ ITag 공유 인터페이스
│   ├── TagDefinitions.cs         # 14개 Tag 정의
│   │
│   ├── MPF/                      # MPF 파서 (Phase8)
│   │   ├── MPFParser.cs
│   │   └── MPFStructures.cs
│   │
│   ├── Rendering/                # 렌더링 (Phase8)
│   │   ├── RenderEngine.cs
│   │   ├── GeometryRenderer.cs
│   │   └── SelectionManager.cs
│   │
│   └── UI/                       # UI 컴포넌트
│       ├── ProgramInfoPanel.cs   # 프로그램 정보 패널
│       ├── ITagTestForm.cs       # ITag 테스트 팝업 (인터페이스 기반)
│       └── CoordinateDisplay.cs  # 좌표 표시
│
├── TestApp/                      # 테스트 애플리케이션
│   ├── Form1.cs
│   └── TestApp.csproj
│
├── Siemens.Runtime.ControlDev.dll  # ITag COM 라이브러리
├── RealtimeITag.sln
├── build.bat
└── README.md
```

### ⭐ 핵심: 인터페이스 기반 ITag 공유 구조

**문제점 (이전 구조)**:
- 각 클래스에서 독립적으로 ITag 인스턴스 생성
- WinCC에서는 단 하나의 ITag만 허용 → 충돌 발생
- 싱글톤 패턴도 WinCC Site 전달 문제 발생

**해결책 (인터페이스 기반)**:
```csharp
// IITagManager 인터페이스 정의
public interface IITagManager
{
    ITag ITag { get; }
    bool IsConnected { get; }
    bool IsCyclicReading { get; }
    string LastErrorMessage { get; }
    event EventHandler<TagDataEventArgs> DataChanged;
    event EventHandler<bool> ConnectionChanged;
}

// RealtimeITagControl이 IITagManager 구현 (ITag 소유자)
public partial class RealtimeITagControl : UserControl, IITagManager, ITagSink
{
    private ITag m_ITag;  // 메인 컨트롤이 ITag 소유
    
    // WinCC Site로부터 ITag 획득
    m_ITag = (ITag)this.Site.GetService(typeof(ITag));
}

// 팝업에 인터페이스로 전달
ITagTestForm popup = new ITagTestForm(this);  // this = IITagManager
popup.ShowDialog();
```

- ✅ RealtimeITagControl이 ITag를 명확히 소유
- ✅ 팝업은 인터페이스를 통해 ITag 공유
- ✅ WinCC Site 전달 문제 해결
- ✅ 테스트 용이성 향상

---

## 📊 레이아웃 설계

### 전체 크기: 1268 x 630

```
┌─────────────────────────────────────────────────────┐
│                 Realtime ITag Viewer                │
├───────────────┬─────────────────────────────────────┤
│  Program Info │        Realtime Viewer              │
│   (300x550)   │          (968x630)                  │
│               │                                     │
│  - 머신 좌표  │   - MPF 실시간 렌더링               │
│  - 워크 좌표  │   - 진행 방향 표시                  │
│  - 진행 거리  │   - 컨투어 선택                     │
│  - 파트/컨투어│   - 마우스 좌표                     │
│  - 작업 상태  │                                     │
│  - MPF 정보   │                                     │
│               │                                     │
│  [체크박스]   │                                     │
│  □ 파트 번호  │                                     │
│  □ 컨투어번호 │                                     │
│               │                                     │
│  [버튼]       │                                     │
│  [시뮬레이션] │                                     │
│  [엘리먼트선택]│                                     │
└───────────────┴─────────────────────────────────────┘
```

---

## 🏷️ ITag 정의 (14개)

| Tag 이름 | 설명 | 타입 | 용도 |
|---------|------|------|------|
| `HMI_VIEW_X_MCS` | X축 머신 좌표 | Real | 기계 좌표 표시 |
| `HMI_VIEW_Y_MCS` | Y축 머신 좌표 | Real | 기계 좌표 표시 |
| `HMI_VIEW_X_WCS` | X축 워크 좌표 | Real | 워크 좌표 표시 |
| `HMI_VIEW_Y_WCS` | Y축 워크 좌표 | Real | 워크 좌표 표시 |
| `HMI_VIEW_PROGRESS_DISTANCE` | 진행 거리 | Real | 현재 성분 진행 거리 |
| `HMI_VIEW_CURRENT_PART` | 현재 파트 | Int | 현재 실행 중인 파트 번호 |
| `HMI_VIEW_CURRENT_CONT` | 현재 컨투어 | Int | 현재 실행 중인 컨투어 번호 |
| `HMI_VIEW_WORK_DIR` | 작업 폴더명 | String | MPF 파일 경로 |
| `HMI_VIEW_WORK_MPF_NAME` | MPF 명칭 | String | 현재 실행 중인 MPF 파일명 |
| `HMI_VIEW_WORK_STATUS` | 작업 진행 상태 | Int | End:0, Start:1, Reset:2, FeedHold:3 |
| `HMI_VIEW_ACT_LINE_CODE` | 현재 실행 코드 | String | 실행 중인 G-Code 라인 |
| `HMI_VIEW_ACT_LINE_NUM` | 현재 실행 라인 번호 | Int | 프로그램 라인 번호 |
| `HMI_VIEW_SEARCH_PART` | 선택된 파트 | Int | 사용자 선택 파트 |
| `HMI_VIEW_SEARCH_CONT` | 선택된 컨투어 | Int | 사용자 선택 컨투어 |
| `HMI_VIEW_DIR_TYPE` | 장비 타입 | Int | S:1 (Small), L:2 (Large) |

---

## 🔄 동작 흐름

### 1. UserControl 초기화 (인터페이스 기반)
```
Load Event
  → Connect() 호출 (IITagManager 구현)
    - this.Site.GetService(typeof(ITag)) - WinCC에서 ITag 획득
    - 또는 COM ProgID로 생성 (독립 실행 시)
    - ITagSink 등록 (this)
  → StartCyclicRead(500) - 14개 Tag 주기적 읽기
  → MPF 파일 경로 모니터링
```

### 2. ITag 데이터 수신 (OnDataChanged)
```
OnDataChanged (ITagSink 콜백, RealtimeITagControl 구현)
  → Tag 값 파싱 (14개)
  → ProgramInfoPanel 업데이트 (좌표, 진행 정보 등)
  → MPF 파일 변경 감지 시 LoadMpfFile() 호출
  → ProcessTraceLogic(tagData) 호출
    - WorkStatus에 따라 Trace 시작/완료/일시정지
    - UpdateContourStatus() - 실시간 컨투어 상태 업데이트
  → UpdateViewer() - 렌더링 업데이트
  → DataChanged 이벤트 발생 (외부 구독자에게 전달)
```

### 3. 실시간 렌더링 (Trace 로직)
```
WorkStatus에 따른 처리:
  → WorkStatus = 1 (Start): 처음부터 Trace 시작
    - StartTracing() 호출
    - 모든 컨투어를 NotStarted 상태로 초기화
    - UpdateContourStatus()로 실시간 상태 업데이트
      · 현재 파트/컨투어 이전: Completed (초록색)
      · 현재 파트/컨투어: InProgress (노란색)
      · 그 외: NotStarted (회색)
  
  → WorkStatus = 0 (End): 작업 완료
    - CompleteTracing() 호출
    - 모든 컨투어를 Completed 상태로 변경
  
  → WorkStatus = 2,3 (Reset/FeedHold): 일시정지
    - PauseTracing() 호출
    - 현재 상태 유지
  
렌더링:
  → RenderMpfProgram()에서 전체 MPF 그리기
  → 각 컨투어를 상태별 색상으로 렌더링
  → 우하단에 상태 범례 표시
```

### 4. 사용자 상호작용
```
마우스 이동
  → 워크피스 기준 좌표 계산
  → 좌표 표시 업데이트

컨투어 클릭
  → HMI_VIEW_SEARCH_PART Tag에 쓰기
  → HMI_VIEW_SEARCH_CONT Tag에 쓰기
```

---

## 🛠️ 개발 단계

### Phase 1: 프로젝트 설정 ✅
- [x] Phase10 디렉토리 생성
- [x] UserControl 프로젝트 구조 생성
- [x] Phase8/Phase9 코드 복사
- [x] 네임스페이스 변경 (CamViewerPOC → RealtimeITagControl)

### Phase 2: ITag 통합 ✅
- [x] ITagManager 클래스 구현
- [x] 14개 Tag 정의 (TagDefinitions.cs)
- [x] ReadTagCyclic으로 실시간 읽기
- [x] Tag 값 파싱 및 처리 (OnDataChanged)

### Phase 3: UI 구성 ✅
- [x] 1268x630 레이아웃
- [x] 좌측 Program Info 패널 (300x630)
  - [x] **ITag 서버 상태 표시** (최상단)
    - 서버 연결 상태: ✅ 연결됨 / ❌ 연결 안됨
    - 주기 읽기 상태: 🔄 실행 중 / ⏸️ 중지됨
  - [x] 좌표 정보 (X, Y 통합 표시)
  - [x] 진행 정보 (거리, 파트, 컨투어)
  - [x] 작업 정보 (폴더, MPF 파일, 상태)
  - [x] 실행 정보 (코드, 라인)
- [x] 우측 Realtime Viewer (968x630)
- [x] 체크박스 (파트/컨투어)
- [x] 버튼 (시뮬레이션/엘리먼트선택/ITag연결테스트)

### Phase 4: 렌더링 통합 ✅
- [x] Phase8 MPF 파서 복사 (MPF/*.cs)
- [x] Phase8 렌더링 코드 복사 (Rendering/*.cs, Selection/*.cs)
- [x] **Trace 로직 구현** (WorkStatus 기반)
  - StartTracing: 컨투어 상태 초기화
  - CompleteTracing: 모든 컨투어 완료 처리
  - PauseTracing: 일시정지 처리
  - UpdateContourStatus: 실시간 상태 업데이트
- [x] **실시간 렌더링 엔진**
  - RenderMpfProgram: MPF 전체 렌더링
  - 상태별 색상 구분 (미시작: 회색, 진행중: 노란색, 완료: 초록색)
  - 자동 스케일 계산 및 좌표 변환
  - 우하단 상태 범례 표시
- [ ] 진행 방향 화살표 표시 (TODO)
- [ ] 컨투어 선택 시 Tag 쓰기 (TODO)

### Phase 5: 테스트 & 최적화
- [ ] TestApp 구현
- [x] 빌드 스크립트 (build.bat)
- [ ] WinCC 임포트 테스트
- [ ] 성능 최적화
- [ ] 문서화

---

## 🚀 빌드 및 실행

### 빌드
```bash
cd Phase10_RealtimeITag
build.bat
```

### WinCC 임포트
```
1. WinCC Graphics Designer 실행
2. 도구 상자 → 컨트롤 선택...
3. RealtimeITagControl.dll 추가
4. 화면에 드래그하여 배치
```

---

## 📝 주요 변경사항

### Phase8 대비
- ❌ 파일 로드 UI 제거 (ITag로 자동 로드)
- ❌ 시뮬레이션 컨트롤 단순화
- ✅ 프로그램 Info 패널 추가
- ✅ ITag 통신 통합

### Phase9 대비
- ✅ 14개 Tag 동시 읽기
- ✅ MPF 파일 자동 로드
- ✅ 실시간 렌더링 연동

---

## ⚠️ 주의사항

1. **플랫폼**: x86 (32-bit) 필수
2. **WinCC Runtime**: 필수
3. **ITag 서버**: WinCC Runtime 실행 중이어야 함
4. **MPF 파일 경로**: `HMI_VIEW_WORK_DIR` + `HMI_VIEW_WORK_MPF_NAME`

---

## 📚 참고 자료

- Phase8_RealtimeTrace: 실시간 렌더링 기반
- Phase9/ITagCommunication: ITag 통신 기반
- Siemens.Runtime.ControlDev.dll: ITag COM 라이브러리

---

## 🎯 다음 단계

1. UserControl 프로젝트 생성
2. Phase8/9 코드 통합
3. UI 레이아웃 구현
4. ITag 통신 연동
5. 테스트 및 최적화
