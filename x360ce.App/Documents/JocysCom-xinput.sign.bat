@ECHO OFF
SET file=%~1
IF "%file%" == "" (
	CALL:SIG "..\..\x360ce.App\Resources\xinput1_3_32.dll"
	CALL:SIG "..\..\x360ce.App\Resources\xinput1_3_64.dll"
)
IF NOT "%file%" == "" CALL:SIG "%file%"
pause

GOTO:EOF
::=============================================================
:SIG :: Sign and Timestamp Code
::-------------------------------------------------------------
set sgt=%ProgramFiles%\Microsoft SDKs\Windows\v7.1\Bin\signtool.exe
if not exist "%sgt%" set sgt=%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.1A\Bin\signtool.exe
set pfx=D:\_Backup\Configuration\SSL\Standard\Jocys.com.CodeSign.pfx
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