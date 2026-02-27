# 빌드 문제 해결 가이드

## 방금 발생한 문제 해결

### 증상
```
Files은(는) 예상되지 않았습니다.
```

### 원인
Windows 배치 스크립트에서 경로에 공백이 있을 때 따옴표 처리 문제

### 해결 방법

업데이트된 빌드 스크립트를 사용하세요:

---

## C# 빌드 방법 (3가지)

### 방법 1: Visual Studio로 빌드 (가장 쉬움) ⭐ 권장

1. **솔루션 파일 열기**:
   ```
   CamViewerPOC.sln 더블클릭
   ```

2. **빌드**:
   - 메뉴: Build → Build Solution (Ctrl+Shift+B)
   - 또는 상단 툴바에서 "Release" 선택 후 빌드

3. **실행**:
   ```
   WinFormsApp\bin\Release\CamViewerPOC.exe
   ```

### 방법 2: Developer Command Prompt 사용

1. **시작 메뉴**에서 검색:
   ```
   Developer Command Prompt for VS 2022
   ```
   또는
   ```
   Developer Command Prompt for VS 2019
   ```

2. **프로젝트 폴더로 이동**:
   ```cmd
   cd "D:\4. HKHMI\Project\WinCC\Genspark\CamViewerPOC"
   ```

3. **간단한 스크립트 실행**:
   ```cmd
   build_csharp_simple.bat
   ```

### 방법 3: 수동 빌드

1. **Developer Command Prompt** 열기

2. **명령 실행**:
   ```cmd
   cd "D:\4. HKHMI\Project\WinCC\Genspark\CamViewerPOC\WinFormsApp"
   msbuild CamViewerPOC.csproj /p:Configuration=Release /p:Platform="Any CPU" /t:Rebuild
   ```

---

## 전체 빌드 프로세스

### C++ DLL이 이미 빌드되었으므로:

C++ DLL은 이미 성공적으로 빌드되었습니다:
```
✅ NativeRenderer\build\bin\Release\NativeRenderer.dll
✅ WinFormsApp\bin\Release\NativeRenderer.dll (복사됨)
```

이제 C#만 빌드하면 됩니다!

---

## 빠른 테스트

### DLL 확인

```cmd
dir "WinFormsApp\bin\Release\NativeRenderer.dll"
```

출력 예:
```
2024-XX-XX  XX:XX            XX,XXX NativeRenderer.dll
```

### C# 빌드 후 확인

```cmd
dir "WinFormsApp\bin\Release\CamViewerPOC.exe"
```

### 실행 테스트

```cmd
cd WinFormsApp\bin\Release
CamViewerPOC.exe
```

---

## 자주 발생하는 문제

### 1. "MSBuild를 찾을 수 없습니다"

**해결책**:
- Visual Studio를 설치했는지 확인
- "Developer Command Prompt for VS" 사용
- 또는 Visual Studio IDE에서 직접 빌드

### 2. "NativeRenderer.dll을 찾을 수 없습니다"

**해결책**:
```cmd
copy "NativeRenderer\build\bin\Release\NativeRenderer.dll" "WinFormsApp\bin\Release\"
```

### 3. Platform 타겟 불일치

**증상**: BadImageFormatException

**해결책**:
- C++ DLL은 x64
- C# 프로젝트는 "Any CPU" 또는 "x64"로 빌드
- Visual Studio에서: Project → Properties → Build → Platform target

### 4. .NET Framework 버전 오류

**해결책**:
- .NET Framework 4.7.2 이상 설치 필요
- [다운로드](https://dotnet.microsoft.com/download/dotnet-framework)

---

## 현재 상태 확인

### 완료된 항목:
✅ C++ DLL 빌드 성공  
✅ DLL 복사 완료  

### 남은 항목:
⚠️ C# 애플리케이션 빌드

---

## 권장 해결 순서

### 가장 쉬운 방법 (1분):

1. **CamViewerPOC.sln** 파일을 더블클릭 (Visual Studio가 열림)

2. **상단 드롭다운**에서 "Release" 선택

3. **Ctrl+Shift+B** 눌러 빌드

4. **성공 메시지** 확인:
   ```
   ========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========
   ```

5. **실행**:
   ```
   WinFormsApp\bin\Release\CamViewerPOC.exe
   ```

---

## 대체 방법: Visual Studio Code 사용

Visual Studio가 없다면:

### 1. Visual Studio Build Tools 설치

다운로드: https://visualstudio.microsoft.com/downloads/

- "Build Tools for Visual Studio 2022" 선택
- ".NET desktop build tools" 체크
- 설치

### 2. 빌드

```cmd
"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" WinFormsApp\CamViewerPOC.csproj /p:Configuration=Release
```

---

## 지금 바로 실행하기

C++ DLL이 이미 빌드되었으므로, 다음 중 하나를 선택하세요:

### Option A: Visual Studio 사용 (가장 쉬움)
```
1. CamViewerPOC.sln 더블클릭
2. Ctrl+Shift+B (빌드)
3. WinFormsApp\bin\Release\CamViewerPOC.exe 실행
```

### Option B: Developer Command Prompt 사용
```
1. 시작 → "Developer Command Prompt for VS" 검색
2. cd "D:\4. HKHMI\Project\WinCC\Genspark\CamViewerPOC"
3. build_csharp_simple.bat 실행
```

### Option C: 수정된 스크립트 사용
```
build_csharp.bat 다시 실행 (수정됨)
```

---

## 도움이 필요하신가요?

여전히 문제가 있다면:

1. **Visual Studio 버전 확인**:
   ```cmd
   where msbuild
   ```

2. **현재 위치 확인**:
   ```cmd
   cd
   ```

3. **파일 목록 확인**:
   ```cmd
   dir
   ```

위 정보를 공유해주시면 추가 지원이 가능합니다!

---

**빠른 팁**: Visual Studio가 설치되어 있다면, CamViewerPOC.sln을 여는 것이 가장 빠르고 확실한 방법입니다!
