@echo off
::-------------------------------------------------------------
:: Check permissions and run as Administrator.
::-------------------------------------------------------------
ATTRIB %windir%\system32 -h | FINDSTR /I "denied" >nul
IF NOT ERRORLEVEL 1 GOTO:ADM
GOTO:EXE
::-------------------------------------------------------------
:ADM
::-------------------------------------------------------------
:: Create temp batch.
SET tb="%TEMP%\%~n0.tmp.bat"
SET tj="%TEMP%\%~n0.tmp.js"
echo @echo off> %tb%
echo %~d0>> %tb%
echo cd "%~p0">> %tb%
echo call "%~nx0" %1 %2 %3 %4 %5 %6 %7 %8 %9>> %tb%
echo del %tj%>> %tb%
:: Delete itself without generating any error message.
echo (goto) 2^>nul ^& del %tb%>> %tb%
:: Create temp script.
echo var arg = WScript.Arguments;> %tj%
echo var wsh = WScript.CreateObject("WScript.Shell");>> %tj%
echo var sha = WScript.CreateObject("Shell.Application");>> %tj%
echo sha.ShellExecute(arg(0), "", wsh.CurrentDirectory, "runas", 1);>> %tj%
:: Execute as Administrator.
cscript /B /NoLogo %tj% %tb%
GOTO:EOF
::-------------------------------------------------------------
:EXE
::-------------------------------------------------------------

:: Fix Visual Studio "Windows Form Designer: Could not load file or assembly" designer error by 
:: clearing temporary compiled assemblies inside dynamically created folders by Visual Studio.
:: Visual studio must be closed for this batch script to succeed.
::@ECHO OFF
:: Other folders:
::ProgramFiles=C:\Program Files
::ProgramFiles(x86)=C:\Program Files (x86)
CALL:DEL "%USERPROFILE%\AppData\Local\Microsoft\VisualStudio\12.0\ProjectAssemblies\*.*"
CALL:DEL "%USERPROFILE%\AppData\Local\Microsoft\VisualStudio\13.0\ProjectAssemblies\*.*"
CALL:DEL "%USERPROFILE%\AppData\Local\Microsoft\VisualStudio\14.0\ProjectAssemblies\*.*"
CALL:DEL "%USERPROFILE%\AppData\Local\Microsoft\VisualStudio\14.0\ProjectAssemblies\*.*"
CALL:DEL "%USERPROFILE%\AppData\Local\Microsoft\VisualStudio\15.0_8bf248dc\ProjectAssemblies\*.*"
CALL:DEL "%LOCALAPPDATA%\Temp\iisexpress\*.*"
CALL:DEL "%LOCALAPPDATA%\Temp\Temporary ASP.NET Files\*.*"
CALL:DEL "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\Temporary ASP.NET Files\*.*"
CALL:DEL "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files\*.*"

:: Solution Explorer, right-click Solution
::	Properties -> Common Properties -> Debug Source Files -> clean "Do not look for these source files" box.

:: Tools -> Options -> Projects and Solutions -> Build and Run
:: Set "On Run, when build or deployment errors occur:" Prompt to Launch

:: .EditorConfig file.
:: "charset=utf-8" option can trigger "The source file is different from when the module was built." warning when debugging.

GOTO:EOF

:DEL
DEL /S /Q "%~1"
FOR /D %%p IN ("%~1") DO rmdir "%%p" /s /q
GOTO:EOF
