CREATE PROCEDURE dbo.aspnet_Membership_UnlockUser
    @ApplicationName                         nvarchar(256),
    @UserName                                nvarchar(256)
AS
BEGIN

	DECLARE @LoweredUserName  nvarchar(256)
	SET @LoweredUserName = LOWER(@UserName)

	DECLARE @LoweredApplicationName  nvarchar(256)
	SET @LoweredApplicationName = LOWER(@ApplicationName)

    DECLARE @UserId uniqueidentifier
    SELECT  @UserId = NULL
    SELECT  @UserId = u.UserId
    FROM    dbo.aspnet_Users u
	INNER JOIN dbo.aspnet_Applications a ON a.ApplicationId = u.ApplicationId
	INNER JOIN dbo.aspnet_Membership m ON m.UserId = u.UserId
    WHERE   LoweredUserName = @LoweredUserName AND
            @LoweredApplicationName = a.LoweredApplicationName

    IF ( @UserId IS NULL )
        RETURN 1

    UPDATE dbo.aspnet_Membership
    SET IsLockedOut = 0,
        FailedPasswordAttemptCount = 0,
        FailedPasswordAttemptWindowStart = CONVERT( datetime, '17540101', 112 ),
        FailedPasswordAnswerAttemptCount = 0,
        FailedPasswordAnswerAttemptWindowStart = CONVERT( datetime, '17540101', 112 ),
        LastLockoutDate = CONVERT( datetime, '17540101', 112 )
    WHERE @UserId = UserId

    RETURN 0
END