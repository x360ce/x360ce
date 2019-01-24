CREATE FUNCTION [dbo].[x360ce_GetCompletionPoints](
	@PadSettingChecksum uniqueidentifier,
	@UserDeviceId uniqueidentifier
) RETURNS int
BEGIN

DECLARE
	@maxAxis int = 0,
	@maxButtons int = 0,
	@maxMotors int = 0

	SELECT
		-- Get device info.
		@maxAxis = CapAxeCount,
		@maxButtons = CapButtonCount,
		@maxMotors = DiActuatorCount
	FROM x360ce_UserDevices WITH(NOLOCK)
	WHERE id = @UserDeviceId

	-- Xbox 360 Controller have 6 axis, 14 buttons, 2 motors. Do not allow exceed.
	IF @maxAxis > 6
		SET @maxAxis = 6
	IF @maxButtons > 14
		SET @maxButtons = 14
	IF @maxMotors > 2
		SET @maxMotors = 2

	DECLARE
		@axisPoints int,
		@buttonPoints int,
		@dpadMap varchar(16),
		@dpadPoints int
		

	SELECT
		-- Count axis points (max 100 per axis).
		@axisPoints =
			dbo.x360ce_GetCompletionPointsForAxis(ps.LeftThumbAxisX, LeftThumbLeft, LeftThumbRight) +
			dbo.x360ce_GetCompletionPointsForAxis(ps.LeftThumbAxisY, ps.LeftThumbUp, ps.LeftThumbDown) +
			dbo.x360ce_GetCompletionPointsForAxis(ps.RightThumbAxisX, ps.RightThumbLeft, ps.RightThumbRight) +
			dbo.x360ce_GetCompletionPointsForAxis(ps.RightThumbAxisY, ps.RightThumbUp, ps.RightThumbDown) +
			dbo.x360ce_GetCompletionPointsForAxis(ps.LeftTrigger, '', '') +
			dbo.x360ce_GetCompletionPointsForAxis(ps.RightTrigger, '', ''),
		-- Count button points (max 100 per button).
		@buttonPoints =
			CASE LEN(ps.ButtonA) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.ButtonB) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.ButtonX) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.ButtonY) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.ButtonBack) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.ButtonStart) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.LeftThumbButton) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.RightThumbButton) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.LeftShoulder) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.RightShoulder) WHEN 0 THEN 0 ELSE 100 END,
		-- Count DPad points (400 or max 100 per button).
		@dpadMap = ps.DPad,
		@dpadPoints =
			CASE LEN(ps.DPadUp) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.DPadDown) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.DPadLeft) WHEN 0 THEN 0 ELSE 100 END +
			CASE LEN(ps.DPadRight) WHEN 0 THEN 0 ELSE 100 END
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
		SET @dpadPoints = 400

	/*
	var completion = (int)(100m * (
		(axisPoints + buttonPoints + motorPoints) / (maxAxis + maxButtons + maxMotors)
	));
	*/

    RETURN 0
END