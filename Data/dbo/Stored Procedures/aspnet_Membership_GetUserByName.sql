CREATE PROCEDURE dbo.aspnet_Membership_GetUserByName
    @ApplicationName      nvarchar(256),
    @UserName             nvarchar(256),
    @CurrentTimeUtc       datetime,
    @UpdateLastActivity   bit = 0
AS
BEGIN

	DECLARE @LoweredUserName  nvarchar(256)
	SET @LoweredUserName = LOWER(@UserName)

	DECLARE @LoweredApplicationName  nvarchar(256)
	SET @LoweredApplicationName = LOWER(@ApplicationName)

    DECLARE @UserId uniqueidentifier

    IF (@UpdateLastActivity = 1)
    BEGIN
        SELECT TOP 1 m.Email, m.PasswordQuestion, m.Comment, m.IsApproved,
                m.CreateDate, m.LastLoginDate, @CurrentTimeUtc, m.LastPasswordChangedDate,
                u.UserId, m.IsLockedOut,m.LastLockoutDate
        FROM    dbo.aspnet_Users u
		INNER JOIN dbo.aspnet_Applications a ON u.ApplicationId = a.ApplicationId
		INNER JOIN dbo.aspnet_Membership m ON u.UserId = m.UserId
        WHERE    @LoweredApplicationName = a.LoweredApplicationName AND
                @LoweredUserName = u.LoweredUserName

        IF (@@ROWCOUNT = 0) -- Username not found
            RETURN -1

        UPDATE   dbo.aspnet_Users
        SET      LastActivityDate = @CurrentTimeUtc
        WHERE    @UserId = UserId
    END
    ELSE
    BEGIN
        SELECT TOP 1 m.Email, m.PasswordQuestion, m.Comment, m.IsApproved,
                m.CreateDate, m.LastLoginDate, u.LastActivityDate, m.LastPasswordChangedDate,
                u.UserId, m.IsLockedOut,m.LastLockoutDate
        FROM    dbo.aspnet_Users u
		INNER JOIN dbo.aspnet_Applications a ON u.ApplicationId = a.ApplicationId
		INNER JOIN dbo.aspnet_Membership m ON u.UserId = m.UserId
        WHERE    @LoweredApplicationName = a.LoweredApplicationName AND
                @LoweredUserName = u.LoweredUserName

        IF (@@ROWCOUNT = 0) -- Username not found
            RETURN -1
    END

    RETURN 0
END