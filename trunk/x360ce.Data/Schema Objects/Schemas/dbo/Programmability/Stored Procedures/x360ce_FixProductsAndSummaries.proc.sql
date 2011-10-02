CREATE PROCEDURE [dbo].[x360ce_FixProductsAndSummaries]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-----------------------------------------------------------
	-- Fix Products Table
	-----------------------------------------------------------

	-- Declare table to store all products used in Settings table.
	DECLARE @Products AS TABLE(ProductGuid uniqueidentifier, ProductName nvarchar(256))

	-- Fill @Products table with all products used in settings table.
	INSERT INTO @Products(ProductGuid, ProductName)
	 	SELECT ProductGuid, ProductName FROM (
		SELECT ProductGuid, ProductName, row_number() OVER (
				-- List of unique columns.
				PARTITION BY  ProductGuid
				-- Order in such way so original columns will end on the list.
				ORDER BY ProductGuid, ProductName
		) AS RowNumber
		FROM [dbo].[x360ce_Settings]
	) t1
	-- Take only first available product description.
	WHERE t1.RowNumber = 1

	-- Update existing rows.
	UPDATE p SET
		p.ProductName = t1.ProductName
	FROM [dbo].[x360ce_Products] p
		INNER JOIN @Products t1 ON
			p.ProductGuid = t1.ProductGuid

	-- Insert Products which doesn't exists.
	INSERT INTO x360ce_Products(ProductGuid, ProductName)
	SELECT t1.ProductGuid, t1.ProductName FROM @Products t1
		LEFT JOIN [dbo].[x360ce_Products] p ON
			p.ProductGuid = t1.ProductGuid
	WHERE p.ProductGuid is NULL
	
	-----------------------------------------------------------
	-- Remove summary records for settings which doesn't exist.
	-----------------------------------------------------------
	
	DELETE s
	FROM [dbo].[x360ce_Summaries] s 
	LEFT JOIN [dbo].[x360ce_Settings] t1 ON
		t1.ProductGuid = s.ProductGuid AND
		t1.[FileName] = s.[FileName] AND
		t1.FileProductName = s.FileProductName AND
		t1.PadSettingChecksum = s.PadSettingChecksum
	Where t1.SettingId IS NULL

	-----------------------------------------------------------
	-- Insert missing summary records.	
	-----------------------------------------------------------

	INSERT INTO [dbo].[x360ce_Summaries] (ProductGuid, ProductName, [FileName], FileProductName, PadSettingChecksum, Users)
	SELECT t3.ProductGuid, p.ProductName, t3.[FileName], t3.FileProductName, t3.PadSettingChecksum, 1 AS Users
	FROM (
		-- Group into Summary record.
		SELECT t2.ProductGuid, t2.[FileName], t2.FileProductName, t2.PadSettingChecksum
		FROM (
			SELECT t1.ProductGuid, t1.[FileName], t1.FileProductName, t1.PadSettingChecksum
			FROM [dbo].[x360ce_Settings] t1
			LEFT JOIN [dbo].[x360ce_Summaries] s ON
				t1.ProductGuid = s.ProductGuid AND
				t1.[FileName] = s.[FileName] AND
				t1.FileProductName = s.FileProductName AND
				t1.PadSettingChecksum = s.PadSettingChecksum
			WHERE s.SummaryId is null) t2
		GROUP BY ProductGuid, [FileName], FileProductName, PadSettingChecksum
	) t3
	-- Left join for ProductName
	LEFT JOIN [dbo].[x360ce_Products] p ON
		t3.ProductGuid = p.ProductGuid

	-----------------------------------------------------------
	-- Update Summary Records
	-----------------------------------------------------------

	UPDATE t1 SET
		t1.[Users] = t3.Users,
		t1.DateUpdated = GETDATE()
	FROM [dbo].[x360ce_Summaries] t1
	INNER JOIN (	
		SELECT ProductGuid, [FileName], FileProductName, PadSettingChecksum, COUNT(*) AS Users FROM (
			SELECT s.* FROM [dbo].[x360ce_Settings] s
			INNER JOIN [dbo].[x360ce_Settings] t1 ON 
				t1.ProductGuid = s.ProductGuid AND
				t1.[FileName] = s.[FileName] AND
				t1.FileProductName = s.FileProductName AND
				t1.PadSettingChecksum = s.PadSettingChecksum
		) t2
		GROUP BY ProductGuid, [FileName], FileProductName, PadSettingChecksum	
	) t3 ON
		t1.ProductGuid = t3.ProductGuid AND
		t1.[FileName] = t3.[FileName] AND
		t1.FileProductName = t3.FileProductName AND
		t1.PadSettingChecksum = t3.PadSettingChecksum

END