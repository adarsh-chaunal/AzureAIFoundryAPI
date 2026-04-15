CREATE TABLE [dbo].[ClinicalConversationMessage]
(
    [MessageKey]      BIGINT IDENTITY (1, 1) NOT NULL,
    [ConversationKey] NVARCHAR(64)           NOT NULL,
    [Role]            NVARCHAR(32)           NOT NULL,
    [Content]         NVARCHAR(MAX)          NOT NULL,
    [CreatedAtUtc]    DATETIME2              NOT NULL,
    CONSTRAINT [PK_ClinicalConversationMessage] PRIMARY KEY CLUSTERED ([MessageKey] ASC),
    CONSTRAINT [FK_ClinicalConversationMessage_Conversation] FOREIGN KEY ([ConversationKey]) REFERENCES [dbo].[ClinicalConversation] ([ConversationKey])
);
