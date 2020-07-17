@ECHO OFF
SET file=%~1
SET dst32=..\..\x360ce.App\Resources\x86\dinput8.dll
SET dst64=..\..\x360ce.App\Resources\x64\dinput8.dll
IF "%file%" == "" (
	IF NOT EXIST "%dst32%" COPY "..\..\x360ce\dinput8\bin\Release\dinput8.dll" "%dst32%"
	CALL:SIG "%dst32%"
	IF NOT EXIST "%dst64%" COPY "..\..\x360ce\dinput8\bin64\Release\dinput8.dll" "%dst64%"
	CALL:SIG "%dst64%"
)
:: If file name was supplied then sign file.
IF NOT "%file%" == "" CALL:SIG "%file%"
pause

GOTO:EOF
::=============================================================
:SIG :: Sign and Timestamp Code
::-------------------------------------------------------------
SET sgt=Tools\signtool.exe
IF NOT EXIST "%sgt%" SET sgt=%ProgramFiles(x86)%\Windows Kits\10\App Certification Kit\signtool.exe
IF NOT EXIST "%sgt%" SET sgt=%ProgramFiles(x86)%\Windows Kits\10\bin\x86\signtool.exe
IF NOT EXIST "%sgt%" SET sgt=%ProgramFiles%\Windows Kits\10\bin\x86\signtool.exe
:: Other options.
set pfx=D:\_Backup\Configuration\SSL\Code Sign - Evaldas Jocys\2020\Evaldas_Jocys.pfx
set d=X360CE - DirectInput Library
set du=http://www.x360ce.com
set vsg=http://timestamp.verisign.com/scripts/timestamp.dll
if not exist "%sgt%" CALL:Error "%sgt%"
if not exist "%~1"   CALL:Error "%~1"
if not exist "%pfx%" CALL:Error "%pfx%"
"%sgt%" sign /f "%pfx%" /d "%d%" /du "%du%" /t "%vsg%" /v "%~1"
GOTO:EOF

:Error
echo File doesn't Exist: "%~1"