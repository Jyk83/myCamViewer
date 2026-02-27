# ITag Communication for WinCC (C# Direct Implementation)

Siemens WinCC ITag 통신을 위한 C# UserControl 프로젝트입니다.  
**HKCAMLib.dll 분석 결과를 반영하여 C#에서 직접 ITag를 구현했습니다.**

## 프로젝트 구조

```
ITagCommunication/
├── CSharpUserControl/             # C# UserControl (WinCC Import용)
│   ├── ITagTestControl.cs        # UserControl 구현 (ITagSink 포함)
│   ├── ITagTestControl.Designer.cs
│   ├── ITagTestControl.resx
│   └── ITagTestControl.csproj    # C# 프로젝트 파일
│
├── Siemens.Runtime.ControlDev.dll # ITag COM Interop 라이브러리
├── HKCAMLib_Analysis.md           # HKCAMLib.dll 역공학 분석 결과
├── ITagCommunication.sln          # Visual Studio 솔루션 파일
├── build.bat                      # 빌드 스크립트
└── README.md                      # 이 파일
```

## 아키텍처

### C# UserControl (ITagTestControl.dll)
- **목적**: WinCC에 임포트 가능한 UI 컨트롤
- **구현**: C#에서 직접 ITag 및 ITagSink 인터페이스 구현
- **방식**: HKCAMLib.dll 분석 결과 적용
- **기능**:
  - ITag 서버 자동 연결 (Load 이벤트)
  - Site.GetService 우선 사용 (WinCC 내부)
  - CreateObject (Type.GetTypeFromProgID) 대체 (독립 실행)
  - ITagSink 콜백 메서드 구현
  - Tag 읽기/쓰기 (동기)
  - 실시간 로그 표시
  - 연결 상태 표시

### ⚠️ VB.NET Wrapper 제거됨
**이전 버전**에서는 VB.NET Wrapper (ITagWrapper.dll)를 사용했지만, HKCAMLib.dll 분석 결과 **C#에서 직접 ITag를 사용할 수 있음**이 확인되어 제거했습니다.

## 빌드 방법

### 필수 요구사항
- Visual Studio 2017 이상
- .NET Framework 4.7.2
- **Siemens.Runtime.ControlDev.dll** (프로젝트에 포함됨)
  - 빌드 시: `ITagCommunication/Siemens.Runtime.ControlDev.dll` 참조
  - 실행 시: WinCC Runtime이 설치되어 있어야 ITag 통신 가능

### 빌드 순서

#### 방법 1: build.bat 사용
```batch
build.bat
```

#### 방법 2: Visual Studio 사용
```
1. Visual Studio에서 ITagCommunication.sln 열기
2. 솔루션 정리 (Clean Solution)
3. 솔루션 다시 빌드 (Rebuild Solution, Ctrl+Shift+B)
4. CSharpUserControl/bin/Debug/ITagTestControl.dll 생성 확인
```

## 사용 방법

### WinCC에 임포트
```
1. WinCC Graphics Designer 실행
2. 도구 상자에서 "컨트롤 선택..." 메뉴
3. "ITagTestControl.dll" 찾아서 추가
4. 도구 상자에 "ITagTestControl" 컨트롤 추가됨
5. 화면에 드래그하여 배치
6. Load 이벤트에서 자동으로 ITag 서버 연결 시도
```

### 자동 연결 방식 (HKCAMLib 스타일)

UserControl이 로드되면 자동으로 다음 순서로 ITag 서버 연결을 시도합니다:

```csharp
// 1순위: Site.GetService (WinCC 내부)
if (Site != null)
{
    m_ITag = (ITag)Site.GetService(typeof(ITag));
}

// 2순위: COM ProgID로 생성 (독립 실행)
if (m_ITag == null)
{
    Type itagType = Type.GetTypeFromProgID("CCITagControl.ITagControl.1");
    m_ITag = (ITag)Activator.CreateInstance(itagType);
}

// ITagSink 콜백 등록
m_RegisterCookie = m_ITag.Register(this);
```

## C#에서 직접 ITag 사용

### HKCAMLib.dll 분석 결과

HP Inc.에서 개발한 **HKCAMLib.dll**을 역공학 분석한 결과:
- ✅ C# UserControl이 직접 ITagSink 구현 가능
- ✅ Site.GetService를 통한 안정적인 WinCC 통합
- ✅ Type.GetTypeFromProgID + Activator로 COM 객체 생성
- ✅ VB.NET Wrapper 불필요

### 장점
- **단순한 구조**: C# 프로젝트만으로 완성
- **WinCC 통합 강화**: Site.GetService로 직접 연동
- **안정성 향상**: HKCAMLib 검증된 방식 적용
- **유지보수 용이**: 단일 언어 (C#)로 구현

### 이전 구조 vs 현재 구조

| 특성 | 이전 (VB Wrapper) | 현재 (C# Direct) |
|------|------------------|-----------------|
| **프로젝트 수** | 3개 (VB + C# + TestApp) | 1개 (C#만) |
| **ITagSink** | VB에서 구현 | C#에서 직접 구현 |
| **Site.GetService** | ❌ 불가능 | ✅ 가능 |
| **복잡도** | 높음 | 낮음 |
| **유지보수** | 어려움 | 쉬움 |

## API 사용 예제

### ITagSink 인터페이스 구현

```csharp
public partial class ITagTestControl : UserControl, ITagSink
{
    private ITag m_ITag;
    private long m_RegisterCookie;

    // ITagSink 구현
    public void OnDataChanged(int RegisterCookie, object TagNames, 
        object Values, object Qualities, object VarStates, 
        object TimeStamps, object Cookies)
    {
        // Tag 데이터 변경 시 호출
    }

    public void OnWriteComplete(int RegisterCookie, 
        object TagNames, object Cookies)
    {
        // 비동기 쓰기 완료 시 호출
    }

    public void OnError(int RegisterCookie, object TagNames, 
        object Cookies, object Errors)
    {
        // 에러 발생 시 호출
    }

    public void OnCanceled(int RegisterCookie)
    {
        // 주기적 읽기 취소 시 호출
    }

    public void OnRemoved(int RegisterCookie, object Cookies)
    {
        // Tag 제거 시 호출
    }
}
```

### Tag 읽기/쓰기

```csharp
// 1. Tag 단일 읽기 (동기)
object values = m_ITag.ReadTag(m_RegisterCookie, "TestTag1");
if (values is Array arr && arr.Length > 0)
{
    object value = arr.GetValue(0);
    Console.WriteLine($"Value: {value}");
}

// 2. Tag 쓰기 (동기)
m_ITag.WriteTag(m_RegisterCookie, "TestTag1", 100);

// 3. Tag 주기적 읽기 (값 변경 시 자동 업데이트)
// ⚠️ 중요: 모든 파라미터를 배열로 전달
string[] tagNames = new string[] { "TestTag1", "TestTag2", "TestTag3" };
int[] cycles = new int[] { 500, 500, 500 };     // 각 Tag의 주기 (ms)
int[] cookies = new int[] { 1, 2, 3 };          // 각 Tag의 쿠키
object serverCookie = null;

m_ITag.ReadTagCyclic(
    (int)m_RegisterCookie,      // 등록 쿠키 (checked 제거)
    (object)tagNames,           // Tag 이름 배열
    (object)cycles,             // 주기 배열
    (object)cookies,            // 쿠키 배열
    out serverCookie            // 서버 쿠키 (out 파라미터)
);

// serverCookie 처리 (배열일 수 있음)
if (serverCookie != null)
{
    if (serverCookie is Array arr && arr.Length > 0)
    {
        long cookie = Convert.ToInt64(arr.GetValue(0));
    }
}

// 4. 주기적 읽기 중지
m_ITag.Cancel(m_RegisterCookie);
```

### ITagSink 콜백 처리

주기적 읽기 시작 후 값이 변경되면 자동으로 `OnDataChanged` 콜백이 호출됩니다:

```csharp
public void OnDataChanged(int RegisterCookie, object TagNames, 
    object Values, object Qualities, object VarStates, 
    object TimeStamps, object Cookies)
{
    if (TagNames is Array tagArr && Values is Array valArr)
    {
        for (int i = 0; i < tagArr.Length; i++)
        {
            string tagName = tagArr.GetValue(i)?.ToString();
            object value = valArr.GetValue(i);
            
            // UI 업데이트
            txtValue.Text = value?.ToString() ?? "";
            LogMessage($"[OnDataChanged] {tagName} = {value}");
        }
    }
}
```

## 주요 기능

### ITagTestControl (C#)

#### 연결 관리
- **자동 연결**: Load 이벤트에서 ITag 서버 자동 연결
- **수동 연결**: 'Connect' 버튼으로 수동 연결
- **연결 해제**: '연결 해제' 버튼으로 안전한 연결 종료
- **Site.GetService**: WinCC 내부에서 안정적 연결 (우선순위 1)
- **CreateObject 대체**: 독립 실행 시 COM 객체 생성 (우선순위 2)

#### Tag 작업
- **단일 읽기**: 'Read' 버튼으로 즉시 Tag 값 읽기
- **단일 쓰기**: 'Write' 버튼으로 Tag 값 쓰기
- **주기적 읽기 (동기화)**: **값이 변경될 때마다 자동으로 읽어오는 기능**
  - '시작' 버튼으로 주기적 읽기 시작
  - '중지' 버튼으로 주기적 읽기 중지
  - 설정한 주기(ms)마다 Tag 값 모니터링
  - **ITagSink.OnDataChanged**로 값 변경 시 자동 업데이트

#### ITagSink 콜백
- **OnDataChanged**: Tag 값 변경 시 자동 호출 (주기적 읽기, 비동기 읽기)
- **OnWriteComplete**: 비동기 쓰기 완료 시 호출
- **OnError**: 에러 발생 시 호출
- **OnCanceled**: 주기적 읽기 취소 시 호출
- **OnRemoved**: Tag 제거 시 호출

#### UI 기능
- **실시간 로그**: UI에 작업 내역 표시 (타임스탬프 포함)
- **연결 상태**: 실시간 연결 상태 표시 (연결됨/연결 안됨)
- **주기적 읽기 상태**: 실행 중/중지됨 표시

## 문제 해결

### "ITag 서버에 연결할 수 없습니다" 오류
- WinCC Runtime이 실행 중인지 확인
- `Siemens.Runtime.ControlDev.dll`이 등록되어 있는지 확인
- 32비트(x86) 플랫폼으로 빌드했는지 확인

### UserControl이 WinCC에서 표시되지 않음
- ITagTestControl.dll이 32비트로 빌드되었는지 확인
- .NET Framework 버전이 WinCC와 호환되는지 확인 (4.7.2 권장)
- Siemens.Runtime.ControlDev.dll이 함께 배포되었는지 확인

### Tag 읽기/쓰기 실패
- ITag 서버 연결 상태 확인
- Tag 이름이 정확한지 확인
- Tag가 WinCC 프로젝트에 정의되어 있는지 확인
- 로그 창에서 상세 오류 메시지 확인

### ⚠️ ReadTagCyclic 오류: "System.Int32[] 형식 개체를 System.IConvertible 형식으로 캐스팅할 수 없습니다"
**문제**: `ReadTagCyclic` 호출 시 `serverCookie`가 배열로 반환되는데, 단일 값으로 변환하려고 해서 발생

**해결 방법**:
```csharp
// ❌ 잘못된 방법
int cookie = 1;
m_ITag.ReadTagCyclic(
    m_RegisterCookie, 
    "TestTag1", 
    500, 
    cookie,                    // 단일 값
    out object serverCookie
);
cyclicServerCookie = Convert.ToInt64(serverCookie);  // 오류!

// ✅ 올바른 방법
object cookie = new int[] { 1 };  // int 배열로 전달
m_ITag.ReadTagCyclic(
    checked((int)m_RegisterCookie), 
    (object)"TestTag1",       // object로 캐스팅
    (object)500,              // object로 캐스팅
    cookie,                   // int 배열
    out object serverCookie
);

// serverCookie 배열 처리
if (serverCookie is Array arr && arr.Length > 0)
{
    cyclicServerCookie = Convert.ToInt64(arr.GetValue(0));
}
```

## 레퍼런스

- `Siemens.Runtime.ControlDev.dll`: ITag COM Interop 라이브러리 (프로젝트에 포함)
- `HKCAMLib_Analysis.md`: HKCAMLib.dll 역공학 분석 결과
- `../Reference/HKCAMLib.dll`: HP Inc. 원본 참고 DLL
- `../Reference/109760182_ActiveXWCCPenUS.pdf`: WinCC Control Development 가이드
- `../Reference/Samples/VB100/CCITagTest/`: Siemens 제공 VB.NET 샘플 코드

## 주요 변경 사항

### v2.0.0 (2025-01-28): C# 직접 구현으로 전환
- ❌ **VB.NET Wrapper 제거** (ITagWrapper.dll)
- ❌ **TestApp 제거** (독립 테스트 앱)
- ✅ **C# 직접 구현** (HKCAMLib.dll 방식)
- ✅ **Site.GetService 지원** (WinCC 통합 강화)
- ✅ **ITagSink 구현** (C#에서 직접)
- ✅ **구조 단순화** (단일 프로젝트)

**장점**:
- VB.NET 지식 불필요
- 빌드 및 배포 간소화
- WinCC 통합 향상
- 유지보수 용이

### v1.0.0 (2025-01-28): 초기 릴리스 (VB Wrapper 방식)
- VB.NET Wrapper 구현 (ITagWrapper.dll)
- C# UserControl 구현 (ITagTestControl.dll)
- 테스트 애플리케이션 구현 (TestApp.exe)

## 라이선스

Copyright © 2025 Genspark CamViewer

## 버전 히스토리

- **v2.0.0** (2025-01-28): C# 직접 구현으로 전환 (HKCAMLib 방식)
  - VB.NET Wrapper 제거
  - C#에서 직접 ITag 및 ITagSink 구현
  - Site.GetService 지원 추가
  - 구조 단순화

- **v1.0.0** (2025-01-28): 초기 릴리스 (VB Wrapper 방식)
  - VB.NET Wrapper 구현
  - C# UserControl 구현
  - 테스트 애플리케이션 구현
