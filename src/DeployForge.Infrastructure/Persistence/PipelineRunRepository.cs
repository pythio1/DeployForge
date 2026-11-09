using DeployForge.Application.PipelineRuns;
using DeployForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeployForge.Infrastructure.Persistence;

public sealed class PipelineRunRepository(AppDbContext dbContext)
    : IPipelineRunRepository
{
    public async Task AddAsync(
        PipelineRun pipelineRun,
        CancellationToken cancellationToken = default)
    {
        await dbContext.PipelineRuns.AddAsync(
            pipelineRun,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<PipelineRun?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PipelineRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(
                run => run.Id == id,
                cancellationToken);
    }

    public Task<PipelineRun?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PipelineRuns
            .FirstOrDefaultAsync(
                run => run.Id == id,
                cancellationToken);
    }


    public async Task<IReadOnlyList<PipelineRun>> GetByPipelineIdAsync(
        Guid pipelineId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PipelineRuns
            .AsNoTracking()
            .Where(run => run.PipelineId == pipelineId)
            .OrderByDescending(run => run.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
