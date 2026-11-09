namespace DeployForge.Application.PipelineRuns;

public sealed record PipelineRunRequested(
    Guid RunId,
    Guid PipelineId,
    DateTimeOffset RequestedAt);
