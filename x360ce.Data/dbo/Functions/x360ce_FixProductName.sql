CREATE FUNCTION [dbo].[x360ce_FixProductName](
	@ProductName nvarchar(256),
	@OldProductName nvarchar(256)
) RETURNS nvarchar(256)
BEGIN

	-- SELECT [dbo].[x360ce_FixProductName]('Controller (Gamepad F310)', NULL)
	-- SELECT [dbo].[x360ce_FixProductName]('"Gamepad F310"', NULL)

	SET @ProductName = ISNULL(@ProductName, '')
	SET @ProductName = LTRIM(RTRIM(@ProductName))
	SET @ProductName = REPLACE(@ProductName,'?','')
	SET @ProductName = REPLACE(@ProductName,'  ',' ')
	SET @ProductName = REPLACE(@ProductName,'  ',' ')
	SET @ProductName = REPLACE(@ProductName,'  ',' ')
	IF LEN(@ProductName) > 14 AND LEFT(@ProductName, 12) = 'Controller (' AND RIGHT(@ProductName, 1) = ')'
	BEGIN
		SET @ProductName = SUBSTRING(@ProductName, 13, LEN(@ProductName) - 14)
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
	
	-- If old name specified then...
	IF LEN(ISNULL(@OldProductName, '')) > 0
	BEGIN
		-- If old name is ASCII then...
		IF LEN(REPLACE(CAST(@OldProductName AS varchar(256)), '?', '')) = LEN(@OldProductName)
		BEGIN
			-- If new name is not ASCII then...
			IF LEN(REPLACE(CAST(@ProductName AS varchar(256)), '?', '')) <> LEN(@ProductName)
			BEGIN
				-- Keep old name.
				SET @ProductName = @OldProductName
			END
		END
		
		--DECLARE @famous bit
		---- If old name is famous then...
		--IF
		--	LEFT(@OldProductName, 8) = 'Logitech'
		--	OR LEFT(@OldProductName, 9) = 'Microsoft'
		--	OR LEFT(@OldProductName, 3) = 'Wii'
		--BEGIN
		--	-- Keep old name.
		--	SET @ProductName = @OldProductName
		--	SET @famous = 1
		--END
		---- If not famous and old name is smaller than new name but big enough then...
		--IF @famous <> 1 AND (LEN(@OldProductName) < LEN(@ProductName)) AND LEN(@OldProductName) > 16
		--BEGIN
		--	-- Keep old name.
		--	SET @ProductName = @OldProductName
		--END

		-- If not famous and old name is smaller than new name but big enough then...
		IF (LEN(@OldProductName) < LEN(@ProductName)) AND LEN(@OldProductName) > 16
		BEGIN
			-- Keep old name.
			SET @ProductName = @OldProductName
		END

	END
	

    RETURN @ProductName
END