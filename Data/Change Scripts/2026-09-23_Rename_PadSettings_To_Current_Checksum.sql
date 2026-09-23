/*
    Gives every row of [dbo].[x360ce_PadSettings] the checksum the program and the server compute
    for it today, and points the settings that use it at that checksum.

    Why
        A pad setting is stored under the checksum of its contents, and the server looks up the
        setting a program saves by computing that checksum again. The way the checksum is made
        has changed twice: until 2015 it covered unnamed values in a fixed order, and from 2015
        to 2020 the version 3 program used a list of named values in another order. Rows saved
        in those years carry a checksum nothing computes any more. When anyone saves the same
        mapping again, the server does not find the old row, stores a second copy under the new
        checksum, and the people who use that mapping are counted in two places. A preset loaded
        from the service and saved back becomes a new row every time.

    What it does
        1. Computes the current checksum of every row, exactly as PadSetting.CleanAndGetCheckSum
           does: each setting that differs from its default becomes the line "Name=Value", the
           lines are put in a fixed order and joined with CR LF, and the MD5 of those ASCII bytes
           is the checksum. The order is given by [Rank] below, which is the program's order.
        2. Adds a row under each new checksum that does not exist yet, copied from the old row.
        3. Points each setting at its row's new checksum and moves its summaries with it.
           Products, programs and completion points depend on what a row holds, not on its
           name, and stay as they are. Summaries with no setting behind them, which the presets
           list still shows, follow their row as well.
        4. Deletes the old rows that no setting uses any more.

    What it costs
        Rows saved since 2020 already carry the current checksum and are not touched. The first
        step reads the whole table once; the settings are changed in batches of @Batch rows by
        their primary key, each batch with its summaries in its own transaction, so the site
        keeps answering while it runs and a stop leaves nothing half done. A program saving at
        the same time computes the current checksum, which is the one this script writes.

    Requires
        SQL Server 2016 or later, for HASHBYTES over more than 8000 bytes.

    Safe to run twice, and safe to stop: a row already renamed is not renamed again, and a run
    that stopped part way finishes the rest. With @Apply = 0 it only reports what it would do.

    Keep [Rank] in step with PadSetting.CleanAndGetCheckSum: the test
    PadSettingChecksumTest.The_change_script_measures_as_the_program_does compares them.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

DECLARE @Apply BIT = 0;
DECLARE @Batch INT = 10000;

IF OBJECT_ID(N'[dbo].[x360ce_PadSettings]', 'U') IS NULL
BEGIN
    RAISERROR('Table [dbo].[x360ce_PadSettings] does not exist. Nothing to rename.', 16, 1);
    RETURN;
END;

/* 1. The checksum each row has today, for the rows whose stored checksum differs. */
IF OBJECT_ID('tempdb..#Map') IS NOT NULL DROP TABLE #Map;
CREATE TABLE #Map ([Old] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, [New] UNIQUEIDENTIFIER NOT NULL);

WITH [Current] AS (
    SELECT
        p.[PadSettingChecksum] AS [Old],
        ISNULL(CAST(HASHBYTES('MD5', c.[Lines]) AS UNIQUEIDENTIFIER), '00000000-0000-0000-0000-000000000000') AS [New]
    FROM [dbo].[x360ce_PadSettings] p
    CROSS APPLY (
        SELECT STUFF((
            SELECT CHAR(13) + CHAR(10) + v.[Name] + '=' + v.[Value]
            FROM (VALUES
        ( 1, 'AxisToDPadDeadZone', p.[AxisToDPadDeadZone], '256'),
        ( 2, 'AxisToDPadEnabled', p.[AxisToDPadEnabled], '0'),
        ( 3, 'AxisToDPadOffset', p.[AxisToDPadOffset], '0'),
        ( 4, 'ButtonA', p.[ButtonA], '0'),
        ( 5, 'ButtonADeadZone', p.[ButtonADeadZone], '0'),
        ( 6, 'ButtonB', p.[ButtonB], '0'),
        ( 7, 'ButtonBack', p.[ButtonBack], '0'),
        ( 8, 'ButtonBackDeadZone', p.[ButtonBackDeadZone], '0'),
        ( 9, 'ButtonBDeadZone', p.[ButtonBDeadZone], '0'),
        (10, 'ButtonGuide', p.[ButtonGuide], '0'),
        (11, 'ButtonStart', p.[ButtonStart], '0'),
        (12, 'ButtonStartDeadZone', p.[ButtonStartDeadZone], '0'),
        (13, 'ButtonX', p.[ButtonX], '0'),
        (14, 'ButtonXDeadZone', p.[ButtonXDeadZone], '0'),
        (15, 'ButtonY', p.[ButtonY], '0'),
        (16, 'ButtonYDeadZone', p.[ButtonYDeadZone], '0'),
        (17, 'DPad', p.[DPad], '0'),
        (18, 'DPadDown', p.[DPadDown], '0'),
        (19, 'DPadDownDeadZone', p.[DPadDownDeadZone], '0'),
        (20, 'DPadLeft', p.[DPadLeft], '0'),
        (21, 'DPadLeftDeadZone', p.[DPadLeftDeadZone], '0'),
        (22, 'DPadRight', p.[DPadRight], '0'),
        (23, 'DPadRightDeadZone', p.[DPadRightDeadZone], '0'),
        (24, 'DPadUp', p.[DPadUp], '0'),
        (25, 'DPadUpDeadZone', p.[DPadUpDeadZone], '0'),
        (26, 'ForceEnable', p.[ForceEnable], '0'),
        (27, 'ForceOverall', p.[ForceOverall], '100'),
        (28, 'ForcePassThrough', p.[ForcePassThrough], '0'),
        (29, 'ForcePassThroughIndex', p.[ForcePassThroughIndex], '0'),
        (30, 'ForceSpringEnable', p.[ForceSpringEnable], '0'),
        (31, 'ForceSpringStrength', p.[ForceSpringStrength], '0'),
        (32, 'ForceSwapMotor', p.[ForceSwapMotor], '0'),
        (33, 'ForceType', p.[ForceType], '0'),
        (34, 'GamePadType', p.[GamePadType], '0'),
        (35, 'LeftMotorDirection', p.[LeftMotorDirection], '0'),
        (36, 'LeftMotorPeriod', p.[LeftMotorPeriod], '0'),
        (37, 'LeftMotorStrength', p.[LeftMotorStrength], '100'),
        (38, 'LeftShoulder', p.[LeftShoulder], '0'),
        (39, 'LeftShoulderDeadZone', p.[LeftShoulderDeadZone], '0'),
        (40, 'LeftThumbAntiDeadZoneX', p.[LeftThumbAntiDeadZoneX], '0'),
        (41, 'LeftThumbAntiDeadZoneY', p.[LeftThumbAntiDeadZoneY], '0'),
        (42, 'LeftThumbAxisX', p.[LeftThumbAxisX], '0'),
        (43, 'LeftThumbAxisY', p.[LeftThumbAxisY], '0'),
        (44, 'LeftThumbButton', p.[LeftThumbButton], '0'),
        (45, 'LeftThumbButtonDeadZone', p.[LeftThumbButtonDeadZone], '0'),
        (46, 'LeftThumbDeadZoneX', p.[LeftThumbDeadZoneX], '0'),
        (47, 'LeftThumbDeadZoneY', p.[LeftThumbDeadZoneY], '0'),
        (48, 'LeftThumbDown', p.[LeftThumbDown], '0'),
        (49, 'LeftThumbLeft', p.[LeftThumbLeft], '0'),
        (50, 'LeftThumbLinearX', p.[LeftThumbLinearX], '0'),
        (51, 'LeftThumbLinearY', p.[LeftThumbLinearY], '0'),
        (52, 'LeftThumbRight', p.[LeftThumbRight], '0'),
        (53, 'LeftThumbUp', p.[LeftThumbUp], '0'),
        (54, 'LeftTrigger', p.[LeftTrigger], '0'),
        (55, 'LeftTriggerAntiDeadZone', p.[LeftTriggerAntiDeadZone], '0'),
        (56, 'LeftTriggerDeadZone', p.[LeftTriggerDeadZone], '0'),
        (57, 'LeftTriggerLinear', p.[LeftTriggerLinear], '0'),
        (58, 'PassThrough', p.[PassThrough], '0'),
        (59, 'RightMotorDirection', p.[RightMotorDirection], '0'),
        (60, 'RightMotorPeriod', p.[RightMotorPeriod], '0'),
        (61, 'RightMotorStrength', p.[RightMotorStrength], '100'),
        (62, 'RightShoulder', p.[RightShoulder], '0'),
        (63, 'RightShoulderDeadZone', p.[RightShoulderDeadZone], '0'),
        (64, 'RightThumbAntiDeadZoneX', p.[RightThumbAntiDeadZoneX], '0'),
        (65, 'RightThumbAntiDeadZoneY', p.[RightThumbAntiDeadZoneY], '0'),
        (66, 'RightThumbAxisX', p.[RightThumbAxisX], '0'),
        (67, 'RightThumbAxisY', p.[RightThumbAxisY], '0'),
        (68, 'RightThumbButton', p.[RightThumbButton], '0'),
        (69, 'RightThumbButtonDeadZone', p.[RightThumbButtonDeadZone], '0'),
        (70, 'RightThumbDeadZoneX', p.[RightThumbDeadZoneX], '0'),
        (71, 'RightThumbDeadZoneY', p.[RightThumbDeadZoneY], '0'),
        (72, 'RightThumbDown', p.[RightThumbDown], '0'),
        (73, 'RightThumbLeft', p.[RightThumbLeft], '0'),
        (74, 'RightThumbLinearX', p.[RightThumbLinearX], '0'),
        (75, 'RightThumbLinearY', p.[RightThumbLinearY], '0'),
        (76, 'RightThumbRight', p.[RightThumbRight], '0'),
        (77, 'RightThumbUp', p.[RightThumbUp], '0'),
        (78, 'RightTrigger', p.[RightTrigger], '0'),
        (79, 'RightTriggerAntiDeadZone', p.[RightTriggerAntiDeadZone], '0'),
        (80, 'RightTriggerDeadZone', p.[RightTriggerDeadZone], '0'),
        (81, 'RightTriggerLinear', p.[RightTriggerLinear], '0'),
        (82, 'WheelRange', p.[WheelRange], '0')
            ) v([Rank], [Name], [Value], [Default])
            /* A value counts when it is not empty and not its default. DATALENGTH, because
               "=" ignores trailing spaces and the program does not. */
            WHERE DATALENGTH(v.[Value]) > 0
                AND NOT (v.[Value] = v.[Default] AND DATALENGTH(v.[Value]) = DATALENGTH(v.[Default]))
            ORDER BY v.[Rank]
            FOR XML PATH(''), TYPE).value('.', 'VARCHAR(MAX)'), 1, 2, '') AS [Lines]
    ) c
)
INSERT INTO #Map ([Old], [New])
SELECT [Old], [New] FROM [Current] WHERE [New] <> [Old];

/* The settings to move, found once by a single pass over the settings table. */
IF OBJECT_ID('tempdb..#Settings') IS NOT NULL DROP TABLE #Settings;
CREATE TABLE #Settings ([SettingId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, [New] UNIQUEIDENTIFIER NOT NULL);
INSERT INTO #Settings ([SettingId], [New])
SELECT s.[SettingId], m.[New]
FROM [dbo].[x360ce_Settings] s
INNER JOIN #Map m ON m.[Old] = s.[PadSettingChecksum];

DECLARE @Rows INT = (SELECT COUNT(*) FROM [dbo].[x360ce_PadSettings]);
DECLARE @Renamed INT = (SELECT COUNT(*) FROM #Map);
DECLARE @Targets INT = (SELECT COUNT(DISTINCT [New]) FROM #Map);
DECLARE @Existing INT = (SELECT COUNT(DISTINCT m.[New]) FROM #Map m WHERE EXISTS (SELECT 1 FROM [dbo].[x360ce_PadSettings] p WHERE p.[PadSettingChecksum] = m.[New]));
DECLARE @Moved INT = (SELECT COUNT(*) FROM #Settings);
PRINT CONCAT('Pad settings:              ', @Rows);
PRINT CONCAT('Under an old checksum:     ', @Renamed);
PRINT CONCAT('Distinct current checksums:', ' ', @Targets, ' (', @Existing, ' already stored)');
PRINT CONCAT('Settings to point anew:    ', @Moved);

IF @Apply = 0
BEGIN
    PRINT 'Report only. Set @Apply = 1 to rename.';
    RETURN;
END;

/* 2. A row under each new checksum that is not stored yet, copied from one of its old rows. */
DECLARE @Columns NVARCHAR(MAX) = STUFF((
    SELECT N', ' + QUOTENAME(c.[name])
    FROM sys.columns c
    WHERE c.[object_id] = OBJECT_ID(N'[dbo].[x360ce_PadSettings]') AND c.[name] <> N'PadSettingChecksum'
    ORDER BY c.[column_id]
    FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, N'');
DECLARE @Source NVARCHAR(MAX) = REPLACE(N'p.' + @Columns, N', [', N', p.[');
DECLARE @Insert NVARCHAR(MAX) = N'
INSERT INTO [dbo].[x360ce_PadSettings] ([PadSettingChecksum], ' + @Columns + N')
SELECT f.[New], ' + @Source + N'
FROM (
    SELECT [Old], [New], ROW_NUMBER() OVER (PARTITION BY [New] ORDER BY [Old]) AS [Pick]
    FROM #Map m
    WHERE NOT EXISTS (SELECT 1 FROM [dbo].[x360ce_PadSettings] e WHERE e.[PadSettingChecksum] = m.[New])
) f
INNER JOIN [dbo].[x360ce_PadSettings] p ON p.[PadSettingChecksum] = f.[Old]
WHERE f.[Pick] = 1;';
EXEC sys.sp_executesql @Insert;
PRINT CONCAT('Rows added:                ', @@ROWCOUNT);

/* 3. The settings, in batches by their primary key, each batch with its summaries in one
      transaction. The settings trigger is off only inside that transaction: it recounts products,
      programs and completion points too, none of which a new name for the same row changes, and
      doing that for every batch would hold the table for hours. Other sessions wait for the
      batch to commit and then find the trigger on. */
IF OBJECT_ID('tempdb..#Next') IS NOT NULL DROP TABLE #Next;
CREATE TABLE #Next ([SettingId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, [New] UNIQUEIDENTIFIER NOT NULL);
IF OBJECT_ID('tempdb..#Moved') IS NOT NULL DROP TABLE #Moved;
CREATE TABLE #Moved (
    [Old] UNIQUEIDENTIFIER, [New] UNIQUEIDENTIFIER, [ProductGuid] UNIQUEIDENTIFIER,
    [ProductName] NVARCHAR(256), [FileName] NVARCHAR(128), [FileProductName] NVARCHAR(256));
DECLARE @inserted [dbo].[x360ce_SummariesTableType];
DECLARE @deleted [dbo].[x360ce_SummariesTableType];
DECLARE @Done INT = 0, @Step INT = 1;
WHILE 1 = 1
BEGIN
    TRUNCATE TABLE #Next;
    TRUNCATE TABLE #Moved;
    DELETE FROM @inserted;
    DELETE FROM @deleted;
    INSERT INTO #Next ([SettingId], [New]) SELECT TOP (@Batch) [SettingId], [New] FROM #Settings ORDER BY [SettingId];
    SET @Step = @@ROWCOUNT;
    IF @Step = 0 BREAK;
    BEGIN TRANSACTION;
    DISABLE TRIGGER [dbo].[TR_x360ce_Settings_AfterAction] ON [dbo].[x360ce_Settings];
    UPDATE s SET s.[PadSettingChecksum] = n.[New]
    OUTPUT deleted.[PadSettingChecksum], inserted.[PadSettingChecksum], inserted.[ProductGuid],
        inserted.[ProductName], inserted.[FileName], inserted.[FileProductName] INTO #Moved
    FROM [dbo].[x360ce_Settings] s
    INNER JOIN #Next n ON n.[SettingId] = s.[SettingId];
    ENABLE TRIGGER [dbo].[TR_x360ce_Settings_AfterAction] ON [dbo].[x360ce_Settings];
    INSERT INTO @deleted ([PadSettingChecksum], [ProductGuid], [ProductName], [FileName], [FileProductName])
    SELECT DISTINCT [Old], [ProductGuid], [ProductName], [FileName], [FileProductName] FROM #Moved;
    INSERT INTO @inserted ([PadSettingChecksum], [ProductGuid], [ProductName], [FileName], [FileProductName])
    SELECT DISTINCT [New], [ProductGuid], [ProductName], [FileName], [FileProductName] FROM #Moved;
    EXEC [dbo].[x360ce_UpdateSummariesTable] @inserted, @deleted;
    COMMIT TRANSACTION;
    DELETE s FROM #Settings s INNER JOIN #Next n ON n.[SettingId] = s.[SettingId];
    SET @Done += @Step;
    IF @Done % (@Batch * 20) = 0
        RAISERROR('Settings pointed anew: %d', 0, 1, @Done) WITH NOWAIT;
END;
PRINT CONCAT('Settings pointed anew:     ', @Done);

/* 3b. Summaries with no setting behind them, which step 3 cannot reach. The presets list still
       shows them, so they follow their row to its new checksum. Where that summary already exists
       under the new checksum, or two of them become one, one is kept and the rest go. */
IF OBJECT_ID('tempdb..#Summaries') IS NOT NULL DROP TABLE #Summaries;
SELECT m.[SummaryId], map.[New],
    ROW_NUMBER() OVER (PARTITION BY m.[ProductGuid], m.[FileName], m.[FileProductName], map.[New] ORDER BY m.[Users] DESC, m.[SummaryId]) AS [Pick],
    CASE WHEN EXISTS (
        SELECT 1 FROM [dbo].[x360ce_Summaries] n
        WHERE n.[ProductGuid] = m.[ProductGuid] AND n.[FileName] = m.[FileName]
            AND n.[FileProductName] = m.[FileProductName] AND n.[PadSettingChecksum] = map.[New]
    ) THEN 1 ELSE 0 END AS [Taken]
INTO #Summaries
FROM [dbo].[x360ce_Summaries] m
INNER JOIN #Map map ON map.[Old] = m.[PadSettingChecksum];
BEGIN TRANSACTION;
DELETE m FROM [dbo].[x360ce_Summaries] m
INNER JOIN #Summaries t ON t.[SummaryId] = m.[SummaryId]
WHERE t.[Taken] = 1 OR t.[Pick] > 1;
DECLARE @SummariesRemoved INT = @@ROWCOUNT;
UPDATE m SET m.[PadSettingChecksum] = t.[New]
FROM [dbo].[x360ce_Summaries] m
INNER JOIN #Summaries t ON t.[SummaryId] = m.[SummaryId]
WHERE t.[Taken] = 0 AND t.[Pick] = 1;
DECLARE @SummariesMoved INT = @@ROWCOUNT;
COMMIT TRANSACTION;
PRINT CONCAT('Summaries without settings:', ' ', @SummariesMoved, ' moved, ', @SummariesRemoved, ' merged');

/* 4. The old rows that no setting uses any more. A setting saved under an old checksum while this
      ran keeps its row; running the script again moves it. */
IF OBJECT_ID('tempdb..#Gone') IS NOT NULL DROP TABLE #Gone;
CREATE TABLE #Gone ([PadSettingChecksum] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);
INSERT INTO #Gone ([PadSettingChecksum])
SELECT [Old] FROM #Map
EXCEPT
SELECT [PadSettingChecksum] FROM [dbo].[x360ce_Settings];
DECLARE @Removed INT = 0;
SET @Step = 1;
WHILE @Step > 0
BEGIN
    DELETE TOP (@Batch) p
    FROM [dbo].[x360ce_PadSettings] p
    INNER JOIN #Gone g ON g.[PadSettingChecksum] = p.[PadSettingChecksum];
    SET @Step = @@ROWCOUNT;
    SET @Removed += @Step;
END;
PRINT CONCAT('Old rows removed:          ', @Removed);
