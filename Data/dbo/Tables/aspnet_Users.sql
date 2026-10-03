CREATE TABLE [dbo].[aspnet_Users] (
    [ApplicationId]    UNIQUEIDENTIFIER NOT NULL,
    [UserId]           UNIQUEIDENTIFIER CONSTRAINT [DF__aspnet_Users__UserId] DEFAULT (newid()) NOT NULL,
    [UserName]         NVARCHAR (256)   NOT NULL,
    [LoweredUserName]  NVARCHAR (256)   NOT NULL,
    [MobileAlias]      NVARCHAR (16)    CONSTRAINT [DF__aspnet_Users__MobileAlias] DEFAULT (NULL) NULL,
    [IsAnonymous]      BIT              CONSTRAINT [DF__aspnet_Users__IsAnonymous] DEFAULT ((0)) NOT NULL,
    [LastActivityDate] DATETIME         NOT NULL,
    [FirstName]        NVARCHAR (256)   CONSTRAINT [DF__aspnet_Users__FirstName] DEFAULT ('') NOT NULL,
    [LastName]         NVARCHAR (256)   CONSTRAINT [DF__aspnet_Users__LastName] DEFAULT ('') NOT NULL,
    [Gender]           CHAR (1)         CONSTRAINT [DF__aspnet_Users__Gender] DEFAULT ('') NOT NULL,
    [Description]      NVARCHAR (1024)  CONSTRAINT [DF__aspnet_Users__Description] DEFAULT ('') NOT NULL,
    [DateBirth]        DATETIME         CONSTRAINT [DF__aspnet_Users__DateBirth] DEFAULT (getdate()) NULL,
    CONSTRAINT [PK__aspnet_Users__UserId] PRIMARY KEY NONCLUSTERED ([UserId] ASC),
    CONSTRAINT [FK__aspnet_Users__ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[aspnet_Applications] ([ApplicationId])
);


GO
CREATE NONCLUSTERED INDEX [IX__aspnet_Users_LastActivityDate]
    ON [dbo].[aspnet_Users]([ApplicationId] ASC, [LastActivityDate] ASC);


GO
CREATE UNIQUE CLUSTERED INDEX [IX__aspnet_Users_LoweredUserName]
    ON [dbo].[aspnet_Users]([ApplicationId] ASC, [LoweredUserName] ASC);

