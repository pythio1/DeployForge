using DeployForge.Application.Pipelines;
using DeployForge.Domain.Entities;

namespace DeployForge.Application.PipelineRuns;

public sealed class CreatePipelineRun(
    IPipelineRepository pipelineRepository,
    IPipelineRunRepository pipelineRunRepository,
    IPipelineRunQueue pipelineRunQueue)
{
    public async Task<PipelineRun> ExecuteAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default)
    {
        var pipeline = await pipelineRepository.GetByIdAsync(
            pipelineId,
            cancellationToken);

        if (pipeline is null)
        {
            throw new KeyNotFoundException(
                $"Pipeline with ID '{pipelineId}' was not found.");
        }

        var pipelineRun = new PipelineRun(pipelineId);

        await pipelineRunRepository.AddAsync(
            pipelineRun,
            cancellationToken);

        var message = new PipelineRunRequested(
            pipelineRun.Id,
            pipelineRun.PipelineId,
            DateTimeOffset.UtcNow);

        await pipelineRunQueue.PublishAsync(
            message,
            cancellationToken);

        return pipelineRun;
    }
}
