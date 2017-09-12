@ECHO OFF
:: Remove BIN, OBJ as SQL folders from all projects.

FOR /D %%X IN (*) DO CALL:SUB "%%X"
pause
GOTO:EOF

:SUB
IF "%~1" == "Resources" GOTO:EOF
IF "%~1" == "Releases" GOTO:EOF
FOR /D %%Y IN ("%~1\*") DO CALL:DEL "%%Y"
GOTO:EOF

:DEL
IF EXIST "%~1\bin" (
  ECHO %~1\bin
  RMDIR /S /Q "%~1\bin"
)
IF EXIST "%~1\obj" (
  ECHO %~1\obj
  RMDIR /S /Q "%~1\obj"
)
IF EXIST "%~1\sql" (
  ECHO %~1\sql
  RMDIR /S /Q "%~1\sql"
)
GOTO:EOF
