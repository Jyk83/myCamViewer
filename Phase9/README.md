# Phase 9: ITag Communication for WinCC

Siemens WinCC ITag 통신 프로그램 개발 프로젝트입니다.

## 📁 프로젝트 구조

```
Phase9/
├── ITagCommunication/           # ITag 통신 솔루션
│   ├── ITagCommunication.sln   # Visual Studio 솔루션
│   ├── README.md                # 상세 문서
│   │
│   ├── VBWrapper/               # VB.NET Wrapper DLL
│   │   ├── ITagManager.vb      # ITag 통신 관리자
│   │   ├── ITagConstants.vb    # 상수 정의
│   │   └── ITagWrapper.vbproj
│   │
│   ├── CSharpUserControl/       # C# UserControl (WinCC용)
│   │   ├── ITagTestControl.cs
│   │   └── ITagTestControl.csproj
│   │
│   └── TestApp/                 # 테스트 애플리케이션
│       ├── MainForm.cs
│       └── TestApp.csproj
│
├── Reference/                   # 참조 파일
│   ├── Siemens.Runtime.ControlDev.dll
│   ├── 109760182_ActiveXWCCPenUS.pdf
│   └── Samples/                 # Siemens 샘플 코드
│
└── Docs/                        # 문서
```

## 🎯 프로젝트 목표

C#에서 Siemens WinCC의 ITag 통신을 수행하기 위한 프로그램 개발

### 주요 요구사항
1. ✅ C#에서 ITag 직접 사용 가능 여부 확인
2. ✅ 필요 시 VB.NET 래퍼 DLL 구현
3. ✅ C# UserControl 형태로 WinCC 임포트 가능하도록 구현
4. ✅ Tag 이름 입력하여 Read/Write 테스트 가능

## 🔍 분석 결과

### C# 직접 사용 불가 확인
- `Siemens.Runtime.ControlDev.dll`은 VB.NET에 최적화된 COM 인터페이스
- C#에서 직접 사용 시 COM Interop 문제 발생 가능
- **결론**: VB.NET Wrapper 방식 채택 (안정성 및 호환성 보장)

### 아키텍처 설계
```
┌─────────────────────────────────────────────┐
│           WinCC Graphics Designer           │
│  (ITagTestControl.dll을 도구 상자에 추가)   │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│    C# UserControl (ITagTestControl.dll)     │
│  • UI (연결/읽기/쓰기 버튼)                 │
│  • 로그 표시                                 │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│    VB.NET Wrapper (ITagWrapper.dll)         │
│  • ITag 서버 연결                            │
│  • Tag 읽기/쓰기 관리                        │
│  • 에러 처리                                 │
└────────────────┬────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────┐
│  Siemens.Runtime.ControlDev.dll (COM)      │
│  • ITag 인터페이스                           │
│  • WinCC Runtime과 통신                     │
└─────────────────────────────────────────────┘
```

## 🚀 빌드 및 실행

### 1. 환경 요구사항
- **Visual Studio 2017 이상** (VB.NET 지원 필요)
- **.NET Framework 4.7.2**
- **Siemens WinCC** (실제 ITag 통신용)
- **Windows OS** (32비트/x86 플랫폼)

### 2. 빌드 순서
```bash
# Visual Studio에서 ITagCommunication.sln 열기

# 1. VBWrapper 빌드 (먼저!)
   → 솔루션 탐색기에서 ITagWrapper 프로젝트 우클릭
   → 빌드
   → bin/Debug/ITagWrapper.dll 생성 확인

# 2. CSharpUserControl 빌드
   → ITagTestControl 프로젝트 빌드
   → bin/Debug/ITagTestControl.dll 생성 확인

# 3. TestApp 빌드 및 실행
   → TestApp을 시작 프로젝트로 설정
   → F5 (디버깅 시작) 또는 Ctrl+F5 (디버깅 없이 시작)
```

### 3. 실행 및 테스트

#### 독립 실행 모드 (TestApp)
1. `TestApp.exe` 실행
2. "연결" 버튼 클릭 (WinCC Runtime 실행 중이어야 함)
3. Tag 이름 입력 (예: `TestTag1`)
4. 값 입력 후 "쓰기" 또는 "읽기" 버튼 클릭
5. 로그 창에서 결과 확인

#### WinCC 환경에서 사용
1. WinCC Graphics Designer 실행
2. 도구 상자 → 우클릭 → "컨트롤 선택..."
3. `.NET Framework 구성 요소` 탭
4. "찾아보기" → `ITagTestControl.dll` 선택
5. 도구 상자에 "ITagTestControl" 추가됨
6. 화면에 드래그하여 배치

## 📝 주요 기능

### ITagManager (VB.NET Wrapper)
```vb
' 연결
Dim manager As New ITagManager()
manager.Connect()

' Tag 읽기
Dim value As Object = manager.ReadTag("TestTag1")

' Tag 쓰기
manager.WriteTag("TestTag1", 100)

' 연결 해제
manager.Disconnect()
```

### ITagTestControl (C# UserControl)
- ✅ ITag 서버 연결/해제
- ✅ Tag 이름 입력
- ✅ Tag 값 읽기/쓰기
- ✅ 자동 타입 변환 (int, double, bool, string)
- ✅ 실시간 로그 표시
- ✅ 연결 상태 표시

## 🔧 문제 해결

### "ITag 서버에 연결할 수 없습니다" 오류
```
원인: WinCC Runtime이 실행되지 않음
해결: 
1. WinCC Runtime 시작
2. Siemens.Runtime.ControlDev.dll이 등록되어 있는지 확인
3. 32비트(x86) 플랫폼으로 빌드했는지 확인
```

### UserControl이 WinCC에서 보이지 않음
```
원인: 플랫폼 불일치 또는 .NET Framework 버전 문제
해결:
1. ITagTestControl.csproj → 속성 → 플랫폼: x86 확인
2. .NET Framework 4.7.2 버전 확인
3. DLL을 관리자 권한으로 등록
```

### Tag 읽기/쓰기 실패
```
원인: Tag 이름 오류 또는 권한 문제
해결:
1. WinCC Tag Management에서 Tag 이름 정확히 확인
2. Tag 읽기/쓰기 권한 확인
3. 로그 창에서 상세 에러 메시지 확인
```

## 📚 참고 자료

### 레퍼런스 파일
- **Siemens.Runtime.ControlDev.dll**: ITag COM 인터페이스
- **109760182_ActiveXWCCPenUS.pdf**: WinCC Control Development 공식 가이드
- **Samples/VB100/CCITagTest**: Siemens 제공 VB.NET 샘플 코드

### 주요 인터페이스
```vb
' ITag 인터페이스 주요 메서드
ITag.Register(sink)              ' 콜백 등록
ITag.Unregister(cookie)          ' 등록 해제
ITag.ReadTag(cookie, tagName)    ' 동기 읽기
ITag.WriteTag(cookie, tagName, value)  ' 동기 쓰기
ITag.ReadTagAsync(...)           ' 비동기 읽기
ITag.WriteTagAsync(...)          ' 비동기 쓰기
```

## 🎉 완료된 작업

- [x] Siemens 샘플 코드 분석
- [x] C# 직접 사용 가능 여부 확인 → **불가능**
- [x] VB.NET Wrapper DLL 설계 및 구현
- [x] C# UserControl 구현
- [x] Test Application 구현
- [x] 프로젝트 문서 작성
- [x] Git 커밋 및 푸시

## 📋 다음 단계

1. **Visual Studio 2019에서 빌드**
   - 모든 프로젝트 빌드 성공 확인
   - 경고 및 에러 해결

2. **WinCC 환경에서 테스트**
   - ITagTestControl을 WinCC에 임포트
   - 실제 Tag와 연동 테스트
   - Read/Write 동작 검증

3. **성능 및 안정성 테스트**
   - 다수의 Tag 동시 처리
   - 장시간 실행 안정성 확인
   - 에러 처리 검증

## 📞 지원

문제가 발생하거나 질문이 있으면:
1. `README.md` 문제 해결 섹션 확인
2. 로그 창의 상세 에러 메시지 확인
3. Siemens WinCC 문서 참조

---

**프로젝트**: CamViewer Phase9  
**날짜**: 2025-01-28  
**버전**: v1.0.0  
**커밋**: `4d00830` (feat: ITag 통신 프로그램 구현)
