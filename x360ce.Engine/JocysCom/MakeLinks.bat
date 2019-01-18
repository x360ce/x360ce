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

::-------------------------------------------------------------
:: Main
::-------------------------------------------------------------
:: List   symbolic links: dir /A:L
:: Remote symbolic links: rmdir Skype
SET upr=C:\Projects\Jocys.com\Class Library
IF EXIST "D:\Projects\Jocys.com\Class Library" SET upr=D:\Projects\Jocys.com\Class Library
CALL:MKJ ClassTools
CALL:MKJ Collections
CALL:MKJ Common
CALL:MKJ ComponentModel
CALL:MKJ Configuration
CALL:MKJ Controls
CALL:MKJ Data
CALL:MKJ Diagnostics
CALL:MKJ Drawing
CALL:MKJ Extensions
CALL:MKJ Files
CALL:MKJ IO
CALL:MKJ Mail
CALL:MKJ Network
CALL:MKJ Processes
CALL:MKJ Resources
CALL:MKJ Runtime
CALL:MKJ Security
CALL:MKJ Text
CALL:MKJ Threading
CALL:MKJ Web
CALL:MKJ Win32
pause
GOTO:EOF

::=============================================================
:MKL
::-------------------------------------------------------------

IF NOT EXIST "%~pd1" mkdir "%~pd1"
IF EXIST "%~1" (
  echo Already exists: %~1
) ELSE (
  echo Map: %~1
  fsutil hardlink create "%~1" "%upr%\%~1" > nul
)
GOTO:EOF

::=============================================================
:MKJ
::-------------------------------------------------------------

IF NOT EXIST "%~pd1" mkdir "%~pd1"
IF EXIST "%~1" (
  echo Already exists: %~1
) ELSE (
  echo Map: %~1
  mklink /J "%~1" "%upr%\%~1" > nul
)
GOTO:EOF



