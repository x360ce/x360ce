CREATE PROCEDURE [dbo].[x360ce_Tools_UpdateCompletionPoints]
AS

-- EXEC [dbo].[x360ce_Tools_UpdateCompletionPoints]

DECLARE
	-- Define range to update.
	@rangeStart datetime = CAST('1753-01-01' AS datetime),
	@rangeEnd datetime = GetDate(),
	@currentStart datetime,
	@currentEnd datetime,
	-- Will be used for progress report.
	@last bigint = -1,
	@done bigint = 0,
	@total bigint = 0,
	@error sysname = '',
	@reported datetime = GETDATE(),
	@updated bigint = 0

-- Create Index on DateCteated column.
-- CREATE NONCLUSTERED INDEX IX_x360ce_Settings_DateCreated ON dbo.x360ce_Settings (DateCreated) ON [PRIMARY]

SELECT @total = COUNT(*)
FROM x360ce_Settings WITH(NOLOCK)
WHERE DateCreated >= @rangeStart AND DateCreated < @rangeEnd

SELECT 'Total: ' + CAST(@total AS sysname)

WHILE @last <> 0
BEGIN
	SET NOCOUNT ON

	SET @currentStart = ISNULL(@currentEnd, @rangeStart)

	-- Select date to stop.
	SELECT TOP(1000)
		@currentEnd = DateCreated
	FROM x360ce_Settings s WITH(NOLOCK)
	WHERE DateCreated > @currentStart
	ORDER BY DateCreated

	SET @last = @@ROWCOUNT

	DECLARE @completion int

	-- UPDATE the rows in a batch to minimize impact by not locking table for long time.
	UPDATE s SET
		@completion = dbo.x360ce_GetCompletionPoints(s.PadSettingChecksum, s.InstanceGuid),
		s.Completion = @completion
	FROM x360ce_Settings s
	WHERE DateCreated > @currentStart AND DateCreated <= @currentEnd
		-- Do not update same records.
		AND s.Completion <> @completion

	-- Must come directly after update command.
	SET @updated = @updated + @@ROWCOUNT
	SET @done = @done + @last

	-- Report every 5 seconds if processed.
	IF @last > 0 AND DATEDIFF(SECOND, @reported, GETDATE()) > 5
	BEGIN
		SET @reported = GETDATE()
		SET @error =
			'Done: ' + CAST(@done AS sysname) +
			', Updated: ' + CAST(@updated AS sysname) +
			--', Last: ' + CAST(@last AS sysname) +
			', Percent: ' + CAST(ROUND((@done * 100 / CAST(@total AS money)), 2) AS varchar(6)) +
			-- Custom options.
			', Current End: ' +FORMAT(@currentEnd, 'yyyy-MM-dd hh:mm:ss')
		RAISERROR(@error, -1, -1) WITH NOWAIT
	END

	WAITFOR DELAY '00:00:00.500'

	SET NOCOUNT OFF
END

SELECT 'Done: ' + CAST(@done AS sysname)
