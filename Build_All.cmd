@echo off
:: Build_All.cmd
:: Build all 5 solution configurations of x360ce.slnx in one shot.
:: Stops on the first failure.
::
:: Usage:
::   Build_All.cmd            (defaults to Release)
::   Build_All.cmd Debug
::   Build_All.cmd Release

setlocal EnableExtensions

set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Release"

set "SLN=%~dp0x360ce.slnx"

:: ---------------------------------------------------------------------------
:: Locate MSBuild via vswhere.
:: ---------------------------------------------------------------------------
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    echo ERROR: vswhere.exe not found at "%VSWHERE%".
    echo        Install Visual Studio 2022+ or VS 2026 Build Tools, including the
    echo        "MSVC v141 - VS 2017 C++ build tools" individual component, which a
    echo        default installation does not carry.
    exit /b 1
)

set "_TMP=%TEMP%\x360ce_msbuild_path.txt"
"%VSWHERE%" -latest -find "MSBuild\**\Bin\MSBuild.exe" > "%_TMP%"
set "MSBUILD="
set /p MSBUILD=<"%_TMP%"
del "%_TMP%" >nul 2>&1
if not defined MSBUILD (
    echo ERROR: MSBuild.exe not found via vswhere.
    exit /b 1
)
if not exist "%MSBUILD%" (
    echo ERROR: MSBuild.exe path returned by vswhere does not exist: "%MSBUILD%"
    exit /b 1
)

if not exist "%SLN%" (
    echo ERROR: Solution not found: "%SLN%"
    exit /b 1
)

echo ============================================================
echo MSBuild         : %MSBUILD%
echo Solution        : %SLN%
echo Configuration   : %CONFIG%
echo ============================================================
echo.

set "FAILED="

:: Both native bitnesses are embedded into each application, whichever bitness the
:: application itself is, so every DLL platform is built before any APP platform.
:: This is the order Documents\App_0_Release.ps1 uses for the same reason.
call :BuildPlatform "DLL_x86_v3" || goto :Failure
call :BuildPlatform "DLL_x64_v3" || goto :Failure
call :BuildPlatform "APP_x86_v3" || goto :Failure
call :BuildPlatform "APP_x64_v3" || goto :Failure
call :BuildPlatform "APP_Any_v4" || goto :Failure

echo.
echo ============================================================
echo All 5 platforms built successfully (%CONFIG%).
echo ============================================================
exit /b 0

:Failure
echo.
echo ============================================================
echo BUILD FAILED for %CONFIG% ^| %FAILED%
echo ============================================================
exit /b 1

:: ---------------------------------------------------------------------------
:: :BuildPlatform <PlatformName>
:: ---------------------------------------------------------------------------
:BuildPlatform
set "PLAT=%~1"
echo --- %CONFIG% ^| %PLAT% ---
:: The toolset is stated once, by each .vcxproj. Passing it here as well would be a
:: second source of truth that Documents\App_0_Release.ps1 does not pass, letting the
:: two entry points build with different tools without anyone choosing that.
"%MSBUILD%" "%SLN%" /p:Configuration=%CONFIG% "/p:Platform=%PLAT%" /m /v:m /nologo
if errorlevel 1 (
    set "FAILED=%PLAT%"
    exit /b 1
)
echo.
exit /b 0
