using DeployForge.Domain.Entities;

namespace DeployForge.Application.Projects;

public sealed class GetProjectById(
    IProjectRepository projectRepository)
{
    public Task<Project?> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return projectRepository.GetByIdAsync(id, cancellationToken);
    }
}
