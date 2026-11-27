CREATE TABLE [x360ce].[TelemetryGameStep] (
    [TelemetryGameStepId] UNIQUEIDENTIFIER CONSTRAINT [DF_TelemetryGameStep_TelemetryGameStepId] DEFAULT (newid()) NOT NULL,
    [TelemetryGameId]     UNIQUEIDENTIFIER NOT NULL,
    [StepOrder]           INT              CONSTRAINT [DF_TelemetryGameStep_StepOrder] DEFAULT ((0)) NOT NULL,
    [IsWarning]           BIT              CONSTRAINT [DF_TelemetryGameStep_IsWarning] DEFAULT ((0)) NOT NULL,
    [Text]                NVARCHAR (1024)  CONSTRAINT [DF_TelemetryGameStep_Text] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK_TelemetryGameStep] PRIMARY KEY CLUSTERED ([TelemetryGameStepId] ASC),
    CONSTRAINT [FK_TelemetryGameStep_TelemetryGame] FOREIGN KEY ([TelemetryGameId]) REFERENCES [x360ce].[TelemetryGame] ([TelemetryGameId]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_TelemetryGameStep_TelemetryGameId_IsWarning_StepOrder]
    ON [x360ce].[TelemetryGameStep]([TelemetryGameId] ASC, [IsWarning] ASC, [StepOrder] ASC);
