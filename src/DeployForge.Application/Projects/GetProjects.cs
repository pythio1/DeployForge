using DeployForge.Domain.Entities;

namespace DeployForge.Application.Projects;

public sealed class GetProjects(
    IProjectRepository projectRepository)
{
    public Task<IReadOnlyList<Project>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        return projectRepository.GetAllAsync(cancellationToken);
    }
}
