CREATE TABLE [x360ce].[TelemetryGame] (
    [TelemetryGameId]   UNIQUEIDENTIFIER CONSTRAINT [DF_TelemetryGame_TelemetryGameId] DEFAULT (newid()) NOT NULL,
    [FileName]          NVARCHAR (128)   CONSTRAINT [DF_TelemetryGame_FileName] DEFAULT ('') NOT NULL,
    [FileProductName]   NVARCHAR (256)   CONSTRAINT [DF_TelemetryGame_FileProductName] DEFAULT ('') NOT NULL,
    [GameName]          NVARCHAR (256)   CONSTRAINT [DF_TelemetryGame_GameName] DEFAULT ('') NOT NULL,
    [TelemetryFormatId] NVARCHAR (32)    NOT NULL,
    [Edition]           NVARCHAR (64)    CONSTRAINT [DF_TelemetryGame_Edition] DEFAULT ('') NOT NULL,
    [LaunchArguments]   NVARCHAR (256)   CONSTRAINT [DF_TelemetryGame_LaunchArguments] DEFAULT ('') NOT NULL,
    [Comment]           NVARCHAR (1024)  CONSTRAINT [DF_TelemetryGame_Comment] DEFAULT ('') NOT NULL,
    [IsEnabled]         BIT              CONSTRAINT [DF_TelemetryGame_IsEnabled] DEFAULT ((1)) NOT NULL,
    [DateCreated]       DATETIME         CONSTRAINT [DF_TelemetryGame_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]       DATETIME         CONSTRAINT [DF_TelemetryGame_DateUpdated] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_TelemetryGame] PRIMARY KEY CLUSTERED ([TelemetryGameId] ASC),
    CONSTRAINT [FK_TelemetryGame_TelemetryFormat] FOREIGN KEY ([TelemetryFormatId]) REFERENCES [x360ce].[TelemetryFormat] ([TelemetryFormatId])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_TelemetryGame_FileName_FileProductName]
    ON [x360ce].[TelemetryGame]([FileName] ASC, [FileProductName] ASC);
