using Application.Dtos.ClientSummary;
using Application.Interfaces;
using Infrastructure.Data.Sql;
using Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;

namespace Application.Services;

public sealed class ClientSummaryService : IClientSummaryService
{
    private readonly ISqlDataAccess _sql;
    private readonly IServiceBusPublisher _bus;
    private readonly IConfiguration _configuration;

    public ClientSummaryService(ISqlDataAccess sql, IServiceBusPublisher bus, IConfiguration configuration)
    {
        _sql = sql;
        _bus = bus;
        _configuration = configuration;
    }

    private sealed record ClientSummaryQueueMessage(Guid RequestId, int ClientId);

    public async Task<CreateClientSummaryResponseDto> QueueClientSummaryAsync(int clientId, CancellationToken cancellationToken = default)
    {
        if (clientId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(clientId), "ClientId must be a positive integer.");
        }

        var requestId = Guid.NewGuid();

        // 1) Create request row (Queued)
        await _sql.ExecuteAsync(
                "EXEC dbo.usp_AIClientSummaryRequest_Create @RequestId, @ClientId;",
                new { RequestId = requestId, ClientId = clientId },
                cancellationToken)
            .ConfigureAwait(false);

        // 2) Publish to Service Bus
        var queueName = _configuration["ServiceBus:ClientSummaryQueueName"]
            ?? _configuration["ClientSummaryQueueName"]
            ?? "client-summary-requests";

        try
        {
            await _bus.PublishJsonAsync(queueName, new ClientSummaryQueueMessage(requestId, clientId), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Mark as Failed if enqueue fails
            await _sql.ExecuteAsync(
                    "EXEC dbo.usp_AIClientSummaryRequest_UpdateStatus @RequestId, @Status, @ErrorMessage, @LatestSummaryId;",
                    new { RequestId = requestId, Status = "Failed", ErrorMessage = $"Queue publish failed: {ex.Message}", LatestSummaryId = (Guid?)null },
                    cancellationToken)
                .ConfigureAwait(false);
            throw;
        }

        return new CreateClientSummaryResponseDto { RequestId = requestId, Status = "Queued" };
    }

    public async Task<ClientSummaryRequestStatusDto?> GetRequestStatusAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var status = await _sql.QuerySingleOrDefaultAsync<ClientSummaryRequestStatusDto>(
                "EXEC dbo.usp_AIClientSummaryRequest_Get @RequestId;",
                new { RequestId = requestId },
                cancellationToken)
            .ConfigureAwait(false);

        if (status is null)
        {
            return null;
        }

        if (status.LatestSummaryId is not null && string.Equals(status.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            var summary = await _sql.QuerySingleOrDefaultAsync<ClientSummaryRowDto>(
                    "SELECT TOP (1) SummaryId, RequestId, ClientId, CreatedAtUtc, TokensUsed, Model, SummaryText FROM dbo.AIClientSummary WHERE SummaryId = @SummaryId;",
                    new { SummaryId = status.LatestSummaryId },
                    cancellationToken)
                .ConfigureAwait(false);

            status.LatestSummaryText = summary?.SummaryText;
        }

        return status;
    }

    public async Task<IReadOnlyList<ClientSummaryRowDto>> ListClientSummariesAsync(int clientId, int take = 50, CancellationToken cancellationToken = default)
    {
        if (clientId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(clientId), "ClientId must be a positive integer.");
        }

        if (take < 1 || take > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be between 1 and 200.");
        }

        var rows = await _sql.QueryAsync<ClientSummaryRowDto>(
                "EXEC dbo.usp_AIClientSummary_ListByClient @ClientId, @Take;",
                new { ClientId = clientId, Take = take },
                cancellationToken)
            .ConfigureAwait(false);

        return rows;
    }
}

