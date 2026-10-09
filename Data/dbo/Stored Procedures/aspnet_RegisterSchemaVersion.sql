CREATE PROCEDURE [dbo].aspnet_RegisterSchemaVersion
    @Feature                   nvarchar(128),
    @CompatibleSchemaVersion   nvarchar(128),
    @IsCurrentVersion          bit,
    @RemoveIncompatibleSchema  bit
AS
BEGIN

	DECLARE @LoweredFeature nvarchar(128)
	SET     @LoweredFeature = LOWER(@Feature)

    IF( @RemoveIncompatibleSchema = 1 )
    BEGIN
        DELETE FROM dbo.aspnet_SchemaVersions WHERE Feature = @LoweredFeature
    END
    ELSE
    BEGIN
        IF(@IsCurrentVersion = 1)
        BEGIN
            UPDATE dbo.aspnet_SchemaVersions
            SET IsCurrentVersion = 0
            WHERE Feature = @LoweredFeature
        END
    END

    INSERT  dbo.aspnet_SchemaVersions(Feature, CompatibleSchemaVersion, IsCurrentVersion)
    VALUES(@LoweredFeature, @CompatibleSchemaVersion, @IsCurrentVersion )
END