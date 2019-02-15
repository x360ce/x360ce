
CREATE PROCEDURE [dbo].[Tools_Find_Obsolete_Indexes]
	
AS
-- http://www.sqlservercentral.com/scripts/Indexing/68374/

-- EXEC [dbo].[Tools_Find_Obsolete_Insexes]

SELECT
	'IF INDEXPROPERTY(OBJECT_ID('''+s.table_name+'''), '''+i.[name]+''', ''IndexId'') IS NOT NULL ' +
	'DROP INDEX ' + s.table_name + '.' + i.[name] AS DropIndexStatement,
	s.table_name AS TableName,
	i.[name] AS IndexName,
	i.[type_desc] AS IndexType,
	s.seeks + s.scans + s.lookups AS TotalAccesses,
	s.seeks AS Seeks,
	s.scans AS Scans,
	s.lookups AS Lookups
FROM (
	SELECT
		i.[object_id],
		OBJECT_NAME(i.[object_id]) AS table_name,
		i.index_id,
		SUM(i.user_seeks) AS seeks,
		SUM(i.user_scans) AS scans,
		SUM(i.user_lookups) AS lookups
	FROM sys.tables t
	INNER JOIN sys.dm_db_index_usage_stats i ON t.[object_id] = i.[object_id]
	GROUP BY i.[object_id], i.index_id
) AS s
INNER JOIN sys.indexes i ON s.[object_id] = i.[object_id] AND s.index_id = i.index_id
WHERE
	s.seeks + s.scans + s.lookups = 0 -- Not being used
	AND i.[type_desc] = 'NONCLUSTERED' -- Only NONCLUSTERED
	AND i.is_primary_key = 0 -- Not a Primary Key
	AND i.is_unique = 0 -- Not a unique index
	AND s.table_name NOT LIKE 'sys%'
ORDER BY s.table_name, i.[name]