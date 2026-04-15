CREATE OR ALTER VIEW dbo.vw_Clinical_ConversationDigest
AS
SELECT c.ConversationKey,
       COUNT_BIG(m.MessageKey) AS MessageCount,
       MAX(m.CreatedAtUtc)     AS LastMessageAtUtc,
       c.UpdatedAtUtc
FROM dbo.ClinicalConversation AS c
         LEFT JOIN dbo.ClinicalConversationMessage AS m
                   ON m.ConversationKey = c.ConversationKey
GROUP BY c.ConversationKey, c.UpdatedAtUtc;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Clinical_ListRecentConversations
    @Take INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Take) ConversationKey,
                       UpdatedAtUtc
    FROM dbo.ClinicalConversation
    ORDER BY UpdatedAtUtc DESC;
END;
GO
