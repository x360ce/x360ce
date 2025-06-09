:: TocaEdit_clone_as_GIT.bat
::-------------------------------------------------------------
SET prg="%ProgramFiles%\Git\cmd\git.exe"
IF NOT EXIST %prg% SET prg="%ProgramFiles(x86)%\Git\cmd\git.exe"
IF NOT EXIST %prg% SET prg="%ProgramW6432%\Git\cmd\git.exe"
%prg% clone https://github.com/x360ce/x360ce.git ".\TocaEdit"
pause