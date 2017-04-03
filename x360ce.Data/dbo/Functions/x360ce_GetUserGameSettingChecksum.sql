CREATE FUNCTION dbo.x360ce_GetUserGameSettingChecksum(@GameId uniqueidentifier) RETURNS uniqueidentifier
BEGIN

	--ALTER TABLE x360ce_Games ADD COLUMN [SettingChecksum] AS (dbo.x360ce_GetGameSettingChecksum([GameId]))

    DECLARE @checksum uniqueidentifier;
	DECLARE @s nvarchar(1024);
	DECLARE @rn nvarchar(2);
	SET @rn = char(13) + char(10);

    SELECT
        @checksum = HashBytes('MD5', 
			(CASE WHEN x.HookMask > 0 THEN 'HookMask=' + CAST(x.HookMask as varchar(16)) + @rn ELSE '' END) +
			(CASE WHEN x.XInputMask > 0 THEN 'XInputMask=' + CAST(x.XInputMask as varchar(16)) + @rn ELSE '' END) +
			(CASE WHEN x.DInputMask > 0 THEN 'DInputMask=' + CAST(x.DInputMask as varchar(16)) + @rn ELSE '' END) +
			(CASE WHEN LEN(x.DInputFile) > 0 THEN 'DInputFile=' + x.DInputFile + @rn ELSE '' END) +
			(CASE WHEN x.FakeVID > 0 THEN 'FakeVID=' + CAST(x.FakeVID as varchar(16)) + @rn ELSE '' END) +
			(CASE WHEN x.FakePID > 0 THEN 'FakePID=' + CAST(x.FakePID as varchar(16)) + @rn ELSE '' END) +
			(CASE WHEN x.Timeout > 0 THEN 'Timeout=' + CAST(x.[Timeout] as varchar(16)) + @rn ELSE '' END)
		)
    FROM
        x360ce_UserGames x
    WHERE
        GameId = @GameId
    RETURN @checksum
END