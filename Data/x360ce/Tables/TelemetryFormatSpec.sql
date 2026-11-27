CREATE TABLE [x360ce].[TelemetryFormatSpec] (
    [TelemetryFormatSpecId] UNIQUEIDENTIFIER CONSTRAINT [DF_TelemetryFormatSpec_TelemetryFormatSpecId] DEFAULT (newid()) NOT NULL,
    [TelemetryFormatId]     NVARCHAR (32)    NOT NULL,
    [SpecOrder]             INT              CONSTRAINT [DF_TelemetryFormatSpec_SpecOrder] DEFAULT ((0)) NOT NULL,
    [Url]                   NVARCHAR (512)   CONSTRAINT [DF_TelemetryFormatSpec_Url] DEFAULT ('') NOT NULL,
    [Version]               NVARCHAR (128)   CONSTRAINT [DF_TelemetryFormatSpec_Version] DEFAULT ('') NOT NULL,
    [SpecDate]              DATE             NULL,
    [Note]                  NVARCHAR (1024)  CONSTRAINT [DF_TelemetryFormatSpec_Note] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK_TelemetryFormatSpec] PRIMARY KEY CLUSTERED ([TelemetryFormatSpecId] ASC),
    CONSTRAINT [FK_TelemetryFormatSpec_TelemetryFormat] FOREIGN KEY ([TelemetryFormatId]) REFERENCES [x360ce].[TelemetryFormat] ([TelemetryFormatId]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_TelemetryFormatSpec_TelemetryFormatId_SpecOrder]
    ON [x360ce].[TelemetryFormatSpec]([TelemetryFormatId] ASC, [SpecOrder] ASC);
