using DeployForge.Application.Projects;
using DeployForge.Domain.Entities;

namespace DeployForge.Application.Pipelines;

public sealed class GetPipelinesByProject(
    IPipelineRepository pipelineRepository,
    IProjectRepository projectRepository)
{
    public async Task<IReadOnlyList<Pipeline>> ExecuteAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(
            projectId,
            cancellationToken);

        if (project is null)
        {
            throw new KeyNotFoundException(
                $"Project with ID '{projectId}' was not found.");
        }

        return await pipelineRepository.GetByProjectIdAsync(
            projectId,
            cancellationToken);
    }
}
