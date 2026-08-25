CREATE PROCEDURE [dbo].[Tools_Info_UserDevicesStatistics]
AS

/*
	EXEC [dbo].[Tools_Info_UserDevicesStatistics]
*/

-- Count from last year
DECLARE @now datetime = GETDATE()
DECLARE @from_date datetime = DATEADD(YEAR, -1, @now)
DECLARE @total_days int = DATEDIFF(WEEK, @from_date, @now)

DECLARE @total int

-- Create table to store results.
DECLARE @week AS TABLE([id] int, [week_day] sysname, [hour] int, [count] int, [tpm] decimal(5,1), [percent] decimal(5,4))

-----------------------------------------------------------------
---- Trips per week, hour
-----------------------------------------------------------------

DELETE FROM @week

INSERT INTO @week([id], [week_day], [hour], [count])

SELECT [id], [week_day], [hour], COUNT(*) AS [count]
FROM
(
    SELECT
		-- Monday is first day of week
		DATEDIFF(DAY, '17530101', [DateCreated]) % 7 + 1 AS [id],
		DATENAME(WEEKDAY, [DateCreated]) AS [week_day],
		DATEPART(HOUR, [DateCreated]) AS [hour]
    FROM [x360ce_UserDevices] (NOLOCK)
	WHERE [DateCreated] IS NOT NULL AND [DateCreated] >= @from_date
) dt
GROUP BY [id], [week_day], [hour]
ORDER BY [id], [week_day], [hour]

SELECT @total = SUM([count]) FROM @week

--INSERT INTO @week([id], [week_day], [count])
--SELECT 0, 'TOTAL', @total

UPDATE @week SET
	[percent] = [count] / CAST(@total AS decimal),
	-- Counts per minute.
	[tpm] = [count] * 7 / 60 / CAST(@total_days AS decimal)

DECLARE @stats AS TABLE([Hour] int, Mon decimal(5,1), Tue decimal(5,1), Wed decimal(5,1), Thu decimal(5,1), Fri decimal(5,1), Sat decimal(5,1), Sun decimal(5,1))

INSERT INTO @stats([Hour])
SELECT [hour]
FROM @week
GROUP BY [Hour]

UPDATE s SET Mon = [tpm] FROM @stats s INNER JOIN @week w ON w.[hour] = s.[Hour] WHERE w.id = 1
UPDATE s SET Tue = [tpm] FROM @stats s INNER JOIN @week w ON w.[hour] = s.[Hour] WHERE w.id = 2
UPDATE s SET Wed = [tpm] FROM @stats s INNER JOIN @week w ON w.[hour] = s.[Hour] WHERE w.id = 3
UPDATE s SET Thu = [tpm] FROM @stats s INNER JOIN @week w ON w.[hour] = s.[Hour] WHERE w.id = 4
UPDATE s SET Fri = [tpm] FROM @stats s INNER JOIN @week w ON w.[hour] = s.[Hour] WHERE w.id = 5
UPDATE s SET Sat = [tpm] FROM @stats s INNER JOIN @week w ON w.[hour] = s.[Hour] WHERE w.id = 6
UPDATE s SET Sun = [tpm] FROM @stats s INNER JOIN @week w ON w.[hour] = s.[Hour] WHERE w.id = 7

SELECT * FROM @stats


--SELECT [week_day], [hour], [count], [percent], [tpm]
--FROM @week
--ORDER BY [id], [week_day], [hour]

-----------------------------------------------------------------
---- Counts per week
-----------------------------------------------------------------

SELECT [week_day], SUM([count]) AS [count], SUM([percent]) AS [percent]
FROM @week
GROUP BY [id], [week_day]
ORDER BY [id], [week_day]