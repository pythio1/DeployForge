using DeployForge.Application.PipelineRuns;
using DeployForge.Application.Pipelines;

namespace DeployForge.Api.Endpoints;

public static class PipelinesEndpoints
{
    public static IEndpointRouteBuilder MapPipelinesEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/projects/{projectId:guid}/pipelines",
            async (
                Guid projectId,
                CreatePipelineRequest request,
                CreatePipeline createPipeline,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var steps = request.Steps
                        .Select(step => new CreatePipelineStep(
                            step.Name,
                            step.Order,
                            step.Command))
                        .ToArray();

                    var pipeline = await createPipeline.ExecuteAsync(
                        projectId,
                        request.Name,
                        steps,
                        cancellationToken);

                    return Results.Created(
                        $"/api/projects/{projectId}/pipelines/{pipeline.Id}",
                        new
                        {
                            pipeline.Id,
                            pipeline.ProjectId,
                            pipeline.Name,
                            pipeline.CreatedAt,
                            Steps = pipeline.Steps
                                .Select(step => new
                                {
                                    step.Id,
                                    step.Name,
                                    step.Order,
                                    step.Command
                                })
                        });
                }
                catch (KeyNotFoundException exception)
                {
                    return Results.NotFound(new
                    {
                        error = exception.Message
                    });
                }
                catch (ArgumentException exception)
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["pipeline"] = [exception.Message]
                        });
                }
                catch (InvalidOperationException exception)
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["pipeline"] = [exception.Message]
                        });
                }
            })
            .WithName("CreatePipeline");
        app.MapGet(
            "/api/projects/{projectId:guid}/pipelines",
            async (
                Guid projectId,
                GetPipelinesByProject getPipelines,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var pipelines = await getPipelines.ExecuteAsync(
                        projectId,
                        cancellationToken);

                    return Results.Ok(pipelines.Select(pipeline => new
                    {
                        pipeline.Id,
                        pipeline.ProjectId,
                        pipeline.Name,
                        pipeline.CreatedAt,
                        Steps = pipeline.Steps.Select(step => new
                        {
                            step.Id,
                            step.Name,
                            step.Order,
                            step.Command
                        })
                    }));
                }
                catch (KeyNotFoundException exception)
                {
                    return Results.NotFound(new { error = exception.Message });
                }
            })
            .WithName("GetPipelinesByProject");
        app.MapGet(
            "/api/projects/{projectId:guid}/pipelines/{pipelineId:guid}",
            async (
                Guid projectId,
                Guid pipelineId,
                GetPipelineById getPipeline,
                CancellationToken cancellationToken) =>
            {
                var pipeline = await getPipeline.ExecuteAsync(
                    projectId,
                    pipelineId,
                    cancellationToken);

                if (pipeline is null)
                    return Results.NotFound();

                return Results.Ok(new
                {
                    pipeline.Id,
                    pipeline.ProjectId,
                    pipeline.Name,
                    pipeline.CreatedAt,
                    Steps = pipeline.Steps.Select(step => new
                    {
                        step.Id,
                        step.Name,
                        step.Order,
                        step.Command
                    })
                });
            })
            .WithName("GetPipelineById");
            app.MapDelete(
                "/api/projects/{projectId:guid}/pipelines/{pipelineId:guid}",
                async (
                    Guid projectId,
                    Guid pipelineId,
                    DeletePipeline deletePipeline,
                    CancellationToken cancellationToken) =>
                {
                    var deleted = await deletePipeline.ExecuteAsync(
                        projectId,
                        pipelineId,
                        cancellationToken);

                    return deleted
                        ? Results.NoContent()
                        : Results.NotFound();
                });
        app.MapPost(
            "/api/projects/{projectId:guid}/pipelines/{pipelineId:guid}/runs",
            async (
                Guid projectId,
                Guid pipelineId,
                CreatePipelineRun createPipelineRun,
                GetPipelineById getPipelineById,
                CancellationToken cancellationToken) =>
            {
                var pipeline = await getPipelineById.ExecuteAsync(
                    projectId,
                    pipelineId,
                    cancellationToken);

                if (pipeline is null)
                    return Results.NotFound();

                var pipelineRun = await createPipelineRun.ExecuteAsync(
                    pipelineId,
                    cancellationToken);

                return Results.Created(
                    $"/api/projects/{projectId}/pipelines/{pipelineId}/runs/{pipelineRun.Id}",
                    new
                    {
                        pipelineRun.Id,
                        pipelineRun.PipelineId,
                        Status = pipelineRun.Status.ToString(),
                        pipelineRun.CreatedAt,
                        pipelineRun.StartedAt,
                        pipelineRun.FinishedAt
                    });
            });
        app.MapGet(
            "/api/projects/{projectId:guid}/pipelines/{pipelineId:guid}/runs",
            async (
                Guid projectId,
                Guid pipelineId,
                GetPipelineRuns getPipelineRuns,
                GetPipelineById getPipelineById,
                CancellationToken cancellationToken) =>
            {
                var pipeline = await getPipelineById.ExecuteAsync(
                    projectId,
                    pipelineId,
                    cancellationToken);

                if (pipeline is null)
                    return Results.NotFound();

                var runs = await getPipelineRuns.ExecuteAsync(
                    pipelineId,
                    cancellationToken);

                return Results.Ok(runs.Select(run => new
                {
                    run.Id,
                    run.PipelineId,
                    Status = run.Status.ToString(),
                    run.CreatedAt,
                    run.StartedAt,
                    run.FinishedAt
                }));
            });
        app.MapGet(
            "/api/projects/{projectId:guid}/pipelines/{pipelineId:guid}/runs/{runId:guid}",
            async (
                Guid projectId,
                Guid pipelineId,
                Guid runId,
                GetPipelineRunById getPipelineRunById,
                GetPipelineById getPipelineById,
                CancellationToken cancellationToken) =>
            {
                var pipeline = await getPipelineById.ExecuteAsync(
                    projectId,
                    pipelineId,
                    cancellationToken);

                if (pipeline is null)
                    return Results.NotFound();

                var run = await getPipelineRunById.ExecuteAsync(
                    pipelineId,
                    runId,
                    cancellationToken);

                if (run is null)
                    return Results.NotFound();

                return Results.Ok(new
                {
                    run.Id,
                    run.PipelineId,
                    Status = run.Status.ToString(),
                    run.CreatedAt,
                    run.StartedAt,
                    run.FinishedAt
                });
            });
        return app;
    }
}

public sealed record CreatePipelineRequest(
    string Name,
    List<CreatePipelineStepRequest> Steps);

public sealed record CreatePipelineStepRequest(
    string Name,
    int Order,
    string Command);
