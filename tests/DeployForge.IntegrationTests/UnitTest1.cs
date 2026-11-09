
using DeployForge.Application.Projects;
using DeployForge.Domain.Entities;
using DeployForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeployForge.IntegrationTests;

public class ProjectRepositoryTests
{
    [Fact]
    public async Task AddAsync_ShouldSaveProjectToPostgreSql()
    {
        // Arrange
        const string connectionString =
            "Host=localhost;Port=5432;Database=deployforge_test;" +
            "Username=deployforge;Password=dev_password";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new AppDbContext(options);

        // Применяем миграции к отдельной тестовой базе.
        await dbContext.Database.MigrateAsync();

        var repository = new ProjectRepository(dbContext);

        var project = new Project(
            "IntegrationTestProject",
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
        const string connectionString =
            "Host=localhost;Port=5432;Database=deployforge_test;" +
            "Username=deployforge;Password=dev_password";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new AppDbContext(options);

        await dbContext.Database.MigrateAsync();

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
        const string connectionString =
            "Host=localhost;Port=5432;Database=deployforge_test;" +
            "Username=deployforge;Password=dev_password";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new AppDbContext(options);

        await dbContext.Database.MigrateAsync();

        var repository = new ProjectRepository(dbContext);

        var nonExistingId = Guid.NewGuid();

        // Act
        var project = await repository.GetByIdAsync(nonExistingId);

        // Assert
        Assert.Null(project);
    }
}
