CREATE PROCEDURE dbo.aspnet_Membership_GetUserByEmail
	@ApplicationName  nvarchar(256),
	@Email            nvarchar(256)
AS
BEGIN

	DECLARE @LoweredApplicationName  nvarchar(256)
	SET @LoweredApplicationName = LOWER(@ApplicationName)

	DECLARE @LoweredEmail nvarchar(256)
	SET @LoweredEmail = LOWER(@Email)

	IF( @Email IS NULL )
		SELECT  u.UserName
		FROM    dbo.aspnet_Users u
		INNER JOIN dbo.aspnet_Applications a ON u.ApplicationId = a.ApplicationId
		INNER JOIN dbo.aspnet_Membership m ON u.UserId = m.UserId
		WHERE   @LoweredApplicationName = a.LoweredApplicationName AND
				m.LoweredEmail IS NULL
	ELSE

		SELECT  u.UserName
		FROM    dbo.aspnet_Users u
		INNER JOIN dbo.aspnet_Applications a ON u.ApplicationId = a.ApplicationId
		INNER JOIN dbo.aspnet_Membership m ON u.UserId = m.UserId
		WHERE   @LoweredApplicationName = a.LoweredApplicationName AND
				@LoweredEmail = ISNULL(m.LoweredEmail, '')

	IF (@@rowcount = 0)
		RETURN(1)
	RETURN(0)
END