using DeployForge.Application.RepositoryCredentials;
using DeployForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeployForge.Infrastructure.Persistence;

public sealed class RepositoryCredentialRepository(
    AppDbContext dbContext) : IRepositoryCredentialRepository
{
    public Task<RepositoryCredential?> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.RepositoryCredentials.FirstOrDefaultAsync(
            credential => credential.ProjectId == projectId,
            cancellationToken);
    }

    public async Task AddAsync(
        RepositoryCredential credential,
        CancellationToken cancellationToken = default)
    {
        await dbContext.RepositoryCredentials.AddAsync(
            credential,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        RepositoryCredential credential,
        CancellationToken cancellationToken = default)
    {
        dbContext.RepositoryCredentials.Update(credential);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.RepositoryCredentials
            .Where(credential => credential.ProjectId == projectId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }
}
