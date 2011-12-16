CREATE PROCEDURE [dbo].[x360ce_GetMostPopularProducts]
AS

SELECT p.ProductName, t2.InstanceCount, t2.ProductGuid FROM (
	SELECT t1.ProductGuid, COUNT(*) AS InstanceCount FROM (
		SELECT DISTINCT ProductGuid, InstanceGuid from x360ce_Settings
	) t1
	GROUP BY ProductGuid
) t2
INNER JOIN x360ce_Products p ON
	t2.ProductGuid = p.ProductGuid
ORDER BY t2.InstanceCount DESC