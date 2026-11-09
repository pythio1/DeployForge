
namespace DeployForge.Application.Pipelines;

public sealed class DeletePipeline(
    IPipelineRepository pipelineRepository)
{
    public async Task<bool> ExecuteAsync(
        Guid projectId,
        Guid pipelineId,
        CancellationToken cancellationToken = default)
    {
        var pipeline = await pipelineRepository.GetByIdAsync(
            pipelineId,
            cancellationToken);

        if (pipeline is null || pipeline.ProjectId != projectId)
            return false;

        await pipelineRepository.DeleteAsync(
            pipeline,
            cancellationToken);

        return true;
    }
}
