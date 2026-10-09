CREATE PROCEDURE dbo.aspnet_Membership_SetPassword
    @ApplicationName  nvarchar(256),
    @UserName         nvarchar(256),
    @NewPassword      nvarchar(128),
    @PasswordSalt     nvarchar(128),
    @CurrentTimeUtc   datetime,
    @PasswordFormat   int = 0
AS
BEGIN
    DECLARE @LoweredApplicationName  nvarchar(256)
	SET @LoweredApplicationName = LOWER(@ApplicationName)

	DECLARE @LoweredUserName  nvarchar(256)
	SET @LoweredUserName = LOWER(@UserName)
	
	DECLARE @UserId uniqueidentifier
    SELECT  @UserId = NULL
    SELECT  @UserId = u.UserId
    FROM    dbo.aspnet_Users u
			INNER JOIN dbo.aspnet_Applications a ON u.ApplicationId = a.ApplicationId
			INNER JOIN  dbo.aspnet_Membership m ON u.UserId = m.UserId
    WHERE   LoweredUserName = @LoweredUserName AND
            @LoweredApplicationName = a.LoweredApplicationName

    IF (@UserId IS NULL)
        RETURN(1)

    UPDATE dbo.aspnet_Membership
    SET [Password] = @NewPassword,
		PasswordFormat = @PasswordFormat,
		PasswordSalt = @PasswordSalt,
		LastPasswordChangedDate = @CurrentTimeUtc
    WHERE @UserId = UserId
    RETURN(0)
END