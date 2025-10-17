# ⚡ HK Laser Cutting Viewer

**HK 레이저 절단 프로그램 시각화 도구**

Siemens Sinumerik ONE NCU1760을 사용하는 HK 레이저 절단 장비의 MPF 파트 프로그램 파일을 시각화하는 웹 기반 뷰어입니다.

![HK Laser Viewer](https://img.shields.io/badge/HK-Laser%20Viewer-blue)
![React](https://img.shields.io/badge/React-19.x-61DAFB?logo=react)
![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript)
![Three.js](https://img.shields.io/badge/Three.js-0.180-000000?logo=three.js)

## 🎯 주요 기능

### ✨ 핵심 기능
- **📁 MPF 파일 파싱**: HK 전용 레이저 함수 및 G-code 완벽 지원
- **🎨 실시간 시각화**: Three.js 기반 2D/3D 렌더링
- **📊 네스팅 표시**: 워크피스 내 파트 배치 시각화
- **🔍 상세 경로 표시**:
  - 피어싱 위치 (Piercing Points)
  - 리드인 경로 (Lead-in Path)
  - 어프로치 경로 (Approach Path)
  - 절단 경로 (Cutting Path)
  - 마킹 경로 (Marking Path)

### 🛠️ 지원하는 HK 함수
```
HKLDB - 절단 데이터베이스 로드
HKINI - 초기화
HKOST - 파트 원점 설정
HKPPP - 파트 프로그램 포인터
HKEND - 프로그램 종료
HKSTR - 컨투어 시작
HKPIE - 피어싱
HKLEA - 리드인
HKCUT - 절단
HKSTO - 컨투어 종료
HKPED - 파트 종료
HKSCRC - 잔재 절단
```

### 📋 지원하는 G-code
- **G0**: 급속 이동 (레이저 OFF, 직선 이동)
- **G1**: 직선 보간 (레이저 ON, 직선 절단)
- **G2**: 시계방향 원호 보간 (레이저 ON)
- **G3**: 반시계방향 원호 보간 (레이저 ON)
- **M30**: 프로그램 종료

## 🚀 빠른 시작

### 필수 요구사항
- Node.js 18.x 이상
- npm 또는 yarn

### 설치 및 실행

```bash
# 의존성 설치
npm install

# 개발 서버 시작
npm run dev

# 프로덕션 빌드
npm run build

# 빌드 미리보기
npm run preview
```

### 사용 방법

1. **파일 업로드**: 좌측 상단의 "MPF 파일 선택" 버튼 클릭
2. **파일 선택**: `.txt`, `.mpf`, `.nc` 확장자의 HK MPF 파일 선택
3. **시각화 확인**: 자동으로 파싱 및 렌더링됨
4. **옵션 조정**: 좌측 패널에서 표시 옵션 토글
5. **뷰어 조작**:
   - 🖱️ **마우스 왼쪽**: 이동 (팬)
   - 🖱️ **마우스 휠**: 확대/축소
   - 🖱️ **마우스 오른쪽**: 회전 (3D 모드)

## 📁 프로젝트 구조

```
hk-laser-viewer/
├── src/
│   ├── parser/          # MPF 파일 파서
│   │   └── MPFParser.ts # HK 함수 및 G-code 파싱
│   ├── types/           # TypeScript 타입 정의
│   │   └── index.ts     # 모든 타입 정의
│   ├── viewer/          # 3D 뷰어
│   │   └── LaserViewer.tsx # Three.js 렌더링
│   ├── components/      # UI 컴포넌트
│   │   ├── FileUploader.tsx
│   │   ├── ProgramInfo.tsx
│   │   ├── ViewControls.tsx
│   │   └── PartsList.tsx
│   ├── App.tsx          # 메인 앱
│   └── main.tsx         # 진입점
├── public/
│   └── samples/         # 샘플 MPF 파일
│       └── MS1T_A05.txt
└── package.json
```

## 🎨 기술 스택

- **Frontend Framework**: React 19.x
- **Language**: TypeScript 5.x
- **3D Rendering**: Three.js 0.180
- **3D React Integration**: @react-three/fiber, @react-three/drei
- **Build Tool**: Vite 7.x
- **Styling**: CSS-in-JS (Inline Styles)

## 📖 MPF 파일 형식

HK MPF 파일은 다음과 같은 구조를 가집니다:

```
;!V16A05                              # 버전 정보
N1                                    # 프로그램 시작
HKLDB(1,"MS010",1,0,0,0)              # DB 로드
HKINI(5,400.,400.,0,0,0)              # 초기화

# 네스팅 정보
N10000 HKOST(10.,10.,0.,10001,11,0,0,0)
HKPPP
N20000 HKOST(120.,10.,0.,10001,11,0,0,0)
HKPPP
N30000 HKEND(0,0,0)

# 파트 정보
N10001 HKSTR(1,1,50.,50.,0,100.,100.,0)
HKPIE(0,0,0)
HKLEA(1,49.,50.,0,0,0,0,0)
HKCUT(0,0,0)
G1 X0. Y50.
G1 X0. Y0.
G1 X100. Y0.
G1 X100. Y100.
HKSTO(1,100.,50.,0,0,0,0,0)
N10002 HKPED(0,0,0)

N10 M30                               # 프로그램 종료
```

## 🎯 참고 문서

프로젝트 개발 시 참조한 HK 기술 문서:
- **Part Program File Format V16 A05**: HK MPF 파일 형식 명세서
- **싸이클 프로그램 매뉴얼**: HK 함수 사용 매뉴얼

## 🔧 개발 정보

### 컴포넌트 설명

#### MPFParser
- HK 함수 및 G-code 파싱
- 네스팅 정보 추출
- 파트 및 컨투어 구조 생성

#### LaserViewer
- Three.js 기반 렌더링
- 워크피스, 그리드, 파트 시각화
- 카메라 및 컨트롤 관리

#### UI Components
- **FileUploader**: 파일 업로드 인터페이스
- **ProgramInfo**: 프로그램 정보 표시
- **ViewControls**: 표시 옵션 제어
- **PartsList**: 파트 목록 및 선택

## 🤝 기여

HK Global의 레이저 절단 장비 제어 시스템 전용 뷰어입니다.

### 개발자
- **회사**: HK Global (www.hk-global.com)
- **용도**: Siemens Sinumerik ONE NCU1760 기반 레이저 절단 장비
- **제품**: 2D 레이저 절단 시스템

## 📄 라이선스

이 프로젝트는 HK Global의 내부 도구로 개발되었습니다.

## 🐛 이슈 및 피드백

버그 리포트나 기능 제안이 있으시면 개발팀에 문의해주세요.

---

**Made with ❤️ for HK Global Laser Cutting Systems**
