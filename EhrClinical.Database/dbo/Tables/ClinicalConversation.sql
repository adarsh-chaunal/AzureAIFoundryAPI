CREATE TABLE [dbo].[ClinicalConversation]
(
    [ConversationKey] NVARCHAR(64) NOT NULL,
    [CreatedAtUtc]      DATETIME2    NOT NULL,
    [UpdatedAtUtc]      DATETIME2    NOT NULL,
    CONSTRAINT [PK_ClinicalConversation] PRIMARY KEY CLUSTERED ([ConversationKey] ASC)
);
