CREATE PROCEDURE dbo.aspnet_Membership_ChangePasswordQuestionAndAnswer
	@ApplicationName       nvarchar(256),
	@UserName              nvarchar(256),
	@NewPasswordQuestion   nvarchar(256),
	@NewPasswordAnswer     nvarchar(128)
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
	INNER JOIN dbo.aspnet_Membership m ON u.UserId = m.UserId
	INNER JOIN dbo.aspnet_Applications a ON u.ApplicationId = a.ApplicationId
	WHERE   LoweredUserName = @LoweredUserName AND
			@LoweredApplicationName = a.LoweredApplicationName
	IF (@UserId IS NULL)
	BEGIN
		RETURN(1)
	END

	UPDATE dbo.aspnet_Membership
	SET    PasswordQuestion = @NewPasswordQuestion, PasswordAnswer = @NewPasswordAnswer
	WHERE  UserId=@UserId
	RETURN(0)
END