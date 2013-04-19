CREATE TABLE [dbo].[x360ce_Programs] (
    [ProgramId]       UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_Programs_ProgramId] DEFAULT (newid()) NOT NULL,
    [FileName]        NVARCHAR (128)   CONSTRAINT [DF_x360ce_Programs_FileName] DEFAULT ('') NOT NULL,
    [FileProductName] NVARCHAR (256)   CONSTRAINT [DF_x360ce_Programs_FileProductName] DEFAULT ('') NOT NULL,
    [HookMask]        INT              CONSTRAINT [DF_x360ce_Programs_HookMask] DEFAULT ((0)) NOT NULL,
    [XInputMask]      INT              CONSTRAINT [DF_x360ce_Programs_XInputFileName] DEFAULT ((4)) NOT NULL,
    [InstanceCount]   INT              CONSTRAINT [DF_x360ce_Programs_InstanceCount] DEFAULT ((0)) NOT NULL,
    [Comment]         NVARCHAR (1024)  CONSTRAINT [DF_x360ce_Programs_Comment] DEFAULT ('') NOT NULL,
    [IsEnabled]       BIT              CONSTRAINT [DF_x360ce_Programs_IsEnabled] DEFAULT ((1)) NOT NULL,
    [DateCreated]     DATETIME         CONSTRAINT [DF_x360ce_Programs_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]     DATETIME         NULL,
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

