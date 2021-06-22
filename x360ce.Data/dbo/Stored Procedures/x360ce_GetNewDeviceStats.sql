CREATE PROCEDURE [dbo].[x360ce_GetNewDeviceStats]
AS
BEGIN

-- exec [dbo].[x360ce_GetNewDeviceStats]
-- UPDATE dbo.x360ce_NewDeviceStats SET Created = '2019-12-05' 

DECLARE
	@now datetime = GetDate(),
	@oldHours int = 0

SELECT @oldHours = DATEDIFF(HOUR, MAX(Created), @now) FROM dbo.x360ce_NewDeviceStats

-- If last record is more than 12 hours old then...
IF @oldHours > 12
BEGIN

	-- Begin process of updating the table.
	IF OBJECT_ID('tempdb.dbo.#StatsTemp') IS NOT NULL
	DROP TABLE #StatsTemp

	SELECT
	   CAST(
		  CAST([Year] AS VARCHAR(4)) +
		  RIGHT('0' + CAST([Month] AS VARCHAR(2)), 2) +
		  '01' 
	   AS DATE) as [Date],
	   NewDevices, @now as Created
	INTO #StatsTemp
	FROM (
		SELECT [Year], [Month], Count(*) AS NewDevices FROM (
			SELECT
				InstanceGuid,
				MIN(DateCreated) as DateCreated,
				DATEPART(YEAR, MIN(DateCreated)) AS [Year],
				DATEPART(MONTH, MIN(DateCreated)) AS [Month]
			  FROM [x360ce_Settings] WITH (NOLOCK)
			GROUP BY InstanceGuid
		) t1
		GROUP BY [Year], [Month]
	)t2
		ORDER BY [Year], [Month] 

	-- Update existing records.
	UPDATE s SET
		s.NewDevices = t.NewDevices,
		s.Created = @now
	FROM  dbo.x360ce_NewDeviceStats s
	INNER JOIN #StatsTemp t ON t.[Date] = s.[Date]
	WHERE t.NewDevices <> s.NewDevices OR t.Created <> s.Created

	-- Insert missing records.
	INSERT INTO dbo.x360ce_NewDeviceStats([Date], [NewDevices], [Created])
	SELECT
		t.[Date], t.[NewDevices], t.[Created]
	FROM #StatsTemp t
	LEFT JOIN dbo.x360ce_NewDeviceStats s ON t.[Date] = s.[Date]
	WHERE s.[Date] IS NULL

END

SELECT 
 t1.*, 
(	SELECT SUM(NewDevices)
	FROM dbo.x360ce_NewDeviceStats t2
	WHERE (t2.[Date] <= t1.[Date])
) AS NewDevicesSum
FROM dbo.x360ce_NewDeviceStats t1
ORDER BY t1.[Date]

END
