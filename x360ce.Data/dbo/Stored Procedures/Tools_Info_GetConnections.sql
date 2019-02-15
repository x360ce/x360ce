
CREATE PROCEDURE [dbo].[Tools_Info_GetConnections]
AS

-- EXEC [dbo].[Tools_Info_GetConnections]
	
SELECT
	s.spid,
	s.login_time,
	s.last_batch,
	DATEDIFF(SECOND, s.last_batch, GETDATE()) AS age,
	s.[status],
	s.[hostname],
	DB_NAME(s.[dbid]) AS [database_name],
	s.[program_name],
	s.[cmd],
	(SELECT [text] FROM master.sys.dm_exec_sql_text(s.[sql_handle])) AS last_sql
FROM master.sys.sysprocesses s WITH(NOLOCK)
WHERE s.[dbid] > 0
	AND s.hostname <> ''
	AND s.[program_name] <> '.Net SqlClient Data Provider'
	AND s.[program_name] <> 'Microsoft SQL Server Management Studio - Query'
	AND s.[program_name] <> 'Microsoft SQL Server Management Studio'
	--AND DB_NAME(dbid) = '<my_database_name>'
	--AND loginame = '<my_application_login>'
ORDER BY
	-- Every time new sql is run, the 'last_batch' timestamp gets updated.
	-- Frozen connections will float to the top of this query. 
	s.last_batch ASC