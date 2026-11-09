using DeployForge.Domain.Entities;

namespace DeployForge.Application.Projects;

public sealed class CreateProject(
    IProjectRepository projectRepository)
{
    public async Task<Guid> ExecuteAsync(
        string name,
        string repositoryUrl,
        CancellationToken cancellationToken = default)
    {
        var project = new Project(name, repositoryUrl);

        await projectRepository.AddAsync(project, cancellationToken);

        return project.Id;
    }
}
