CREATE TABLE [dbo].[x360ce_UserInstances] (
    [Id]           UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserInstances_Id] DEFAULT (newid()) NOT NULL,
    [ControllerId] UNIQUEIDENTIFIER NOT NULL,
    [InstanceGuid] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_x360ce_ControllerInstances] PRIMARY KEY CLUSTERED ([Id] ASC)
);

