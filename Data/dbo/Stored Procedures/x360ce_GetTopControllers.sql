CREATE PROCEDURE [dbo].[x360ce_GetTopControllers]
	@take int = 20
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	/*
	-- To update all records run:
	exec [dbo].[x360ce_GetTopControllers]
	*/

	SELECT TOP(@take)
		ROW_NUMBER() OVER (ORDER BY t1.InstanceCount DESC) AS RowNumber,
		t1.*
	FROM (
		SELECT  p.ProductName, SUM(InstanceCount) AS InstanceCount
		FROM dbo.x360ce_Products p (NOLOCK)
		GROUP BY p.ProductName
	) t1
	ORDER BY t1.InstanceCount DESC

END