namespace Infrastructure.Messaging;

public interface IServiceBusPublisher
{
    Task PublishJsonAsync<T>(string queueName, T payload, CancellationToken cancellationToken = default);
}

