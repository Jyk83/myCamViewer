@echo off
REM ============================================
REM ITag Communication 빌드 스크립트
REM C# UserControl만 빌드 (VB.NET Wrapper 제거됨)
REM Visual Studio 2019 MSBuild 사용
REM ============================================

echo.
echo ============================================
echo   ITag Communication Build Script
echo   (C# Direct Implementation - HKCAMLib Style)
echo ============================================
echo.

REM Visual Studio 경로 설정
set VS2019_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin
set VS2019_PRO_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin
set VS2019_ENT_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin

set MSBUILD_PATH=

REM MSBuild 경로 찾기
if exist "%VS2019_PATH%\MSBuild.exe" (
    set MSBUILD_PATH=%VS2019_PATH%\MSBuild.exe
    echo [OK] Visual Studio 2019 Community 발견
) else if exist "%VS2019_PRO_PATH%\MSBuild.exe" (
    set MSBUILD_PATH=%VS2019_PRO_PATH%\MSBuild.exe
    echo [OK] Visual Studio 2019 Professional 발견
) else if exist "%VS2019_ENT_PATH%\MSBuild.exe" (
    set MSBUILD_PATH=%VS2019_ENT_PATH%\MSBuild.exe
    echo [OK] Visual Studio 2019 Enterprise 발견
) else (
    echo [ERROR] Visual Studio 2019 MSBuild를 찾을 수 없습니다.
    echo.
    echo 다음 경로를 확인하세요:
    echo   %VS2019_PATH%
    echo   %VS2019_PRO_PATH%
    echo   %VS2019_ENT_PATH%
    echo.
    pause
    exit /b 1
)

echo MSBuild 경로: %MSBUILD_PATH%
echo.

REM 빌드 구성 설정
set BUILD_CONFIG=Debug
set BUILD_PLATFORM=Any CPU

echo ============================================
echo   빌드 시작
echo ============================================
echo 구성: %BUILD_CONFIG%
echo 플랫폼: %BUILD_PLATFORM%
echo.

REM 솔루션 빌드
echo [1/1] C# UserControl 빌드 중...
"%MSBUILD_PATH%" ITagCommunication.sln /p:Configuration=%BUILD_CONFIG% /p:Platform="%BUILD_PLATFORM%" /v:minimal /t:Rebuild

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] 빌드 실패! 에러 코드: %ERRORLEVEL%
    echo.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [OK] 빌드 완료 확인...
if exist "CSharpUserControl\bin\%BUILD_CONFIG%\ITagTestControl.dll" (
    echo [OK] ITagTestControl.dll 생성 완료
) else (
    echo [ERROR] ITagTestControl.dll 생성 실패
    pause
    exit /b 1
)

echo.
echo ============================================
echo   빌드 완료!
echo ============================================
echo.
echo 생성된 파일:
echo   CSharpUserControl\bin\%BUILD_CONFIG%\ITagTestControl.dll
echo   CSharpUserControl\bin\%BUILD_CONFIG%\Siemens.Runtime.ControlDev.dll
echo.
echo 다음 단계:
echo   1. WinCC Graphics Designer 실행
echo   2. 도구 상자에서 "컨트롤 선택..." 클릭
echo   3. ITagTestControl.dll 찾아서 추가
echo   4. 화면에 드래그하여 배치
echo   5. Load 이벤트에서 자동으로 ITag 연결 시도
echo.
echo 참고:
echo   - VB.NET Wrapper 제거됨 (불필요)
echo   - C#에서 직접 ITag 및 ITagSink 구현
echo   - HKCAMLib.dll 방식 적용
echo   - Site.GetService 우선, CreateObject 대체
echo.

pause
