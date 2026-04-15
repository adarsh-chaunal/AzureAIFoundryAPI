CREATE NONCLUSTERED INDEX [IX_ClinicalConversationMessage_ConversationKey_CreatedAtUtc]
    ON [dbo].[ClinicalConversationMessage] ([ConversationKey] ASC, [CreatedAtUtc] ASC);
