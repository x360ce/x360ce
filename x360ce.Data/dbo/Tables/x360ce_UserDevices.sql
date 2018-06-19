CREATE TABLE [dbo].[x360ce_UserDevices] (
    [Id]                                    UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserDevices_Id] DEFAULT (newid()) NOT NULL,
    [ComputerId]                            UNIQUEIDENTIFIER NOT NULL,
    [InstanceGuid]                          UNIQUEIDENTIFIER NOT NULL,
    [InstanceName]                          NVARCHAR (256)   NOT NULL,
    [ProductGuid]                           UNIQUEIDENTIFIER NOT NULL,
    [ProductName]                           NVARCHAR (256)   NOT NULL,
    [CapAxeCount]                           INT              NOT NULL,
    [CapButtonCount]                        INT              NOT NULL,
    [CapDriverVersion]                      INT              NOT NULL,
    [CapFirmwareRevision]                   INT              NOT NULL,
    [CapFlags]                              INT              NOT NULL,
    [CapForceFeedbackMinimumTimeResolution] INT              NOT NULL,
    [CapForceFeedbackSamplePeriod]          INT              NOT NULL,
    [CapHardwareRevision]                   INT              NOT NULL,
    [CapPovCount]                           INT              NOT NULL,
    [CapIsHumanInterfaceDevice]             BIT              NOT NULL,
    [CapSubtype]                            INT              NOT NULL,
    [CapType]                               INT              NOT NULL,
    [DiAxeMask]                             INT              CONSTRAINT [DF_x360ce_UserDevices_DiAxeMask] DEFAULT ((0)) NOT NULL,
    [DiSliderMask]                          INT              CONSTRAINT [DF_x360ce_UserDevices_DiSliderMask] DEFAULT ((0)) NOT NULL,
    [DiActuatorMask]                        INT              CONSTRAINT [DF_x360ce_UserDevices_DiActuatorMask] DEFAULT ((0)) NOT NULL,
    [DiActuatorCount]                       INT              CONSTRAINT [DF_x360ce_UserDevices_DiActuatorCount] DEFAULT ((0)) NOT NULL,
    [HidManufacturer]                       NVARCHAR (256)   NOT NULL,
    [HidVendorId]                           INT              NOT NULL,
    [HidProductId]                          INT              NOT NULL,
    [HidRevision]                           INT              NOT NULL,
    [HidDescription]                        NVARCHAR (512)   NOT NULL,
    [HidDeviceId]                           NVARCHAR (512)   NOT NULL,
    [HidDevicePath]                         NVARCHAR (512)   NOT NULL,
    [HidParentDeviceId]                     NVARCHAR (512)   NOT NULL,
    [HidClassGuid]                          UNIQUEIDENTIFIER NOT NULL,
    [HidClassDescription]                   NVARCHAR (256)   NOT NULL,
    [DevManufacturer]                       NVARCHAR (256)   NOT NULL,
    [DevVendorId]                           INT              NOT NULL,
    [DevProductId]                          INT              NOT NULL,
    [DevRevision]                           INT              NOT NULL,
    [DevDescription]                        NVARCHAR (512)   NOT NULL,
    [DevDeviceId]                           NVARCHAR (512)   NOT NULL,
    [DevDevicePath]                         NVARCHAR (512)   NOT NULL,
    [DevParentDeviceId]                     NVARCHAR (512)   NOT NULL,
    [DevClassGuid]                          UNIQUEIDENTIFIER NOT NULL,
    [DevClassDescription]                   NVARCHAR (256)   NOT NULL,
    [DateCreated]                           DATETIME         CONSTRAINT [DF_x360ce_UserDevices_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]                           DATETIME         CONSTRAINT [DF_x360ce_UserDevices_DateUpdated] DEFAULT (getdate()) NOT NULL,
    [IsHidden]                              BIT              CONSTRAINT [DF_x360ce_UserDevices_IsHidden] DEFAULT ((0)) NOT NULL,
    [IsEnabled]                             BIT              CONSTRAINT [DF_x360ce_UserDevices_IsEnabled] DEFAULT ((1)) NOT NULL,
    [Checksum]                              UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserDevices_Checksum] DEFAULT ('00000000-0000-0000-0000-000000000000') NOT NULL,
    CONSTRAINT [PK_x360ce_UserDevices] PRIMARY KEY CLUSTERED ([Id] ASC)
);


















GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_x360ce_UserDevices_InstanceGuid]
    ON [dbo].[x360ce_UserDevices]([InstanceGuid] ASC);

