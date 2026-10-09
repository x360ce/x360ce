CREATE PROCEDURE dbo.aspnet_Roles_GetAllRoles (
    @ApplicationName           nvarchar(256))
AS
BEGIN

	DECLARE @LoweredApplicationName  nvarchar(256)
	SET @LoweredApplicationName = LOWER(@ApplicationName)

    DECLARE @ApplicationId uniqueidentifier
    SELECT  @ApplicationId = NULL
    SELECT  @ApplicationId = ApplicationId FROM aspnet_Applications WHERE @LoweredApplicationName = LoweredApplicationName
    IF (@ApplicationId IS NULL)
        RETURN
    SELECT RoleName
    FROM   dbo.aspnet_Roles WHERE ApplicationId = @ApplicationId
    ORDER BY RoleName
END