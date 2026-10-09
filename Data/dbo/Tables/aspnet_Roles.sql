CREATE TABLE [dbo].[aspnet_Roles] (
    [ApplicationId]   UNIQUEIDENTIFIER NOT NULL,
    [RoleId]          UNIQUEIDENTIFIER CONSTRAINT [DF__aspnet_Roles__RoleId] DEFAULT (newid()) NOT NULL,
    [RoleName]        NVARCHAR (256)   NOT NULL,
    [LoweredRoleName] NVARCHAR (256)   NOT NULL,
    [Description]     NVARCHAR (256)   NULL,
    CONSTRAINT [PK__aspnet_Roles__RoleId] PRIMARY KEY NONCLUSTERED ([RoleId] ASC),
    CONSTRAINT [FK__aspnet_Roles__ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[aspnet_Applications] ([ApplicationId])
);


GO
CREATE UNIQUE CLUSTERED INDEX [IX__aspnet_Roles__LoweredRoleName]
    ON [dbo].[aspnet_Roles]([ApplicationId] ASC, [LoweredRoleName] ASC);

