using DeployForge.Domain.Entities;

namespace DeployForge.Application.PipelineRuns;

public interface IPipelineStepExecutor
{
    Task ExecuteAsync(
        string repositoryUrl,
        IReadOnlyList<PipelineStep> steps,
        CancellationToken cancellationToken = default);
}
