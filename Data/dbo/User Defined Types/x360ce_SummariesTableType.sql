CREATE TYPE [dbo].[x360ce_SummariesTableType] AS TABLE (
    [PadSettingChecksum] UNIQUEIDENTIFIER NULL,
    [ProductGuid]        UNIQUEIDENTIFIER NULL,
    [ProductName]        NVARCHAR (256)   NULL,
    [FileName]           NVARCHAR (128)   NULL,
    [FileProductName]    NVARCHAR (256)   NULL);

