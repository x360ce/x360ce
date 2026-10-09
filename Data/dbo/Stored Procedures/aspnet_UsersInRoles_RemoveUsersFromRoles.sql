CREATE PROCEDURE dbo.aspnet_UsersInRoles_RemoveUsersFromRoles
	@ApplicationName  nvarchar(256),
	@UserNames		  nvarchar(4000),
	@RoleNames		  nvarchar(4000)
AS
BEGIN

    DECLARE @LoweredApplicationName  nvarchar(256)
	SET @LoweredApplicationName = LOWER(@ApplicationName)

	DECLARE @AppId uniqueidentifier
	SELECT  @AppId = NULL
	SELECT  @AppId = ApplicationId FROM aspnet_Applications WHERE @LoweredApplicationName = LoweredApplicationName
	IF (@AppId IS NULL)
		RETURN(2)

	DECLARE @TranStarted   bit
	SET @TranStarted = 0

	IF( @@TRANCOUNT = 0 )
	BEGIN
		BEGIN TRANSACTION
		SET @TranStarted = 1
	END

	DECLARE @tbNames  table(Name nvarchar(256) NOT NULL PRIMARY KEY)
	DECLARE @tbRoles  table(RoleId uniqueidentifier NOT NULL PRIMARY KEY)
	DECLARE @tbUsers  table(UserId uniqueidentifier NOT NULL PRIMARY KEY)
	DECLARE @Num	  int
	DECLARE @Pos	  int
	DECLARE @NextPos  int
	DECLARE @Name	  nvarchar(256)
	DECLARE @CountAll int
	DECLARE @CountU	  int
	DECLARE @CountR	  int

	SET @Num = 0
	SET @Pos = 1
	WHILE(@Pos <= LEN(@RoleNames))
	BEGIN
		SELECT @NextPos = CHARINDEX(N',', @RoleNames,  @Pos)
		IF (@NextPos = 0 OR @NextPos IS NULL)
			SELECT @NextPos = LEN(@RoleNames) + 1
		SELECT @Name = RTRIM(LTRIM(SUBSTRING(@RoleNames, @Pos, @NextPos - @Pos)))
		SELECT @Pos = @NextPos+1

		INSERT INTO @tbNames VALUES (@Name)
		SET @Num = @Num + 1
	END

	INSERT INTO @tbRoles
	SELECT RoleId
	FROM   dbo.aspnet_Roles ar
	INNER JOIN @tbNames t ON LOWER(t.Name) = ar.LoweredRoleName
	WHERE  ar.ApplicationId = @AppId

	SELECT @CountR = @@ROWCOUNT

	IF (@CountR <> @Num)
	BEGIN
		SELECT TOP 1 N'', Name
		FROM   @tbNames
		WHERE  LOWER(Name) NOT IN (
			SELECT ar.LoweredRoleName
			FROM dbo.aspnet_Roles ar
			INNER JOIN @tbRoles r ON r.RoleId = ar.RoleId)
		IF( @TranStarted = 1 )
			ROLLBACK TRANSACTION
		RETURN(2)
	END


	DELETE FROM @tbNames WHERE 1=1
	SET @Num = 0
	SET @Pos = 1


	WHILE(@Pos <= LEN(@UserNames))
	BEGIN
		SELECT @NextPos = CHARINDEX(N',', @UserNames,  @Pos)
		IF (@NextPos = 0 OR @NextPos IS NULL)
			SELECT @NextPos = LEN(@UserNames) + 1
		SELECT @Name = RTRIM(LTRIM(SUBSTRING(@UserNames, @Pos, @NextPos - @Pos)))
		SELECT @Pos = @NextPos+1

		INSERT INTO @tbNames VALUES (@Name)
		SET @Num = @Num + 1
	END

	INSERT INTO @tbUsers
	SELECT UserId
	FROM   dbo.aspnet_Users ar
	INNER JOIN @tbNames t ON LOWER(t.Name) = ar.LoweredUserName
	WHERE  ar.ApplicationId = @AppId

	SELECT @CountU = @@ROWCOUNT
	IF (@CountU <> @Num)
	BEGIN
		SELECT TOP 1 Name, N''
		FROM   @tbNames
		WHERE  LOWER(Name) NOT IN (
			SELECT au.LoweredUserName
			FROM dbo.aspnet_Users au
			INNER JOIN @tbUsers u ON u.UserId = au.UserId)
		IF( @TranStarted = 1 )
			ROLLBACK TRANSACTION
		RETURN(1)
	END

	SELECT  @CountAll = COUNT(*)
	FROM	dbo.aspnet_UsersInRoles ur
	INNER JOIN @tbUsers u ON ur.UserId = u.UserId
	INNER JOIN @tbRoles r ON ur.RoleId = r.RoleId

	IF (@CountAll <> @CountU * @CountR)
	BEGIN
		SELECT TOP 1 u.UserName, r.RoleName
		FROM	@tbUsers tu
		CROSS JOIN @tbRoles tr
		INNER JOIN dbo.aspnet_Users u ON u.UserId = tu.UserId
		INNER JOIN dbo.aspnet_Roles r ON r.RoleId = tr.RoleId
		LEFT JOIN aspnet_UsersInRoles ur ON ur.RoleId = tu.UserId AND ur.RoleId = tr.RoleId
		WHERE ur.RoleId IS NULL
		IF( @TranStarted = 1 )
			ROLLBACK TRANSACTION
		--Provider_this_user_already_not_in_role
		RETURN(3)
	END

	DELETE FROM dbo.aspnet_UsersInRoles
	WHERE UserId IN (SELECT UserId FROM @tbUsers)
	  AND RoleId IN (SELECT RoleId FROM @tbRoles)
	IF( @TranStarted = 1 )
		COMMIT TRANSACTION
	RETURN(0)
END