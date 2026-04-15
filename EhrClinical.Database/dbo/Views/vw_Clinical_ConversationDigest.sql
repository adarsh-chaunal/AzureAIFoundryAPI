CREATE VIEW [dbo].[vw_Clinical_ConversationDigest]
AS
SELECT conv.[ConversationKey],
       COUNT_BIG(msg.[MessageKey]) AS [MessageCount],
       MAX(msg.[CreatedAtUtc])     AS [LastMessageAtUtc],
       conv.[UpdatedAtUtc]
FROM [dbo].[ClinicalConversation] AS conv
         LEFT JOIN [dbo].[ClinicalConversationMessage] AS msg
                   ON msg.[ConversationKey] = conv.[ConversationKey]
GROUP BY conv.[ConversationKey], conv.[UpdatedAtUtc];
