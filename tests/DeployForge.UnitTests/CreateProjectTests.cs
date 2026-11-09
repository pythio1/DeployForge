
using DeployForge.Application.Projects;
using DeployForge.Domain.Entities;

namespace DeployForge.UnitTests;

public class CreateProjectTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldNotSaveProject_WhenNameIsEmpty()
    {
        // Arrange
        var repository = new FakeProjectRepository();
        var useCase = new CreateProject(repository);

        // Act
            await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(
                " ",
                "https://github.com/user/deployforge"));

        // Assert
        var projects = await repository.GetAllAsync();
        Assert.Empty(projects);
    }
    [Fact]
    public async Task ExecuteAsync_ShouldCreateProjectAndSaveItToRepository()
    {
        // Arrange
        var repository = new FakeProjectRepository();
        var useCase = new CreateProject(repository);

        const string name = "DeployForge";
        const string repositoryUrl = "https://github.com/user/deployforge";

        // Act
        var projectId = await useCase.ExecuteAsync(name, repositoryUrl);

        // Assert
        var savedProject = await repository.GetByIdAsync(projectId);

        Assert.NotEqual(Guid.Empty, projectId);
        Assert.NotNull(savedProject);
        Assert.Equal(name, savedProject.Name);
        Assert.Equal(repositoryUrl, savedProject.RepositoryUrl);
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
