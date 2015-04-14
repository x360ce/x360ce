CREATE PROCEDURE [dbo].[x360ce_GetNewDeviceStats]
AS
BEGIN

-- exec [dbo].[x360ce_GetNewDeviceStats]

IF OBJECT_ID('tempdb.dbo.#StatsTemp') IS NOT NULL
DROP TABLE #StatsTemp

SELECT
   CAST(
      CAST([Year] AS VARCHAR(4)) +
      RIGHT('0' + CAST([Month] AS VARCHAR(2)), 2) +
      '01' 
   AS DATE) as [Date],
   NewDevices
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

SELECT 
 t1.[Date], t1.NewDevices, 
(	SELECT SUM(NewDevices)
	FROM #StatsTemp t2
	WHERE (t2.[Date] <= t1.[Date])
) AS NewDevicesSum
FROM #StatsTemp t1

END