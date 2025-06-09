@echo off
:: Get current directory.
for /f "tokens=*" %%a in ('cd') DO SET DIR0=%%a

:: Set destination file, source list and exclusion list.
:: %~n0 equals to current batch file name without extension.
SET fil=%~n0
SET dst="%DIR0%\%fil%.zip"
SET lst="%DIR0%\%fil%-Include.txt"
SET exc="%DIR0%\%fil%-Exclude.txt"
SET wra="%ProgramFiles%\WinRAR\winrar.exe"
if NOT EXIST %wra% SET wra="%ProgramFiles(x86)%\WinRAR\winrar.exe"
if NOT EXIST %wra% SET wra="%ProgramW6432%\WinRAR\winrar.exe"
SET zip=%wra% a -r -afzip -x@%exc%
echo DST=%dst%
echo LST=%lst%
echo EXC=%exc%
echo ZIP=%zip%

:: ---------------------------------------------------------------------------
:: Go to current folder
cd %DIR0%
echo --- Delete file if file with same name already exist.
IF EXIST %dst% del %dst%
:: Go back to Solution root folder.
cd "..\.."
cd
echo --- Pack all files in the list.
%zip% %dst% @%lst%
:: Go back to home/current folder.
cd %DIR0%

:end
pause