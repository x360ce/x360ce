CREATE TABLE [x360ce].[TelemetryGameFile] (
    [TelemetryGameFileId] UNIQUEIDENTIFIER CONSTRAINT [DF_TelemetryGameFile_TelemetryGameFileId] DEFAULT (newid()) NOT NULL,
    [TelemetryGameId]     UNIQUEIDENTIFIER NOT NULL,
    [ResourceName]        NVARCHAR (256)   CONSTRAINT [DF_TelemetryGameFile_ResourceName] DEFAULT ('') NOT NULL,
    [TargetPath]          NVARCHAR (256)   CONSTRAINT [DF_TelemetryGameFile_TargetPath] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK_TelemetryGameFile] PRIMARY KEY CLUSTERED ([TelemetryGameFileId] ASC),
    CONSTRAINT [FK_TelemetryGameFile_TelemetryGame] FOREIGN KEY ([TelemetryGameId]) REFERENCES [x360ce].[TelemetryGame] ([TelemetryGameId]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_TelemetryGameFile_TelemetryGameId_TargetPath]
    ON [x360ce].[TelemetryGameFile]([TelemetryGameId] ASC, [TargetPath] ASC);
