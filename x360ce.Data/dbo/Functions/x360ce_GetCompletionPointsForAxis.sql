CREATE FUNCTION [dbo].[x360ce_GetCompletionPointsForAxis](
	@map varchar(16),
	@mapMin varchar(16),
	@mapMax varchar(16)
) RETURNS decimal(18,4)
BEGIN

DECLARE
	@points decimal(18,4) = 0.0,
	@isRange bit = 0,
	@isRangeMin bit = 0,
	@isRangeMax bit = 0

-- Check if axis: Axis = "a", HAxis = "x", Slider = "s", HSlider = "h"
SET @isRange = CASE WHEN @map LIKE '%[nxsh]%' THEN 1 ELSE 0 END
SET @isRangeMin = CASE WHEN @mapMin LIKE '%[nxsh]%' THEN 1 ELSE 0 END
SET @isRangeMax = CASE WHEN @mapMax LIKE '%[nxsh]%' THEN 1 ELSE 0 END

-- For range  map - full points (100 max).
-- For binary map - half points.

-- If range mapped to axis then...
IF @isRange = 1
-- Give 1 point.
	RETURN 1.0

-- If range mapped then give half point for half range.
IF @isRangeMin = 1
	SET @points = @points + 0.50
-- If binary map then give quarter point for half range.
ELSE IF LEN(@mapMin) > 0
	SET @points = @points + 0.25

-- If range mapped then give half point for half range.
IF @isRangeMax = 1
	SET @points = @points + 0.50
-- If binary map then give quarter point for half range.
ELSE IF LEN(@mapMax) > 0
	SET @points = @points + 0.25

-- If just binary mapped to whole axis then...
IF @points = 0.0 AND LEN(@map) > 0
	-- Give quarter points.
	SET @points = 0.25

RETURN @points
END
