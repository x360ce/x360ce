CREATE PROCEDURE [dbo].[x360ce_UpdateSummariesTable]
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
	exec [dbo].[x360ce_UpdateSummariesTable] @updated, @updated, 1
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
		EXEC [dbo].[x360ce_UpdateSummariesTable] @summaries, @summaries
		RETURN 0
	END

	-- Delete redundant records.
	DELETE s
	FROM [dbo].[x360ce_Summaries] s 
	-- Limit select to updated records only.
	--INNER JOIN @settings t0 ON
	INNER JOIN @deleted t0 ON
		t0.ProductGuid = s.ProductGuid AND
		t0.[FileName] = s.[FileName] AND
		t0.FileProductName = s.FileProductName AND
		t0.PadSettingChecksum = s.PadSettingChecksum
	-- Join Settings table to check if they exist.
	LEFT JOIN [dbo].[x360ce_Settings] t1 ON
		t1.ProductGuid = s.ProductGuid AND
		t1.[FileName] = s.[FileName] AND
		t1.FileProductName = s.FileProductName AND
		t1.PadSettingChecksum = s.PadSettingChecksum
	WHERE t1.SettingId IS NULL

	PRINT 'DELETED: ' + CAST(@@ROWCOUNT as varchar)

	-- Insert missing records.
	INSERT INTO [dbo].[x360ce_Summaries] (ProductGuid, ProductName, [FileName], FileProductName, PadSettingChecksum, Users)
	SELECT t3.ProductGuid, p.ProductName, t3.[FileName], t3.FileProductName, t3.PadSettingChecksum, 1 AS Users
	FROM (
		SELECT t2.ProductGuid, t2.[FileName], t2.FileProductName, t2.PadSettingChecksum
		FROM (
			SELECT t1.ProductGuid, t1.[FileName], t1.FileProductName, t1.PadSettingChecksum
			FROM @inserted t1
			LEFT JOIN [dbo].[x360ce_Summaries] s ON
				t1.ProductGuid = s.ProductGuid AND
				t1.[FileName] = s.[FileName] AND
				t1.FileProductName = s.FileProductName AND
				t1.PadSettingChecksum = s.PadSettingChecksum
			WHERE s.SummaryId is null) t2
		GROUP BY ProductGuid, [FileName], FileProductName, PadSettingChecksum
	) t3
	LEFT JOIN [dbo].[x360ce_Products] p ON
		t3.ProductGuid = p.ProductGuid

	PRINT 'INSERTED: ' + CAST(@@ROWCOUNT as varchar)

	-- Update records
	UPDATE t1 SET
		t1.[Users] = t3.Users,
		t1.DateUpdated = GETDATE()
	FROM [dbo].[x360ce_Summaries] t1
	INNER JOIN (	
		SELECT ProductGuid, [FileName], FileProductName, PadSettingChecksum, COUNT(*) AS Users FROM (
			SELECT t1.ProductGuid, t1.[FileName], t1.FileProductName, t1.PadSettingChecksum FROM (
				SELECT ProductGuid, [FileName], FileProductName, PadSettingChecksum FROM @inserted UNION
				SELECT ProductGuid, [FileName], FileProductName, PadSettingChecksum FROM @deleted
			) t1
			INNER JOIN [dbo].[x360ce_Settings] s ON
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

	PRINT 'UPDATED: ' + CAST(@@ROWCOUNT as varchar)

END