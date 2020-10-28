CREATE TABLE [dbo].[x360ce_UserKeyboardMaps] (
    [Id]              UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_UserKeyboardMaps_Id] DEFAULT (newid()) NOT NULL,
    [ComputerId]      UNIQUEIDENTIFIER NOT NULL,
    [ProfileId]       UNIQUEIDENTIFIER NOT NULL,
    [FileName]        NVARCHAR (128)   NOT NULL,
    [FileProductName] NVARCHAR (256)   NOT NULL,
    [Name]            NVARCHAR (256)   NOT NULL,
    [MapTo]           INT              NOT NULL,
    [MapType]         INT              NOT NULL,
    [MapIndex]        INT              NOT NULL,
    [MapRangeMin]     INT              NOT NULL,
    [MapRangeMax]     INT              NOT NULL,
    [ScriptType]      INT              NOT NULL,
    [ScriptText]      NVARCHAR (1024)  NOT NULL,
    [IsEnabled]       BIT              CONSTRAINT [DF_x360ce_UserKeyboardMaps_IsEnabled] DEFAULT ((1)) NOT NULL,
    [Checksum]        UNIQUEIDENTIFIER NOT NULL,
    [DateCreated]     DATETIME         CONSTRAINT [DF_x360ce_UserKeyboardMaps_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]     DATETIME         CONSTRAINT [DF_x360ce_UserKeyboardMaps_DateUpdated] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_x360ce_UserKeyboardMaps] PRIMARY KEY CLUSTERED ([Id] ASC)
);

