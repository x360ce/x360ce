CREATE TABLE [dbo].[x360ce_Games] (
    [GameId]                UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_Games_GameId] DEFAULT (newid()) NOT NULL,
    [DiskDriveId]           UNIQUEIDENTIFIER NOT NULL,
    [FileName]              NVARCHAR (128)   CONSTRAINT [DF_x360ce_Games_FileName] DEFAULT ('') NOT NULL,
    [FileProductName]       NVARCHAR (256)   CONSTRAINT [DF_x360ce_Games_FileProductName] DEFAULT ('') NOT NULL,
    [FileVersion]           NVARCHAR (32)    NOT NULL,
    [FullPath]              NVARCHAR (256)   CONSTRAINT [DF_x360ce_Games_FullPath] DEFAULT ('') NOT NULL,
    [CompanyName]           NVARCHAR (128)   CONSTRAINT [DF_x360ce_Games_CompanyName] DEFAULT ('') NOT NULL,
    [HookMask]              INT              CONSTRAINT [DF_x360ce_Games_HookMask] DEFAULT ((0)) NOT NULL,
    [XInputMask]            INT              CONSTRAINT [DF_x360ce_Games_XInputFileName] DEFAULT ((4)) NOT NULL,
    [ProcessorArchitecture] INT              CONSTRAINT [DF_x360ce_Games_ProcessorArchitecture] DEFAULT ((0)) NOT NULL,
    [Comment]               NVARCHAR (1024)  CONSTRAINT [DF_x360ce_Games_Comment] DEFAULT ('') NOT NULL,
    [IsEnabled]             BIT              CONSTRAINT [DF_x360ce_Games_IsEnabled] DEFAULT ((1)) NOT NULL,
    [DateCreated]           DATETIME         CONSTRAINT [DF_x360ce_Games_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]           DATETIME         NULL,
    CONSTRAINT [PK_x360ce_Games] PRIMARY KEY CLUSTERED ([GameId] ASC)
);




GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Games_DateUpdated]
    ON [dbo].[x360ce_Games]([DateUpdated] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_x360ce_Games_FileName_FileProductName]
    ON [dbo].[x360ce_Games]([FileName] ASC, [FileProductName] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Games_XInputMask]
    ON [dbo].[x360ce_Games]([XInputMask] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Games_HookMask]
    ON [dbo].[x360ce_Games]([HookMask] ASC);

