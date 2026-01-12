# Phase 13: Canvas Orientation & Mouse Coordinate Display

## 📋 Overview

Phase 13에서는 CAM Viewer의 사용자 경험을 개선하기 위해 다음 기능을 추가합니다:

1. **캔버스 방향 기준점 변경**
2. **마우스 실시간 좌표 반환**

---

## 🎯 Phase 13 Goals

### 1. 캔버스 방향 기준점 변경
- **목적**: 사용자가 원하는 방향으로 캔버스 기준점 설정
- **기능**:
  - 좌하단 원점 (기본 OpenGL 좌표계)
  - 좌상단 원점 (Windows 좌표계)
  - 우하단 원점
  - 우상단 원점
- **사용 사례**: 다양한 CAM 시스템의 좌표 기준에 맞춤

### 2. 마우스 실시간 좌표 반환
- **목적**: 사용자가 마우스 위치의 정확한 좌표를 실시간으로 확인
- **기능**:
  - 마우스 커서 위치의 Object Space 좌표 표시
  - mm 단위로 좌표 표시
  - UI에 실시간 업데이트
- **사용 사례**: 정밀한 위치 확인, 측정, 검증

---

## 🔧 Implementation Plan

### Task 1: Canvas Orientation Settings
- [ ] RenderSettings에 CanvasOrientation 설정 추가
- [ ] 4가지 방향 옵션 구현:
  - BottomLeft (0,0 = 좌하단)
  - TopLeft (0,0 = 좌상단)
  - BottomRight (0,0 = 우하단)
  - TopRight (0,0 = 우상단)
- [ ] 좌표 변환 로직 수정
- [ ] UI에서 방향 선택 옵션 추가

### Task 2: Real-time Mouse Coordinate Display
- [ ] MouseMove 이벤트 핸들러 추가
- [ ] ScreenToObject 좌표 변환 적용
- [ ] 좌표 표시 UI 컴포넌트 추가
- [ ] mm 단위로 포맷팅
- [ ] 성능 최적화 (throttling)

---

## 📁 Project Structure

```
Phase13/
├── RealtimeITagControl/
│   ├── CamViewerCore.cs              # 마우스 좌표 이벤트 처리
│   ├── RealtimeITagControl.cs        # UI 통합
│   ├── Rendering/
│   │   └── RenderSettings.cs         # Canvas orientation 설정
│   ├── UI/
│   │   ├── ProgramInfoPanel.cs       # 좌표 표시 UI
│   │   └── ContourColorLegendForm.cs
│   ├── NativeRenderer.dll            # 업데이트된 렌더러 (Phase13)
│   └── ...
├── NativeRenderer/
│   ├── renderer.cpp                  # Canvas orientation 지원
│   └── renderer.h
└── PHASE13.md                        # 이 파일
```

---

## 🚀 Getting Started

### Prerequisites
- Phase 12 완료 상태
- Visual Studio 2019 이상
- .NET Framework 4.7.2 이상
- OpenGL 지원 그래픽 카드

### Build & Run
```bash
cd /home/user/CamViewer/Phase13
# Visual Studio에서 솔루션 열기
# 빌드 및 실행
```

---

## 📝 Changes from Phase 12

### Updated Files
- `RealtimeITagControl/NativeRenderer.dll` - Phase13 버전으로 업데이트

### New Features
- Canvas orientation configuration
- Real-time mouse coordinate display

---

## ✅ Testing Checklist

### Canvas Orientation
- [ ] 좌하단 원점 (기본) 정상 동작
- [ ] 좌상단 원점 전환 정상 동작
- [ ] 우하단 원점 전환 정상 동작
- [ ] 우상단 원점 전환 정상 동작
- [ ] 컨투어 렌더링이 방향에 맞게 표시
- [ ] 컨투어 선택이 방향에 맞게 동작

### Mouse Coordinate Display
- [ ] 마우스 이동 시 좌표 실시간 업데이트
- [ ] 좌표 정확도 검증
- [ ] mm 단위 포맷팅 정상 동작
- [ ] 성능 이슈 없음 (부드러운 업데이트)
- [ ] Zoom/Pan 시 좌표 정확도 유지

---

## 🐛 Known Issues
- TBD

---

## 📚 References
- Phase 12: Contour Selection & Zoom Enhancement
- OpenGL Coordinate Systems
- Windows Forms MouseMove Events

---

## 👥 Contributors
- Jyk83
- GenSpark AI

---

**Last Updated**: 2026-01-12
**Version**: Phase 13 Initial Setup
