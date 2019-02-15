
CREATE PROCEDURE [dbo].[Tools_Fill_ProcedureStats]
	
AS

-- EXEC [dbo].[Tools_Fill_ProcedureStats]

/*

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tools_ProcedureStats](
	[name] [sysname] NOT NULL,
	[first_execution_time] [datetime] NULL,
	[last_execution_time] [datetime] NULL,
 CONSTRAINT [PK_Tools_ProcedureStats] PRIMARY KEY CLUSTERED 
(
	[name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

*/

-- Select data into temp table.
DECLARE @table as TABLE([name] sysname, [object_id] int, last_execution_time datetime)

INSERT INTO @table([name], [object_id], last_execution_time)
SELECT p.[name], p.[object_id], s.last_execution_time
FROM sys.procedures AS p
LEFT JOIN (
	 SELECT ps.[object_id], MAX(ps.last_execution_time) AS last_execution_time
	 FROM sys.dm_exec_procedure_stats ps
	 GROUP BY ps.[object_id]
) s ON p.[object_id] = s.[object_id]
LEFT JOIN Tools_ProcedureStats ps ON ps.[name] = p.[name]
ORDER BY p.[name]

-- Insert new records.
INSERT INTO Tools_ProcedureStats([name], first_execution_time)
SELECT t.[name], t.last_execution_time
FROM @table t
LEFT JOIN Tools_ProcedureStats ps ON ps.[name] = t.[name]
-- Procedure not exist in this table
WHERE ps.[name] IS NULL

-- Update existing records.
UPDATE ps SET
	ps.last_execution_time = t.last_execution_time
FROM Tools_ProcedureStats ps
INNER JOIN @table t ON t.[name] = ps.[name]
WHERE t.last_execution_time IS NOT NULL 

--SELECT * FROM @table
SELECT * FROM Tools_ProcedureStats