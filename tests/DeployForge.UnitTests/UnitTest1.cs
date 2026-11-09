
using DeployForge.Domain.Entities;

namespace DeployForge.UnitTests;

public class ProjectTests
{
    [Fact]
    public void Constructor_ShouldCreateProject_WhenDataIsValid()
    {
        // Arrange
        const string name = "My first project";
        const string repositoryUrl = "https://github.com/user/repository";

        // Act
        var project = new Project(name, repositoryUrl);

        // Assert
        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal(name, project.Name);
        Assert.Equal(repositoryUrl, project.RepositoryUrl);
        Assert.True(project.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Constructor_ShouldTrimNameAndUrl()
    {
        // Act
        var project = new Project(
            "  My project  ",
            "  https://github.com/user/repository  ");

        // Assert
        Assert.Equal("My project", project.Name);
        Assert.Equal(
            "https://github.com/user/repository",
            project.RepositoryUrl);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNameIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Project(
                " ",
                "https://github.com/user/repository"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenRepositoryUrlIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Project(
                "My project",
                " "));
    }
}
