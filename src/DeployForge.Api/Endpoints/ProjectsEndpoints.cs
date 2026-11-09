
using DeployForge.Application.Projects;

namespace DeployForge.Api.Endpoints;

public static class ProjectsEndpoints
{
    public static IEndpointRouteBuilder MapProjectsEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects");

        // Создать проект
        group.MapPost("/", async (
            CreateProject command,
            CreateProjectRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var id = await command.ExecuteAsync(
                    request.Name,
                    request.RepositoryUrl,
                    cancellationToken);

                return Results.Created(
                    $"/api/projects/{id}",
                    new { id });
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["request"] = [exception.Message]
                    });
            }
        })
        .WithName("CreateProject");

        // Получить все проекты
        group.MapGet("/", async (
            GetProjects query,
            CancellationToken cancellationToken) =>
        {
            var projects = await query.ExecuteAsync(cancellationToken);
            return Results.Ok(projects);
        })
        .WithName("GetProjects");

        // Получить проект по ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            IProjectRepository repository,
            CancellationToken cancellationToken) =>
        {
            var project = await repository.GetByIdAsync(
                id,
                cancellationToken);

            return project is null
                ? Results.NotFound()
                : Results.Ok(project);
        })
        .WithName("GetProjectById");

        return app;
    }

    public sealed record CreateProjectRequest(
        string Name,
        string RepositoryUrl);
}
