CREATE TABLE [dbo].[x360ce_Settings] (
    [SettingId]          UNIQUEIDENTIFIER NOT NULL,
    [InstanceGuid]       UNIQUEIDENTIFIER NOT NULL,
    [InstanceName]       NVARCHAR (256)   NOT NULL,
    [ProductGuid]        UNIQUEIDENTIFIER NOT NULL,
    [ProductName]        NVARCHAR (256)   NOT NULL,
    [DeviceType]         INT              NOT NULL,
    [FileName]           NVARCHAR (128)   NOT NULL,
    [FileProductName]    NVARCHAR (256)   NOT NULL,
    [Comment]            NVARCHAR (1024)  NOT NULL,
    [DateCreated]        DATETIME         CONSTRAINT [DF_x360ce_Settings_DateCreated] DEFAULT (getdate()) NOT NULL,
    [DateUpdated]        DATETIME         CONSTRAINT [DF_x360ce_Settings_DateUpdated] DEFAULT (getdate()) NOT NULL,
    [DateSelected]       DATETIME         CONSTRAINT [DF_x360ce_Settings_DateSelected] DEFAULT (getdate()) NOT NULL,
    [IsEnabled]          BIT              CONSTRAINT [DF_x360ce_Settings_IsEnabled] DEFAULT ((1)) NOT NULL,
    [PadSettingChecksum] UNIQUEIDENTIFIER NOT NULL,
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