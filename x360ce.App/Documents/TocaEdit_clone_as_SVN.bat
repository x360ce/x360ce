SET prg="%ProgramFiles%\TortoiseSVN\bin\svn.exe"
IF NOT EXIST %prg% SET prg="%ProgramFiles(x86)%\TortoiseSVN\bin\svn.exe"
IF NOT EXIST %prg% SET prg="%ProgramW6432%\TortoiseSVN\bin\svn.exe"
%prg% checkout https://github.com/x360ce/x360ce.git/trunk ".\TocaEdit"
pause