CREATE PROCEDURE dbo.aspnet_UsersInRoles_GetRolesForUser
	@ApplicationName  nvarchar(256),
	@UserName         nvarchar(256)
AS
BEGIN

	DECLARE @LoweredApplicationName  nvarchar(256)
	SET @LoweredApplicationName = LOWER(@ApplicationName)

	DECLARE @LoweredUserName  nvarchar(256)
	SET @LoweredUserName = LOWER(@UserName)

	DECLARE @ApplicationId uniqueidentifier
	SELECT  @ApplicationId = NULL
	SELECT  @ApplicationId = ApplicationId FROM aspnet_Applications WHERE @LoweredApplicationName = LoweredApplicationName
	IF (@ApplicationId IS NULL)
		RETURN(1)
	DECLARE @UserId uniqueidentifier
	SELECT  @UserId = NULL

	SELECT  @UserId = UserId
	FROM    dbo.aspnet_Users
	WHERE   LoweredUserName = @LoweredUserName AND ApplicationId = @ApplicationId

	IF (@UserId IS NULL)
		RETURN(1)

	SELECT r.RoleName
	FROM   dbo.aspnet_Roles r
		INNER JOIN dbo.aspnet_UsersInRoles ur ON r.RoleId = ur.RoleId
	WHERE  r.ApplicationId = @ApplicationId AND ur.UserId = @UserId
	ORDER BY r.RoleName
	RETURN (0)
END