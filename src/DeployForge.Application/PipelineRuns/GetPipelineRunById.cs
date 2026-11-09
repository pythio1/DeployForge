using DeployForge.Domain.Entities;

namespace DeployForge.Application.PipelineRuns;

public sealed class GetPipelineRunById(
    IPipelineRunRepository pipelineRunRepository)
{
    public async Task<PipelineRun?> ExecuteAsync(
        Guid pipelineId,
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await pipelineRunRepository.GetByIdAsync(
            runId,
            cancellationToken);

        if (run is null || run.PipelineId != pipelineId)
            return null;

        return run;
    }
}
