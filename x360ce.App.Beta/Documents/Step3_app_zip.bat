@echo off
SET wra="%ProgramFiles%\WinRAR\winrar.exe"
if NOT EXIST %wra% SET wra="%ProgramFiles(x86)%\WinRAR\winrar.exe"
if NOT EXIST %wra% SET wra="%ProgramW6432%\WinRAR\winrar.exe"
SET zip=%wra% a -ep
:: ---------------------------------------------------------------------------
SET xMSr=Files\x360ce.zip
SET xMSd=Files\x360ce_debug.zip
SET xMSf=..\bin\Debug
echo --- Delete files
IF NOT EXIST Files\nul MKDIR Files
IF EXIST %xMSr% DEL %xMSr%
IF EXIST %xMSd% DEL %xMSd%
::-------------------------------------------------------------
:: Archive x86 application.
%zip% %xMSr% %xMSf%\x360ce.exe
:: Archive x86 application with debug info.
%zip% %xMSd% %xMSf%\x360ce.exe
%zip% %xMSd% %xMSf%\x360ce.pdb
%zip% %xMSd% %xMSf%\x360ce.Engine.dll
%zip% %xMSd% %xMSf%\x360ce.Engine.pdb
pause
