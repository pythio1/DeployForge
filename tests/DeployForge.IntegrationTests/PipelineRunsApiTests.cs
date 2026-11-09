
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DeployForge.IntegrationTests;

public sealed class PipelineRunsApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PipelineRunsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedRun()
    {
        var projectId = await CreateProjectAsync();
        var pipelineId = await CreatePipelineAsync(projectId);

        var response = await _client.PostAsync(
            $"/api/projects/{projectId}/pipelines/{pipelineId}/runs",
            null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var run = json.RootElement;

        Assert.Equal(
            pipelineId,
            run.GetProperty("pipelineId").GetGuid());

        Assert.Equal(
            "Queued",
            run.GetProperty("status").GetString());

        Assert.True(
            run.GetProperty("createdAt").GetDateTimeOffset() > DateTimeOffset.MinValue);

        Assert.Equal(
            JsonValueKind.Null,
            run.GetProperty("startedAt").ValueKind);

        Assert.Equal(
            JsonValueKind.Null,
            run.GetProperty("finishedAt").ValueKind);
    }

    [Fact]
    public async Task GetByPipeline_ShouldReturnRunHistory()
    {
        var projectId = await CreateProjectAsync();
        var pipelineId = await CreatePipelineAsync(projectId);

        await _client.PostAsync(
            $"/api/projects/{projectId}/pipelines/{pipelineId}/runs",
            null);

        var response = await _client.GetAsync(
            $"/api/projects/{projectId}/pipelines/{pipelineId}/runs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var runs = json.RootElement.EnumerateArray().ToList();

        Assert.Single(runs);

        Assert.Equal(
            pipelineId,
            runs[0].GetProperty("pipelineId").GetGuid());

        Assert.Equal(
            "Queued",
            runs[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetById_ShouldReturnRun()
    {
        var projectId = await CreateProjectAsync();
        var pipelineId = await CreatePipelineAsync(projectId);

        var createResponse = await _client.PostAsync(
            $"/api/projects/{projectId}/pipelines/{pipelineId}/runs",
            null);

        createResponse.EnsureSuccessStatusCode();

        using var createJson = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        var runId = createJson.RootElement
            .GetProperty("id")
            .GetGuid();

        var response = await _client.GetAsync(
            $"/api/projects/{projectId}/pipelines/{pipelineId}/runs/{runId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal(
            runId,
            json.RootElement.GetProperty("id").GetGuid());

        Assert.Equal(
            pipelineId,
            json.RootElement.GetProperty("pipelineId").GetGuid());

        Assert.Equal(
            "Queued",
            json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetById_WhenRunBelongsToAnotherPipeline_ShouldReturnNotFound()
    {
        var projectId = await CreateProjectAsync();

        var firstPipelineId = await CreatePipelineAsync(projectId);
        var secondPipelineId = await CreatePipelineAsync(projectId);

        var createResponse = await _client.PostAsync(
            $"/api/projects/{projectId}/pipelines/{firstPipelineId}/runs",
            null);

        createResponse.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        var runId = json.RootElement.GetProperty("id").GetGuid();

        var response = await _client.GetAsync(
            $"/api/projects/{projectId}/pipelines/{secondPipelineId}/runs/{runId}");

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
                Name = $"Build Pipeline {Guid.NewGuid()}",
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
