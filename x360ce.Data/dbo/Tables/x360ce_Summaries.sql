CREATE TABLE [dbo].[x360ce_Summaries] (
    [SummaryId]          UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_Summaries_SummaryId] DEFAULT (newid()) NOT NULL,
    [Users]              INT              CONSTRAINT [DF_x360ce_Summaries_Users] DEFAULT ((0)) NOT NULL,
    [PadSettingChecksum] UNIQUEIDENTIFIER NOT NULL,
    [ProductGuid]        UNIQUEIDENTIFIER NOT NULL,
    [ProductName]        NVARCHAR (256)   CONSTRAINT [DF_x360ce_Summaries_ProductName] DEFAULT ('') NOT NULL,
    [FileName]           NVARCHAR (128)   NOT NULL,
    [FileProductName]    NVARCHAR (256)   NOT NULL,
    [DateCreated]        DATETIME         CONSTRAINT [DF_x360ce_Summaries_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]        DATETIME         CONSTRAINT [DF_x360ce_Summaries_DateUpdated] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_x360ce_Summaries] PRIMARY KEY CLUSTERED ([SummaryId] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Summaries_ProductGuid_FileName_FileProductName_PadSettingChecksum]
    ON [dbo].[x360ce_Summaries]([ProductGuid] ASC, [FileName] ASC, [FileProductName] ASC, [PadSettingChecksum] ASC);

