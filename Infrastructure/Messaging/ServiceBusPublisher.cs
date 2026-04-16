using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Messaging;

public sealed class ServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;

    public ServiceBusPublisher(IConfiguration configuration)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"]
            ?? configuration["ServiceBusConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Service Bus connection string is not configured. Set ServiceBus:ConnectionString to the namespace SAS connection string " +
                "(Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=...).");
        }

        // Common mistake: users paste the namespace/queue HTTPS URL instead of the SAS connection string.
        if (connectionString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid ServiceBus:ConnectionString value. You provided an HTTP URL, but Azure Service Bus requires a SAS connection string like: " +
                "Endpoint=sb://client-summary-requests.servicebus.windows.net/;SharedAccessKeyName=<policy>;SharedAccessKey=<key>.");
        }

        if (!connectionString.Contains("Endpoint=sb://", StringComparison.OrdinalIgnoreCase) ||
            !connectionString.Contains("SharedAccessKey=", StringComparison.OrdinalIgnoreCase) ||
            !connectionString.Contains("SharedAccessKeyName=", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid ServiceBus:ConnectionString format. Expected: " +
                "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<policy>;SharedAccessKey=<key>.");
        }

        _client = new ServiceBusClient(connectionString);
    }

    public async Task PublishJsonAsync<T>(string queueName, T payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("Queue name is required.", nameof(queueName));
        }

        var sender = _client.CreateSender(queueName);
        var body = JsonSerializer.SerializeToUtf8Bytes(payload);
        var message = new ServiceBusMessage(body)
        {
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
        await sender.DisposeAsync().ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}

