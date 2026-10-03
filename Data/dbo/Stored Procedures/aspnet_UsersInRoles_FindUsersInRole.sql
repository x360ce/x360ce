CREATE PROCEDURE dbo.aspnet_UsersInRoles_FindUsersInRole
    @ApplicationName  nvarchar(256),
    @RoleName         nvarchar(256),
    @UserNameToMatch  nvarchar(256)
AS
BEGIN

	DECLARE @LoweredRoleName varchar(256)
	SET @LoweredRoleName = LOWER(@RoleName)

	DECLARE @LoweredUserNameToMatch  nvarchar(256)
	SET @LoweredUserNameToMatch = LOWER(@UserNameToMatch)

	DECLARE @LoweredApplicationName  nvarchar(256)
	SET @LoweredApplicationName = LOWER(@ApplicationName)

    DECLARE @ApplicationId uniqueidentifier
    SELECT  @ApplicationId = NULL
    SELECT  @ApplicationId = ApplicationId FROM aspnet_Applications WHERE @LoweredApplicationName = LoweredApplicationName
    IF (@ApplicationId IS NULL)
        RETURN(1)
     DECLARE @RoleId uniqueidentifier
     SELECT  @RoleId = NULL

     SELECT  @RoleId = RoleId
     FROM    dbo.aspnet_Roles
     WHERE   @LoweredRoleName = LoweredRoleName AND ApplicationId = @ApplicationId

     IF (@RoleId IS NULL)
         RETURN(1)

    SELECT u.UserName
    FROM   dbo.aspnet_Users u
	INNER JOIN dbo.aspnet_UsersInRoles ur ON u.UserId = ur.UserId
    WHERE  @RoleId = ur.RoleId AND u.ApplicationId = @ApplicationId AND LoweredUserName LIKE @LoweredUserNameToMatch
    ORDER BY u.UserName
    RETURN(0)
END