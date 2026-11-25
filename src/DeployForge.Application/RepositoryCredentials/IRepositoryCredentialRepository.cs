using DeployForge.Domain.Entities;

namespace DeployForge.Application.RepositoryCredentials;

public interface IRepositoryCredentialRepository
{
    Task<RepositoryCredential?> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        RepositoryCredential credential,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        RepositoryCredential credential,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
