using DeployForge.Domain.Enums;

namespace DeployForge.Domain.Entities;

public sealed class PipelineRun
{
    public Guid Id { get; private set; }

    public Guid PipelineId { get; private set; }

    public PipelineRunStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    // EF Core uses this constructor.
    private PipelineRun() { }

    public PipelineRun(Guid pipelineId)
    {
        if (pipelineId == Guid.Empty)
        {
            throw new ArgumentException(
                "Pipeline ID is required.",
                nameof(pipelineId));
        }

        Id = Guid.NewGuid();
        PipelineId = pipelineId;
        Status = PipelineRunStatus.Queued;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Start()
    {
        if (Status != PipelineRunStatus.Queued)
        {
            throw new InvalidOperationException(
                "Only queued pipeline runs can be started.");
        }

        Status = PipelineRunStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Succeed()
    {
        Complete(PipelineRunStatus.Succeeded);
    }

    public void Fail()
    {
        Complete(PipelineRunStatus.Failed);
    }

    public void Cancel()
    {
        if (Status != PipelineRunStatus.Queued &&
            Status != PipelineRunStatus.Running)
        {
            throw new InvalidOperationException(
                "Only queued or running pipeline runs can be cancelled.");
        }

        Status = PipelineRunStatus.Cancelled;
        FinishedAt = DateTimeOffset.UtcNow;
    }

    private void Complete(PipelineRunStatus status)
    {
        if (Status != PipelineRunStatus.Running)
        {
            throw new InvalidOperationException(
                "Only running pipeline runs can be completed.");
        }

        Status = status;
        FinishedAt = DateTimeOffset.UtcNow;
    }
}
