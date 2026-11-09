
using DeployForge.Application.Projects;
using DeployForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeployForge.Infrastructure.Persistence;

public sealed class ProjectRepository(AppDbContext dbContext) : IProjectRepository
{
    public async Task AddAsync(
        Project project,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Projects.AddAsync(project, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .OrderBy(project => project.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Project?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(
                project => project.Id == id,
                cancellationToken);
    }
}
