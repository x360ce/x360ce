CREATE TABLE [dbo].[x360ce_ChangeLogs] (
    [ChangeId]      BIGINT           IDENTITY (1, 1) NOT NULL,
    [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
    [UserId]        UNIQUEIDENTIFIER NOT NULL,
    [TableName]     NVARCHAR (128)   CONSTRAINT [DF_x360ce_ChangeLogs_TableName] DEFAULT ('') NOT NULL,
    [IsDelete]      BIT              CONSTRAINT [DF_x360ce_ChangeLogs_IsDelete] DEFAULT ((0)) NOT NULL,
    [IsInsert]      BIT              CONSTRAINT [DF_x360ce_ChangeLogs_IsInsert] DEFAULT ((0)) NOT NULL,
    [Reference]     VARCHAR (128)    NOT NULL,
    [OldValues]     XML              NOT NULL,
    [NewValues]     XML              NOT NULL,
    [Created]       DATETIME         CONSTRAINT [DF_x360ce_ChangeLogs_Created] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_x360ce_ChangeLogs] PRIMARY KEY CLUSTERED ([ChangeId] ASC)
);

