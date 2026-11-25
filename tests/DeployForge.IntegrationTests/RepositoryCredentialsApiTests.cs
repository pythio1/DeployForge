using System.Net;
using System.Net.Http.Json;
using DeployForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DeployForge.IntegrationTests;

public class RepositoryCredentialsApiTests(
    CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CredentialLifecycle_ShouldNeverExposeSecret()
    {
        using var client = factory.CreateClient();
        var projectId = await CreateProjectAsync(client);
        const string firstSecret = "github_pat_first-secret";

        var createResponse = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/repository-credential",
            new { Provider = "github", Secret = firstSecret });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(firstSecret, createBody);
        Assert.DoesNotContain("encryptedSecret", createBody, StringComparison.OrdinalIgnoreCase);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();
            var storedCredential = await dbContext.RepositoryCredentials
                .AsNoTracking()
                .SingleAsync(
                    credential => credential.ProjectId == projectId);

            Assert.NotEqual(firstSecret, storedCredential.EncryptedSecret);
            Assert.DoesNotContain(
                firstSecret,
                storedCredential.EncryptedSecret);
        }

        const string secondSecret = "github_pat_second-secret";
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/repository-credential",
            new { Provider = "GitHub", Secret = secondSecret });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.DoesNotContain(
            secondSecret,
            await updateResponse.Content.ReadAsStringAsync());

        var getResponse = await client.GetAsync(
            $"/api/projects/{projectId}/repository-credential");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(firstSecret, getBody);
        Assert.DoesNotContain(secondSecret, getBody);
        Assert.DoesNotContain("encryptedSecret", getBody, StringComparison.OrdinalIgnoreCase);

        var deleteResponse = await client.DeleteAsync(
            $"/api/projects/{projectId}/repository-credential");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missingResponse = await client.GetAsync(
            $"/api/projects/{projectId}/repository-credential");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task PutCredential_ShouldRejectUnsupportedProvider()
    {
        using var client = factory.CreateClient();
        var projectId = await CreateProjectAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/repository-credential",
            new { Provider = "gitlab", Secret = "secret" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutCredential_ShouldReturnNotFound_WhenProjectIsMissing()
    {
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/projects/{Guid.NewGuid()}/repository-credential",
            new { Provider = "github", Secret = "secret" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/projects",
            new
            {
                Name = $"Credential test {Guid.NewGuid():N}",
                RepositoryUrl =
                    "https://github.com/example/private-repository"
            });
        response.EnsureSuccessStatusCode();

        var project = await response.Content
            .ReadFromJsonAsync<CreateProjectResponse>();

        return project!.Id;
    }

    private sealed record CreateProjectResponse(Guid Id);
}
