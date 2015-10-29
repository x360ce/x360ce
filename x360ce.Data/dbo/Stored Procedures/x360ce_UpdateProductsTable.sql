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
	DECLARE @mostPopularNames AS TABLE(ProductGuid uniqueidentifier, ProductName nvarchar(256))

	INSERT INTO @mostPopularNames
	SELECT
		t3.ProductGuid,
		t3.ProductName
	FROM (
		SELECT
			t2.ProductGuid,
			t2.ProductName,
			t2.Users,
			ROW_NUMBER() OVER (PARTITION BY t2.ProductGuid ORDER BY t2.ProductGuid, t2.Users DESC) AS RowNumber
		FROM (
			SELECT
				t1.ProductGuid,
				t1.ProductName,
				SUM(Users) AS Users
			FROM (
				SELECT
					s.ProductGuid,
					dbo.x360ce_FixProductName(ProductName, NULL) AS ProductName,
					SUM(s.Users) AS Users
				FROM x360ce_Summaries s
					-- Limit select to updated records only.
				INNER JOIN (
					SELECT ProductGuid FROM @inserted UNION
					SELECT ProductGuid FROM @deleted
				) p ON
				s.ProductGuid = p.ProductGuid
				GROUP BY s.ProductGuid, s.ProductName
			) t1
			GROUP BY t1.ProductGuid, t1.ProductName
		) t2
	) t3
	WHERE t3.RowNumber = 1

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

	PRINT 'INSERTED: ' + CAST(@@ROWCOUNT as varchar)

	-- Update count.
	UPDATE xp SET
		xp.InstanceCount = t3.InstanceCount
	FROM [x360ce_Products] xp
	INNER JOIN (
		SELECT t1.ProductGuid, COUNT(*) AS InstanceCount FROM (
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

	PRINT 'UPDATED: ' + CAST(@@ROWCOUNT as varchar)
	
END