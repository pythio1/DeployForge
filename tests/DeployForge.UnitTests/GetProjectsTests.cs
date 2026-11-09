
using DeployForge.Application.Projects;
using DeployForge.Domain.Entities;

namespace DeployForge.UnitTests;

public class GetProjectsTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnAllProjects()
    {
        // Arrange
        var repository = new FakeProjectRepository();

        var firstProject = new Project(
            "DeployForge",
            "https://github.com/user/deployforge");

        var secondProject = new Project(
            "AnotherProject",
            "https://github.com/user/another-project");

        await repository.AddAsync(firstProject);
        await repository.AddAsync(secondProject);

        var useCase = new GetProjects(repository);

        // Act
        var projects = await useCase.ExecuteAsync();

        // Assert
        Assert.Equal(2, projects.Count);
        Assert.Contains(projects, p => p.Id == firstProject.Id);
        Assert.Contains(projects, p => p.Id == secondProject.Id);
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
