CREATE PROCEDURE [dbo].[x360ce_Cleanup_Keyboards_and_Mouse]
	@confirm char(3) = null
AS

-- EXEC [dbo].[x360ce_Cleanup_Keyboards_and_Mouse] 'YES'

-- Find records with keyboard and mouse guid.
SELECT COUNT(*) RecordsFoundInSettings
FROM x360ce_Settings s WITH(NOLOCK)
WHERE s.InstanceGuid IN ('6F1D2B61-D5A0-11CF-BFC7-444553540000', '6F1D2B60-D5A0-11CF-BFC7-444553540000')

IF ISNULL(@confirm, '') <> 'YES' BEGIN; SELECT 'Enter ''YES'' as a parameter to run script' AS Info; RETURN ; END;

-- Wipe records.
DELETE s
FROM x360ce_Settings s WITH(NOLOCK)
WHERE s.InstanceGuid IN ('6F1D2B61-D5A0-11CF-BFC7-444553540000', '6F1D2B60-D5A0-11CF-BFC7-444553540000')