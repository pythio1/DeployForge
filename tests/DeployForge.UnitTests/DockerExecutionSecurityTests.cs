using DeployForge.Domain.Entities;
using DeployForge.Infrastructure.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DeployForge.UnitTests;

public class DockerExecutionSecurityTests
{
    [Fact]
    public void CloneContainer_ShouldKeepSecretOutOfScript()
    {
        const string token = "github_pat_secret-value";
        var executor = CreateExecutor();

        var parameters = executor.CreateCloneContainerParameters(
            "https://github.com/example/private-repository.git",
            token,
            "test-volume");

        Assert.DoesNotContain(token, parameters.Cmd.Single());
        Assert.Contains(
            parameters.Env,
            value => value == $"DEPLOYFORGE_REPOSITORY_SECRET={token}");
        Assert.Contains("GIT_ASKPASS", parameters.Cmd.Single());
    }

    [Fact]
    public void ExecutionContainer_ShouldNotReceiveRepositorySecret()
    {
        var executor = CreateExecutor();
        var steps = new[]
        {
            new PipelineStep("second", 2, "dotnet test"),
            new PipelineStep("first", 1, "dotnet restore")
        };

        var parameters = executor.CreateExecutionContainerParameters(
            steps,
            "test-volume");

        Assert.Empty(parameters.Env);
        var script = parameters.Cmd.Single();
        Assert.DoesNotContain("DEPLOYFORGE_REPOSITORY_SECRET", script);
        Assert.True(
            script.IndexOf("dotnet restore", StringComparison.Ordinal) <
            script.IndexOf("dotnet test", StringComparison.Ordinal));
    }

    private static DockerPipelineStepExecutor CreateExecutor()
    {
        return new DockerPipelineStepExecutor(
            null!,
            Options.Create(new DockerExecutorOptions()),
            NullLogger<DockerPipelineStepExecutor>.Instance);
    }
}
