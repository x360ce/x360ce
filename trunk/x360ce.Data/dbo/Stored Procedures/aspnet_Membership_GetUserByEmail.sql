CREATE PROCEDURE dbo.aspnet_Membership_GetUserByEmail
	@ApplicationName  nvarchar(256),
	@Email            nvarchar(256)
AS
BEGIN

	DECLARE @LoweredApplicationName  nvarchar(256)
	SET @LoweredApplicationName = LOWER(@ApplicationName)

	IF( @Email IS NULL )
		SELECT  u.UserName
		FROM    dbo.aspnet_Applications a, dbo.aspnet_Users u, dbo.aspnet_Membership m
		WHERE   @LoweredApplicationName = a.LoweredApplicationName AND
				u.ApplicationId = a.ApplicationId    AND
				u.UserId = m.UserId AND
				m.LoweredEmail IS NULL
	ELSE
		DECLARE @LoweredEmail nvarchar(256)
		SET @LoweredEmail = LOWER(@Email)

		SELECT  u.UserName
		FROM    dbo.aspnet_Applications a, dbo.aspnet_Users u, dbo.aspnet_Membership m
		WHERE   @LoweredApplicationName = a.LoweredApplicationName AND
				u.ApplicationId = a.ApplicationId    AND
				u.UserId = m.UserId AND
				@LoweredEmail = m.LoweredEmail

	IF (@@rowcount = 0)
		RETURN(1)
	RETURN(0)
END