using DeployForge.Domain.Entities;

namespace DeployForge.Application.Projects;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Project?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
