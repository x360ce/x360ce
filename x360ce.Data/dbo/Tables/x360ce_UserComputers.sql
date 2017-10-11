CREATE TABLE [dbo].[x360ce_UserComputers] (
    [Id]            UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserComputers_Id] DEFAULT (newid()) NOT NULL,
    [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
    [UserId]        UNIQUEIDENTIFIER NOT NULL,
    [ComputerId]    UNIQUEIDENTIFIER NOT NULL,
    [ComputerName]  NVARCHAR (256)   CONSTRAINT [DF_x360ce_UserComputers_ComputerName] DEFAULT ('') NOT NULL,
    [DateCreated]   DATETIME         CONSTRAINT [DF_x360ce_UserComputers_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]   DATETIME         CONSTRAINT [DF_x360ce_UserComputers_DateUpdated] DEFAULT (getdate()) NOT NULL,
    [Checksum]      UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_x360ce_UserComputers] PRIMARY KEY CLUSTERED ([Id] ASC)
);




GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_x360ce_UserComputers_ApplicationId_UserId_ComputerId]
    ON [dbo].[x360ce_UserComputers]([ApplicationId] ASC, [UserId] ASC, [ComputerId] ASC);

