using DeployForge.Application.Projects;

namespace DeployForge.Application.RepositoryCredentials;

public sealed class DeleteRepositoryCredential(
    IProjectRepository projectRepository,
    IRepositoryCredentialRepository credentialRepository)
{
    public async Task<bool> ExecuteAsync(
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

        return await credentialRepository.DeleteByProjectIdAsync(
            projectId,
            cancellationToken);
    }
}
