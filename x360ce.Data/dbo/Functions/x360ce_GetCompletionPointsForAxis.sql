CREATE FUNCTION [dbo].[x360ce_GetCompletionPointsForAxis](
	@axis varchar(16),
	@axisMin varchar(16),
	@axisMax varchar(16)
) RETURNS int
BEGIN

DECLARE
	@points int = 0,
	@isAxis bit = 0,
	@isAxisMin bit = 0,
	@isAxisMax bit = 0

-- Check if axis: Axis = "a", HAxis = "x", Slider = "s", HSlider = "h"
SET @isAxis = CASE WHEN @axis LIKE '%[nxsh]%' THEN 1 ELSE 0 END
SET @isAxisMin = CASE WHEN @axisMin LIKE '%[nxsh]%' THEN 1 ELSE 0 END
SET @isAxisMax = CASE WHEN @axisMax LIKE '%[nxsh]%' THEN 1 ELSE 0 END

-- If proper axis mapped then...
IF @isAxis = 1
-- Give 100 points.
SET @points = 100

-- For half axis map to axis give
IF @isAxisMin = 1
	SET @points = @points + 50
IF @isAxisMax = 1
	SET @points = @points + 50

-- For everything else give 25 points max.
IF LEN(@axis) > 0
BEGIN
	SET @points = @points + 25
END
ELSE
BEGIN
	IF LEN(@axisMin) > 0
		SET @points = @points + 25
	IF LEN(@axisMax) > 0
		SET @points = @points + 25
END

-- Limit points to 100
IF @points > 100
	SET @points = 100

RETURN @points
END