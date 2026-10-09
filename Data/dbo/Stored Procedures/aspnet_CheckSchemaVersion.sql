CREATE PROCEDURE [dbo].aspnet_CheckSchemaVersion
    @Feature                   nvarchar(128),
    @CompatibleSchemaVersion   nvarchar(128)
AS
BEGIN

	DECLARE @LoweredFeature varchar(128)
	SET @LoweredFeature = LOWER(@Feature)

    IF (EXISTS( SELECT  *
                FROM    dbo.aspnet_SchemaVersions
                WHERE   Feature = @LoweredFeature AND
                        CompatibleSchemaVersion = @CompatibleSchemaVersion ))
        RETURN 0

    RETURN 1
END