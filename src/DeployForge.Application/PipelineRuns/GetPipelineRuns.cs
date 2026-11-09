namespace DeployForge.Application.PipelineRuns;

public sealed class GetPipelineRuns(
    IPipelineRunRepository pipelineRunRepository)
{
    public Task<IReadOnlyList<Domain.Entities.PipelineRun>> ExecuteAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default)
    {
        return pipelineRunRepository.GetByPipelineIdAsync(
            pipelineId,
            cancellationToken);
    }
}
