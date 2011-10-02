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
    [DateCreated]        DATETIME         NOT NULL,
    [DateUpdated]        DATETIME         NOT NULL,
    [DateSelected]       DATETIME         NOT NULL,
    [IsEnabled]          BIT              NOT NULL,
    [PadSettingChecksum] UNIQUEIDENTIFIER NOT NULL
);



