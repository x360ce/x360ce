CREATE PROCEDURE [dbo].[x360ce_GetUserInstances]
	@args dbo.x360ce_SearchParameterTableType READONLY
AS
BEGIN

/*
	DECLARE @table dbo.x360ce_SearchParameterTableType

	-- Create search parameter.
	INSERT INTO @table
	SELECT TOP 10 
		null, s.InstanceGuid, null, null
	FROM dbo.x360ce_Settings s
	GROUP BY s.InstanceGuid

	EXEC dbo.x360ce_GetUserInstances @table

*/

-- Get all device instances of the user.
SELECT s.*
FROM dbo.x360ce_Settings s
WHERE s.InstanceGuid IN (
	SELECT DISTINCT a.InstanceGuid
	FROM @args a
	WHERE
		ISNULL(a.InstanceGuid, '00000000-0000-0000-0000-000000000000') <> '00000000-0000-0000-0000-000000000000'
	)
ORDER BY s.ProductName, s.[FileName], s.FileProductName

END