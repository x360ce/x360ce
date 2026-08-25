CREATE TABLE [dbo].[x360ce_UserMacros] (
    [Id]           UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserMacros_Id] DEFAULT (newid()) NOT NULL,
    [SettingId]    UNIQUEIDENTIFIER NOT NULL,
    [MapType]      INT              NOT NULL,
    [MapCode]      INT              NOT NULL,
    [MapIndex]     INT              NOT NULL,
    [MapRangeMin]  INT              NOT NULL,
    [MapRangeMax]  INT              NOT NULL,
    [MapEventType] INT              NOT NULL,
    [MapRpmType]   INT              NOT NULL,
    [MapRpmMin]    INT              NOT NULL,
    [MapRpmMax]    INT              NOT NULL,
    [Name]         NVARCHAR (256)   NOT NULL,
    [Text]         NVARCHAR (1024)  NOT NULL,
    [IsEnabled]    BIT              CONSTRAINT [DF_x360ce_UserMacros_IsEnabled] DEFAULT ((1)) NOT NULL,
    [Checksum]     UNIQUEIDENTIFIER NOT NULL,
    [Created]      DATETIME         CONSTRAINT [DF_x360ce_UserMacros_DateCreated] DEFAULT (getdate()) NOT NULL,
    [Updated]      DATETIME         CONSTRAINT [DF_x360ce_UserMacros_DateUpdated] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_x360ce_UserMacros] PRIMARY KEY CLUSTERED ([Id] ASC)
);

