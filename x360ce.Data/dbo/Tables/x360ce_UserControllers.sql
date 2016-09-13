CREATE TABLE [dbo].[x360ce_UserControllers] (
    [Id]            UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserControllers_Id] DEFAULT (newid()) NOT NULL,
    [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
    [UserId]        UNIQUEIDENTIFIER NOT NULL,
    [VendorName]    NVARCHAR (256)   NOT NULL,
    [ProductName]   NVARCHAR (256)   NOT NULL,
    [DeviceId]      VARCHAR (512)    NOT NULL,
    [FriendlyName]  NVARCHAR (256)   NOT NULL,
    [DateCreated]   DATETIME         CONSTRAINT [DF_x360ce_UserControllers_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]   DATETIME         NOT NULL,
    [IsEnabled]     BIT              CONSTRAINT [DF_x360ce_UserControllers_IsEnabled] DEFAULT ((1)) NOT NULL,
    [IsOnline]      BIT              CONSTRAINT [DF_x360ce_UserControllers_IsOnline] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_x360ce_UserControllers] PRIMARY KEY CLUSTERED ([Id] ASC)
);

