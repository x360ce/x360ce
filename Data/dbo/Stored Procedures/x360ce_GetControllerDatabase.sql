CREATE PROCEDURE [dbo].[x360ce_GetControllerDatabase]
AS
BEGIN
	-- x360ce's controller database: for each controller (product GUID), the three settings most people use with it, and
	-- those settings. The server writes it as gamecontrollerdb_x360ce.windows.txt (X360ceDatabase.Write) at most once a
	-- day; the writer keeps each controller's most used setting that sets something.
	SET NOCOUNT ON;

	/*
	EXEC [dbo].[x360ce_GetControllerDatabase]
	*/

	DECLARE @table as TABLE(
		ProductGuid uniqueidentifier,
		ProductName nvarchar(256),
		PadSettingChecksum uniqueidentifier,
		Users int
	)

	INSERT INTO @table
	SELECT t2.ProductGuid, ISNULL(p.ProductName, t2.ProductName), t2.PadSettingChecksum, t2.Users
	FROM (
		SELECT
			ROW_NUMBER() OVER (PARTITION BY t1.ProductGuid ORDER BY t1.Users DESC, t1.PadSettingChecksum) AS RowNumber,
			t1.*
		FROM (
			SELECT s.ProductGuid, MAX(s.ProductName) AS ProductName, s.PadSettingChecksum, SUM(s.Users) AS Users
			FROM dbo.x360ce_Summaries s WITH(NOLOCK)
			-- Only a setting that can be loaded.
			INNER JOIN dbo.x360ce_PadSettings ps WITH(NOLOCK) ON ps.PadSettingChecksum = s.PadSettingChecksum
			GROUP BY s.ProductGuid, s.PadSettingChecksum
		) t1
	) t2
	LEFT JOIN dbo.x360ce_Products p WITH(NOLOCK) ON p.ProductGuid = t2.ProductGuid
	WHERE t2.RowNumber <= 3

	-- Controllers.
	SELECT * FROM @table t ORDER BY t.Users DESC

	-- Their settings.
	SELECT DISTINCT ps.* FROM @table t
	INNER JOIN dbo.x360ce_PadSettings ps ON ps.PadSettingChecksum = t.PadSettingChecksum

END