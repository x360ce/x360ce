CREATE TABLE [dbo].[x360ce_NewDeviceStats] (
    [Date]       DATE     NOT NULL,
    [NewDevices] INT      NULL,
    [Created]    DATETIME NULL,
    CONSTRAINT [PK_x360ce_NewDeviceStats] PRIMARY KEY CLUSTERED ([Date] ASC)
);

