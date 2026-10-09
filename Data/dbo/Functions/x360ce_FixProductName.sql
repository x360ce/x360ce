CREATE FUNCTION [dbo].[x360ce_FixProductName](
	@ProductName nvarchar(256)
) RETURNS nvarchar(256)
BEGIN

	-- SELECT [dbo].[x360ce_FixProductName]('Controller (Gamepad F310)')
	-- SELECT [dbo].[x360ce_FixProductName]('"Gamepad F310"')
	-- SELECT [dbo].[x360ce_FixProductName]('Xbox 360 Wireless Receiver for Windows')

	SET @ProductName = ISNULL(@ProductName, '')
	SET @ProductName = LTRIM(RTRIM(@ProductName))
	SET @ProductName = REPLACE(@ProductName,'?','')
	SET @ProductName = REPLACE(@ProductName,'  ',' ')
	SET @ProductName = REPLACE(@ProductName,'  ',' ')
	SET @ProductName = REPLACE(@ProductName,'  ',' ')
	IF LEN(@ProductName) > 14 AND LEFT(@ProductName, 12) = 'Controller (' AND RIGHT(@ProductName, 1) = ')'
	BEGIN
		SET @ProductName = SUBSTRING(@ProductName, 13, LEN(@ProductName) - 13)
	END
	SET @ProductName = REPLACE(@ProductName,'(Controller)','')
	SET @ProductName = LTRIM(RTRIM(@ProductName))
	IF LEN(@ProductName) > 2 AND LEFT(@ProductName, 1) = '"' AND RIGHT(@ProductName, 1) = '"'
	BEGIN
		SET @ProductName = SUBSTRING(@ProductName, 2, LEN(@ProductName) - 2)
	END
	IF LEN(@ProductName) > 2 AND LEFT(@ProductName, 1) = '<' AND RIGHT(@ProductName, 1) = '>'
	BEGIN
		SET @ProductName = SUBSTRING(@ProductName, 2, LEN(@ProductName) - 2)
	END
	IF LEN(@ProductName) > 2 AND LEFT(@ProductName, 1) = '[' AND RIGHT(@ProductName, 1) = ']'
	BEGIN
		SET @ProductName = SUBSTRING(@ProductName, 2, LEN(@ProductName) - 2)
	END
	SET @ProductName = LTRIM(RTRIM(@ProductName))

    RETURN @ProductName
END