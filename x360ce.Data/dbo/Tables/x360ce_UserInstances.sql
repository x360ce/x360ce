CREATE TABLE [dbo].[x360ce_UserInstances] (
    [Id]           UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserInstances_Id] DEFAULT (newid()) NOT NULL,
    [ComputerId]   UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserInstances_ComputerId] DEFAULT ('00000000-0000-0000-0000-000000000000') NOT NULL,
    [ProfileId]    UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserInstances_ProfileId] DEFAULT ('00000000-0000-0000-0000-000000000000') NOT NULL,
    [ControllerId] UNIQUEIDENTIFIER NOT NULL,
    [InstanceGuid] UNIQUEIDENTIFIER NOT NULL,
    [DateCreated]  DATETIME         CONSTRAINT [DF_x360ce_UserInstances_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]  DATETIME         CONSTRAINT [DF_x360ce_UserInstances_DateUpdated] DEFAULT (getdate()) NOT NULL,
    [Checksum]     UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_x360ce_ControllerInstances] PRIMARY KEY CLUSTERED ([Id] ASC)
);





