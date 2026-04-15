CREATE PROCEDURE [dbo].[usp_Clinical_ListRecentConversations]
    @Take INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Take) [ConversationKey],
                       [UpdatedAtUtc]
    FROM [dbo].[ClinicalConversation]
    ORDER BY [UpdatedAtUtc] DESC;
END;
