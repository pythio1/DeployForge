using DeployForge.Domain.Entities;
using DeployForge.Domain.Enums;

namespace DeployForge.UnitTests;

public sealed class PipelineRunTests
{
    [Fact]
    public void Constructor_ShouldCreateQueuedRun()
    {
        var pipelineId = Guid.NewGuid();

        var run = new PipelineRun(pipelineId);

        Assert.NotEqual(Guid.Empty, run.Id);
        Assert.Equal(pipelineId, run.PipelineId);
        Assert.Equal(PipelineRunStatus.Queued, run.Status);
        Assert.Null(run.StartedAt);
        Assert.Null(run.FinishedAt);
    }

    [Fact]
    public void Constructor_WhenPipelineIdIsEmpty_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new PipelineRun(Guid.Empty));
    }

    [Fact]
    public void Start_ShouldChangeStatusToRunning()
    {
        var run = new PipelineRun(Guid.NewGuid());

        run.Start();

        Assert.Equal(PipelineRunStatus.Running, run.Status);
        Assert.NotNull(run.StartedAt);
    }

    [Fact]
    public void Start_WhenAlreadyRunning_ShouldThrow()
    {
        var run = new PipelineRun(Guid.NewGuid());
        run.Start();

        Assert.Throws<InvalidOperationException>(
            () => run.Start());
    }

    [Fact]
    public void Succeed_ShouldCompleteRunSuccessfully()
    {
        var run = new PipelineRun(Guid.NewGuid());
        run.Start();

        run.Succeed();

        Assert.Equal(PipelineRunStatus.Succeeded, run.Status);
        Assert.NotNull(run.FinishedAt);
    }

    [Fact]
    public void Fail_ShouldCompleteRunWithFailure()
    {
        var run = new PipelineRun(Guid.NewGuid());
        run.Start();

        run.Fail();

        Assert.Equal(PipelineRunStatus.Failed, run.Status);
        Assert.NotNull(run.FinishedAt);
    }

    [Fact]
    public void Cancel_WhenQueued_ShouldCancelRun()
    {
        var run = new PipelineRun(Guid.NewGuid());

        run.Cancel();

        Assert.Equal(PipelineRunStatus.Cancelled, run.Status);
        Assert.NotNull(run.FinishedAt);
    }

    [Fact]
    public void Cancel_WhenRunning_ShouldCancelRun()
    {
        var run = new PipelineRun(Guid.NewGuid());
        run.Start();

        run.Cancel();

        Assert.Equal(PipelineRunStatus.Cancelled, run.Status);
        Assert.NotNull(run.FinishedAt);
    }

    [Fact]
    public void Succeed_WhenNotRunning_ShouldThrow()
    {
        var run = new PipelineRun(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(
            () => run.Succeed());
    }

    [Fact]
    public void Cancel_WhenAlreadyCompleted_ShouldThrow()
    {
        var run = new PipelineRun(Guid.NewGuid());
        run.Start();
        run.Succeed();

        Assert.Throws<InvalidOperationException>(
            () => run.Cancel());
    }
}
