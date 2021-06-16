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

DECLARE @instances as TABLE(InstanceGuid uniqueidentifier)

-- Get unique instances.
INSERT INTO @instances
SELECT DISTINCT a.InstanceGuid
FROM @args a
WHERE ISNULL(a.InstanceGuid, '00000000-0000-0000-0000-000000000000') <> '00000000-0000-0000-0000-000000000000'
-- Workaround: make sure to exclude virtual devices which share same instance on all computers.
-- Unique anonymous computer or profile ID must be supplied to this method in order to get these.
AND a.InstanceGuid <> '6f1d2b60-d5a0-11cf-bfc7-444553540000' -- SysMouse
AND a.InstanceGuid <> '6f1d2b61-d5a0-11cf-bfc7-444553540000' -- SysKeyboard
AND a.InstanceGuid <> '6f1d2b70-d5a0-11cf-bfc7-444553540000' -- Joystick

-- Get all device instances of the user.
SELECT s.*
FROM dbo.x360ce_Settings s
INNER JOIN @instances i ON s.InstanceGuid = i.InstanceGuid
ORDER BY s.ProductName, s.[FileName], s.FileProductName

---------------------------------------------------------------

DECLARE @checksums as TABLE(PadSettingChecksum uniqueidentifier)

-- Get uniqe checksums
INSERT INTO @checksums
SELECT DISTINCT s.PadSettingChecksum
FROM dbo.x360ce_Settings s
INNER JOIN @instances i ON s.InstanceGuid = i.InstanceGuid

-- Select records from PadSettings table.
SELECT DISTINCT p.*
FROM @checksums t
INNER JOIN dbo.x360ce_PadSettings p ON t.PadSettingChecksum = p.PadSettingChecksum

END