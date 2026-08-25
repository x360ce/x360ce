CREATE PROCEDURE [dbo].[x360ce_Cleanup_Settings_NoPadSettings]
	@confirm char(3) = null
AS

-- EXEC [dbo].[x360ce_Cleanup_Settings_NoPadSettings] 'YES'

-- Find records.
SELECT COUNT(*) RecordsFoundInSettings FROM x360ce_Settings s WITH(NOLOCK)
LEFT JOIN x360ce_PadSettings ps WITH(NOLOCK) ON ps.PadSettingChecksum = s.PadSettingChecksum
WHERE ps.PadSettingChecksum IS NULL

-- Find records.
SELECT COUNT(*) RecordsFoundInSummaries FROM x360ce_Summaries s WITH(NOLOCK)
LEFT JOIN x360ce_PadSettings ps WITH(NOLOCK) ON ps.PadSettingChecksum = s.PadSettingChecksum
WHERE ps.PadSettingChecksum IS NULL

IF ISNULL(@confirm, '') <> 'YES' BEGIN; SELECT 'Enter ''YES'' as a parameter to run script' AS Info; RETURN ; END;

-- Wipe records.
DELETE s FROM x360ce_Settings s WITH(NOLOCK)
LEFT JOIN x360ce_PadSettings ps WITH(NOLOCK) ON ps.PadSettingChecksum = s.PadSettingChecksum
WHERE ps.PadSettingChecksum IS NULL

-- Wipe records.
DELETE s FROM x360ce_Summaries s WITH(NOLOCK)
LEFT JOIN x360ce_PadSettings ps WITH(NOLOCK) ON ps.PadSettingChecksum = s.PadSettingChecksum
WHERE ps.PadSettingChecksum IS NULL