CREATE TABLE [dbo].[aspnet_Applications] (
    [ApplicationName]        NVARCHAR (256)   NOT NULL,
    [LoweredApplicationName] NVARCHAR (256)   NOT NULL,
    [ApplicationId]          UNIQUEIDENTIFIER CONSTRAINT [DF__aspnet_Applications__ApplicationId] DEFAULT (newid()) NOT NULL,
    [Description]            NVARCHAR (256)   NULL,
    CONSTRAINT [PK__aspnet_Applications__ApplicationId] PRIMARY KEY NONCLUSTERED ([ApplicationId] ASC),
    CONSTRAINT [UQ__aspnet_Applications__LoweredApplicationName] UNIQUE NONCLUSTERED ([LoweredApplicationName] ASC),
    CONSTRAINT [UQ__aspnet_Applications__ApplicationName] UNIQUE NONCLUSTERED ([ApplicationName] ASC)
);


GO
CREATE CLUSTERED INDEX [IX__aspnet_Applications__LoweredApplicationName]
    ON [dbo].[aspnet_Applications]([LoweredApplicationName] ASC);

