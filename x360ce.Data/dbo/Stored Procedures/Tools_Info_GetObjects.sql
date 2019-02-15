
CREATE PROCEDURE [dbo].[Tools_Info_GetObjects]
	@AnsiOrQuoteId varchar(3) = null
AS

	-- EXEC [dbo].[Tools_Info_GetObjects]
	-- EXEC [dbo].[Tools_Info_GetObjects] 'OFF'
SELECT * FROM (	
	SELECT  
		s.[type],
		s.type_desc,
		SCHEMA_NAME(s.schema_id)  + '.' + s.name AS name,
		s.create_date, 
		s.modify_date,
		[QuotedIdent] =
			CASE OBJECTPROPERTYEX(s.object_id, 'IsQuotedIdentOn')
			WHEN 1 THEN 'ON'
			WHEN 0 THEN 'OFF'
			ELSE ''
			END ,
		[ExecQuotedIdent] =
				CASE OBJECTPROPERTYEX(s.object_id, 'ExecIsQuotedIdentOn')
				WHEN 1 THEN 'ON'
				WHEN 0 THEN 'OFF'
				ELSE ''
				END 
	FROM sys.objects s  
	WHERE  
		-- FN	SQL_SCALAR_FUNCTION
		-- IF	SQL_INLINE_TABLE_VALUED_FUNCTION
		-- V 	VIEW
		-- P 	SQL_STORED_PROCEDURE
		-- TF	SQL_TABLE_VALUED_FUNCTION
		-- TR	SQL_TRIGGER
		s.type IN ('P','TR','IF','FN','TF')
) t1
WHERE @AnsiOrQuoteId IS NULL OR (
	--t1.[AnsiPadded] = @AnsiOrQuoteId OR
	[QuotedIdent] = @AnsiOrQuoteId OR
	[ExecQuotedIdent] = @AnsiOrQuoteId
)
ORDER BY
	t1.[type],
	t1.name