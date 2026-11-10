using DeployForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeployForge.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options
) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Pipeline> Pipelines => Set<Pipeline>();

    public DbSet<PipelineStep> PipelineSteps => Set<PipelineStep>();
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();
    public DbSet<RepositoryCredential> RepositoryCredentials =>
    Set<RepositoryCredential>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasMany(project => project.Pipelines)
            .WithOne()
            .HasForeignKey(pipeline => pipeline.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Pipeline>()
            .HasMany(pipeline => pipeline.Steps)
            .WithOne()
            .HasForeignKey(step => step.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Pipeline>()
            .HasIndex(pipeline => new
            {
                pipeline.ProjectId,
                pipeline.Name
            });

        modelBuilder.Entity<PipelineStep>()
            .HasIndex(step => new
            {
                step.PipelineId,
                step.Order
            })
            .IsUnique();
        modelBuilder.Entity<Pipeline>()
            .HasMany<PipelineRun>()
            .WithOne()
            .HasForeignKey(run => run.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Project>()
            .HasOne(project => project.RepositoryCredential)
            .WithOne()
            .HasForeignKey<RepositoryCredential>(
                credential => credential.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
