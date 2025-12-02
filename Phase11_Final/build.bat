@echo off
REM Phase11 Final - WinCC ITag Realtime MPF Viewer Build Script

echo ====================================
echo Phase11 Final 빌드
echo Phase8 OpenGL + Phase9 ITag + Phase10 Layout
echo ====================================
echo.

REM MSBuild 경로 찾기
set MSBUILD_PATH=""

REM Visual Studio 2022
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

REM Visual Studio 2019
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

REM Visual Studio 2017
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
)

if %MSBUILD_PATH%=="" (
    echo [오류] MSBuild를 찾을 수 없습니다.
    echo Visual Studio 2017 이상을 설치하세요.
    pause
    exit /b 1
)

echo MSBuild 경로: %MSBUILD_PATH%
echo.

REM 솔루션 정리
echo [1/3] 솔루션 정리...
%MSBUILD_PATH% RealtimeITagControl.sln /t:Clean /p:Configuration=Release /p:Platform="Any CPU" /v:minimal
if errorlevel 1 (
    echo [오류] 솔루션 정리 실패
    pause
    exit /b 1
)

REM 솔루션 빌드 (Release, AnyCPU)
echo [2/3] 솔루션 빌드 (Release, AnyCPU)...
echo [알림] WinCC 호환성을 위해 AnyCPU(Prefer32Bit=false)로 빌드합니다.
%MSBUILD_PATH% RealtimeITagControl.sln /t:Build /p:Configuration=Release /p:Platform="Any CPU" /v:minimal
if errorlevel 1 (
    echo [오류] 빌드 실패
    pause
    exit /b 1
)

REM DLL 파일들 복사
echo [3/3] DLL 복사...
if exist "RealtimeITagControl\bin\Release\RealtimeITagControl.dll" (
    copy /Y "Siemens.Runtime.ControlDev.dll" "RealtimeITagControl\bin\Release\"
    copy /Y "RealtimeITagControl\NativeRenderer.dll" "RealtimeITagControl\bin\Release\"
    echo.
    echo ====================================
    echo Phase11 빌드 완료! (AnyCPU, Prefer32Bit=false)
    echo Phase8 OpenGL 렌더링 100%% 적용
    echo WinCC Runtime 호환 빌드
    echo ====================================
    echo.
    echo 출력 파일: RealtimeITagControl\bin\Release\RealtimeITagControl.dll
    echo.
    echo WinCC 임포트 방법:
    echo 1. WinCC Graphics Designer 실행
    echo 2. 도구 상자 -^> 컨트롤 선택...
    echo 3. RealtimeITagControl.dll 추가
    echo 4. 화면에 드래그하여 배치
    echo.
) else (
    echo [오류] DLL 파일을 찾을 수 없습니다.
    pause
    exit /b 1
)

pause
