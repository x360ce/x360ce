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
			SELECT * FROM dbo.x360ce_Settings
		) t1
		GROUP BY ProductGuid, ProductName, [FileName], FileProductName, PadSettingChecksum
		-- Execute procedure.
		EXEC [dbo].[x360ce_UpdateProductsTable] @summaries, @summaries
		RETURN 0
	END

	-- Declare table to store product guids and names
	DECLARE @products AS TABLE(ProductGuid uniqueidentifier, ProductName nvarchar(256))

	-- Fill @products table with all products used in settings table.
	INSERT INTO @products(ProductGuid, ProductName)
	 	SELECT ProductGuid, ProductName FROM (
		SELECT ProductGuid, ProductName, row_number() OVER (
				-- List of unique columns.
				PARTITION BY  ProductGuid
				-- Order in such way so original columns will end on the list.
				ORDER BY ProductGuid, ProductName
		) AS RowNumber
		FROM @inserted
	) t1
	-- Take only first available product description.
	WHERE t1.RowNumber = 1

	-- Insert missing records.
	INSERT INTO [dbo].[x360ce_Products] (ProductGuid, ProductName)
	SELECT t2.ProductGuid, t2.ProductName
	FROM (
		SELECT t1.ProductGuid, t1.ProductName
		FROM @products t1
		LEFT JOIN  [dbo].[x360ce_Products] s ON
			t1.ProductGuid = s.ProductGuid
		WHERE s.ProductName is null) t2
	GROUP BY ProductGuid, ProductName

	-- Update count.
	UPDATE xp SET
		xp.InstanceCount = t3.InstanceCount
	FROM [x360ce_Products] xp
	INNER JOIN (
		SELECT t1.ProductGuid, COUNT(*) AS InstanceCount FROM (
			SELECT DISTINCT s.ProductGuid, s.InstanceGuid
			FROM dbo.x360ce_Settings s
			-- Limit select to updated records only.
			INNER JOIN (
				SELECT * FROM @inserted UNION
				SELECT * FROM @deleted
			) p ON
			s.ProductGuid = p.ProductGuid
		) t1
		GROUP BY ProductGuid
	) t3 ON xp.ProductGuid = t3.ProductGuid

END