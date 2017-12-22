@ECHO OFF
SET file=%~1
SET dst32=..\..\x360ce.App\Resources\x86\xBoxInterface.dll
SET dst64=..\..\x360ce.App\Resources\x64\xBoxInterface.dll
:: If file name was supplied then sign file.
IF NOT "%file%" == "" CALL:SIG "%file%"
pause

GOTO:EOF
::=============================================================
:SIG :: Sign and Timestamp Code
::-------------------------------------------------------------
set sgt=%ProgramFiles%\Microsoft SDKs\Windows\v7.1\Bin\signtool.exe
if not exist "%sgt%" set sgt=%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.1A\Bin\signtool.exe
set pfx=D:\_Backup\Configuration\SSL\CodeSign_Standard\2016\Evaldas_Jocys.CodeSign.pfx
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