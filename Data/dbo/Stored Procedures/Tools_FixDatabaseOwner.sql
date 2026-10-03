
CREATE PROCEDURE [dbo].[Tools_FixDatabaseOwner]
    @login sysname = null,
	@apply bit = 0
AS

/*

-- Show current owners of databases.
EXEC [dbo].[Tools_FixDatabaseOwner]

-- Reset owner to 'sa' login.
EXEC [dbo].[Tools_FixDatabaseOwner] 'sa', 1

-- Reset owner permissions.
EXEC [dbo].[Tools_FixDatabaseOwner] null, 1

*/

---------------------------------------------------------------
-- Fix Database Owner (Optional)
---------------------------------------------------------------

DECLARE @database sysname = db_name()
DECLARE @command varchar(max)
DECLARE @newLine varchar(2) = CHAR(13) + CHAR(10)

DECLARE @currentLogin sysname

SELECT @currentLogin = sl.Name
FROM master..sysdatabases sd
JOIN master..syslogins sl ON sd.[sid] = sl.[sid]
WHERE sd.Name = @database

IF @apply = 1
BEGIN

	IF @login IS NULL
	SET @login = @currentLogin

	---- Fix database Owner
	SELECT @command = 'ALTER AUTHORIZATION ON DATABASE::'+@database+' TO ['+@login+']' 
	PRINT @Command
	EXEC(@Command)

END
ELSE
BEGIN

	-- List Databases and owners
	SELECT SD.Name AS DatabaseName, SL.Name AS LoginName
	FROM master..sysdatabases SD 
	JOIN master..syslogins SL ON  SD.SID = SL.SID

END