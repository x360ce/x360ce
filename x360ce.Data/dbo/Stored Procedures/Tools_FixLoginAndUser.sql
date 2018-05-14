CREATE PROCEDURE [dbo].[Tools_FixLoginAndUser]
    @username sysname,
    @password sysname,
	@apply bit = 0
AS

/*

DECLARE @defaultUsername sysname = db_name()+'Admin'
DECLARE @defaultPassword sysname = 'localdev'

EXEC [dbo].[Tools_FixLoginAndUser] @defaultUsername, @defaultPassword, 1

*/

---------------------------------------------------------------
-- Fix SQL Login.
---------------------------------------------------------------

DECLARE @database sysname = db_name()
DECLARE @command varchar(max)
DECLARE @newLine varchar(2) = CHAR(13) + CHAR(10)

---------------------------------------------------------------
-- If SQL Server Login doesn't exist then...
---------------------------------------------------------------

IF EXISTS (SELECT [loginname] FROM [master].[dbo].[syslogins] WHERE [name] = @username)
BEGIN
	SET @command = 'ALTER LOGIN ['+@username+'] WITH PASSWORD = '''+@password+''''
	SELECT @command AS Command
	IF @apply = 1
		EXEC(@command)
END
ELSE
BEGIN
	-- Create property to store unique SID of the server login.
	DECLARE @user_sid varbinary(85)
	-- Get user SID.
	SELECT  @user_sid = dp.sid
	FROM sys.database_principals dp
	WHERE dp.name = @username
	-- Create script to create missing server login.
	SET @command = 
		'-- Create database login.' + @newLine +
		'CREATE LOGIN [' + @username + '] WITH ' + @newLine +
	    'PASSWORD=''' + @password + ''',' + @newLine +
		'DEFAULT_DATABASE=[' + @database +'],' + @newLine +
		'DEFAULT_LANGUAGE=[us_english],' + @newLine +
		'CHECK_EXPIRATION=OFF,' + @newLine +
		'CHECK_POLICY=OFF'
	IF @user_sid IS NOT NULL SET @command = @command + ',' + @newLine +
		'SID='+[master].dbo.fn_varbintohexstr(@user_sid)
     -- Create SQL Server Login.
    SELECT @command AS Command
	IF @apply = 1
		EXEC(@command)
END

---------------------------------------------------------------
-- If SQL Database User doesn't exist then...
---------------------------------------------------------------

IF USER_ID(@username) IS NULL
BEGIN
	-- Create script to create missing database user.
	SET @command = 
	'-- Create database user.' + @newLine +
	' CREATE USER ' + @username + ' FOR LOGIN ' + @username + @newLine +
	'-- Make it owner.'+ @newLine+
	'ALTER ROLE [db_owner] ADD MEMBER ['+@username+']'
    SELECT @command AS Command
	IF @apply = 1
		EXEC(@command)
END

---------------------------------------------------------------
-- Fix Login and user.
---------------------------------------------------------------

IF @apply = 1
BEGIN
	---- Disable login.
	EXEC('ALTER LOGIN ['+@username+'] DISABLE')
	---- Map SQL 'Server Login' to 'Database User'
	EXEC sp_change_users_login 'AUTO_FIX', @username
	---- Enable login.
	EXEC ('ALTER LOGIN ['+@username+'] ENABLE')
END

---- Fix database Owner
SELECT @command = 
	'ALTER AUTHORIZATION ON DATABASE::'+sd.Name+' TO ['+sl.Name+']' 
--	'ALTER AUTHORIZATION ON DATABASE::'+@database+' TO ['+@username+']' 
FROM master..sysdatabases sd
JOIN master..syslogins sl ON sd.[sid] = sl.[sid]
WHERE sd.Name = @database

IF @apply = 1 EXEC(@command)
ELSE PRINT @command

IF @apply = 1 
BEGIN
	-- Check mismatched SID's between Database Users and Server Logins
	SELECT
		dp.name AS DatabaseUser,
		sp.name AS ServerLogin,
		[master].dbo.fn_varbintohexstr(dp.[sid]) AS DatabaseSid,
		[master].dbo.fn_varbintohexstr(spn.[sid]) AS ServerSid,
		(CASE
			WHEN dp.[sid] = spn.[sid] THEN 'OK'
			ELSE 'MISMATCHED' END
		) AS [Status]
	FROM sys.database_principals dp
	LEFT JOIN sys.server_principals sp ON dp.[sid] = sp.[sid]
	LEFT JOIN sys.server_principals spn ON dp.name COLLATE DATABASE_DEFAULT = spn.name COLLATE DATABASE_DEFAULT
	WHERE dp.principal_id > 4 AND dp.type_desc = 'SQL_USER'
END