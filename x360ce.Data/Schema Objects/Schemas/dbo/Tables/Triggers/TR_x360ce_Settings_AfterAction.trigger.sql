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