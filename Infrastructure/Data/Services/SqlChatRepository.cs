using Domain.Cloud;
using Infrastructure.Data.Interfaces;
using Infrastructure.Data.Sql;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Data.Services;

public sealed class SqlChatRepository : IChatRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlChatRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ChatConversation> GetConversationAsync(string conversationId)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);

        const string headerSql = """
            SELECT ConversationKey AS Id, CreatedAtUtc AS CreatedAt, UpdatedAtUtc AS UpdatedAt
            FROM dbo.ClinicalConversation
            WHERE ConversationKey = @Id
            """;

        await using var headerCmd = new SqlCommand(headerSql, connection);
        headerCmd.Parameters.AddWithValue("@Id", conversationId);

        await using var reader = await headerCmd.ExecuteReaderAsync().ConfigureAwait(false);
        if (!await reader.ReadAsync().ConfigureAwait(false))
        {
            return new ChatConversation { Id = conversationId };
        }

        var conversation = new ChatConversation
        {
            Id = reader.GetString(0),
            CreatedAt = reader.GetDateTime(1),
            UpdatedAt = reader.IsDBNull(2) ? null : reader.GetDateTime(2)
        };

        await reader.CloseAsync().ConfigureAwait(false);

        const string messagesSql = """
            SELECT Role, Content, CreatedAtUtc AS Timestamp
            FROM dbo.ClinicalConversationMessage
            WHERE ConversationKey = @Id
            ORDER BY CreatedAtUtc ASC
            """;

        await using var messageCmd = new SqlCommand(messagesSql, connection);
        messageCmd.Parameters.AddWithValue("@Id", conversationId);

        await using var messageReader = await messageCmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await messageReader.ReadAsync().ConfigureAwait(false))
        {
            conversation.Messages.Add(new ChatMessage
            {
                Role = messageReader.GetString(0),
                Content = messageReader.GetString(1),
                Timestamp = messageReader.GetDateTime(2)
            });
        }

        return conversation;
    }

    public async Task SaveConversationAsync(ChatConversation conversation)
    {
        if (string.IsNullOrWhiteSpace(conversation.Id))
        {
            conversation.Id = Guid.NewGuid().ToString();
            conversation.CreatedAt = DateTime.UtcNow;
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);
        using var transaction = connection.BeginTransaction();

        try
        {
            const string mergeHeader = """
                MERGE dbo.ClinicalConversation AS target
                USING (SELECT @Id AS ConversationKey) AS source
                ON target.ConversationKey = source.ConversationKey
                WHEN MATCHED THEN
                    UPDATE SET UpdatedAtUtc = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (ConversationKey, CreatedAtUtc, UpdatedAtUtc)
                    VALUES (@Id, SYSUTCDATETIME(), SYSUTCDATETIME());
                """;

            await using (var mergeCmd = new SqlCommand(mergeHeader, connection, transaction))
            {
                mergeCmd.Parameters.AddWithValue("@Id", conversation.Id);
                await mergeCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            const string deleteMessages = """
                DELETE FROM dbo.ClinicalConversationMessage
                WHERE ConversationKey = @Id
                """;

            await using (var deleteCmd = new SqlCommand(deleteMessages, connection, transaction))
            {
                deleteCmd.Parameters.AddWithValue("@Id", conversation.Id);
                await deleteCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            const string insertMessage = """
                INSERT INTO dbo.ClinicalConversationMessage (ConversationKey, Role, Content, CreatedAtUtc)
                VALUES (@Id, @Role, @Content, @CreatedAtUtc)
                """;

            foreach (var message in conversation.Messages)
            {
                await using var insertCmd = new SqlCommand(insertMessage, connection, transaction);
                insertCmd.Parameters.AddWithValue("@Id", conversation.Id);
                insertCmd.Parameters.AddWithValue("@Role", message.Role);
                insertCmd.Parameters.AddWithValue("@Content", message.Content);
                insertCmd.Parameters.AddWithValue("@CreatedAtUtc", message.Timestamp == default ? DateTime.UtcNow : message.Timestamp);
                await insertCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<List<ChatMessage>> GetConversationHistoryAsync(string conversationId, int limit = 10)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync().ConfigureAwait(false);

        const string sql = """
            SELECT Role, Content, CreatedAtUtc AS Timestamp
            FROM (
                SELECT Role, Content, CreatedAtUtc,
                       ROW_NUMBER() OVER (ORDER BY CreatedAtUtc DESC) AS rn
                FROM dbo.ClinicalConversationMessage
                WHERE ConversationKey = @Id
            ) AS ranked
            WHERE ranked.rn <= @Limit
            ORDER BY ranked.CreatedAtUtc ASC
            """;

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", conversationId);
        cmd.Parameters.AddWithValue("@Limit", limit);

        var list = new List<ChatMessage>();
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            list.Add(new ChatMessage
            {
                Role = reader.GetString(0),
                Content = reader.GetString(1),
                Timestamp = reader.GetDateTime(2)
            });
        }

        return list;
    }
}
