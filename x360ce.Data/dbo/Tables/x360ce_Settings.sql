CREATE TABLE [dbo].[x360ce_Settings] (
    [SettingId]          UNIQUEIDENTIFIER NOT NULL,
    [InstanceGuid]       UNIQUEIDENTIFIER NOT NULL,
    [InstanceName]       NVARCHAR (256)   NOT NULL,
    [ProductGuid]        UNIQUEIDENTIFIER NOT NULL,
    [ProductName]        NVARCHAR (256)   NOT NULL,
    [DeviceType]         INT              NOT NULL,
    [MapTo]              INT              CONSTRAINT [DF_x360ce_Settings_MapTo] DEFAULT ((0)) NOT NULL,
    [FileName]           NVARCHAR (128)   NOT NULL,
    [FileProductName]    NVARCHAR (256)   NOT NULL,
    [Comment]            NVARCHAR (1024)  NOT NULL,
    [DateCreated]        DATETIME         CONSTRAINT [DF_x360ce_Settings_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]        DATETIME         CONSTRAINT [DF_x360ce_Settings_DateUpdated] DEFAULT (getdate()) NOT NULL,
    [DateSelected]       DATETIME         CONSTRAINT [DF_x360ce_Settings_DateSelected] DEFAULT (getdate()) NOT NULL,
    [IsEnabled]          BIT              CONSTRAINT [DF_x360ce_Settings_IsEnabled] DEFAULT ((1)) NOT NULL,
    [PadSettingChecksum] UNIQUEIDENTIFIER NOT NULL,
    [Completion]         INT              CONSTRAINT [DF_x360ce_Settings_Completion] DEFAULT ((0)) NOT NULL,
    [Checksum]           UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_Settings_Checksum] DEFAULT ('00000000-0000-0000-0000-000000000000') NOT NULL,
    [ComputerId]         UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_Settings_ComputerId] DEFAULT ('00000000-0000-0000-0000-000000000000') NOT NULL,
    [ProfileId]          UNIQUEIDENTIFIER CONSTRAINT [DF_x360ce_Settings_ProfileId] DEFAULT ('00000000-0000-0000-0000-000000000000') NOT NULL,
    CONSTRAINT [PK_x360ce_Settings] PRIMARY KEY CLUSTERED ([SettingId] ASC)
);


















GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Settings_ProductGuid_InstanceGuid]
    ON [dbo].[x360ce_Settings]([ProductGuid] ASC, [InstanceGuid] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Settings_ProductGuid_ProductName_FileName]
    ON [dbo].[x360ce_Settings]([ProductGuid] ASC, [ProductName] ASC, [FileName] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Settings_InstanceGuid_IsEnabled]
    ON [dbo].[x360ce_Settings]([InstanceGuid] ASC, [IsEnabled] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Settings]
    ON [dbo].[x360ce_Settings]([ProductGuid] ASC, [FileName] ASC, [FileProductName] ASC, [PadSettingChecksum] ASC);


GO
CREATE TRIGGER [dbo].[TR_x360ce_Settings_AfterAction]
   ON  [dbo].[x360ce_Settings]
   AFTER INSERT, DELETE, UPDATE
AS 
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-----------------------------------------------------------
	-- Update Completion points.
	-----------------------------------------------------------

	--PRINT CAST(OBJECT_NAME(@@PROCID) AS sysname) + ' TRIGGER_NESTLEVEL: ' + CAST(TRIGGER_NESTLEVEL() AS sysname)

	-- This trigger will run second time if table is updated inside this trigger.
	-- If trigger was triggered from this trigger then return.
	IF ((SELECT TRIGGER_NESTLEVEL()) > 1)
		RETURN


	DECLARE @table AS TABLE (SettingId uniqueidentifier PRIMARY KEY, Completion int NOT NULL)

	-- Recalculate completion values.	
	INSERT INTO @table
	SELECT
		s.SettingId,
		dbo.x360ce_GetCompletionPoints(s.PadSettingChecksum, s.InstanceGuid)
	FROM x360ce_Settings s
	INNER JOIN inserted i ON s.SettingId = i.SettingId

	-- Update completion values.
	UPDATE s SET
		s.Completion = dbo.x360ce_GetCompletionPoints(s.PadSettingChecksum, s.InstanceGuid)
	FROM x360ce_Settings s
	INNER JOIN @table t ON t.SettingId = s.SettingId
	-- Update only if changed.
	WHERE s.Completion <> t.Completion

	/* TEST:
	SELECT TOP 1 Completion FROM x360ce_Settings where SettingId = '3206CDDC-A941-4236-B874-00005012DF91'
	-- '0' will be overridden by recalculated value.
	UPDATE x360ce_Settings SET Completion = 0 WHERE SettingId = '3206CDDC-A941-4236-B874-00005012DF91'
	*/

	-----------------------------------------------------------
	-- Track changes.
	-----------------------------------------------------------

	/*
	SELECT FileProductName, * FROM x360ce_Settings WHERE SettingId = '20BF42C0-63BB-483A-AF77-0001C26D3637'
	DELETE x360ce_ChangeLogs
	
	UPDATE x360ce_Settings SET FileName='F1RaceStars', FileProductName = 'F1RaceStars.exe' WHERE SettingId = '20BF42C0-63BB-483A-AF77-0001C26D3637'
	UPDATE x360ce_Settings SET Completion = 10 WHERE SettingId = '20BF42C0-63BB-483A-AF77-0001C26D3637'
	*/

	DECLARE @TableName nvarchar(128) = 'x360ce_Settings'

	-----------------------------------------------------------
	-- Skip large updates.
	-----------------------------------------------------------

	-- Select list of updated columns.
	DECLARE @updated_columns AS TABLE([column] sysname)
	-- Get list of updated columns.
	INSERT INTO @updated_columns
	SELECT COLUMN_NAME FROM (
		SELECT COLUMN_NAME,
			COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'ColumnID') AS COLUMN_ID
		FROM INFORMATION_SCHEMA.COLUMNS  
		WHERE TABLE_NAME = @TableName
	) t1 WHERE sys.fn_IsBitSetInBitmask (COLUMNS_UPDATED(), COLUMN_ID) <> 0

	DECLARE @columns varchar(max) = ''
	SELECT @columns = @columns + [column] + ', '
	FROM @updated_columns

	IF LEN(@columns) > 2 AND RIGHT(@columns, 2) = ', '
		SET @columns = LEFT(@columns, LEN(@columns) - 1);

	-- Add excluded columns
	DELETE FROM @updated_columns WHERE [column] IN ('Completion')

	-- Return if heavy updates inside the trigger is not needed.
	IF NOT EXISTS(SELECT * FROM @updated_columns)
	BEGIN
		--PRINT 'Skip'
		RETURN
	END

	PRINT 'Updated Columns: ' + @columns

	-----------------------------------------------------------
	-- Track changes.
	-----------------------------------------------------------

	DECLARE @TrackingEnabled bit = 0

	IF @TrackingEnabled = 1
	BEGIN
	
		-- Get information from context: <profile_id><comment_size><comment><session_id>
		DECLARE
			@profile_id int = SUBSTRING (CONTEXT_INFO(), 1 , 4),
			@session_id int = SUBSTRING (CONTEXT_INFO(), 128 - 4 + 1, 4),
			@comment_size int = SUBSTRING(CONTEXT_INFO(), 5, 4),
			@comment varchar(116)
			IF @comment_size > 0
				SET @comment = CAST(substring ( context_info ( ) , 9 , @comment_size ) as varchar(116)) + '. '

		DECLARE
			@ApplicationId uniqueidentifier = '00000000-0000-0000-0000-000000000000',
			@UserId uniqueidentifier = '00000000-0000-0000-0000-000000000000'

		-- INSERT
		INSERT INTO x360ce_ChangeLogs([ApplicationId],[UserId],[TableName],[IsDelete],[IsInsert],[Reference],[OldValues],[NewValues])
		SELECT
			@ApplicationId,
			@UserId,
			[TableName] = @TableName,
			[IsDelete] = 0,
			[IsInsert] = 1,
			[Reference] = CAST(i.SettingId as varchar(128)),
			[oldValues] = '',
			[newValues] = (SELECT * FROM inserted i2 where i.SettingId=i2.SettingId FOR XML PATH('row'), ELEMENTS XSINIL)
		FROM inserted i
		LEFT JOIN deleted d ON d.SettingId = i.SettingId
		WHERE d.SettingId IS NULL

		-- DELETE, UPDATE
		INSERT INTO x360ce_ChangeLogs([ApplicationId],[UserId],[TableName],[IsDelete],[IsInsert],[Reference],[OldValues],[NewValues])
		SELECT
			@ApplicationId,
			@UserId,
			[TableName] = @TableName,
			[IsDelete] = 1,
			[IsInsert] = CASE WHEN i.SettingId IS NULL THEN 0 ELSE 1 END,
			[Reference] = CAST(d.SettingId as varchar(128)),
			[oldValues] = (SELECT * FROM deleted d2 where d2.SettingId=d.SettingId FOR XML PATH('row'), ELEMENTS XSINIL),
			[newValues] =
				CASE WHEN i.SettingId IS NULL
					THEN ''
					ELSE (SELECT * FROM inserted i2 where i2.SettingId=i.SettingId FOR XML PATH('row'), ELEMENTS XSINIL)
				END
		FROM deleted d
		LEFT JOIN inserted i ON i.SettingId = d.SettingId

	END

	-----------------------------------------------------------

	DECLARE @inserted as x360ce_SummariesTableType
	INSERT INTO @inserted SELECT DISTINCT PadSettingChecksum, ProductGuid, ProductName, [FileName], FileProductName FROM inserted
	DECLARE @deleted as x360ce_SummariesTableType
	INSERT INTO @deleted SELECT DISTINCT PadSettingChecksum, ProductGuid, ProductName, [FileName], FileProductName FROM deleted

	-----------------------------------------------------------
	-- Update [x360ce_Products] and [x360ce_Summaries]
	-----------------------------------------------------------

	EXEC [dbo].[x360ce_UpdateProductsTable] @inserted, @deleted
	EXEC [dbo].[x360ce_UpdateSummariesTable] @inserted, @deleted

	-----------------------------------------------------------
	-- Update [x360ce_Programs]
	-----------------------------------------------------------

	DECLARE @inserted2 as x360ce_ProgramsTableType
	INSERT INTO @inserted2 SELECT DISTINCT [FileName], FileProductName FROM inserted
	DECLARE @deleted2 as x360ce_ProgramsTableType
	INSERT INTO @deleted2 SELECT DISTINCT [FileName], FileProductName FROM deleted
	EXEC [dbo].[x360ce_UpdateProgramsTable] @inserted2, @deleted2

END
GO
CREATE NONCLUSTERED INDEX [IX_x360ce_Settings_DateCreated]
    ON [dbo].[x360ce_Settings]([DateCreated] ASC);

