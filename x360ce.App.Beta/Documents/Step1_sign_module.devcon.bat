@ECHO OFF
CALL:SIG "..\Resources\HidGuardian\x86\devcon.exe"
CALL:SIG "..\Resources\HidGuardian\x64\devcon.exe"
CALL:SIG "..\Resources\ViGEmBus\x64\devcon.exe"
CALL:SIG "..\Resources\ViGEmBus\x64\devcon.exe"
pause

GOTO:EOF
::=============================================================
:SIG :: Sign and Timestamp Code
::-------------------------------------------------------------
:: SIGNTOOL.EXE Note:
:: Use the Windows 7 Platform SDK web installer that lets you
:: download just the components you need - so just choose the
:: ".NET developer \ Tools" and deselect everything else.
SET sgt=Tools\signtool.exe
IF NOT EXIST "%sgt%" SET sgt=%ProgramFiles(x86)%\Windows Kits\10\App Certification Kit\signtool.exe
IF NOT EXIST "%sgt%" SET sgt=%ProgramFiles(x86)%\Windows Kits\10\bin\x86\signtool.exe
IF NOT EXIST "%sgt%" SET sgt=%ProgramFiles%\Windows Kits\10\bin\x86\signtool.exe
:: Other options.
set pfx=D:\_Backup\Configuration\SSL\CodeSign_Standard\2016\Evaldas_Jocys.CodeSign.pfx
set d=X360CE - Devcon Tool
set du=http://www.x360ce.com
set vsg=http://timestamp.verisign.com/scripts/timestamp.dll
if not exist "%sgt%" CALL:Error "%sgt%"
if not exist "%~1"   CALL:Error "%~1"
if not exist "%pfx%" CALL:Error "%pfx%"
"%sgt%" sign /f "%pfx%" /d "%d%" /du "%du%" /t "%vsg%" /v "%~1"
GOTO:EOF

:Error
echo File doesn't Exist: "%~1"
pause