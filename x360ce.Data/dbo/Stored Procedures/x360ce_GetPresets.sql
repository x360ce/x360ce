CREATE PROCEDURE [dbo].[x360ce_GetPresets]
AS
BEGIN

SELECT
	t3.ProductName,
	t3.PadSettingChecksum,
	 (SELECT SUM(s2.Users) FROM x360ce_Summaries s2 WITH(NOLOCK)
	  WHERE s2.PadSettingChecksum = t3.PadSettingChecksum) AS Users
FROM (
	SELECT
		t2.ProductName,
		t2.PadSettingChecksum,
		-- PadRowNumber will be used to pick most popular Setting.
		ROW_NUMBER () OVER (PARTITION BY t2.PadSettingChecksum ORDER BY t2.PadSettingChecksum, t2.Users DESC) AS PadRowNumber
	FROM (
		SELECT 
			-- NameRowNumber will be used to pick most popular Products (Devices)
			ROW_NUMBER () OVER (PARTITION BY t1.ProductName ORDER BY t1.ProductName, t1.Users DESC) AS NameRowNumber,
			t1.*
		FROM (
			-- Select most popular Products (Devices) and PAD Settings. 
			SELECT p.ProductName, s.PadSettingChecksum, SUM(Users) AS Users FROM x360ce_Summaries s
			INNER JOIN x360ce_Products p ON s.ProductGuid = p.ProductGuid
			GROUP BY p.ProductName, s.PadSettingChecksum
		) t1
	) t2
	-- Pick only most popular Product (Device).
	WHERE t2.NameRowNumber < 2 AND t2.Users > 100
) t3
-- Pick only most popular setting.
WHERE t3.PadRowNumber < 2
ORDER BY ProductName

END