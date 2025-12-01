# Phase 1: MPF Parser - 기본 구현

## 🎯 목표

MPF 파일 파싱 및 기본 렌더링 기능 구현

---

## ✅ 포함된 기능

### 1. MPF Parser
- HK 함수 파싱 (HKLDB, HKINI, HKOST, HKSTR, HKCUT, etc.)
- G-code 파싱 (G0, G1, G2, G3)
- 네스팅 정보 추출
- 파트/컨투어 구조 분석

### 2. OpenGL 렌더링
- DrawLine: 직선 그리기
- DrawArc: 원호 그리기 (G2/G3)
- DrawPoint: 피어싱/원점 표시

### 3. 기본 UI
- CamViewerControl (UserControl)
- MainForm (테스트 폼)
- Load MPF File 버튼
- 기본 조작 버튼 (Clear, Reset, Sample)

### 4. 뷰어 조작
- 마우스 휠: 줌
- 마우스 오른쪽 드래그: 팬
- 자동 뷰 맞춤

---

## 🚫 포함되지 않은 기능

- ❌ 시뮬레이션 엔진
- ❌ 파일 탐색 UI (TreeView/ListView)
- ❌ 진행률 표시
- ❌ 코드 뷰어

---

## 📂 파일 구조

```
Phase1_MPFParser/
├── WinFormsApp/
│   ├── MPF/                    # MPF 데이터 구조 및 파서
│   │   ├── Point2D.cs
│   │   ├── PathSegment.cs
│   │   ├── Commands.cs
│   │   ├── Contour.cs
│   │   ├── Part.cs
│   │   ├── MPFProgram.cs
│   │   └── MPFParser.cs       # 핵심 파서
│   ├── CamViewerControl.cs     # 뷰어 컨트롤
│   ├── MainForm.cs             # 메인 폼
│   ├── DiagnosticHelper.cs
│   └── CamViewerPOC.csproj
├── NativeRenderer/
│   ├── renderer.h
│   ├── renderer.cpp
│   └── CMakeLists.txt
├── SampleMPF/
│   └── simple_test.mpf
└── README_PHASE1.md (이 파일)
```

---

## 🚀 빌드 방법

### Windows:
```batch
REM C++ DLL 빌드
build_native.bat

REM C# 애플리케이션 빌드
build_csharp.bat
```

### Visual Studio:
1. `CamViewerPOC.sln` 열기
2. F5 또는 Ctrl+F5로 실행

---

## 🧪 테스트 방법

1. 애플리케이션 실행
2. **"Load MPF File"** 버튼 클릭
3. `SampleMPF/simple_test.mpf` 선택
4. 결과 확인:
   - 흰색 워크피스 경계선
   - 녹색 파트 원점 (2개)
   - 빨강 피어싱 포인트
   - 노랑 Lead-in 경로
   - 청록 절단 경로

---

## 📊 검증 체크리스트

### 빌드 검증
- [ ] C++ DLL 빌드 성공 (`NativeRenderer.dll` 생성)
- [ ] C# 애플리케이션 빌드 성공 (`CamViewerPOC.exe` 생성)
- [ ] DLL 복사 확인 (bin/Release 또는 bin/Debug)

### 기능 검증
- [ ] 애플리케이션 시작 시 에러 없음
- [ ] Sample Shapes 버튼 작동
- [ ] Load MPF File 버튼 작동
- [ ] MPF 파일 로드 성공
- [ ] 워크피스 경계선 표시
- [ ] 파트 원점 표시 (녹색 점)
- [ ] 피어싱 위치 표시 (빨강 점)
- [ ] Lead-in 경로 표시 (노랑 선)
- [ ] 절단 경로 표시 (청록 선)

### 조작 검증
- [ ] 마우스 휠 줌 작동
- [ ] 마우스 드래그 팬 작동
- [ ] Reset View 버튼 작동
- [ ] Clear Scene 버튼 작동

---

## 🐛 알려진 제한사항

1. **시뮬레이션 없음**: 정적 표시만 가능
2. **파일 탐색 없음**: 직접 파일 선택 필요
3. **진행률 없음**: 로딩 중 피드백 없음

---

## ➡️ 다음 단계

Phase 1 검증 완료 후 **Phase 2 (시뮬레이션 엔진)** 진행

---

**Phase**: 1 of 3  
**Status**: ✅ Ready for Testing  
**Date**: 2025-11-18
