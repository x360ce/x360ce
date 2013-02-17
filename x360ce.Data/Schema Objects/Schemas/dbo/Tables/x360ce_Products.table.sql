CREATE TABLE [dbo].[x360ce_Products] (
    [ProductGuid]   UNIQUEIDENTIFIER NOT NULL,
    [ProductName]   NVARCHAR (256)   NOT NULL,
    [InstanceCount] INT              CONSTRAINT [DF_x360ce_Products_InstanceCount] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_x360ce_Products] PRIMARY KEY CLUSTERED ([ProductGuid] ASC)
);






GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Products_InstanceCount]
    ON [dbo].[x360ce_Products]([InstanceCount] DESC);

