CREATE PROCEDURE [dbo].[aspnet_Setup_RestorePermissions]
@name [sysname]
AS
BEGIN
    DECLARE @object sysname
    DECLARE @protectType char(10)
    DECLARE @action varchar(60)
    DECLARE @grantee sysname
    DECLARE @cmd nvarchar(500)
    
	IF (OBJECT_ID('tempdb.#aspnet_Permissions') IS NULL)  
	BEGIN 
	CREATE TABLE #aspnet_Permissions  
	(  
		[Owner]     sysname,  
		[Object]    sysname,  
		Grantee   sysname,  
		Grantor   sysname,  
		ProtectType char(10),  
		[Action]    varchar(60),  
		[Column]    sysname  
	)  
	END
    
    
    DECLARE c1 cursor FORWARD_ONLY FOR
        SELECT [Object], ProtectType, [Action], Grantee FROM #aspnet_Permissions where ISNULL([Object], '') = @name

    OPEN c1

    FETCH c1 INTO @object, @protectType, @action, @grantee
    WHILE (@@fetch_status = 0)
    BEGIN
        SET @cmd = @protectType + ' ' + @action + ' on ' + @object + ' TO [' + @grantee + ']'
        EXEC (@cmd)
        FETCH c1 INTO @object, @protectType, @action, @grantee
    END

    CLOSE c1
    DEALLOCATE c1
END