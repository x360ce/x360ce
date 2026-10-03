CREATE TABLE [dbo].[x360ce_ControllerInstances] (
    [Id]           UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_ControllerInstances_Id] DEFAULT (newid()) NOT NULL,
    [ControllerId] UNIQUEIDENTIFIER NOT NULL,
    [InstanceGuid] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_x360ce_ControllerInstances] PRIMARY KEY CLUSTERED ([Id] ASC)
);

