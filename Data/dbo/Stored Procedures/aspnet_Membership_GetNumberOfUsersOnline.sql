CREATE PROCEDURE dbo.aspnet_Membership_GetNumberOfUsersOnline
    @ApplicationName            nvarchar(256),
    @MinutesSinceLastInActive   int,
    @CurrentTimeUtc             datetime
AS
BEGIN

	DECLARE @LoweredApplicationName  nvarchar(256)
	SET @LoweredApplicationName = LOWER(@ApplicationName)

    DECLARE @DateActive datetime
    SELECT  @DateActive = DATEADD(minute,  -(@MinutesSinceLastInActive), @CurrentTimeUtc)

    DECLARE @NumOnline int
    SELECT  @NumOnline = COUNT(*)
    FROM    dbo.aspnet_Users u WITH(NOLOCK)
    INNER JOIN dbo.aspnet_Applications a WITH(NOLOCK) ON u.ApplicationId = a.ApplicationId
	INNER JOIN dbo.aspnet_Membership m WITH(NOLOCK) ON u.UserId = m.UserId
    WHERE
        LastActivityDate > @DateActive                     AND
        a.LoweredApplicationName = @LoweredApplicationName
            
    RETURN(@NumOnline)
END