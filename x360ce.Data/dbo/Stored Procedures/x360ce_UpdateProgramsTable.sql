CREATE PROCEDURE [dbo].[x360ce_UpdateProgramsTable]
	@inserted x360ce_ProgramsTableType READONLY,
	@deleted x360ce_ProgramsTableType READONLY,
	@updateAll bit = 0
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	/*
	-- To update all records run:
	DECLARE @updated as x360ce_ProgramsTableType
	exec [dbo].[x360ce_UpdateProgramsTable] @updated, @updated, 1
	*/

	IF @updateAll = 1
	BEGIN
		-- Declare temp table
		DECLARE @programs as x360ce_ProgramsTableType
		-- Fill table with records.
		INSERT INTO @programs([FileName], FileProductName)
		SELECT  t1.[FileName], t1.FileProductName
		FROM (
			SELECT DISTINCT [FileName], FileProductName
			FROM dbo.x360ce_Settings
		) t1
		GROUP BY t1.[FileName], t1.FileProductName
		-- Execute procedure.
		EXEC [dbo].[x360ce_UpdateProgramsTable] @programs, @programs
		RETURN 0
	END

	-- Delete redundant records.
	DELETE xp
	FROM dbo.x360ce_Programs xp 
	-- Limit select to updated records only.
	INNER JOIN @deleted p ON
		xp.[FileName] = p.[FileName] AND
		xp.FileProductName = p.FileProductName
	-- Join actual table to check if records exist.
	LEFT JOIN dbo.x360ce_Programs t1 ON
		xp.[FileName] = t1.[FileName] AND
		xp.FileProductName = t1.FileProductName
	WHERE t1.[FileName] IS NULL

	PRINT 'DELETED: ' + CAST(@@ROWCOUNT as varchar)

	-- Insert missing records.
	INSERT INTO dbo.x360ce_Programs(p.[FileName], p.FileProductName)
	SELECT p.[FileName], p.FileProductName
	FROM @inserted p
	LEFT JOIN dbo.x360ce_Programs xp ON
		p.[FileName] = xp.[FileName] AND
		p.FileProductName = xp.FileProductName
	WHERE xp.[FileName] IS NULL

	PRINT 'INSERTED: ' + CAST(@@ROWCOUNT as varchar)

	-- Update records.
	UPDATE t1 SET
		t1.InstanceCount = t3.InstanceCount,
		t1.DateUpdated = GETDATE()
	FROM dbo.x360ce_Programs t1
	INNER JOIN (	
		SELECT [FileName], FileProductName, COUNT(*) AS InstanceCount FROM (
			SELECT s.[FileName], s.FileProductName, s.InstanceGuid
			FROM [dbo].[x360ce_Settings] s
			-- Limit select to updated records only.
			INNER JOIN (
				SELECT [FileName], FileProductName FROM @inserted UNION
				SELECT [FileName], FileProductName FROM @deleted
			) p ON
			s.[FileName] = p.[FileName] AND
			s.FileProductName = p.FileProductName
		) t2
		GROUP BY [FileName], FileProductName
	) t3 ON
		t1.[FileName] = t3.[FileName] AND
		t1.FileProductName = t3.FileProductName

	PRINT 'UPDATED: ' + CAST(@@ROWCOUNT as varchar)

END