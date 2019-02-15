
CREATE PROCEDURE [dbo].[Tools_Info_UnmachedUsers]
	
AS

-- EXEC [dbo].[Tools_Info_UnmachedUsers]

---------------------------------------------------------------
-- List unmached database users (Optional)
---------------------------------------------------------------

SELECT dp.type_desc as UserType, dp.name AS DatabaseUser, sp.name AS ServerLogin,
       [master].dbo.fn_varbintohexstr(dp.[sid]) AS DatabaseSid,
       [master].dbo.fn_varbintohexstr(spn.[sid]) AS ServerSid,
       (CASE WHEN dp.[sid] = spn.[sid] THEN 'OK' ELSE 'MISMATCHED' END) AS Status
FROM sys.database_principals dp
LEFT JOIN sys.server_principals sp ON dp.[sid] = sp.[sid]
LEFT JOIN sys.server_principals spn ON dp.name = spn.name COLLATE Latin1_General_CI_AS
WHERE
dp.principal_id > 4 and
dp.type_desc IN ('SQL_USER', 'WINDOWS_USER', 'WINDOWS_GROUP')