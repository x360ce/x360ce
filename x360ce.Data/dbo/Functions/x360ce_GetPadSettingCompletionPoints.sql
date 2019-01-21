CREATE FUNCTION [dbo].[x360ce_GetPadSettingCompletionPoints](
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

	-- Count axis points (maximum 6 points).
	/*
		var axisPoints = 0m;
		axisPoints += GetAxisMapPoints(ps.LeftThumbAxisX, ps.LeftThumbLeft, ps.LeftThumbRight);
		axisPoints += GetAxisMapPoints(ps.LeftThumbAxisY, ps.LeftThumbUp, ps.LeftThumbDown);
		axisPoints += GetAxisMapPoints(ps.RightThumbAxisX, ps.RightThumbLeft, ps.RightThumbRight);
		axisPoints += GetAxisMapPoints(ps.RightThumbAxisY, ps.RightThumbUp, ps.RightThumbDown);
		axisPoints += GetAxisMapPoints(ps.LeftTrigger);
		axisPoints += GetAxisMapPoints(ps.RightTrigger);
	*/

    RETURN 0
END