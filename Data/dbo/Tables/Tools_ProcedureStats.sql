CREATE TABLE [dbo].[Tools_ProcedureStats] (
    [name]                 [sysname] NOT NULL,
    [first_execution_time] DATETIME  NULL,
    [last_execution_time]  DATETIME  NULL,
    CONSTRAINT [PK_Tools_ProcedureStats] PRIMARY KEY CLUSTERED ([name] ASC)
);

