CREATE PROCEDURE [dbo].[x360ce_CleanupPadSettings]
AS
BEGIN

-- exec [dbo].[x360ce_CleanupPadSettings]

-- Delete settings with no buttons not mapped.
DELETE
-- SELECT *
FROM [x360ce_PadSettings]
WHERE [ButtonA] = '' AND
      [ButtonB] = '' AND
      [ButtonBig] = '' AND
      [ButtonBack] = '' AND
      [ButtonGuide] = '' AND
      [ButtonStart] = '' AND
      [ButtonX] = '' AND
      [ButtonY] = ''
      --[LeftShoulder] = '' AND
      --[LeftThumbButton] = '' AND
      --[RightShoulder] = '' AND
      --[RightThumbButton] = ''


DELETE
-- SELECT *
FROM [x360ce_PadSettings]
WHERE LEFT(CAST([PadSettingChecksum] AS varchar(36)),8) IN
	--Profiles with Passthrough enabled.
	('F93A66CC','14018148','E9F92BE7','B746A761')


END