CREATE TABLE [dbo].[x360ce_UserProfiles] (
    [Id]          UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserProfiles_Id] DEFAULT (newid()) NOT NULL,
    [ComputerId]  UNIQUEIDENTIFIER NOT NULL,
    [ProfileId]   UNIQUEIDENTIFIER NOT NULL,
    [ProfilePath] NVARCHAR (256)   CONSTRAINT [DF_x360ce_UserProfiles_ProfilePath] DEFAULT ('') NOT NULL,
    [DateCreated] DATETIME         CONSTRAINT [DF_x360ce_UserProfiles_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated] DATETIME         CONSTRAINT [DF_x360ce_UserProfiles_DateUpdated] DEFAULT (getdate()) NOT NULL,
    [Checksum]    UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_x360ce_UserProfiles] PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_x360ce_UserProfiles_ComputerId_ProfileId]
    ON [dbo].[x360ce_UserProfiles]([ComputerId] ASC, [ProfileId] ASC);

