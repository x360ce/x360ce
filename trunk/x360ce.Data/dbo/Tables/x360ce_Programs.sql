CREATE TABLE [dbo].[x360ce_Programs] (
    [ProgramId]       UNIQUEIDENTIFIER NOT NULL,
    [FileName]        NVARCHAR (128)   CONSTRAINT [DF_x360ce_Programs_FileName] DEFAULT ('') NOT NULL,
    [FileProductName] NVARCHAR (256)   CONSTRAINT [DF_x360ce_Programs_FileProductName] DEFAULT ('') NOT NULL,
    [HookMask]        INT              CONSTRAINT [DF_x360ce_Programs_HookMask] DEFAULT ((0)) NOT NULL,
    [XInputFileName]  NVARCHAR (128)   CONSTRAINT [DF_x360ce_Programs_XInputFileName] DEFAULT (N'xinput1_3.dll') NOT NULL,
    CONSTRAINT [PK_x360ce_Programs] PRIMARY KEY CLUSTERED ([ProgramId] ASC)
);

