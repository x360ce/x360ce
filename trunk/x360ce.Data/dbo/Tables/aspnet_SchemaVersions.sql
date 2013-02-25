CREATE TABLE [dbo].[aspnet_SchemaVersions] (
    [Feature]                 NVARCHAR (128) NOT NULL,
    [CompatibleSchemaVersion] NVARCHAR (128) NOT NULL,
    [IsCurrentVersion]        BIT            NOT NULL,
    PRIMARY KEY CLUSTERED ([Feature] ASC, [CompatibleSchemaVersion] ASC)
);

