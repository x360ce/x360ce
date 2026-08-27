/*
    Widens the mapping columns of [dbo].[x360ce_PadSettings] from VARCHAR(16) to VARCHAR(128).

    Why
        A mapping column used to hold only a control name, such as 'a1', which fits in sixteen
        characters. It can now also hold a formula, such as '=sign(a1)*deadzone(abs(a1),0.24)',
        which does not. Sixteen characters is the smallest limit in the chain, so it, and not the
        program, decides how long a formula may be.

    What it costs
        Nothing measurable. VARCHAR does not pad, so no row grows and no page is rewritten;
        widening within the 8000 byte VARCHAR limit is a change to the catalogue only. No index
        touches these columns, so nothing is rebuilt and no query plan changes. Every stored
        checksum stays valid, because the checksum is taken over the values and the values are
        unchanged.

    Row size
        The row's declared maximum goes from 1,248 bytes to 4,608, against a limit of 8,060. That
        limit is why 128 was chosen: at 256 the declared row would be 8,448 and the table would no
        longer be creatable.

    Safe to run twice. A column that is already 128 or wider is left alone.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @Table SYSNAME = N'[dbo].[x360ce_PadSettings]';
DECLARE @NewSize INT = 128;

IF OBJECT_ID(@Table, 'U') IS NULL
BEGIN
    RAISERROR('Table %s does not exist. Nothing to widen.', 16, 1, @Table);
    RETURN;
END;

/* The columns that hold a mapping. Every other column in this table holds a number, and a number
   never needed more than sixteen characters. */
DECLARE @Mapping TABLE ([Name] SYSNAME PRIMARY KEY);
INSERT INTO @Mapping ([Name]) VALUES
    ('ButtonA'),
    ('ButtonB'),
    ('ButtonBack'),
    ('ButtonGuide'),
    ('ButtonStart'),
    ('ButtonX'),
    ('ButtonY'),
    ('DPad'),
    ('DPadDown'),
    ('DPadLeft'),
    ('DPadRight'),
    ('DPadUp'),
    ('LeftShoulder'),
    ('LeftThumbAxisX'),
    ('LeftThumbAxisY'),
    ('LeftThumbButton'),
    ('LeftThumbDown'),
    ('LeftThumbLeft'),
    ('LeftThumbRight'),
    ('LeftThumbUp'),
    ('LeftTrigger'),
    ('RightShoulder'),
    ('RightThumbAxisX'),
    ('RightThumbAxisY'),
    ('RightThumbButton'),
    ('RightThumbDown'),
    ('RightThumbLeft'),
    ('RightThumbRight'),
    ('RightThumbUp'),
    ('RightTrigger');

/* A name that is not in the table means this script and the schema have drifted apart. Say so
   rather than silently widening a smaller set than intended. */
IF EXISTS (
    SELECT 1 FROM @Mapping m
    WHERE NOT EXISTS (
        SELECT 1 FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(@Table) AND c.name = m.[Name]))
BEGIN
    SELECT [Missing column] = m.[Name] FROM @Mapping m
    WHERE NOT EXISTS (
        SELECT 1 FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(@Table) AND c.name = m.[Name]);
    RAISERROR('The columns listed above are not in %s. Widen nothing until that is explained.', 16, 1, @Table);
    RETURN;
END;

/* An index over one of these would make ALTER COLUMN fail part way through, leaving the table
   half widened. Find out before starting rather than during. */
IF EXISTS (
    SELECT 1
    FROM sys.index_columns ic
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    JOIN @Mapping m ON m.[Name] = c.name
    WHERE ic.object_id = OBJECT_ID(@Table))
BEGIN
    RAISERROR('An index covers one of the mapping columns. Drop it, widen, then recreate it.', 16, 1);
    RETURN;
END;

DECLARE @Name SYSNAME, @Sql NVARCHAR(MAX), @Widened INT = 0, @Skipped INT = 0;

/* Counted before anything is altered. Counting afterwards would count the columns this run just
   widened and report them as having needed nothing. */
SELECT @Skipped = COUNT(*)
FROM sys.columns c
JOIN @Mapping m ON m.[Name] = c.name
WHERE c.object_id = OBJECT_ID(@Table)
  AND (c.max_length >= @NewSize OR c.max_length = -1);

DECLARE Widen CURSOR LOCAL FAST_FORWARD FOR
    SELECT c.name
    FROM sys.columns c
    JOIN @Mapping m ON m.[Name] = c.name
    WHERE c.object_id = OBJECT_ID(@Table)
      AND c.max_length <> -1               /* already VARCHAR(MAX): leave it */
      AND c.max_length < @NewSize          /* already wide enough: leave it */
    ORDER BY c.name;

OPEN Widen;
FETCH NEXT FROM Widen INTO @Name;
WHILE @@FETCH_STATUS = 0
BEGIN
    /* NOT NULL is repeated deliberately. ALTER COLUMN without it makes the column nullable.
       The DEFAULT constraint is unaffected and does not need dropping. */
    SET @Sql = N'ALTER TABLE ' + @Table + N' ALTER COLUMN ' + QUOTENAME(@Name)
             + N' VARCHAR(' + CAST(@NewSize AS NVARCHAR(10)) + N') NOT NULL;';
    EXEC sp_executesql @Sql;
    SET @Widened = @Widened + 1;
    FETCH NEXT FROM Widen INTO @Name;
END;
CLOSE Widen;
DEALLOCATE Widen;

PRINT 'Widened ' + CAST(@Widened AS VARCHAR(10)) + ' column(s) to VARCHAR(' + CAST(@NewSize AS VARCHAR(10)) + ').';
PRINT 'Already wide enough: ' + CAST(@Skipped AS VARCHAR(10)) + ' column(s).';
GO

/* What the table looks like afterwards. All thirty mapping columns should read 128, and nothing
   else should appear in this list. */
SELECT
    [Column] = c.name,
    [Size]   = c.max_length,
    [Null]   = CASE WHEN c.is_nullable = 1 THEN 'yes' ELSE 'no' END
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'[dbo].[x360ce_PadSettings]')
  AND c.system_type_id = TYPE_ID('varchar')
  AND c.max_length <> 16
ORDER BY c.name;
GO
