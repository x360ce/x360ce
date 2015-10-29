CREATE PROCEDURE [dbo].[x360ce_GetPresets]
	@ProductGuid uniqueidentifier = null,
	@FileName nvarchar(128)= null,
	@MaxRecords int = 50,
	@MaxPerProductFile int = 2
AS
BEGIN

-- Select most popular presets for Products (Devices)
-- exec [dbo].[x360ce_GetPresets]

-- Select most popular preset for Product (Device)
-- exec [dbo].[x360ce_GetPresets] '00060079-0000-0000-0000-504944564944'

-- Select most popular preset for Product (Device) and File Name
-- exec [dbo].[x360ce_GetPresets] '00060079-0000-0000-0000-504944564944', 'x360ce.exe'

-- Limit maximum number of records.
IF @MaxRecords > 100 SET @MaxRecords = 100
IF @FileName IS NULL SET @MaxPerProductFile = 1

DECLARE @table as TABLE(
	ProductName nvarchar(256),
	VendorName nvarchar(256),
	ProductGuid uniqueidentifier,
	PadSettingChecksum uniqueidentifier,
	Users int)

INSERT INTO @table
SELECT *
FROM (
	SELECT TOP (@MaxRecords)
		p.ProductName,
		v.VendorName,
		t2.ProductGuid,
		t2.PadSettingChecksum,
		t2.Users
	FROM (
		SELECT 
			-- NameRowNumber will be used to pick most popular ProductName/FileName (Devices)
			ROW_NUMBER () OVER (PARTITION BY t1.ProductGuid, t1.[FileName] ORDER BY t1.ProductGuid, t1.[FileName], t1.Users DESC) AS RowNumberT1,
			t1.*
		FROM (
			-- Select most popular settings when FileName was specified.
			SELECT
				s.ProductGuid,
				s.[FileName],
				s.PadSettingChecksum, SUM(Users) AS Users
			FROM x360ce_Summaries s
			WHERE @FileName IS NOT NULL
				-- Filter results by Product GUID.
				AND (@ProductGuid IS NULL OR s.ProductGuid = @ProductGuid)
				-- Filter results by File Name.
				AND @FileName = s.[FileName]
			GROUP BY s.ProductGuid, s.[FileName], s.PadSettingChecksum
			-- Select most popular settings when FileName was not specified.
			UNION
			SELECT
				s.ProductGuid,
				'',
				s.PadSettingChecksum, SUM(Users) AS Users
			FROM x360ce_Summaries s
			WHERE @FileName IS NULL
				-- Filter results by Product GUID.
				AND (@ProductGuid IS NULL OR s.ProductGuid = @ProductGuid)
			GROUP BY s.ProductGuid, s.PadSettingChecksum
		) t1
	) t2
	INNER JOIN x360ce_Products p ON t2.ProductGuid = p.ProductGuid
	INNER JOIN x360ce_Vendors v ON v.VendorId = p.VendorId
	-- Limit to Most popular settings per Product/File.
	WHERE t2.RowNumberT1 <= @MaxPerProductFile
	ORDER BY t2.Users DESC
) t3
ORDER BY ProductName, VendorName

-- Select records from Settings table.
SELECT * FROM @table t

-- Select records from PadSettings table.
SELECT p.* FROM @table t
INNER JOIN dbo.x360ce_PadSettings p ON t.PadSettingChecksum = p.PadSettingChecksum

END