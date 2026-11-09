using DeployForge.Application.Projects;
using System.Text.Json;
using DeployForge.Application.Pipelines;
using DeployForge.Application.PipelineRuns;
using DeployForge.Domain.Enums;
using DeployForge.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DeployForge.Runner;

public sealed class Worker(
    ILogger<Worker> logger,
    ConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const string QueueName = "pipeline-runs";

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await using var connection =
            await connectionFactory.CreateConnectionAsync(stoppingToken);

        await using var channel =
            await connection.CreateChannelAsync(
                cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            PipelineRunRequested? message;

            try
            {
                message = JsonSerializer.Deserialize<PipelineRunRequested>(
                    eventArgs.Body.Span);
            }
            catch (JsonException exception)
            {
                logger.LogError(
                    exception,
                    "Invalid pipeline run message received.");

                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: CancellationToken.None);

                return;
            }

            if (message is null)
            {
                logger.LogError("Empty pipeline run message received.");

                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: CancellationToken.None);

                return;
            }

            try
            {
                await using var scope =
                    scopeFactory.CreateAsyncScope();

                var repository = scope.ServiceProvider
                    .GetRequiredService<IPipelineRunRepository>();

                var dbContext = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var pipelineRun = await repository.GetByIdForUpdateAsync(
                    message.RunId,
                    stoppingToken);

                if (pipelineRun is null)
                {
                    logger.LogWarning(
                        "Pipeline run {RunId} was not found.",
                        message.RunId);

                    await channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        cancellationToken: CancellationToken.None);

                    return;
                }

                // Защита от повторной доставки сообщения.
                if (pipelineRun.Status != PipelineRunStatus.Queued)
                {
                    logger.LogWarning(
                        "Pipeline run {RunId} is already in status {Status}. Skipping.",
                        pipelineRun.Id,
                        pipelineRun.Status);

                    await channel.BasicAckAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        cancellationToken: CancellationToken.None);

                    return;
                }

                var pipelineRepository = scope.ServiceProvider
                    .GetRequiredService<IPipelineRepository>();

                var pipeline = await pipelineRepository.GetByIdAsync(
                    message.PipelineId,
                    stoppingToken);

                if (pipeline is null)
                {
                    throw new InvalidOperationException(
                        $"Pipeline {message.PipelineId} was not found.");
                }

                var projectRepository = scope.ServiceProvider
                    .GetRequiredService<IProjectRepository>();

                var project = await projectRepository.GetByIdAsync(
                    pipeline.ProjectId,
                    stoppingToken);

                if (project is null)
                {
                    throw new InvalidOperationException(
                        $"Project {pipeline.ProjectId} was not found.");
                }

                logger.LogInformation(
                    "Loaded pipeline {PipelineName} with {StepCount} steps.",
                    pipeline.Name,
                    pipeline.Steps.Count);

                // Переводим запуск в Running.
                pipelineRun.Start();

                await dbContext.SaveChangesAsync(stoppingToken);

                logger.LogInformation(
                    "Pipeline run {RunId} started.",
                    pipelineRun.Id);

                var executor = scope.ServiceProvider
                    .GetRequiredService<IPipelineStepExecutor>();

                try
                {
                    // Выполняем все шаги в одном Docker-контейнере.
                    await executor.ExecuteAsync(
                        project.RepositoryUrl,
                        pipeline.Steps.ToList(),
                        stoppingToken);
                    pipelineRun.Succeed();

                    await dbContext.SaveChangesAsync(stoppingToken);

                    logger.LogInformation(
                        "Pipeline run {RunId} succeeded.",
                        pipelineRun.Id);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Pipeline run {RunId} failed.",
                        pipelineRun.Id);

                    pipelineRun.Fail();

                    await dbContext.SaveChangesAsync(
                        CancellationToken.None);
                }

                // Результат выполнения уже сохранён в БД.
                // Повторно выполнять завершённый запуск не нужно.
                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to process pipeline run message {RunId}.",
                    message.RunId);

                // Ошибка инфраструктуры: разрешаем повторную доставку.
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: CancellationToken.None);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "Runner started. Listening to queue: {QueueName}",
            QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
