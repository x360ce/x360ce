ALTER TABLE [dbo].[x360ce_Summaries]
    ADD CONSTRAINT [DF_x360ce_Summaries_DateCreated] DEFAULT (getdate()) FOR [DateCreated];

