# 🔧 빌드 및 테스트 가이드 V2 (중요한 수정 포함!)

**날짜**: 2025-11-20  
**버전**: V2 - 좌표 변환 수식 수정

---

## ⚠️ 중요: V2 업데이트 내용

### 무엇이 바뀌었나?

**첫 번째 수정 (V1)에 치명적인 오류가 있었습니다!**

```csharp
// V1 (잘못됨)
float worldX = (ndcX / zoom) - panX;  // ✗ MINUS는 틀림!

// V2 (올바름)
float objectX = (ndcX / zoom) + panX;  // ✓ PLUS가 맞음!
```

**이유**: `glOrtho` 투영 변환의 역변환은 `+ panX`를 사용해야 합니다.

---

## 🚀 빌드 순서 (필수!)

### 1단계: Native Renderer DLL 재빌드

```cmd
cd D:\Genspark\Phase5_RealData\NativeRenderer\build
cmake --build . --config Release
```

**확인사항:**
- ✅ `NativeRenderer.dll` 생성 확인
- ✅ 위치: `NativeRenderer\build\bin\Release\NativeRenderer.dll`

### 2단계: WinFormsApp 재빌드

Visual Studio에서:
1. Solution Explorer에서 `WinFormsApp` 우클릭
2. **"Rebuild"** 선택 (Build가 아닌 Rebuild!)
3. 빌드 성공 확인

또는 명령줄:
```cmd
cd D:\Genspark\Phase5_RealData
msbuild WinFormsApp\CamViewerPOC.csproj /t:Rebuild /p:Configuration=Release /p:Platform=x64
```

**⚠️ 중요**: 반드시 **Rebuild**를 해야 합니다! (Build만 하면 변경사항이 반영 안 될 수 있음)

### 3단계: DLL 복사 확인

```cmd
# 자동 복사 확인
dir D:\Genspark\Phase5_RealData\WinFormsApp\bin\x64\Release\NativeRenderer.dll

# 수동 복사 (필요시)
copy D:\Genspark\Phase5_RealData\NativeRenderer\build\bin\Release\NativeRenderer.dll ^
     D:\Genspark\Phase5_RealData\WinFormsApp\bin\x64\Release\
```

---

## 🧪 테스트 체크리스트

### Issue #1: 파트 경계 점선

**테스트:**
- [ ] MPF 파일 로드
- [ ] 파트 경계선 확인
- [ ] 점선 패턴이 거의 연속적으로 보이는지 (`------` 스타일)

**예상 결과:**
```
이전: ─ ─ ─ ─ ─ ─  (간격 있음)
V1:   ── ── ── ──  (좀 촘촘)
V2:   ────────────  (매우 촘촘, 거의 연속)
```

---

### Issue #3: 컨투어 선택 (핵심!)

**테스트 시나리오:**

#### 시나리오 1: 기본 줌에서 선택
1. MPF 로드 후 기본 뷰 상태
2. 왼쪽 파트 (자동차 모양) 클릭
3. 로그 확인

**예상 로그 (V2):**
```
[14:23:45] ScreenToObject: Screen(54,183) → NDC(-0.972,-0.021) → Object(0.010,0.010)
[14:23:45]   Part[0]: Origin(10.00,10.00mm), Offset(0.01,0.01)
[14:23:45] ✓ Selected: Part 0, Contour X
```

**이전 로그 (V1 - 틀림):**
```
[14:23:45] ScreenToWorld: Screen(54,183) → World(-0.166,-0.009)
[14:23:45] WorldToObject: World(-0.166,-0.009) → Object(0.135,0.014)  ← 너무 큼!
[14:23:45] ✗ No Contour found at click position
```

**핵심 차이점:**
- ❌ V1: Object(0.135, 0.014) - Part offset (0.01, 0.01)과 차이 큼!
- ✅ V2: Object(0.010, 0.010) - Part offset과 거의 일치!

#### 시나리오 2: 줌 인 후 선택
1. 마우스 휠로 확대 (zoom > 10)
2. 컨투어 클릭
3. 로그에서 좌표 확인

**체크 포인트:**
- [ ] 줌 레벨이 달라도 선택 성공
- [ ] Object 좌표가 Part offset 범위 내에 있음

#### 시나리오 3: 팬 후 선택
1. 마우스 드래그로 이동 (pan)
2. 컨투어 클릭
3. 로그에서 좌표 확인

**체크 포인트:**
- [ ] 팬 위치와 관계없이 선택 성공
- [ ] Object 좌표 계산이 정확함

---

### Issue #5: 엘리먼트 선택

**테스트:**
1. 엘리먼트 선택 모드 활성화
2. 특정 컨투어의 엘리먼트 (선분) 클릭
3. 색상 변화 확인
4. 로그에서 선택된 파트/컨투어/엘리먼트 번호 확인

**예상 결과:**
- ✅ 클릭한 엘리먼트가 정확히 선택됨
- ✅ 선택된 엘리먼트가 노란색으로 변경
- ✅ 로그에 올바른 번호 표시

**이전 문제 (V1):**
- ❌ 컨투어 5를 클릭했는데 컨투어 7 엘리먼트 8이 선택됨
- ❌ 좌표 오차 때문에 엉뚱한 위치로 매칭됨

---

### Issue #4 & #8: 로그 출력

**테스트:**
- [ ] 컨투어 클릭 시 로그에 정보 표시
- [ ] 로그 형식 확인:
  ```
  [HH:mm:ss] ScreenToObject: Screen(...) → NDC(...) → Object(...)
  [HH:mm:ss]   Part[X]: Origin(...), Offset(...)
  [HH:mm:ss] ✓ Selected: Part X, Contour Y
  ```
- [ ] 타임스탬프 확인

---

### Issue #6: 회전 옵션

**테스트:**
- [ ] 렌더 설정 버튼 클릭
- [ ] "270° CW (반시계방향)" 옵션 확인

---

### Issue #7: 진단 버튼

**테스트:**
- [ ] 시뮬레이션 패널 하단 확인
- [ ] "Diagnostics (진단)" 버튼이 **없는지** 확인

---

## 🐛 문제 해결

### 여전히 선택이 안 되면?

#### 1. 로그 확인
```
[14:23:45] ScreenToObject: Screen(54,183) → NDC(-0.972,-0.021) → Object(???,???)
```

**Object 좌표가 Part offset과 비슷한가?**
- Part[0] Offset이 (0.01, 0.01)이면
- Object 좌표도 대략 (0.0x, 0.0x) 범위여야 함
- 만약 (0.1x, 0.1x) 이상이면 여전히 좌표 문제

#### 2. DLL이 최신인지 확인
```cmd
# DLL 타임스탬프 확인
dir /T:W D:\Genspark\Phase5_RealData\WinFormsApp\bin\x64\Release\NativeRenderer.dll

# 최근에 빌드한 시간과 비교
```

**해결책**: DLL 수동 복사 후 재실행

#### 3. 캐시 클리어
```cmd
cd D:\Genspark\Phase5_RealData
# Clean solution
msbuild CamViewerPOC.sln /t:Clean /p:Configuration=Release /p:Platform=x64

# Rebuild all
msbuild CamViewerPOC.sln /t:Rebuild /p:Configuration=Release /p:Platform=x64
```

---

### 엘리먼트 선택이 여전히 틀리면?

**가능한 원인:**
1. SelectionManager의 hit-testing tolerance가 너무 작음
2. 엘리먼트 좌표가 workpieceScale 적용 안 됨
3. zoom 변수가 SelectionManager에 잘못 전달됨

**디버깅:**
```csharp
// SelectionManager.FindElementAtPoint에 로그 추가
Log($"Testing element at ({elemX}, {elemY}), distance = {distance}, threshold = {threshold}");
```

---

## 📊 기대되는 로그 예시

### 성공적인 컨투어 선택
```
[16:25:31] ScreenToObject: Screen(245,178) → NDC(0.152,-0.023) → Object(0.024,0.007)
[16:25:31]   Zoom: 6.289, Pan: (0.134,0.066), Scale: 0.001
[16:25:31]   Part[0]: Origin(10.00,10.00mm), Offset(0.01,0.01), Size(50.00x30.00mm)
[16:25:31] ✓ Selected: Part 0, Contour 3
```

### 성공적인 엘리먼트 선택
```
[16:26:15] ScreenToObject: Screen(398,245) → NDC(0.487,0.124) → Object(0.077,0.086)
[16:26:15]   Part[0]: Origin(10.00,10.00mm), Offset(0.01,0.01)
[16:26:15] ✓ Selected: Part 0, Contour 5, Element 12
```

---

## 🎯 성공 기준

**모든 체크 항목이 ✅ 여야 성공:**

- [ ] ✅ 파트 경계 점선이 매우 촘촘함 (`------`)
- [ ] ✅ 컨투어를 클릭하면 해당 컨투어가 선택됨 (모든 줌/팬 레벨)
- [ ] ✅ 엘리먼트를 클릭하면 해당 엘리먼트가 선택됨 (정확한 번호)
- [ ] ✅ 로그에 ScreenToObject 좌표 표시
- [ ] ✅ Object 좌표가 Part offset과 비슷한 범위
- [ ] ✅ 선택 결과가 로그에 표시 (✓ 기호 포함)
- [ ] ✅ 진단 버튼이 사라짐
- [ ] ✅ 회전 옵션에 270° CW 있음

---

## 📝 테스트 후 작업

### 성공 시

1. **스크린샷 캡처**
   - 점선 경계 (Dash3 패턴)
   - 컨투어 선택 성공 화면
   - 로그 창 (좌표 정보 포함)

2. **Git Push**
   ```cmd
   cd D:\Genspark\Phase5_RealData
   # 또는 상위 폴더에서
   cd D:\Genspark
   
   git status
   git push origin genspark_ai_developer
   ```

3. **Pull Request 생성**
   - GitHub으로 이동
   - "Compare & pull request" 클릭
   - Base: `master`, Compare: `genspark_ai_developer`
   - 제목: "Fix Phase 5 Issues: Corrected Coordinate Transform V2"

---

### 실패 시

**로그 복사해서 공유:**
```
컨투어 선택 시도:
[로그 내용 붙여넣기]

엘리먼트 선택 시도:
[로그 내용 붙여넣기]

Object 좌표: (X, Y)
Part offset: (X, Y)
차이: ...
```

---

## 🔬 좌표 변환 이해하기

### glOrtho가 하는 일

```cpp
glOrtho(left, right, bottom, top, near, far)

// renderer.cpp에서:
left   = -viewWidth/2 + panX = -1/zoom + panX
right  =  viewWidth/2 + panX =  1/zoom + panX
bottom = -viewHeight/2 + panY
top    =  viewHeight/2 + panY
```

**의미**: NDC 공간 [-1, 1]을 object 공간 [left, right]로 매핑

**역변환**:
```
objectX = ndcX * (right - left) / 2 + (right + left) / 2
        = ndcX * (2/zoom) / 2 + (panX + panX) / 2
        = ndcX / zoom + panX  ✓
```

### 왜 PLUS인가?

**직관적 설명:**
- `panX > 0`: 카메라를 오른쪽으로 이동 = 물체가 왼쪽으로 보임
- 화면 왼쪽을 클릭하면 (`ndcX < 0`)
- 실제 object 좌표는 `panX` 기준에서 왼쪽 (`ndcX/zoom < 0`)
- 따라서: `objectX = ndcX/zoom + panX`

**MINUS를 사용하면**:
- `objectX = ndcX/zoom - panX`
- `panX > 0`일 때 좌표가 더 왼쪽으로 이동
- 실제 렌더링과 반대 방향! ✗

---

## 💡 V1과 V2의 차이 요약

| 항목 | V1 (틀림) | V2 (올바름) |
|------|----------|------------|
| 메서드 이름 | `ScreenToWorld()` | `ScreenToObject()` |
| 변환 공식 | `world = ndc/zoom - pan` ✗ | `object = ndc/zoom + pan` ✓ |
| 중간 메서드 | `WorldToObject()` 필요 | 필요 없음 (직접 변환) |
| Dash3 길이 | 0.008 | 0.004 (더 짧음) |
| Dash3 간격 | 0.003 | 0.001 (더 촘촘) |

**결과:**
- V1: 좌표가 틀려서 선택 실패
- V2: 좌표가 정확해서 선택 성공 (예상)

---

## 🎉 마무리

V2 수정사항:
1. ✅ 좌표 변환 수식 수정 (`-pan` → `+pan`)
2. ✅ 불필요한 메서드 제거 (`WorldToObject`)
3. ✅ Dash3 점선 더 촘촘하게

**이제 정말로 작동해야 합니다!**

빌드 후 테스트 결과를 알려주세요! 🚀
