# NativeRenderer.dll 빌드 빠른 가이드

## ⚠️ 중요: 이 DLL을 빌드해야 파트/컨투어 번호가 표시됩니다!

현재 오류:
```
DLL 'NativeRenderer.dll'에서 이름이 'SwapBuffersNow'인 진입점을 찾을 수 없습니다.
```

이유: 기존 DLL에는 `SwapBuffersNow()` 함수가 없습니다. 새로 추가된 함수이므로 DLL을 다시 빌드해야 합니다.

---

## 빌드 방법 (Visual Studio 2022/2019)

### Option 1: Visual Studio 내에서 빌드 (권장)

1. **Phase4_Graphics.sln 열기**
   ```
   Phase4_Graphics/Phase4_Graphics.sln
   ```

2. **Solution Explorer에서 NativeRenderer 프로젝트 찾기**
   - Solution Explorer 패널에서 `NativeRenderer` 항목 찾기

3. **NativeRenderer 프로젝트 빌드**
   - `NativeRenderer` 우클릭
   - **"Build"** (빌드) 선택
   - 또는 **"Rebuild"** (다시 빌드) 선택

4. **빌드 출력 확인**
   - Output 창에서 빌드 성공 메시지 확인
   - `NativeRenderer.dll` 생성 위치 확인

5. **DLL 복사** (자동으로 안 된 경우)
   ```
   Phase4_Graphics/NativeRenderer/x64/Release/NativeRenderer.dll
   →
   Phase4_Graphics/WinFormsApp/bin/x64/Release/NativeRenderer.dll
   ```

6. **WinFormsApp 다시 실행**
   - F5 또는 실행 버튼 클릭

---

### Option 2: Developer Command Prompt 사용

1. **Developer Command Prompt for VS 2022 열기**
   - 시작 메뉴에서 "Developer Command Prompt" 검색

2. **프로젝트 디렉토리로 이동**
   ```cmd
   cd C:\...\Phase4_Graphics
   ```

3. **NativeRenderer만 빌드**
   ```cmd
   msbuild NativeRenderer/NativeRenderer.vcxproj /p:Configuration=Release /p:Platform=x64
   ```

4. **DLL 복사**
   ```cmd
   copy NativeRenderer\x64\Release\NativeRenderer.dll WinFormsApp\bin\x64\Release\
   ```

---

### Option 3: 전체 솔루션 리빌드

1. **Visual Studio에서 Phase4_Graphics.sln 열기**

2. **Build 메뉴 → Rebuild Solution**

3. **Platform이 x64인지 확인**
   - Configuration Manager에서 확인

4. **F5로 실행**

---

## 빌드 후 확인사항

### ✅ 성공 시:
- Output 창에 "Build succeeded" 메시지
- `NativeRenderer.dll` 파일 생성됨
- WinFormsApp 실행 시 오류 없음
- 파트/컨투어 번호 버튼이 작동함

### ❌ 실패 시 확인사항:

#### 1. "Cannot open include file 'windows.h'"
**해결**: Windows SDK 설치 필요
- Visual Studio Installer 실행
- "Modify" 클릭
- "Windows SDK" 체크 후 설치

#### 2. "Cannot find opengl32.lib"
**해결**: 일반적으로 Windows SDK와 함께 설치됨
- 프로젝트 속성 → Linker → Input → Additional Dependencies 확인

#### 3. Platform Mismatch (x86 vs x64)
**해결**: Platform을 x64로 변경
- Configuration Manager
- Active solution platform → x64

---

## 빌드 없이 임시 테스트 (번호 표시 제외)

번호 표시 기능을 제외하고 다른 기능만 테스트하려면:

1. **MainForm.cs에서 번호 버튼 비활성화**
   - 버튼들을 `Enabled = false`로 설정

2. **또는 CamViewerControl.cs에서 SwapBuffersNow() 호출 주석 처리**
   ```csharp
   // NativeRenderer.SwapBuffersNow(); // DLL 업데이트 전까지 주석
   ```

---

## 문제 해결

### Q: 빌드는 성공했는데 여전히 에러가 난다?
**A**: DLL이 올바른 위치에 복사되지 않았을 수 있습니다.
```cmd
# DLL 위치 확인
dir /s NativeRenderer.dll

# 수동 복사
copy NativeRenderer\x64\Release\NativeRenderer.dll WinFormsApp\bin\x64\Debug\
copy NativeRenderer\x64\Release\NativeRenderer.dll WinFormsApp\bin\x64\Release\
```

### Q: 어떤 Configuration을 사용해야 하나?
**A**: 
- 개발 중: **Debug** + **x64**
- 최종 배포: **Release** + **x64**

### Q: CMake로 빌드하고 싶다?
**A**: 
```cmd
cd Phase4_Graphics\NativeRenderer\build
cmake ..
cmake --build . --config Release
copy Release\NativeRenderer.dll ..\..\WinFormsApp\bin\x64\Release\
```

---

## 새로 추가된 함수

**SwapBuffersNow()** - 텍스트 렌더링 후 버퍼 교체
```cpp
RENDERER_API void SwapBuffersNow() {
    if (!g_hDC) return;
    SwapBuffers(g_hDC);
}
```

이 함수는 OpenGL 렌더링 후 GDI+ 텍스트를 그린 다음에 호출되어,
두 가지를 모두 화면에 표시합니다.

---

## 요약

1. ✅ Visual Studio에서 `NativeRenderer` 프로젝트 빌드
2. ✅ `NativeRenderer.dll` 복사 (필요시)
3. ✅ WinFormsApp 실행
4. ✅ 파트/컨투어 번호 버튼 테스트

**이 과정이 완료되면 모든 기능이 정상 작동합니다!** 🚀
