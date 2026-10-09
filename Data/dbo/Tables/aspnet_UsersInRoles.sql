CREATE TABLE [dbo].[aspnet_UsersInRoles] (
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK__aspnet_UsersInRoles__UserId__RoleId] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    CONSTRAINT [FK__aspnet_UsersInRoles__RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[aspnet_Roles] ([RoleId]),
    CONSTRAINT [FK__aspnet_UsersInRoles__UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[aspnet_Users] ([UserId])
);


GO
CREATE NONCLUSTERED INDEX [IX__aspnet_UsersInRoles_index]
    ON [dbo].[aspnet_UsersInRoles]([RoleId] ASC);

