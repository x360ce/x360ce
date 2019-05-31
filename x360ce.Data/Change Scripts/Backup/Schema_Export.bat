set pac=Data\x360ce.dacpac
set srv=Server=localhost;Database=x360ce;Trusted_Connection=True;
Tools\SqlPackage.exe ^
/a:Extract ^
/scs:"%srv%" ^
/tf:"%pac%"
:: /p:VerifyExtraction=True ^
pause
