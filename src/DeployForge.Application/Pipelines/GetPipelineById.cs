using DeployForge.Domain.Entities;

namespace DeployForge.Application.Pipelines;

public sealed class GetPipelineById(
    IPipelineRepository pipelineRepository)
{
    public async Task<Pipeline?> ExecuteAsync(
        Guid projectId,
        Guid pipelineId,
        CancellationToken cancellationToken = default)
    {
        var pipeline = await pipelineRepository.GetByIdAsync(
            pipelineId,
            cancellationToken);

        if (pipeline is null || pipeline.ProjectId != projectId)
            return null;

        return pipeline;
    }
}
