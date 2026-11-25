using DeployForge.Application.PipelineRuns;
using DeployForge.Domain.Entities;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeployForge.Infrastructure.Execution;

public sealed class DockerPipelineStepExecutor : IPipelineStepExecutor
{
    private readonly IDockerClient _dockerClient;
    private readonly DockerExecutorOptions _options;
    private readonly ILogger<DockerPipelineStepExecutor> _logger;

    public DockerPipelineStepExecutor(
        IDockerClient dockerClient,
        IOptions<DockerExecutorOptions> options,
        ILogger<DockerPipelineStepExecutor> logger)
    {
        _dockerClient = dockerClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        string repositoryUrl,
        string? repositorySecret,
        IReadOnlyList<PipelineStep> steps,
        CancellationToken cancellationToken = default)
    {
        if (steps.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot execute a pipeline without steps.");
        }

        var repositoryUri = ValidateRepositoryUrl(repositoryUrl);

        if (!string.IsNullOrEmpty(repositorySecret) &&
            !string.Equals(
                repositoryUri.Host,
                "github.com",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "GitHub credentials can only be used with github.com repositories.",
                nameof(repositoryUrl));
        }

        var volumeName = $"deployforge-{Guid.NewGuid():N}";
        string? cloneContainerId = null;
        string? executionContainerId = null;

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeoutCts.CancelAfter(
            TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            await _dockerClient.Volumes.CreateAsync(
                new VolumesCreateParameters { Name = volumeName },
                timeoutCts.Token);

            cloneContainerId = await CreateAndRunContainerAsync(
                CreateCloneContainerParameters(
                    repositoryUrl,
                    repositorySecret,
                    volumeName),
                "repository clone",
                timeoutCts.Token);

            await RemoveContainerAsync(
                cloneContainerId,
                throwOnFailure: true);
            cloneContainerId = null;

            executionContainerId = await CreateAndRunContainerAsync(
                CreateExecutionContainerParameters(steps, volumeName),
                "pipeline execution",
                timeoutCts.Token);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Pipeline execution exceeded the timeout of " +
                $"{_options.TimeoutSeconds} seconds.");
        }
        finally
        {
            if (cloneContainerId is not null)
                await RemoveContainerAsync(cloneContainerId);

            if (executionContainerId is not null)
                await RemoveContainerAsync(executionContainerId);

            try
            {
                await _dockerClient.Volumes.RemoveAsync(
                    volumeName,
                    force: true,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to remove Docker volume {VolumeName}.",
                    volumeName);
            }
        }
    }

    internal CreateContainerParameters CreateCloneContainerParameters(
        string repositoryUrl,
        string? repositorySecret,
        string volumeName)
    {
        var environment = new List<string>
        {
            "GIT_TERMINAL_PROMPT=0"
        };

        if (!string.IsNullOrEmpty(repositorySecret))
        {
            environment.Add(
                $"DEPLOYFORGE_REPOSITORY_SECRET={repositorySecret}");
        }

        return CreateContainerParameters(
            DockerExecutionScriptBuilder.BuildCloneScript(
                repositoryUrl,
                hasSecret: !string.IsNullOrEmpty(repositorySecret)),
            volumeName,
            environment);
    }

    internal CreateContainerParameters CreateExecutionContainerParameters(
        IReadOnlyList<PipelineStep> steps,
        string volumeName)
    {
        return CreateContainerParameters(
            DockerExecutionScriptBuilder.BuildExecutionScript(steps),
            volumeName,
            environment: []);
    }

    private CreateContainerParameters CreateContainerParameters(
        string script,
        string volumeName,
        IList<string> environment)
    {
        return new CreateContainerParameters
        {
            Image = _options.Image,
            WorkingDir = "/workspace",
            Entrypoint = ["/bin/sh", "-c"],
            Cmd = [script],
            Env = environment,
            AttachStdout = true,
            AttachStderr = true,
            Tty = false,
            HostConfig = new HostConfig
            {
                Memory = _options.MemoryLimitBytes,
                NanoCPUs = _options.NanoCpus,
                PidsLimit = 256,
                Privileged = false,
                CapDrop = ["ALL"],
                SecurityOpt = ["no-new-privileges:true"],
                Mounts =
                [
                    new Mount
                    {
                        Type = "volume",
                        Source = volumeName,
                        Target = "/workspace"
                    }
                ]
            }
        };
    }

    private async Task<string> CreateAndRunContainerAsync(
        CreateContainerParameters parameters,
        string stage,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating Docker container for {Stage} using image {Image}.",
            stage,
            _options.Image);

        var response = await _dockerClient.Containers.CreateContainerAsync(
            parameters,
            cancellationToken);

        var containerId = response.ID;

        try
        {
            var started = await _dockerClient.Containers.StartContainerAsync(
                containerId,
                new ContainerStartParameters(),
                cancellationToken);

            if (!started)
            {
                throw new InvalidOperationException(
                    $"Failed to start {stage} container {containerId}.");
            }

            var result = await _dockerClient.Containers.WaitContainerAsync(
                containerId,
                cancellationToken);

            using var logStream =
                await _dockerClient.Containers.GetContainerLogsAsync(
                    containerId,
                    false,
                    new ContainerLogsParameters
                    {
                        ShowStdout = true,
                        ShowStderr = true,
                        Follow = false,
                        Timestamps = false,
                        Tail = "all"
                    },
                    cancellationToken);

            var (stdout, stderr) =
                await logStream.ReadOutputToEndAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(stdout))
                _logger.LogInformation(
                    "{Stage} stdout:\n{Output}",
                    stage,
                    stdout);

            if (!string.IsNullOrWhiteSpace(stderr))
                _logger.LogWarning(
                    "{Stage} stderr:\n{Output}",
                    stage,
                    stderr);

            if (result.StatusCode != 0)
            {
                throw new InvalidOperationException(
                    $"{stage} failed with exit code {result.StatusCode}.");
            }

            return containerId;
        }
        catch
        {
            await RemoveContainerAsync(containerId);
            throw;
        }
    }

    private async Task RemoveContainerAsync(
        string containerId,
        bool throwOnFailure = false)
    {
        try
        {
            await _dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters
                {
                    Force = true,
                    RemoveVolumes = false
                },
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to remove Docker container {ContainerId}.",
                containerId);

            if (throwOnFailure)
                throw;
        }
    }

    private static Uri ValidateRepositoryUrl(string repositoryUrl)
    {
        if (!Uri.TryCreate(
                repositoryUrl,
                UriKind.Absolute,
                out var repositoryUri) ||
            repositoryUri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(repositoryUri.Host) ||
            !string.IsNullOrEmpty(repositoryUri.UserInfo) ||
            !string.IsNullOrEmpty(repositoryUri.Query) ||
            !string.IsNullOrEmpty(repositoryUri.Fragment))
        {
            throw new ArgumentException(
                "Repository URL must be a valid HTTPS URL without credentials, query, or fragment.",
                nameof(repositoryUrl));
        }

        return repositoryUri;
    }
}
