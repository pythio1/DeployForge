using DeployForge.Domain.Entities;

namespace DeployForge.Application.Pipelines;

public interface IPipelineRepository
{
    Task AddAsync(
        Pipeline pipeline,
        CancellationToken cancellationToken = default);

    Task<Pipeline?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pipeline>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(
        Pipeline pipeline,
        CancellationToken cancellationToken = default);
}
