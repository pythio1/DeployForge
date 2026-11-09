using System.Net;
using System.Net.Http.Json;

namespace DeployForge.IntegrationTests;

public class ProjectsApiTests(
    CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task PostProject_ShouldReturnCreated()
    {
        // Arrange
        using var client = factory.CreateClient();

        var request = new
        {
            Name = "HttpIntegrationTestProject",
            RepositoryUrl = "https://github.com/user/http-test"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/projects",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdProject = await response.Content
            .ReadFromJsonAsync<CreateProjectResponse>();

        Assert.NotNull(createdProject);
        Assert.NotEqual(Guid.Empty, createdProject.Id);
    }
    [Fact]
    public async Task GetProject_ShouldReturnCreatedProject()
    {
        // Arrange
        using var client = factory.CreateClient();

        var request = new
        {
            Name = "GetHttpTestProject",
            RepositoryUrl = "https://github.com/user/get-http-test"
        };

        // Сначала создаём проект.
        var createResponse = await client.PostAsJsonAsync(
            "/api/projects",
            request);

        createResponse.EnsureSuccessStatusCode();

        var createdProject = await createResponse.Content
            .ReadFromJsonAsync<CreateProjectResponse>();

        Assert.NotNull(createdProject);

        // Act — получаем созданный проект по ID.
        var response = await client.GetAsync(
            $"/api/projects/{createdProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var project = await response.Content
            .ReadFromJsonAsync<ProjectResponse>();

        Assert.NotNull(project);
        Assert.Equal(createdProject.Id, project.Id);
        Assert.Equal(request.Name, project.Name);
        Assert.Equal(request.RepositoryUrl, project.RepositoryUrl);
    }
    [Fact]
    public async Task GetProject_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        using var client = factory.CreateClient();

        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync(
            $"/api/projects/{nonExistingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    [Fact]
    public async Task GetProjects_ShouldReturnProjects()
    {
        // Arrange
        using var client = factory.CreateClient();

        var request = new
        {
            Name = "ListHttpTestProject",
            RepositoryUrl = "https://github.com/user/list-test"
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/projects",
            request);

        createResponse.EnsureSuccessStatusCode();

        var createdProject = await createResponse.Content
            .ReadFromJsonAsync<CreateProjectResponse>();

        Assert.NotNull(createdProject);

        // Act
        var response = await client.GetAsync("/api/projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var projects = await response.Content
            .ReadFromJsonAsync<List<ProjectResponse>>();

        Assert.NotNull(projects);
        Assert.Contains(projects, project => project.Id == createdProject.Id);
    }
    private sealed record ProjectResponse(
        Guid Id,
        string Name,
        string RepositoryUrl);
    private sealed record CreateProjectResponse(Guid Id);
}
