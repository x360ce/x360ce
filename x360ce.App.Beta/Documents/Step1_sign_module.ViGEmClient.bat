@ECHO OFF
CALL:SIG "..\Resources\ViGEmClient\x86\ViGEmClient.dll"
CALL:SIG "..\Resources\ViGEmClient\x64\ViGEmClient.dll"
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
set pfx=D:\_Backup\Configuration\SSL\Code Sign - Evaldas Jocys\2020\Evaldas_Jocys.pfx
set d=X360CE - Shauleiz Virtual Xbox Interface
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
