-- Telemetry formats, and the games x360ce helps send telemetry to its Wheel Telemetry plugin, in the [x360ce] schema.
-- Safe to run again: each object is made only when it is missing.
-- Run: sqlcmd -S localhost -E -C -f 65001 -d x360ce -b -i "20261122-01-Telemetry.sql"
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF SCHEMA_ID(N'x360ce') IS NULL
    EXECUTE (N'CREATE SCHEMA [x360ce] AUTHORIZATION [dbo];');
GO
IF OBJECT_ID(N'[x360ce].[TelemetryFormat]', N'U') IS NULL
CREATE TABLE [x360ce].[TelemetryFormat] (
    [TelemetryFormatId] NVARCHAR (32)   NOT NULL,
    [Name]              NVARCHAR (128)  CONSTRAINT [DF_TelemetryFormat_Name] DEFAULT ('') NOT NULL,
    [Publisher]         NVARCHAR (256)  CONSTRAINT [DF_TelemetryFormat_Publisher] DEFAULT ('') NOT NULL,
    [Transport]         NVARCHAR (32)   CONSTRAINT [DF_TelemetryFormat_Transport] DEFAULT ('') NOT NULL,
    [DefaultPort]       INT             CONSTRAINT [DF_TelemetryFormat_DefaultPort] DEFAULT ((0)) NOT NULL,
    [MapName]           NVARCHAR (128)  CONSTRAINT [DF_TelemetryFormat_MapName] DEFAULT ('') NOT NULL,
    [PacketSizes]       NVARCHAR (256)  CONSTRAINT [DF_TelemetryFormat_PacketSizes] DEFAULT ('') NOT NULL,
    [Comment]           NVARCHAR (1024) CONSTRAINT [DF_TelemetryFormat_Comment] DEFAULT ('') NOT NULL,
    [IsEnabled]         BIT             CONSTRAINT [DF_TelemetryFormat_IsEnabled] DEFAULT ((1)) NOT NULL,
    [DateCreated]       DATETIME        CONSTRAINT [DF_TelemetryFormat_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]       DATETIME        CONSTRAINT [DF_TelemetryFormat_DateUpdated] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_TelemetryFormat] PRIMARY KEY CLUSTERED ([TelemetryFormatId] ASC)
);
GO
IF OBJECT_ID(N'[x360ce].[TelemetryFormatSpec]', N'U') IS NULL
CREATE TABLE [x360ce].[TelemetryFormatSpec] (
    [TelemetryFormatSpecId] UNIQUEIDENTIFIER CONSTRAINT [DF_TelemetryFormatSpec_TelemetryFormatSpecId] DEFAULT (newid()) NOT NULL,
    [TelemetryFormatId]     NVARCHAR (32)    NOT NULL,
    [SpecOrder]             INT              CONSTRAINT [DF_TelemetryFormatSpec_SpecOrder] DEFAULT ((0)) NOT NULL,
    [Url]                   NVARCHAR (512)   CONSTRAINT [DF_TelemetryFormatSpec_Url] DEFAULT ('') NOT NULL,
    [Version]               NVARCHAR (128)   CONSTRAINT [DF_TelemetryFormatSpec_Version] DEFAULT ('') NOT NULL,
    [SpecDate]              DATE             NULL,
    [Note]                  NVARCHAR (1024)  CONSTRAINT [DF_TelemetryFormatSpec_Note] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK_TelemetryFormatSpec] PRIMARY KEY CLUSTERED ([TelemetryFormatSpecId] ASC),
    CONSTRAINT [FK_TelemetryFormatSpec_TelemetryFormat] FOREIGN KEY ([TelemetryFormatId]) REFERENCES [x360ce].[TelemetryFormat] ([TelemetryFormatId]) ON DELETE CASCADE
);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[x360ce].[TelemetryFormatSpec]') AND [name] = N'IX_TelemetryFormatSpec_TelemetryFormatId_SpecOrder')
CREATE UNIQUE NONCLUSTERED INDEX [IX_TelemetryFormatSpec_TelemetryFormatId_SpecOrder]
    ON [x360ce].[TelemetryFormatSpec]([TelemetryFormatId] ASC, [SpecOrder] ASC);
GO
IF OBJECT_ID(N'[x360ce].[TelemetryGame]', N'U') IS NULL
CREATE TABLE [x360ce].[TelemetryGame] (
    [TelemetryGameId]   UNIQUEIDENTIFIER CONSTRAINT [DF_TelemetryGame_TelemetryGameId] DEFAULT (newid()) NOT NULL,
    [FileName]          NVARCHAR (128)   CONSTRAINT [DF_TelemetryGame_FileName] DEFAULT ('') NOT NULL,
    [FileProductName]   NVARCHAR (256)   CONSTRAINT [DF_TelemetryGame_FileProductName] DEFAULT ('') NOT NULL,
    [GameName]          NVARCHAR (256)   CONSTRAINT [DF_TelemetryGame_GameName] DEFAULT ('') NOT NULL,
    [TelemetryFormatId] NVARCHAR (32)    NOT NULL,
    [Edition]           NVARCHAR (64)    CONSTRAINT [DF_TelemetryGame_Edition] DEFAULT ('') NOT NULL,
    [Comment]           NVARCHAR (1024)  CONSTRAINT [DF_TelemetryGame_Comment] DEFAULT ('') NOT NULL,
    [IsEnabled]         BIT              CONSTRAINT [DF_TelemetryGame_IsEnabled] DEFAULT ((1)) NOT NULL,
    [DateCreated]       DATETIME         CONSTRAINT [DF_TelemetryGame_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]       DATETIME         CONSTRAINT [DF_TelemetryGame_DateUpdated] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_TelemetryGame] PRIMARY KEY CLUSTERED ([TelemetryGameId] ASC),
    CONSTRAINT [FK_TelemetryGame_TelemetryFormat] FOREIGN KEY ([TelemetryFormatId]) REFERENCES [x360ce].[TelemetryFormat] ([TelemetryFormatId])
);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[x360ce].[TelemetryGame]') AND [name] = N'IX_TelemetryGame_FileName_FileProductName')
CREATE UNIQUE NONCLUSTERED INDEX [IX_TelemetryGame_FileName_FileProductName]
    ON [x360ce].[TelemetryGame]([FileName] ASC, [FileProductName] ASC);
GO
IF OBJECT_ID(N'[x360ce].[TelemetryGameStep]', N'U') IS NULL
CREATE TABLE [x360ce].[TelemetryGameStep] (
    [TelemetryGameStepId] UNIQUEIDENTIFIER CONSTRAINT [DF_TelemetryGameStep_TelemetryGameStepId] DEFAULT (newid()) NOT NULL,
    [TelemetryGameId]     UNIQUEIDENTIFIER NOT NULL,
    [StepOrder]           INT              CONSTRAINT [DF_TelemetryGameStep_StepOrder] DEFAULT ((0)) NOT NULL,
    [IsWarning]           BIT              CONSTRAINT [DF_TelemetryGameStep_IsWarning] DEFAULT ((0)) NOT NULL,
    [Text]                NVARCHAR (1024)  CONSTRAINT [DF_TelemetryGameStep_Text] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK_TelemetryGameStep] PRIMARY KEY CLUSTERED ([TelemetryGameStepId] ASC),
    CONSTRAINT [FK_TelemetryGameStep_TelemetryGame] FOREIGN KEY ([TelemetryGameId]) REFERENCES [x360ce].[TelemetryGame] ([TelemetryGameId]) ON DELETE CASCADE
);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[x360ce].[TelemetryGameStep]') AND [name] = N'IX_TelemetryGameStep_TelemetryGameId_IsWarning_StepOrder')
CREATE UNIQUE NONCLUSTERED INDEX [IX_TelemetryGameStep_TelemetryGameId_IsWarning_StepOrder]
    ON [x360ce].[TelemetryGameStep]([TelemetryGameId] ASC, [IsWarning] ASC, [StepOrder] ASC);
GO
IF OBJECT_ID(N'[x360ce].[TelemetryGameMod]', N'U') IS NULL
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
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[x360ce].[TelemetryGameMod]') AND [name] = N'IX_TelemetryGameMod_TelemetryGameId_ModOrder')
CREATE UNIQUE NONCLUSTERED INDEX [IX_TelemetryGameMod_TelemetryGameId_ModOrder]
    ON [x360ce].[TelemetryGameMod]([TelemetryGameId] ASC, [ModOrder] ASC);
GO
IF OBJECT_ID(N'[x360ce].[TelemetryGameFile]', N'U') IS NULL
CREATE TABLE [x360ce].[TelemetryGameFile] (
    [TelemetryGameFileId] UNIQUEIDENTIFIER CONSTRAINT [DF_TelemetryGameFile_TelemetryGameFileId] DEFAULT (newid()) NOT NULL,
    [TelemetryGameId]     UNIQUEIDENTIFIER NOT NULL,
    [ResourceName]        NVARCHAR (256)   CONSTRAINT [DF_TelemetryGameFile_ResourceName] DEFAULT ('') NOT NULL,
    [TargetPath]          NVARCHAR (256)   CONSTRAINT [DF_TelemetryGameFile_TargetPath] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK_TelemetryGameFile] PRIMARY KEY CLUSTERED ([TelemetryGameFileId] ASC),
    CONSTRAINT [FK_TelemetryGameFile_TelemetryGame] FOREIGN KEY ([TelemetryGameId]) REFERENCES [x360ce].[TelemetryGame] ([TelemetryGameId]) ON DELETE CASCADE
);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[x360ce].[TelemetryGameFile]') AND [name] = N'IX_TelemetryGameFile_TelemetryGameId_TargetPath')
CREATE UNIQUE NONCLUSTERED INDEX [IX_TelemetryGameFile_TelemetryGameId_TargetPath]
    ON [x360ce].[TelemetryGameFile]([TelemetryGameId] ASC, [TargetPath] ASC);
GO
