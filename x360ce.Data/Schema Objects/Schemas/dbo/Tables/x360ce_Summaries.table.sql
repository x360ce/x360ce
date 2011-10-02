CREATE TABLE [dbo].[x360ce_Summaries] (
    [SummaryId]          UNIQUEIDENTIFIER NOT NULL,
    [Users]              INT              NOT NULL,
    [PadSettingChecksum] UNIQUEIDENTIFIER NOT NULL,
    [ProductGuid]        UNIQUEIDENTIFIER NOT NULL,
    [ProductName]        NVARCHAR (256)   NOT NULL,
    [FileName]           NVARCHAR (128)   NOT NULL,
    [FileProductName]    NVARCHAR (256)   NOT NULL,
    [DateCreated]        DATETIME         NOT NULL,
    [DateUpdated]        DATETIME         NOT NULL
);



