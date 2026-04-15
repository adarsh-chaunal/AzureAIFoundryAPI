IF OBJECT_ID(N'dbo.ClinicalConversation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClinicalConversation
    (
        ConversationKey NVARCHAR(64) NOT NULL CONSTRAINT PK_ClinicalConversation PRIMARY KEY,
        CreatedAtUtc      DATETIME2      NOT NULL,
        UpdatedAtUtc      DATETIME2      NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.ClinicalConversationMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClinicalConversationMessage
    (
        MessageKey      BIGINT IDENTITY (1, 1) NOT NULL CONSTRAINT PK_ClinicalConversationMessage PRIMARY KEY,
        ConversationKey NVARCHAR(64)             NOT NULL,
        Role            NVARCHAR(32)             NOT NULL,
        Content         NVARCHAR(MAX)            NOT NULL,
        CreatedAtUtc    DATETIME2                NOT NULL,
        CONSTRAINT FK_ClinicalConversationMessage_Conversation
            FOREIGN KEY (ConversationKey) REFERENCES dbo.ClinicalConversation (ConversationKey)
    );

    CREATE NONCLUSTERED INDEX IX_ClinicalConversationMessage_ConversationKey_CreatedAtUtc
        ON dbo.ClinicalConversationMessage (ConversationKey, CreatedAtUtc);
END;
