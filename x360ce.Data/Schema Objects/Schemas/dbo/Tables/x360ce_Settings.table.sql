CREATE TABLE [dbo].[x360ce_Settings] (
    [SettingId]          UNIQUEIDENTIFIER NOT NULL,
    [InstanceGuid]       UNIQUEIDENTIFIER NOT NULL,
    [InstanceName]       NVARCHAR (256)   NOT NULL,
    [ProductGuid]        UNIQUEIDENTIFIER NOT NULL,
    [ProductName]        NVARCHAR (256)   NOT NULL,
    [DeviceType]         INT              NOT NULL,
    [FileName]           NVARCHAR (128)   NOT NULL,
    [FileProductName]    NVARCHAR (256)   NOT NULL,
    [Comment]            NVARCHAR (1024)  NOT NULL,
    [DateCreated]        DATETIME         CONSTRAINT [DF_x360ce_Settings_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]        DATETIME         CONSTRAINT [DF_x360ce_Settings_DateUpdated] DEFAULT (getdate()) NOT NULL,
    [DateSelected]       DATETIME         CONSTRAINT [DF_x360ce_Settings_DateSelected] DEFAULT (getdate()) NOT NULL,
    [IsEnabled]          BIT              CONSTRAINT [DF_x360ce_Settings_IsEnabled] DEFAULT ((1)) NOT NULL,
    [PadSettingChecksum] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_x360ce_Settings] PRIMARY KEY CLUSTERED ([SettingId] ASC)
);





