using DeployForge.Application.Projects;
using DeployForge.Domain.Entities;
using DeployForge.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DeployForge.IntegrationTests;

public class ProjectRepositoryTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProjectRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddAsync_ShouldSaveProjectToPostgreSql()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var repository = new ProjectRepository(dbContext);

        var project = new Project(
            $"IntegrationTestProject-{Guid.NewGuid():N}",
            "https://github.com/user/integration-test");

        // Act
        await repository.AddAsync(project);

        var savedProject = await repository.GetByIdAsync(project.Id);

        // Assert
        Assert.NotNull(savedProject);
        Assert.Equal(project.Id, savedProject.Id);
        Assert.Equal(project.Name, savedProject.Name);
        Assert.Equal(project.RepositoryUrl, savedProject.RepositoryUrl);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnSavedProjects()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var repository = new ProjectRepository(dbContext);

        var uniqueId = Guid.NewGuid().ToString("N");

        var firstProject = new Project(
            $"Project-A-{uniqueId}",
            "https://github.com/user/project-a");

        var secondProject = new Project(
            $"Project-B-{uniqueId}",
            "https://github.com/user/project-b");

        // Act
        await repository.AddAsync(firstProject);
        await repository.AddAsync(secondProject);

        var projects = await repository.GetAllAsync();

        // Assert
        Assert.Contains(projects, p => p.Id == firstProject.Id);
        Assert.Contains(projects, p => p.Id == secondProject.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProjectDoesNotExist()
    {
        // Arrange
        await using var scope = _factory.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var repository = new ProjectRepository(dbContext);

        var nonExistingId = Guid.NewGuid();

        // Act
        var project = await repository.GetByIdAsync(nonExistingId);

        // Assert
        Assert.Null(project);
    }
}
