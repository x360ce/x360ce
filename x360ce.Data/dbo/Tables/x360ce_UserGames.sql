CREATE TABLE [dbo].[x360ce_UserGames] (
    [GameId]                UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserGames_GameId] DEFAULT (newid()) NOT NULL,
    [ComputerId]            UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserGames_DiskDriveId] DEFAULT ('00000000-0000-0000-0000-000000000000') NOT NULL,
    [FileName]              NVARCHAR (128)   CONSTRAINT [DF_x360ce_UserGames_FileName] DEFAULT ('') NOT NULL,
    [FileProductName]       NVARCHAR (256)   CONSTRAINT [DF_x360ce_UserGames_FileProductName] DEFAULT ('') NOT NULL,
    [FileVersion]           NVARCHAR (32)    CONSTRAINT [DF_x360ce_UserGames_FileVersion] DEFAULT ('') NOT NULL,
    [FullPath]              NVARCHAR (256)   CONSTRAINT [DF_x360ce_UserGames_FullPath] DEFAULT ('') NOT NULL,
    [CompanyName]           NVARCHAR (128)   CONSTRAINT [DF_x360ce_UserGames_CompanyName] DEFAULT ('') NOT NULL,
    [ProcessorArchitecture] INT              CONSTRAINT [DF_x360ce_UserGames_ProcessorArchitecture] DEFAULT ((0)) NOT NULL,
    [HookMask]              INT              CONSTRAINT [DF_x360ce_UserGames_HookMask] DEFAULT ((0)) NOT NULL,
    [XInputMask]            INT              CONSTRAINT [DF_x360ce_UserGames_XInputFileName] DEFAULT ((0)) NOT NULL,
    [DInputMask]            INT              CONSTRAINT [DF_x360ce_UserGames_DInputMask] DEFAULT ((0)) NOT NULL,
    [DInputFile]            NVARCHAR (256)   CONSTRAINT [DF_x360ce_UserGames_DInputFile] DEFAULT ('') NOT NULL,
    [FakeVID]               INT              CONSTRAINT [DF_x360ce_UserGames_FakeVID] DEFAULT ((0)) NOT NULL,
    [FakePID]               INT              CONSTRAINT [DF_x360ce_UserGames_FakePID] DEFAULT ((0)) NOT NULL,
    [Timeout]               INT              CONSTRAINT [DF_x360ce_UserGames_Timeout] DEFAULT ((-1)) NOT NULL,
    [Weight]                INT              CONSTRAINT [DF_x360ce_UserGames_Weight] DEFAULT ((0)) NOT NULL,
    [Comment]               NVARCHAR (1024)  CONSTRAINT [DF_x360ce_UserGames_Comment] DEFAULT ('') NOT NULL,
    [IsEnabled]             BIT              CONSTRAINT [DF_x360ce_UserGames_IsEnabled] DEFAULT ((1)) NOT NULL,
    [DateCreated]           DATETIME         CONSTRAINT [DF_x360ce_UserGames_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]           DATETIME         NULL,
    [AutoMapMask]           INT              CONSTRAINT [DF_x360ce_UserGames_AutoMapMask] DEFAULT ((0)) NOT NULL,
    [Checksum]              UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserGames_Checksum] DEFAULT ('00000000-0000-0000-0000-000000000000') NOT NULL,
    CONSTRAINT [PK_x360ce_UserGames] PRIMARY KEY CLUSTERED ([GameId] ASC)
);


















GO
CREATE NONCLUSTERED INDEX [IX_x360ce_UserGames_DateUpdated]
    ON [dbo].[x360ce_UserGames]([DateUpdated] ASC);


GO



GO
CREATE NONCLUSTERED INDEX [IX_x360ce_UserGames_XInputMask]
    ON [dbo].[x360ce_UserGames]([XInputMask] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_x360ce_UserGames_HookMask]
    ON [dbo].[x360ce_UserGames]([HookMask] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_x360ce_UserGames_ComputerId_FileName_FileProductName]
    ON [dbo].[x360ce_UserGames]([ComputerId] ASC, [FileName] ASC, [FileProductName] ASC);

