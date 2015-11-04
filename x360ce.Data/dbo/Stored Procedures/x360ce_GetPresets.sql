CREATE PROCEDURE [dbo].[x360ce_GetPresets]
	@args dbo.x360ce_SearchParameterTableType READONLY,
	@MaxRecords int = 50,
	@MaxPerProduct int = 2,
	@MaxPerProductFile int = 2
AS
BEGIN

/*
	DECLARE @args dbo.x360ce_SearchParameterTableType
	
	INSERT INTO @args
	SELECT '00060079-0000-0000-0000-504944564944', null, null, null UNION
	SELECT '00060079-0000-0000-0000-504944564944', null, 'x360ce.exe', null UNION
	SELECT '00060079-0000-0000-0000-504944564944', null, '', null
	
	EXEC [dbo].[x360ce_GetPresets] @args
*/

-- Limit maximum number of records.
IF @MaxRecords > 100 SET @MaxRecords = 100


DECLARE @table as TABLE(
	ProductName nvarchar(256),
	VendorName nvarchar(256),
	ProductGuid uniqueidentifier,
	[FileName] nvarchar(256),
	PadSettingChecksum uniqueidentifier,
	Users int,
	ProductId int,
	VendorId int,
	RowNumberT2 int
)

INSERT INTO @table
SELECT *
FROM (
	SELECT
		p.ProductName,
		v.VendorName,
		t2.ProductGuid,
		t2.[FileName],
		t2.PadSettingChecksum,
		t2.Users,
		p.ProductId,	
		v.VendorId,
		ROW_NUMBER () OVER (PARTITION BY p.ProductName, v.VendorName, t2.PadSettingChecksum ORDER BY p.ProductName, v.VendorName, t2.PadSettingChecksum, t2.Users DESC) AS RowNumberT2
	FROM (
		SELECT *
		FROM (
			SELECT
				-- NameRowNumber will be used to pick most popular ProductName/FileName (Devices)
				ROW_NUMBER () OVER (PARTITION BY tpf.ProductGuid, tpf.[FileName] ORDER BY tpf.ProductGuid, tpf.[FileName], tpf.Users DESC) AS RowNumberPF,
				tpf.*
			FROM (
				-- Select most popular settings for product and file.
				SELECT DISTINCT
					s.ProductGuid,
					s.[FileName],
					s.PadSettingChecksum, SUM(Users) AS Users
				FROM x360ce_Summaries s
				INNER JOIN @args a ON
					-- Filter results by Product GUID.
					a.ProductGuid = s.ProductGuid
					-- Filter results by File Name.
					AND a.[FileName] = s.[FileName]
				GROUP BY s.ProductGuid, s.[FileName], s.PadSettingChecksum
			) tpf
		) tpf2
		WHERE tpf2.RowNumberPF <= @MaxPerProductFile
		UNION
		SELECT *
		FROM (
			SELECT
				-- NameRowNumber will be used to pick most popular Product (Devices)
				ROW_NUMBER () OVER (PARTITION BY tp.ProductGuid ORDER BY tp.ProductGuid, tp.Users DESC) AS RowNumberP,
				tp.*
			FROM (
				-- Select most popular settings for product and file.
				SELECT DISTINCT
					s.ProductGuid,
					NULL AS [FileName],
					s.PadSettingChecksum, SUM(Users) AS Users
				FROM x360ce_Summaries s
				INNER JOIN @args a ON
					-- Filter results by Product GUID.
					a.ProductGuid = s.ProductGuid
				GROUP BY s.ProductGuid, s.PadSettingChecksum
			) tp
		) tp2
		WHERE tp2.RowNumberP <= @MaxPerProduct
	) t2
	INNER JOIN x360ce_Products p ON t2.ProductGuid = p.ProductGuid
	INNER JOIN x360ce_Vendors v ON v.VendorId = p.VendorId
	--ORDER BY t2.Users DESC
) t3
ORDER BY ProductName, VendorName

-- Select records from Settings table.
SELECT * FROM @table t
-- Exclude duplicated names where PAD Setting checksum is the same.
WHERE RowNumberT2 = 1

-- Select records from PadSettings table.
SELECT DISTINCT p.* FROM @table t
INNER JOIN dbo.x360ce_PadSettings p ON t.PadSettingChecksum = p.PadSettingChecksum

END