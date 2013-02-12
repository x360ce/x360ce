CREATE TRIGGER [dbo].[TR_x360ce_Settings_AfterAction]
   ON  [dbo].[x360ce_Settings]
   AFTER INSERT, DELETE, UPDATE
AS 
BEGIN

	DECLARE @rowsIns int
	SELECT @rowsIns = Count(*) FROM INSERTED
	DECLARE @rowsDel int
	SELECT @rowsDel = Count(*) FROM DELETED

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-----------------------------------------------------------
	-- Insert missing products if they don't exist.	
	-----------------------------------------------------------

	INSERT INTO [dbo].[x360ce_Products] (ProductGuid, ProductName)
	SELECT t2.ProductGuid, t2.ProductName
	FROM (
		SELECT t1.ProductGuid, t1.ProductName
		FROM inserted t1
		LEFT JOIN  [dbo].[x360ce_Products] s ON
			t1.ProductGuid = s.ProductGuid
		WHERE s.ProductName is null) t2
	GROUP BY ProductGuid, ProductName

	-----------------------------------------------------------
	-- Insert missing programs if they don't exist.	
	-----------------------------------------------------------

	INSERT INTO [dbo].[x360ce_Programs] ([FileName], FileProductName)
	SELECT t2.[FileName], t2.FileProductName
	FROM (
		SELECT t1.[FileName], t1.FileProductName
		FROM inserted t1
		LEFT JOIN  [dbo].[x360ce_Programs] s ON
			t1.[FileName] = s.[FileName]
		WHERE s.[FileName] is null) t2
	GROUP BY [FileName], FileProductName

	-----------------------------------------------------------
	-- Create table for which stats must be recalculated.
	-----------------------------------------------------------

	DECLARE @Table as TABLE(
		PadSettingChecksum uniqueidentifier,
		ProductGuid uniqueidentifier,
		[FileName] nvarchar(128),
		FileProductName nvarchar(256)
	)

	INSERT INTO @Table(ProductGuid, [FileName], FileProductName, PadSettingChecksum)
	SELECT ProductGuid, [FileName], FileProductName, PadSettingChecksum
	FROM (
		SELECT * FROM inserted
		UNION
		SELECT * FROM deleted
	) t1
	GROUP BY ProductGuid, [FileName], FileProductName, PadSettingChecksum

	-----------------------------------------------------------
	-- Remove summary records for settings which doesn't exist.
	-----------------------------------------------------------
	
	DELETE s
	FROM [dbo].[x360ce_Summaries] s 
	-- Limit select to updated records only.
	INNER JOIN @Table t0 ON
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

	-----------------------------------------------------------
	-- Insert missing summary records is they don't exist.	
	-----------------------------------------------------------

	INSERT INTO [dbo].[x360ce_Summaries] (ProductGuid, ProductName, [FileName], FileProductName, PadSettingChecksum, Users)
	SELECT t3.ProductGuid, p.ProductName, t3.[FileName], t3.FileProductName, t3.PadSettingChecksum, 1 AS Users
	FROM (
		SELECT t2.ProductGuid, t2.[FileName], t2.FileProductName, t2.PadSettingChecksum
		FROM (
			SELECT t1.ProductGuid, t1.[FileName], t1.FileProductName, t1.PadSettingChecksum
			FROM inserted t1
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
	-----------------------------------------------------------
	-- Update Stats Table
	-----------------------------------------------------------

	UPDATE t1 SET
		t1.[Users] = t3.Users,
		t1.DateUpdated = GETDATE()
	FROM [dbo].[x360ce_Summaries] t1
	INNER JOIN (	
		SELECT ProductGuid, [FileName], FileProductName, PadSettingChecksum, COUNT(*) AS Users FROM (
			SELECT s.* FROM [dbo].[x360ce_Settings] s
			INNER JOIN @Table t1 ON
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