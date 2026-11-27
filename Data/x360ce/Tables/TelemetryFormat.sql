CREATE TABLE [x360ce].[TelemetryFormat] (
    [TelemetryFormatId] NVARCHAR (32)   NOT NULL,
    [Name]              NVARCHAR (128)  CONSTRAINT [DF_TelemetryFormat_Name] DEFAULT ('') NOT NULL,
    [Publisher]         NVARCHAR (256)  CONSTRAINT [DF_TelemetryFormat_Publisher] DEFAULT ('') NOT NULL,
    [Transport]         NVARCHAR (32)   CONSTRAINT [DF_TelemetryFormat_Transport] DEFAULT ('') NOT NULL,
    [DefaultPort]       INT             CONSTRAINT [DF_TelemetryFormat_DefaultPort] DEFAULT ((0)) NOT NULL,
    [MapName]           NVARCHAR (128)  CONSTRAINT [DF_TelemetryFormat_MapName] DEFAULT ('') NOT NULL,
    [PacketSizes]       NVARCHAR (256)  CONSTRAINT [DF_TelemetryFormat_PacketSizes] DEFAULT ('') NOT NULL,
    [Comment]           NVARCHAR (1024) CONSTRAINT [DF_TelemetryFormat_Comment] DEFAULT ('') NOT NULL,
    [IsEnabled]         BIT             CONSTRAINT [DF_TelemetryFormat_IsEnabled] DEFAULT ((1)) NOT NULL,
    [DateCreated]       DATETIME        CONSTRAINT [DF_TelemetryFormat_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]       DATETIME        CONSTRAINT [DF_TelemetryFormat_DateUpdated] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_TelemetryFormat] PRIMARY KEY CLUSTERED ([TelemetryFormatId] ASC)
);
