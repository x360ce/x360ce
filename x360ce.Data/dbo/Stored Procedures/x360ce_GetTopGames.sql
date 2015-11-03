CREATE PROCEDURE [dbo].[x360ce_GetTopGames]
	@take int = 20
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	/*
	-- To update all records run:
	exec [dbo].[x360ce_GetTopGames]
	*/

	SELECT TOP(@take)
		ROW_NUMBER() OVER (ORDER BY t1.InstanceCount DESC) AS RowNumber,
		t1.*
	FROM (
		SELECT  p.FileProductName, SUM(InstanceCount) AS InstanceCount
		FROM dbo.x360ce_Programs p
		GROUP BY p.FileProductName
	) t1
	WHERE FileProductName NOT IN ('x360ce.exe', 'X360 Controller Emulator', '')
	ORDER BY t1.InstanceCount DESC

END