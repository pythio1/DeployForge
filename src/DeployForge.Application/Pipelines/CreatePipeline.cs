using DeployForge.Application.Projects;
using DeployForge.Domain.Entities;

namespace DeployForge.Application.Pipelines;

public sealed class CreatePipeline(
    IPipelineRepository pipelineRepository,
    IProjectRepository projectRepository)
{
    public async Task<Pipeline> ExecuteAsync(
        Guid projectId,
        string name,
        IReadOnlyCollection<CreatePipelineStep> steps,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(
            projectId,
            cancellationToken);

        if (project is null)
            throw new KeyNotFoundException(
                $"Project with ID '{projectId}' was not found.");

        if (steps.Count == 0)
            throw new ArgumentException(
                "Pipeline must contain at least one step.",
                nameof(steps));

        var pipeline = new Pipeline(projectId, name);

        foreach (var step in steps)
        {
            pipeline.AddStep(new PipelineStep(
                step.Name,
                step.Order,
                step.Command));
        }

        await pipelineRepository.AddAsync(
            pipeline,
            cancellationToken);

        return pipeline;
    }
}

public sealed record CreatePipelineStep(
    string Name,
    int Order,
    string Command);
