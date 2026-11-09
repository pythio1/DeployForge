using DeployForge.Domain.Entities;

namespace DeployForge.UnitTests;

public class PipelineTests
{
    [Fact]
    public void Constructor_ShouldCreateValidPipeline()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var pipeline = new Pipeline(projectId, "Build and Test");

        // Assert
        Assert.NotEqual(Guid.Empty, pipeline.Id);
        Assert.Equal(projectId, pipeline.ProjectId);
        Assert.Equal("Build and Test", pipeline.Name);
        Assert.True(pipeline.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Empty(pipeline.Steps);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenProjectIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new Pipeline(Guid.Empty, "Build and Test"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new Pipeline(Guid.NewGuid(), " "));
    }

    [Fact]
    public void AddStep_ShouldAddStepToPipeline()
    {
        // Arrange
        var pipeline = new Pipeline(Guid.NewGuid(), "Build and Test");
        var step = new PipelineStep("Build", 1, "dotnet build");

        // Act
        pipeline.AddStep(step);

        // Assert
        var addedStep = Assert.Single(pipeline.Steps);

        Assert.Equal(step.Id, addedStep.Id);
        Assert.Equal(pipeline.Id, addedStep.PipelineId);
        Assert.Equal("Build", addedStep.Name);
        Assert.Equal(1, addedStep.Order);
    }

    [Fact]
    public void AddStep_ShouldThrow_WhenOrderAlreadyExists()
    {
        // Arrange
        var pipeline = new Pipeline(Guid.NewGuid(), "Build and Test");

        pipeline.AddStep(
            new PipelineStep("Restore", 1, "dotnet restore"));

        var duplicateStep = new PipelineStep(
            "Build",
            1,
            "dotnet build");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            pipeline.AddStep(duplicateStep));
    }

    [Fact]
    public void AddStep_ShouldThrow_WhenStepIsNull()
    {
        // Arrange
        var pipeline = new Pipeline(Guid.NewGuid(), "Build and Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            pipeline.AddStep(null!));
    }
}
