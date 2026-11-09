
using DeployForge.Domain.Entities;

namespace DeployForge.Application.PipelineRuns;

public interface IPipelineRunRepository
{
    Task AddAsync(
        PipelineRun pipelineRun,
        CancellationToken cancellationToken = default);

    Task<PipelineRun?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PipelineRun?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PipelineRun>> GetByPipelineIdAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default);
}
