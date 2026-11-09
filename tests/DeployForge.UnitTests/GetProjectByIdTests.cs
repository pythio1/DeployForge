
using DeployForge.Application.Projects;
using DeployForge.Domain.Entities;

namespace DeployForge.UnitTests;

public class GetProjectByIdTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnProject_WhenProjectExists()
    {
        // Arrange
        var repository = new FakeProjectRepository();

        var project = new Project(
            "DeployForge",
            "https://github.com/user/deployforge");

        await repository.AddAsync(project);

        var useCase = new GetProjectById(repository);

        // Act
        var result = await useCase.ExecuteAsync(project.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(project.Id, result.Id);
        Assert.Equal(project.Name, result.Name);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNull_WhenProjectDoesNotExist()
    {
        // Arrange
        var repository = new FakeProjectRepository();
        var useCase = new GetProjectById(repository);

        // Act
        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        private readonly List<Project> _projects = [];

        public Task AddAsync(
            Project project,
            CancellationToken cancellationToken = default)
        {
            _projects.Add(project);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Project>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Project> result = _projects.ToList();
            return Task.FromResult(result);
        }

        public Task<Project?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var project = _projects.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(project);
        }
    }
}
