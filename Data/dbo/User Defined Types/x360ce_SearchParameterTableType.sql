CREATE TYPE [dbo].[x360ce_SearchParameterTableType] AS TABLE (
    [ProductGuid]     UNIQUEIDENTIFIER NULL,
    [InstanceGuid]    UNIQUEIDENTIFIER NULL,
    [FileName]        NVARCHAR (128)   NULL,
    [FileProductName] NVARCHAR (256)   NULL);

