CREATE PROCEDURE [dbo].[x360ce_UpdateProductsTable]
	@inserted x360ce_SummariesTableType READONLY,
	@deleted x360ce_SummariesTableType READONLY,
	@updateAll bit = 0
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	/*
	-- To update all records run:
	DECLARE @updated as x360ce_SummariesTableType
	exec [dbo].[x360ce_UpdateProductsTable] @updated, @updated, 1
	*/

	IF @updateAll = 1
	BEGIN
		-- Declare temp table
		DECLARE @summaries AS x360ce_SummariesTableType
		-- Fill table with records.
		INSERT INTO @summaries(ProductGuid, ProductName, [FileName], FileProductName, PadSettingChecksum)
		SELECT ProductGuid, ProductName, [FileName], FileProductName, PadSettingChecksum
		FROM (
			SELECT ProductGuid, ProductName, [FileName], FileProductName, PadSettingChecksum FROM dbo.x360ce_Settings
		) t1
		GROUP BY ProductGuid, ProductName, [FileName], FileProductName, PadSettingChecksum
		-- Execute procedure.
		EXEC [dbo].[x360ce_UpdateProductsTable] @summaries, @summaries
		RETURN 0
	END

	-- Declare table to store product guids and names
	DECLARE @products AS TABLE(ProductGuid uniqueidentifier, ProductName nvarchar(256) NOT NULL, NameUsers int, RowNumber int NOT NULL)

	INSERT INTO @products
	SELECT
		t2.ProductGuid,
		t2.ProductName,
		t2.NameUsers,
		-- Most popular ProductGuid/ProductName record will have highest number of users and RowNumber = 1.
		CAST(ROW_NUMBER() OVER (PARTITION BY t2.ProductGuid ORDER BY t2.ProductGuid, t2.NameUsers DESC) AS INT) AS RowNumber
	FROM (
		-- Select again to recount records with fixed products names.
		SELECT
			t1.ProductGuid,
			t1.ProductName,
			SUM(NameUsers) AS NameUsers
		FROM (
			-- Select ProductGuid and ProductName combination
			SELECT
				s.ProductGuid,
				-- Fix product name.
				dbo.x360ce_FixProductName(ProductName) AS ProductName,
				-- Count users who use ProductGuid/ProductName combination.
				COUNT(*) AS NameUsers
			FROM x360ce_Settings s
			-- Limit select to updated records only.
			INNER JOIN (
				SELECT ProductGuid FROM @inserted UNION
				SELECT ProductGuid FROM @deleted
			) p ON
			s.ProductGuid = p.ProductGuid
			-- Group records by ProductGuid, ProductName
			GROUP BY s.ProductGuid, s.ProductName
		) t1
		GROUP BY t1.ProductGuid, t1.ProductName
	) t2

	-- Declare table to store product guids and names
	DECLARE @mostPopularNames AS TABLE(ProductGuid uniqueidentifier, ProductName nvarchar(256) NOT NULL)

	INSERT INTO @mostPopularNames
	SELECT
		t3.ProductGuid,
		t3.ProductName
	FROM @products t3
	-- Use only most popular name.
	WHERE t3.RowNumber = 1

	-- Update empty names with some value.
	UPDATE pn
		SET pn.ProductName = p.ProductName
	FROM @mostPopularNames pn
	INNER JOIN @products p ON pn.ProductGuid = p.ProductGuid
	WHERE pn.ProductName = '' AND p.ProductName <> ''

	-- Insert missing records.
	INSERT INTO [dbo].[x360ce_Products] (ProductGuid, ProductName)
	SELECT t2.ProductGuid, t2.ProductName
	FROM (
		SELECT pu.ProductGuid, pu.ProductName
		FROM @mostPopularNames pu
		LEFT JOIN  [dbo].[x360ce_Products] p ON	pu.ProductGuid = p.ProductGuid
		WHERE p.ProductGuid IS NULL
	) t2
	GROUP BY t2.ProductGuid, t2.ProductName

	-- Update Product records with better product name value.
	UPDATE p SET
		p.ProductName = pu.ProductName
	FROM [x360ce_Products] p
	INNER JOIN @mostPopularNames pu ON pu.ProductGuid = p.ProductGuid
	WHERE p.ProductName <> pu.ProductName

	--PRINT 'INSERTED: ' + CAST(@@ROWCOUNT as varchar)

	-- Update count.
	UPDATE xp SET
		xp.InstanceCount = t3.InstanceCount
	FROM [x360ce_Products] xp
	INNER JOIN (
		SELECT t1.ProductGuid, COUNT(*) AS InstanceCount FROM (
			-- Select Unique ProductGuid and InstanceGuid combination.
			SELECT DISTINCT s.ProductGuid, s.InstanceGuid
			FROM dbo.x360ce_Settings s WITH(NOLOCK)
			-- Limit select to updated records only.
			INNER JOIN (
				SELECT ProductGuid FROM @inserted UNION
				SELECT ProductGuid FROM @deleted
			) p ON
			s.ProductGuid = p.ProductGuid
		) t1
		GROUP BY ProductGuid
	) t3 ON xp.ProductGuid = t3.ProductGuid

	--PRINT 'UPDATED: ' + CAST(@@ROWCOUNT as varchar)
	
END