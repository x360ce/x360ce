CREATE NONCLUSTERED INDEX [IX_x360ce_Summaries_ProductGuid_FileName_FileProductName]
    ON [dbo].[x360ce_Summaries]([ProductGuid] ASC, [FileName] ASC, [FileProductName] ASC, [PadSettingChecksum] ASC) WITH (ALLOW_PAGE_LOCKS = ON, ALLOW_ROW_LOCKS = ON, PAD_INDEX = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, IGNORE_DUP_KEY = OFF, STATISTICS_NORECOMPUTE = OFF, ONLINE = OFF, MAXDOP = 0)
    ON [PRIMARY];

