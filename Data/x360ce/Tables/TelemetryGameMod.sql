CREATE TABLE [x360ce].[TelemetryGameMod] (
    [TelemetryGameModId] UNIQUEIDENTIFIER CONSTRAINT [DF_TelemetryGameMod_TelemetryGameModId] DEFAULT (newid()) NOT NULL,
    [TelemetryGameId]    UNIQUEIDENTIFIER NOT NULL,
    [ModOrder]           INT              CONSTRAINT [DF_TelemetryGameMod_ModOrder] DEFAULT ((0)) NOT NULL,
    [Name]               NVARCHAR (128)   CONSTRAINT [DF_TelemetryGameMod_Name] DEFAULT ('') NOT NULL,
    [DetectFile]         NVARCHAR (256)   CONSTRAINT [DF_TelemetryGameMod_DetectFile] DEFAULT ('') NOT NULL,
    [DetectProduct]      NVARCHAR (128)   CONSTRAINT [DF_TelemetryGameMod_DetectProduct] DEFAULT ('') NOT NULL,
    [Url]                NVARCHAR (512)   CONSTRAINT [DF_TelemetryGameMod_Url] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK_TelemetryGameMod] PRIMARY KEY CLUSTERED ([TelemetryGameModId] ASC),
    CONSTRAINT [FK_TelemetryGameMod_TelemetryGame] FOREIGN KEY ([TelemetryGameId]) REFERENCES [x360ce].[TelemetryGame] ([TelemetryGameId]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_TelemetryGameMod_TelemetryGameId_ModOrder]
    ON [x360ce].[TelemetryGameMod]([TelemetryGameId] ASC, [ModOrder] ASC);
