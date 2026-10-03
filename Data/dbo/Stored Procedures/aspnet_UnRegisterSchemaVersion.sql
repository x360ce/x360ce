CREATE PROCEDURE [dbo].aspnet_UnRegisterSchemaVersion
    @Feature                   nvarchar(128),
    @CompatibleSchemaVersion   nvarchar(128)
AS
BEGIN

	DECLARE @LoweredFeature varchar(128)
	SET @LoweredFeature = LOWER(@Feature)

    DELETE FROM dbo.aspnet_SchemaVersions
    WHERE   Feature = @LoweredFeature AND @CompatibleSchemaVersion = CompatibleSchemaVersion
END