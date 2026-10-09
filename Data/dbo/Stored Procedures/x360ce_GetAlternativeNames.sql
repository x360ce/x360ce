CREATE PROCEDURE [dbo].[x360ce_GetAlternativeNames]
AS
BEGIN

-- exec [dbo].[x360ce_GetAlternativeNames]

SELECT TOP 100
	 p.[ProductGuid]
	,p.[ProductName]
	,p.[InstanceCount]
	,(SELECT top 1 t1.ProductName + ' - ' + CAST(t1.AltInstanceCount AS VARCHAR(16)) FROM (
	  SELECT s.ProductName, Count(*) AS AltInstanceCount FROM dbo.x360ce_Settings s
	  WHERE s.ProductGuid = p.ProductGuid
	  AND s.ProductName <> p.ProductName
	  GROUP BY ProductName
	  ) t1
		WHERE t1.AltInstanceCount > p.InstanceCount  
		ORDER BY t1.AltInstanceCount DESC
	  )
	AS AlternatviveName
FROM [dbo].[x360ce_Products] p
ORDER BY p.InstanceCount DESC

END