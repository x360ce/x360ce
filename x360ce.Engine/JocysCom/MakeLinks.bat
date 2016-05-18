@echo off
::-------------------------------------------------------------
:: Check permissions and run as Administrator.
::-------------------------------------------------------------
ATTRIB %windir%\system32 -h | FINDSTR /I "denied" >nul
IF NOT ERRORLEVEL 1 GOTO:ADM
GOTO:EXE
:ADM
:: Create temp batch.
echo @echo off> %~n0.tmp.bat
echo %~d0>> %~n0.tmp.bat
echo cd "%~p0">> %~n0.tmp.bat
echo call "%~nx0" %1 %2 %3 %4 %5 %6 %7 %8 %9>> %~n0.tmp.bat
echo del %~n0.tmp.bat>> %~n0.tmp.bat
:: Create temp script.
echo var arg = WScript.Arguments;> %~n0.tmp.js
echo var wsh = WScript.CreateObject("WScript.Shell");>> %~n0.tmp.js
echo var sha = WScript.CreateObject("Shell.Application");>> %~n0.tmp.js
echo sha.ShellExecute(arg(0), "", wsh.CurrentDirectory, "runas", 1);>> %~n0.tmp.js
:: Execute as Administrator.
cscript /B /NoLogo "%~n0.tmp.js" "%~dp0%~n0.tmp.bat"
del %~n0.tmp.js
GOTO:EOF
:EXE

::-------------------------------------------------------------
:: Main
::-------------------------------------------------------------
:: List   symbolic links: dir /A:L
:: Remote symbolic links: rmdir Skype
SET upr=C:\Projects\Jocys.com\Class Library
IF EXIST "D:\Projects\Jocys.com\Class Library" SET upr=D:\Projects\Jocys.com\Class Library
CALL:MKJ ClassTools
CALL:MKJ Common
CALL:MKJ Controls
CALL:MKJ Collections
CALL:MKJ Configuration
CALL:MKJ Drawing
CALL:MKJ IO
CALL:MKJ Mail
CALL:MKJ Security
CALL:MKJ Threading
CALL:MKJ Win32
CALL:MKJ Data
CALL:MKJ Resources
CALL:MKJ Runtime
CALL:MKJ Text

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



