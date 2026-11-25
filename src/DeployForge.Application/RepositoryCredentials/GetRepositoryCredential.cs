using DeployForge.Application.Projects;
using DeployForge.Domain.Entities;

namespace DeployForge.Application.RepositoryCredentials;

public sealed class GetRepositoryCredential(
    IProjectRepository projectRepository,
    IRepositoryCredentialRepository credentialRepository)
{
    public async Task<RepositoryCredential?> ExecuteAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(
            projectId,
            cancellationToken);

        if (project is null)
        {
            throw new KeyNotFoundException(
                $"Project with ID '{projectId}' was not found.");
        }

        return await credentialRepository.GetByProjectIdAsync(
            projectId,
            cancellationToken);
    }
}
