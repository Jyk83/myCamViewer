# HKCAMLib.dll 분석 결과

## 주요 구조

### 클래스: HKCAMViewControl (UserControl)
- **Implements**: ITagSink
- **Base**: UserControl

### 멤버 변수
```csharp
private ITag m_ITag;                    // ITag 인터페이스
private long m_RegisterCookie;          // 등록 쿠키
private bool m_Active;                  // 활성화 상태
private string m_Tagname;               // Tag 이름
private string m_Delimiter;             // 구분자
private TraceControl traceControl;      // UI 컨트롤
```

### 주요 메서드
1. **HKCAMViewControl_Load** - Load 이벤트
2. **HKCAMViewControl_Disposed** - Disposed 이벤트
3. **btnConnect_Click** - 연결 버튼
4. **btnDisconnect_Click** - 연결 해제 버튼
5. **ChangeTagServer** - ITag 서버 변경
6. **ReadTagCyclic** - 주기적 읽기
7. **Cancel** - 취소

### ITagSink 구현
1. **OnDataChanged** - 데이터 변경 콜백
2. **OnWriteComplete** - 쓰기 완료 콜백
3. **OnError** - 에러 콜백
4. **OnCanceled** - 취소 콜백
5. **OnRemoved** - 제거 콜백

### 이벤트
1. **eventStartReadTag** - 읽기 시작 이벤트
2. **eventStartWriteTag** - 쓰기 시작 이벤트  
3. **eventStartReadCyclic** - 주기적 읽기 시작 이벤트
4. **eventCancelReadCyclic** - 주기적 읽기 취소 이벤트

### 핵심 구현 방식
- **UserControl** 직접 구현 (C#)
- **ITagSink** 인터페이스 구현
- **Site.GetService** 사용 가능 (WinCC 내부)
- **CreateObject** 대체 사용 (독립 실행)
- **Register(Me)** - UserControl 자신을 콜백 전달

## 참조 어셈블리
- Siemens.Runtime.ControlDev
- Siemens.Runtime.ITag
- System.Windows.Forms
- System.Drawing

## 빌드 정보
- Framework: .NET Framework 4.8
- Platform: x86 (32-bit)
- Company: HP Inc.
- Copyright: HP Inc. 2023
