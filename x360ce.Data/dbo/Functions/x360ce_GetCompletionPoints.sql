CREATE FUNCTION [dbo].[x360ce_GetCompletionPoints](
	@PadSettingChecksum uniqueidentifier,
	@UserDeviceId uniqueidentifier
) RETURNS int
BEGIN

/*
	SELECT dbo.x360ce_GetCompletionPoints('D220021C-EF18-0BF6-E901-E1F386E021BC', 'D6CB59A0-DBEA-11E6-8001-444553540000')
	SELECT * FROM x360ce_PadSettings WHERE PadSettingChecksum = 'D220021C-EF18-0BF6-E901-E1F386E021BC'
	SELECT * FROM x360ce_UserDevices WHERE InstanceGuid = 'D6CB59A0-DBEA-11E6-8001-444553540000'
*/

/*
DECLARE
	@PadSettingChecksum uniqueidentifier = 'D220021C-EF18-0BF6-E901-E1F386E021BC',
	@UserDeviceId uniqueidentifier = 'D6CB59A0-DBEA-11E6-8001-444553540000'
*/

DECLARE
	@maxAxis decimal(18,4) = 6.0,
	@maxButtons decimal(18,4) = 14.0,
	@maxMotors decimal(18,4) = 2.0

	SELECT
		-- Get device info.
		@maxAxis = CAST(CapAxeCount AS decimal(18,4)),
		@maxButtons = CAST(CapButtonCount AS decimal(18,4)),
		@maxMotors = CAST(DiActuatorCount AS decimal(18,4))
	FROM x360ce_UserDevices WITH(NOLOCK)
	WHERE id = @UserDeviceId

	-- Xbox 360 Controller have 6 axis, 14 buttons, 2 motors. Do not allow exceed.
	IF @maxAxis > 6.0
		SET @maxAxis = 6.0
	IF @maxButtons > 14.0
		SET @maxButtons = 14.0
	IF @maxMotors > 2.0
		SET @maxMotors = 2.0

	DECLARE
		@axisPoints decimal(18,4),
		@buttonPoints decimal(18,4),
		@dpadMap varchar(16),
		@dpadPoints decimal(18,4),
		@motorPoints decimal(18,4)

	SELECT
		-- Count axis points (6 max or 1 per axis).
		@axisPoints =
			dbo.x360ce_GetCompletionPointsForAxis(ps.LeftThumbAxisX, LeftThumbLeft, LeftThumbRight) +
			dbo.x360ce_GetCompletionPointsForAxis(ps.LeftThumbAxisY, ps.LeftThumbUp, ps.LeftThumbDown) +
			dbo.x360ce_GetCompletionPointsForAxis(ps.RightThumbAxisX, ps.RightThumbLeft, ps.RightThumbRight) +
			dbo.x360ce_GetCompletionPointsForAxis(ps.RightThumbAxisY, ps.RightThumbUp, ps.RightThumbDown) +
			dbo.x360ce_GetCompletionPointsForAxis(ps.LeftTrigger, '', '') +
			dbo.x360ce_GetCompletionPointsForAxis(ps.RightTrigger, '', ''),
		-- Count button points (10 max or 1 per button).
		@buttonPoints =
			CASE LEN(ps.ButtonA) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.ButtonB) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.ButtonX) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.ButtonY) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.ButtonBack) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.ButtonStart) WHEN 0 THEN 0 ELSE 1 END +
            CASE LEN(ps.ButtonGuide) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.LeftThumbButton) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.RightThumbButton) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.LeftShoulder) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.RightShoulder) WHEN 0 THEN 0 ELSE 1 END,
		-- Count DPad points (4 max or 1 per button).
		@dpadMap = ps.DPad,
		@dpadPoints =
			CASE LEN(ps.DPadUp) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.DPadDown) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.DPadLeft) WHEN 0 THEN 0 ELSE 1 END +
			CASE LEN(ps.DPadRight) WHEN 0 THEN 0 ELSE 1 END,
		-- Count Actuator points (2 max or 1 per actuator).
		@motorPoints =
			CASE LEN(ps.ForceEnable) WHEN 0 THEN 0 ELSE 2 END
	FROM x360ce_PadSettings ps WITH(NOLOCK)
	WHERE PadSettingChecksum = @PadSettingChecksum

	DECLARE
		@isPOV bit = 0,
		@isRangeMin bit = 0,
		@isRangeMax bit = 0

	-- Check if POV mapped: POV = "p"
	SET @isPOV = CASE WHEN @dpadMap LIKE '%[p]%' THEN 1 ELSE 0 END

	-- Bump DPad points to max if POV is mapped.
	IF @isPOV = 1
		SET @dpadPoints = 4.0

	/*
	PRINT '@axisPoints = ' + CAST(@axisPoints as sysname)
	PRINT '@buttonPoints = ' + CAST(@buttonPoints as sysname)
	PRINT '@dpadPoints = ' + CAST(@dpadPoints as sysname)
	PRINT '@motorPoints = ' + CAST(@motorPoints as sysname)
	*/

	-- Return completion percent.
	DECLARE @completion int
	SET @completion = CAST(100.0 * (@axisPoints + @buttonPoints + @dpadPoints + @motorPoints) / (@maxAxis + @maxButtons + @maxMotors) AS int)
    RETURN ISNULL(@completion, -1)
END
