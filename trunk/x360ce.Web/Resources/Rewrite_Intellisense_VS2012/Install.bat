@echo off
::-------------------------------------------------------------
:: Check permisions and run as Administrator.
::-------------------------------------------------------------
ATTRIB %windir%\system32 -h | FINDSTR /I "denied" >nul
IF NOT ERRORLEVEL 1 GOTO:ADM
GOTO:EXE
::-------------------------------------------------------------
:ADM
::-------------------------------------------------------------
:: Create temp batch.
echo @echo off> %~n0.tmp.bat
echo %~d0>> %~n0.tmp.bat
echo cd "%~p0">> %~n0.tmp.bat
echo call "%~nx0" %1 %2 %3 %4 %5 %6 %7 %8 %9>> %~n0.tmp.bat
echo del %~n0.tmp.js>> %~n0.tmp.bat
echo del %~n0.tmp.bat>> %~n0.tmp.bat
:: Create temp script.
echo var arg = WScript.Arguments;> %~n0.tmp.js
echo var wsh = WScript.CreateObject("WScript.Shell");>> %~n0.tmp.js
echo var sha = WScript.CreateObject("Shell.Application");>> %~n0.tmp.js
echo sha.ShellExecute(arg(0), "", wsh.CurrentDirectory, "runas", 1);>> %~n0.tmp.js
:: Execute as Administrator.
cscript /B /NoLogo "%~n0.tmp.js" "%~dp0%~n0.tmp.bat"
GOTO:EOF
::-------------------------------------------------------------
:EXE
::-------------------------------------------------------------

cscript UpdateSchemaCache.js 