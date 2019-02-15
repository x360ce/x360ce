CREATE PROCEDURE [dbo].[Tools_Info_GetProcessUtilization]
AS

-- EXEC [dbo].[Tools_Info_GetProcessUtilization]
	
-- Get CPU Utilization History for last 30 minutes.
DECLARE @ts_now bigint = (
	SELECT cpu_ticks/(cpu_ticks/ms_ticks)
	FROM sys.dm_os_sys_info
)

-- Get CPU utilization by SQL Server, in one minute increments.
SELECT TOP(30)
	SQLProcessUtilization AS [SQL Server Process CPU Utilization],
	SystemIdle AS [System Idle Process], 
	100 - SystemIdle - SQLProcessUtilization AS [Other Process CPU Utilization], 
	DATEADD(ms, -1 * (@ts_now - [timestamp]), GETDATE()) AS [Event Time] 
FROM ( 
	SELECT
		record.value('(./Record/@id)[1]', 'int') AS record_id, 
		record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'int') AS [SystemIdle], 
		record.value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int') AS [SQLProcessUtilization],
		[timestamp] 
	FROM ( 
		SELECT [timestamp], CONVERT(xml, record) AS [record] 
		FROM sys.dm_os_ring_buffers 
		WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR' 
		AND record LIKE '%<SystemHealth>%'
	) AS x 
) AS y 
ORDER BY record_id DESC