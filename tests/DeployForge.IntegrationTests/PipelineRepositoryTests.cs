using DeployForge.Domain.Entities;
using DeployForge.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DeployForge.IntegrationTests;

public sealed class PipelineRepositoryTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PipelineRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddAsync_ShouldSavePipelineAndSteps()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var project = new Project(
            "Pipeline Test Project",
            "https://github.com/example/pipeline-test");

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        var repository = new PipelineRepository(dbContext);

        var pipeline = new Pipeline(project.Id, "Build and Deploy");

        // Добавляем шаги намеренно в неправильном порядке.
        pipeline.AddStep(new PipelineStep(
            "Deploy",
            2,
            "echo deploy"));

        pipeline.AddStep(new PipelineStep(
            "Build",
            1,
            "dotnet build"));

        // Act
        await repository.AddAsync(pipeline);

        var savedPipeline = await repository.GetByIdAsync(pipeline.Id);

        // Assert
        Assert.NotNull(savedPipeline);
        Assert.Equal("Build and Deploy", savedPipeline.Name);
        Assert.Equal(2, savedPipeline.Steps.Count);

        var steps = savedPipeline.Steps.ToList();

        Assert.Equal(1, steps[0].Order);
        Assert.Equal("Build", steps[0].Name);

        Assert.Equal(2, steps[1].Order);
        Assert.Equal("Deploy", steps[1].Name);

        Assert.All(
            steps,
            step => Assert.Equal(pipeline.Id, step.PipelineId));
    }
}
