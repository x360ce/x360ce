::@ECHO OFF
SET file=%~1
IF "%file%" == "" SET file=..\..\x360ce.App\Presets\xinput1_3.dll
CALL:SIG "%file%"
pause

GOTO:EOF
::=============================================================
:SIG :: Sign and Timestamp Code
::-------------------------------------------------------------
set sgt=C:\Program Files\Microsoft SDKs\Windows\v7.0\Bin\signtool.exe
set pfx=D:\_Backup\Configuration\SSL\Jocys.com.CodeSign.pfx
set d=XBOX 360 Controller Emulator
set du=http://www.jocys.com/projects/x360ce
set vsg=http://timestamp.verisign.com/scripts/timestamp.dll
if not exist "%sgt%" CALL:Error "%sgt%"
if not exist "%~1"   CALL:Error "%~1"
if not exist "%pfx%" CALL:Error "%pfx%"
"%sgt%" sign /f "%pfx%" /d "%d%" /du "%du%" /t "%vsg%" /v "%~1"
GOTO:EOF

:Error
echo File doesn't Exist: "%~1"
exit