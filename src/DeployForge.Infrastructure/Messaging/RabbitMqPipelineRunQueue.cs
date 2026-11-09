using System.Text.Json;
using DeployForge.Application.PipelineRuns;
using RabbitMQ.Client;

namespace DeployForge.Infrastructure.Messaging;

public sealed class RabbitMqPipelineRunQueue(
    ConnectionFactory connectionFactory) : IPipelineRunQueue, IAsyncDisposable
{
    private const string QueueName = "pipeline-runs";

    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public async Task PublishAsync(
        PipelineRunRequested message,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);

        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private async Task<IConnection> GetConnectionAsync(
        CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            _connection = await connectionFactory.CreateConnectionAsync(
                cancellationToken);

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }
}
