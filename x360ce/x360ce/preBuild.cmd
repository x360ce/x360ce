@echo off

SETLOCAL ENABLEEXTENSIONS

SubWCRev.exe "." "svnrev_template.h" "svnrev.h"
if %ERRORLEVEL% NEQ 0 (
  echo Automatic revision update unavailable, using generic template instead.
  echo You can safely ignore this message - see svnrev.h for details.
  copy /Y "svnrev_unknown.h" "svnrev.h"
)

ENDLOCAL
:: Always return an errorlevel of 0 -- this allows compilation to continue if SubWCRev failed.
exit 0
