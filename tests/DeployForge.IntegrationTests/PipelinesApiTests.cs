
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DeployForge.IntegrationTests;

public sealed class PipelinesApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PipelinesApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetByProject_ShouldReturnPipelinesWithSteps()
    {
        var projectId = await CreateProjectAsync();
        var pipelineId = await CreatePipelineAsync(projectId);

        var response = await _client.GetAsync(
            $"/api/projects/{projectId}/pipelines");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var pipelines = json.RootElement;

        Assert.Contains(
            pipelines.EnumerateArray(),
            pipeline => pipeline.GetProperty("id").GetGuid() == pipelineId);

        var createdPipeline = pipelines.EnumerateArray()
            .First(pipeline =>
                pipeline.GetProperty("id").GetGuid() == pipelineId);

        var steps = createdPipeline.GetProperty("steps");

        Assert.Equal(2, steps.GetArrayLength());
        Assert.Equal("Build", steps[0].GetProperty("name").GetString());
        Assert.Equal("Test", steps[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetById_ShouldReturnPipelineWithSteps()
    {
        var projectId = await CreateProjectAsync();
        var pipelineId = await CreatePipelineAsync(projectId);

        var response = await _client.GetAsync(
            $"/api/projects/{projectId}/pipelines/{pipelineId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            pipelineId,
            json.RootElement.GetProperty("id").GetGuid());

        Assert.Equal(
            2,
            json.RootElement.GetProperty("steps").GetArrayLength());
    }

    [Fact]
    public async Task GetById_WhenPipelineDoesNotExist_ShouldReturnNotFound()
    {
        var projectId = await CreateProjectAsync();
        var pipelineId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/api/projects/{projectId}/pipelines/{pipelineId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenPipelineBelongsToAnotherProject_ShouldReturnNotFound()
    {
        var firstProjectId = await CreateProjectAsync();
        var secondProjectId = await CreateProjectAsync();

        var pipelineId = await CreatePipelineAsync(firstProjectId);

        var response = await _client.GetAsync(
            $"/api/projects/{secondProjectId}/pipelines/{pipelineId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateProjectAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/projects",
            new
            {
                Name = $"Test Project {Guid.NewGuid()}",
                RepositoryUrl = "https://github.com/example/test"
            });

        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreatePipelineAsync(Guid projectId)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/pipelines",
            new
            {
                Name = "Build and Test",
                Steps = new[]
                {
                    new
                    {
                        Name = "Build",
                        Order = 1,
                        Command = "dotnet build"
                    },
                    new
                    {
                        Name = "Test",
                        Order = 2,
                        Command = "dotnet test"
                    }
                }
            });

        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("id").GetGuid();
    }
}
