namespace DeployForge.Application.PipelineRuns;

public interface IPipelineRunQueue
{
    Task PublishAsync(
        PipelineRunRequested message,
        CancellationToken cancellationToken = default);
}
