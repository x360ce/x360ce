-- What each telemetry game is started with so that its mods load, such as -nobattleye for GTA V.
-- Safe to run again: the column is added only when it is missing, and the values are set each time.
-- Run: sqlcmd -S localhost -E -C -f 65001 -d x360ce -b -i "20261127-01-TelemetryGameLaunchArguments.sql"
IF COL_LENGTH(N'[x360ce].[TelemetryGame]', N'LaunchArguments') IS NULL
ALTER TABLE [x360ce].[TelemetryGame] ADD [LaunchArguments] NVARCHAR (256) CONSTRAINT [DF_TelemetryGame_LaunchArguments] DEFAULT ('') NOT NULL;
GO
UPDATE [x360ce].[TelemetryGame] SET [LaunchArguments] = N'-nobattleye' WHERE [FileName] IN (N'GTA5.exe', N'GTA5_Enhanced.exe') AND [LaunchArguments] <> N'-nobattleye';
GO
