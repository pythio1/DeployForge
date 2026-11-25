using DeployForge.Domain.Entities;

namespace DeployForge.Application.PipelineRuns;

public interface IPipelineStepExecutor
{
    Task ExecuteAsync(
        string repositoryUrl,
        string? repositorySecret,
        IReadOnlyList<PipelineStep> steps,
        CancellationToken cancellationToken = default);
}
