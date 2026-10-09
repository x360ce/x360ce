CREATE TABLE [dbo].[x360ce_Vendors] (
    [VendorId]   INT            NOT NULL,
    [VendorName] NVARCHAR (256) CONSTRAINT [DF_x360ce_Vendors_VendorName] DEFAULT ('') NOT NULL,
    [ShortName]  NVARCHAR (32)  CONSTRAINT [DF_x360ce_Vendors_ShortName] DEFAULT ('') NOT NULL,
    [WebSite]    NVARCHAR (256) CONSTRAINT [DF_x360ce_Vendors_WebSite] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK_x360ce_Vendors] PRIMARY KEY CLUSTERED ([VendorId] ASC)
);

