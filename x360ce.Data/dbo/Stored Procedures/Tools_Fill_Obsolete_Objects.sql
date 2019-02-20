CREATE PROCEDURE [dbo].[Tools_Fill_Obsolete_Objects]
	@age_limit int = 60
AS

-- EXEC [dbo].[Tools_Fill_Obsolete_Objects]
-- EXEC [dbo].[Tools_Fill_Obsolete_Objects] 5

-- If table do not exists then...
IF OBJECT_ID('Tools_Obsolete_Objects', 'U') IS NULL
BEGIN
	-- DROP TABLE [dbo].[Tools_Obsolete_Objects]
	-- DELETE [dbo].[Tools_Obsolete_Objects]
	-- Create table.
	CREATE TABLE [dbo].[Tools_Obsolete_Objects](
		[name] sysname NOT NULL,
		[type] sysname NOT NULL,
		[last_use_date] datetime NULL,
		[create_date] datetime NULL,
		[modify_date] datetime NULL,
		[drop_reason] varchar(100) DEFAULT('') NOT NULL,
		[drop_script] varchar(500) DEFAULT('') NOT NULL,
		[info_created] datetime DEFAULT(GETDATE()) NOT NULL,
		[info_updated] datetime DEFAULT(GETDATE()) NOT NULL
		CONSTRAINT [PK_Tools_Obsolete_Objects] PRIMARY KEY CLUSTERED ([name] ASC)
	) ON [PRIMARY]
END

-- Create table for temp results.
DECLARE @table AS TABLE(
	[name] sysname NOT NULL PRIMARY KEY CLUSTERED,
	[type] sysname NOT NULL,
	[parent_name] sysname DEFAULT('') NOT NULL,
	[child_name] sysname DEFAULT('') NOT NULL,
	[last_use_date] datetime NULL,
	[create_date] datetime NULL,
	[modify_date] datetime NULL,
	[drop_reason] varchar(100) DEFAULT('') NOT NULL,
	[drop_script] varchar(500) DEFAULT('') NOT NULL,
	[info_created] datetime DEFAULT(GETDATE()) NOT NULL
)

----------------------------------------------------------------
-- TABLES
----------------------------------------------------------------

INSERT INTO @table([name], [type], [last_use_date], [create_date], [modify_date])
SELECT
	t.[name],
	'Table' AS [type],
	-- Get last date when table index was used.
	(SELECT MAX(ls) FROM (SELECT last_seek AS ls UNION SELECT last_scan UNION SELECT last_look) lst) AS last_use_date,
	--p.[rows],
	t.create_date,
	t.modify_date
FROM sys.tables AS t
INNER JOIN sys.indexes AS i ON i.[object_id] = t.[object_id] and i.index_id < 2
INNER JOIN sys.partitions AS p ON p.[object_id] = t.[object_id] AND p.index_id=i.index_id
LEFT JOIN (
	-- Select list of used indexes.
	SELECT
		us.[object_id],
		MAX(us.last_user_seek) AS last_seek,
		MAX(us.last_user_scan) AS last_scan,
		MAX(us.last_user_lookup) AS last_look
	FROM sys.dm_db_index_usage_stats us
	GROUP BY us.[object_id]
) t1 ON t1.[object_id] = t.[object_id]
ORDER BY t.[NAME]

----------------------------------------------------------------
-- PROCEDURES
----------------------------------------------------------------

-- Select data into temp table.
INSERT INTO @table([name], [type], [last_use_date], [create_date], [modify_date])
SELECT
	p.[name],
	'Procedure' AS [type],
	s.last_execution_time,
	p.create_date,
	p.modify_date
FROM sys.procedures AS p
LEFT JOIN (
	 SELECT
		ps.[object_id],
		MAX(ps.last_execution_time) AS last_execution_time
	 FROM sys.dm_exec_procedure_stats ps
	 GROUP BY ps.[object_id]
) s ON p.[object_id] = s.[object_id]
ORDER BY p.[name]

----------------------------------------------------------------
-- INDEXES
----------------------------------------------------------------

INSERT INTO @table([name], [parent_name], [child_name], [type], [last_use_date], [create_date], [modify_date])
SELECT
	s.table_name + '.' + i.[name],
	s.table_name AS parent_name,
	i.[name] AS child_name,
	'Index' AS [type],
	(SELECT MAX(ls) FROM (SELECT last_seek AS ls UNION SELECT last_scan UNION SELECT last_look) lst) AS last_use_date,
	-- There is no direct way of finding the creation date of an index.
	-- Use table date.
	s.create_date,
	s.create_date
FROM (
	SELECT
		us.[object_id],
		OBJECT_NAME(us.[object_id]) AS table_name,
		us.index_id,
		MAX(t.create_date) AS create_date,
		MAX(us.last_user_seek) AS last_seek,
		MAX(us.last_user_scan) AS last_scan,
		MAX(us.last_user_lookup) AS last_look
	FROM sys.tables t
	INNER JOIN sys.dm_db_index_usage_stats us ON t.[object_id] = us.[object_id]
	GROUP BY us.[object_id], us.index_id
) AS s
INNER JOIN sys.indexes i ON s.[object_id] = i.[object_id] AND s.index_id = i.index_id
WHERE
	    i.[type_desc] = 'NONCLUSTERED'
	AND i.is_primary_key = 0
	AND i.is_unique = 0
	AND s.table_name NOT LIKE 'sys%'
ORDER BY s.table_name, i.[name]

----------------------------------------------------------------
-- Update [Tools_Obsolete_Objects] table
----------------------------------------------------------------

-- Use server start time i.e. time when server started to gather statistics.
DECLARE @create_date datetime
SELECT @create_date = sqlserver_start_time
-- SELECT sqlserver_start_time
FROM sys.dm_os_sys_info; 

-- Insert new records.
INSERT INTO [Tools_Obsolete_Objects]([name], [type], [info_created], [info_updated])
SELECT t.[name], t.[type], @create_date, @create_date
FROM @table t
LEFT JOIN [Tools_Obsolete_Objects] o ON o.[name] = t.[name] AND o.[type] = t.[type]
-- Object do not exist.
WHERE o.[name] IS NULL

----------------------------------------------------------------

DECLARE @n datetime = GETDATE()
DECLARE @drop_reason varchar(100)
DECLARE @drop_script varchar(500)
DECLARE @age_days int
DECLARE @last_date datetime

-- Update existing records.
UPDATE o SET
	-- Update temp values first.
	@last_date = CASE WHEN t.last_use_date IS NOT NULL THEN t.last_use_date ELSE o.last_use_date END,
	@age_days = DATEDIFF(DAY, ISNULL(@last_date, o.info_created), @n),
	@drop_reason = 
		CASE
			WHEN @age_days > @age_limit AND @last_date IS NULL THEN 'Unused - ' + CAST(@age_days AS sysname) + ' days old'
			WHEN @age_days > @age_limit THEN 'Used ' + CAST(@age_days AS sysname) + ' days ago'
			ELSE ''
		END,
	@drop_script =
		CASE
			WHEN t.[type] = 'Table' THEN 
				'IF OBJECT_ID('''+t.[name]+''', ''U'') IS NOT NULL DROP TABLE ['+t.[name]+']'
			WHEN t.[type] = 'Index' THEN 
				'IF INDEXPROPERTY(OBJECT_ID('''+t.parent_name+'''), '''+t.[child_name]+''', ''IndexId'') IS NOT NULL ' +
				'DROP INDEX ' + t.parent_name + '.' + t.[name]
			WHEN t.[type] = 'Procedure' THEN 
				'IF OBJECT_ID('''+t.[name]+''', ''P'') IS NOT NULL DROP PROCEDURE ['+t.[name]+']'
			ELSE ''
		END,
	-- Update values.
	o.last_use_date = @last_date,
	o.create_date = t.create_date,
	o.modify_date = t.modify_date,
	o.drop_reason = @drop_reason,
	o.drop_script = CASE WHEN @drop_reason <> '' THEN @drop_script ELSE '' END,
	o.info_updated = @n
FROM [Tools_Obsolete_Objects] o
INNER JOIN @table t ON t.[name] = o.[name] AND t.[type] = o.[type]

----------------------------------------------------------------
-- MARK PARTIAL INDEXES
----------------------------------------------------------------

DECLARE @idxColumns AS TABLE([id] int, [index_id] int, [name] sysname NULL, cols varchar(250) NULL, incs varchar(250) NULL)

INSERT INTO @idxColumns([id], [index_id], [name], [cols], [incs])
SELECT
	[object_id] AS id,
	index_id AS indid,
	[name],
	-- Index columns.
	(
		SELECT CASE keyno WHEN 0 THEN NULL ELSE colid END AS [data()]
		FROM sys.sysindexkeys AS k
		WHERE k.id = i.[object_id]
			AND k.indid = i.index_id
		ORDER BY keyno, colid
		FOR XML PATH('')
	) AS cols,
	-- Included columns.
	(
		SELECT CASE keyno WHEN 0 THEN colid ELSE NULL END AS [data()]
		FROM sys.sysindexkeys AS k
		WHERE k.id = i.[object_id]
		and k.indid = i.index_id
		ORDER BY colid
		FOR XML PATH('')
	) AS inc
FROM sys.indexes AS i

--SELECT * FROM @idxColumns

UPDATE o SET
	o.drop_reason = 'Partial Duplicate of ' + pd.[index],
	o.drop_script = 
		'IF INDEXPROPERTY(OBJECT_ID('''+pd.table_name+'''), '''+pd.partial_duplicate_index+''', ''IndexId'') IS NOT NULL ' +
		'DROP INDEX ' + pd.table_name + '.' + pd.[partial_duplicate_index]
FROM [Tools_Obsolete_Objects] o
INNER JOIN (
	SELECT
		OBJECT_NAME(c1.id) AS table_name,
		OBJECT_NAME(c1.id) + '.' + c1.[name] AS [partial_duplicate_index],
		OBJECT_NAME(c1.id) + '.' + c2.[name] AS [index]
	FROM @idxColumns AS c1
	JOIN @idxColumns AS c2 ON c1.id = c2.id AND c1.index_id < c2.index_id
		-- Make sure that index contains all columns
		AND (c1.cols LIKE c2.cols + '%' OR c2.cols LIKE c1.cols + '%')
		-- Make sure that index contains all included keys
		AND (c1.incs LIKE c2.incs + '%' OR c2.incs LIKE c1.incs + '%')
) pd ON pd.partial_duplicate_index = o.[name] AND [type] = 'Index' AND o.drop_reason = ''

----------------------------------------------------------------
-- Results
----------------------------------------------------------------

DECLARE
	@tCount int = 0,
	@pCount int = 0,
	@iCount int = 0

SELECT @tCount = COUNT(*) FROM sys.objects WHERE TYPE = 'U'
SELECT @pCount = COUNT(*) FROM sys.objects WHERE TYPE = 'P'

SELECT @iCount = COUNT(*)
FROM sys.indexes i
LEFT JOIN sys.objects o ON i.[object_id] = o.[object_id]
-- User tables and views only.
WHERE o.[type] IN ('U', 'V')

SELECT
	[type],
	CASE
		WHEN [type] = 'Table' THEN @tCount
		WHEN [type] = 'Index' THEN @iCount
		WHEN [type] = 'Procedure' THEN @pCount
		ELSE 0
	END AS [total],
	COUNT(*) AS obsolete,
	-- Obsolete percent.
	CASE
		WHEN [type] = 'Table'     AND @tCount > 0 THEN 100 * COUNT(*) / @tCount
		WHEN [type] = 'Index'     AND @iCount > 0 THEN 100 * COUNT(*) / @iCount
		WHEN [type] = 'Procedure' AND @pCount > 0 THEN 100 * COUNT(*) / @pCount
		ELSE 0
	END AS [obsolete_percent]
FROM [Tools_Obsolete_Objects]
WHERE drop_reason <> ''
GROUP BY [type]
ORDER BY [type]

SELECT *
FROM [Tools_Obsolete_Objects]
WHERE drop_reason <> ''
ORDER BY [type], [name]