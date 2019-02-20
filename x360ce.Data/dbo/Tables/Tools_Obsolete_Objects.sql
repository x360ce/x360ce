CREATE TABLE [dbo].[Tools_Obsolete_Objects] (
    [name]          [sysname]     NOT NULL,
    [type]          [sysname]     NOT NULL,
    [last_use_date] DATETIME      NULL,
    [create_date]   DATETIME      NULL,
    [modify_date]   DATETIME      NULL,
    [drop_reason]   VARCHAR (100) DEFAULT ('') NOT NULL,
    [drop_script]   VARCHAR (500) DEFAULT ('') NOT NULL,
    [info_created]  DATETIME      DEFAULT (getdate()) NOT NULL,
    [info_updated]  DATETIME      DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_Tools_Obsolete_Objects] PRIMARY KEY CLUSTERED ([name] ASC)
);

