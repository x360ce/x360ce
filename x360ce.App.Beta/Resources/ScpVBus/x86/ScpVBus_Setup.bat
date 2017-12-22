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

:: ------------------------------------------------------------
:: Show menu
:: ------------------------------------------------------------
ECHO.
ECHO Virtual Xbox devices:
ECHO.
ECHO   1 - Install Virtual Bus
ECHO   2 - UnInstall Virtual Bus
ECHO   0 - EXIT
ECHO.
SET /P X=Type 1-2 or 0 then press ENTER: 
:: Option 1,2,3
IF "%X%"=="1" GOTO:INS
IF "%X%"=="2" GOTO:UNS
GOTO:EOF

:INS
devcon.exe install ScpVBus.inf Root\ScpVBus
GOTO:EOF

:UNS
devcon.exe remove Root\ScpVBus
GOTO:EOF
