using DeployForge.Application.RepositoryCredentials;

namespace DeployForge.Api.Endpoints;

public static class RepositoryCredentialsEndpoints
{
    public static IEndpointRouteBuilder MapRepositoryCredentialsEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
                "/api/projects/{projectId:guid}/repository-credential")
            .WithTags("Repository credentials");

        group.MapPut("/", async (
            Guid projectId,
            SetRepositoryCredentialRequest request,
            SetRepositoryCredential command,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await command.ExecuteAsync(
                    projectId,
                    request.Provider,
                    request.Secret,
                    cancellationToken);

                var response = ToResponse(result.Credential);

                return result.Created
                    ? Results.Created(
                        $"/api/projects/{projectId}/repository-credential",
                        response)
                    : Results.Ok(response);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["credential"] = [exception.Message]
                    });
            }
        })
        .WithName("SetRepositoryCredential");

        group.MapGet("/", async (
            Guid projectId,
            GetRepositoryCredential query,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var credential = await query.ExecuteAsync(
                    projectId,
                    cancellationToken);

                return credential is null
                    ? Results.NotFound()
                    : Results.Ok(ToResponse(credential));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        })
        .WithName("GetRepositoryCredential");

        group.MapDelete("/", async (
            Guid projectId,
            DeleteRepositoryCredential command,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var deleted = await command.ExecuteAsync(
                    projectId,
                    cancellationToken);

                return deleted
                    ? Results.NoContent()
                    : Results.NotFound();
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        })
        .WithName("DeleteRepositoryCredential");

        return app;
    }

    private static RepositoryCredentialResponse ToResponse(
        Domain.Entities.RepositoryCredential credential)
    {
        return new RepositoryCredentialResponse(
            credential.ProjectId,
            credential.Provider,
            credential.CreatedAt);
    }

    public sealed record SetRepositoryCredentialRequest(
        string Provider,
        string Secret);

    public sealed record RepositoryCredentialResponse(
        Guid ProjectId,
        string Provider,
        DateTimeOffset CreatedAt);
}
