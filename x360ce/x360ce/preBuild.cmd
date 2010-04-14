@echo off

SETLOCAL ENABLEEXTENSIONS

SubWCRev.exe "." "%CD%\svnrev_template.h" "%CD%\svnrev.h"
if %ERRORLEVEL% NEQ 0 (
  echo Automatic revision update unavailable, using generic template instead.
  echo You can safely ignore this message - see svnrev.h for details.
  copy /Y "%CD%\svnrev_unknown.h" "%CD%\svnrev.h"
  copy /Y "%CD%\postBuild.unknown" "%CD%\postBuild.cmd"
) else (
  SubWCRev.exe "%~1" "%CD%\postBuild.tmpl" "%CD%\postBuild.cmd" > NUL
)

ENDLOCAL
:: Always return an errorlevel of 0 -- this allows compilation to continue if SubWCRev failed.
exit 0
