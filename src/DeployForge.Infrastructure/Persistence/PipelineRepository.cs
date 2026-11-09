using DeployForge.Application.Pipelines;
using DeployForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeployForge.Infrastructure.Persistence;

public sealed class PipelineRepository(AppDbContext dbContext)
    : IPipelineRepository
{
    public async Task AddAsync(
        Pipeline pipeline,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Pipelines.AddAsync(pipeline, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Pipeline?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Pipelines
            .Include(pipeline => pipeline.Steps.OrderBy(step => step.Order))
            .AsNoTracking()
            .FirstOrDefaultAsync(
                pipeline => pipeline.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Pipeline>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Pipelines
            .Where(pipeline => pipeline.ProjectId == projectId)
            .Include(pipeline => pipeline.Steps.OrderBy(step => step.Order))
            .AsNoTracking()
            .OrderBy(pipeline => pipeline.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    public async Task DeleteAsync(
        Pipeline pipeline,
        CancellationToken cancellationToken = default)
    {
        dbContext.Pipelines.Remove(pipeline);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
