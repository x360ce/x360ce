CREATE TABLE [dbo].[x360ce_Programs] (
    [ProgramId]             UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_Programs_ProgramId] DEFAULT (newid()) NOT NULL,
    [FileName]              NVARCHAR (128)   CONSTRAINT [DF_x360ce_Programs_FileName] DEFAULT ('') NOT NULL,
    [FileProductName]       NVARCHAR (256)   CONSTRAINT [DF_x360ce_Programs_FileProductName] DEFAULT ('') NOT NULL,
    [FileVersion]           NVARCHAR (32)    CONSTRAINT [DF_x360ce_Programs_FileVersion] DEFAULT ('') NOT NULL,
    [ProcessorArchitecture] INT              CONSTRAINT [DF_x360ce_Programs_ProcessorArchitecture] DEFAULT ((0)) NOT NULL,
    [EmulationType]         INT              CONSTRAINT [DF_x360ce_Programs_EmulationType] DEFAULT ((0)) NOT NULL,
    [AutoMapMask]           INT              CONSTRAINT [DF_x360ce_Programs_AutoMapMask] DEFAULT ((0)) NOT NULL,
    [HookMask]              INT              CONSTRAINT [DF_x360ce_Programs_HookMask] DEFAULT ((0)) NOT NULL,
    [XInputMask]            INT              CONSTRAINT [DF_x360ce_Programs_XInputFileName] DEFAULT ((0)) NOT NULL,
    [DInputMask]            INT              CONSTRAINT [DF_x360ce_Programs_DInputMask] DEFAULT ((0)) NOT NULL,
    [DInputFile]            NVARCHAR (256)   CONSTRAINT [DF_x360ce_Programs_DInputFile] DEFAULT ('') NOT NULL,
    [FakeVID]               INT              CONSTRAINT [DF_x360ce_Programs_FakeVID] DEFAULT ((0)) NOT NULL,
    [FakePID]               INT              CONSTRAINT [DF_x360ce_Programs_FakePID] DEFAULT ((0)) NOT NULL,
    [Timeout]               INT              CONSTRAINT [DF_x360ce_Programs_Timeout] DEFAULT ((0)) NOT NULL,
    [Weight]                INT              CONSTRAINT [DF_x360ce_Programs_Weight] DEFAULT ((0)) NOT NULL,
    [SettingChecksum]       UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_Programs_SettingChecksum] DEFAULT ('00000000-0000-0000-0000-000000000000') NOT NULL,
    [Comment]               NVARCHAR (1024)  CONSTRAINT [DF_x360ce_Programs_Comment] DEFAULT ('') NOT NULL,
    [IsEnabled]             BIT              CONSTRAINT [DF_x360ce_Programs_IsEnabled] DEFAULT ((1)) NOT NULL,
    [DateCreated]           DATETIME         CONSTRAINT [DF_x360ce_Programs_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]           DATETIME         CONSTRAINT [DF_x360ce_Programs_DateUpdated] DEFAULT (getdate()) NOT NULL,
    [InstanceCount]         INT              CONSTRAINT [DF_x360ce_Programs_InstanceCount] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_x360ce_Programs] PRIMARY KEY CLUSTERED ([ProgramId] ASC)
);








GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Programs_InstanceCount]
    ON [dbo].[x360ce_Programs]([InstanceCount] DESC);


GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Programs_HookMask]
    ON [dbo].[x360ce_Programs]([HookMask] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_x360ce_Programs_FileName_FileProductName]
    ON [dbo].[x360ce_Programs]([FileName] ASC, [FileProductName] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Programs_DateUpdated]
    ON [dbo].[x360ce_Programs]([DateUpdated] ASC);

