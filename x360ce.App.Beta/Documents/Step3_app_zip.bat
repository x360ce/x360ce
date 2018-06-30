@ECHO off
SET wra="%ProgramFiles%\WinRAR\winrar.exe"
IF NOT EXIST %wra% SET wra="%ProgramFiles(x86)%\WinRAR\winrar.exe"
IF NOT EXIST %wra% SET wra="%ProgramW6432%\WinRAR\winrar.exe"
SET zip=%wra% a -ep
:: ---------------------------------------------------------------------------
IF NOT EXIST Files\nul MKDIR Files
::-------------------------------------------------------------
:: Archive MSIL Application
CALL:CRE ..\bin\Debug     x360ce
:: Archive 32-bit Application
CALL:CRE ..\bin\Debug_x86 x360ce_x86
:: Archive 64-bit Application
CALL:CRE ..\bin\Debug_x64 x360ce_x64
ECHO.
pause
GOTO:EOF

::-------------------------------------------------------------
:CRE :: Sign and Timestamp Code
::-------------------------------------------------------------
SET src=%~1
SET arc=Files\%~2.zip
ECHO.
IF NOT EXIST "%src%\x360ce.exe" (
  ECHO "%src%\x360ce.exe" not exist. Skipping.
  GOTO:EOF
)
ECHO Creating: %arc%
:: Rename file so it can be in the same folder as MSIL exe.
IF NOT "x360ce"=="%~2" (
  COPY /Y %src%\x360ce.exe %src%\%~2.exe
  COPY /Y %src%\x360ce.pdb %src%\%~2.pdb
)
:: Create Archive.
IF EXIST %arc% DEL %arc%
%zip% %arc% %src%\%~2.exe
:: Create archive with debug info.
SET arc=Files\%~2_debug.zip
IF EXIST %arc% DEL %arc%
%zip% %arc% %src%\x360ce.exe
%zip% %arc% %src%\x360ce.pdb
%zip% %arc% %src%\x360ce.Engine.dll
%zip% %arc% %src%\x360ce.Engine.pdb
GOTO:EOF