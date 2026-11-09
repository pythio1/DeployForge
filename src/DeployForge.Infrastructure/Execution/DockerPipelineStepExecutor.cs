
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
        IReadOnlyList<PipelineStep> steps,
        CancellationToken cancellationToken = default)
    {
        if (steps.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot execute a pipeline without steps.");
        }

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
                "Repository URL must be a valid public HTTPS URL without credentials, query, or fragment.",
                nameof(repositoryUrl));
        }

        var orderedSteps = steps
            .OrderBy(step => step.Order)
            .ToList();

        var quotedRepositoryUrl =
            "'" + repositoryUrl.Replace("'", "'\"'\"'") + "'";

        var script =
            "set -eu\n" +
            "command -v git >/dev/null 2>&1 || " +
            "{ echo 'Git is not installed in the executor image.' >&2; exit 127; }\n" +
            "mkdir -p /workspace\n" +
            $"git clone --depth 1 -- {quotedRepositoryUrl} /workspace\n" +
            "cd /workspace\n" +
            string.Join("\n", orderedSteps.Select(step => step.Command));

        string? containerId = null;

        using var timeoutCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeoutCts.CancelAfter(
            TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            _logger.LogInformation(
                "Creating Docker container using image {Image}.",
                _options.Image);

            var response =
                await _dockerClient.Containers.CreateContainerAsync(
                    new CreateContainerParameters
                    {
                        Image = _options.Image,
                        WorkingDir = "/workspace",
                        Entrypoint = new List<string>
                        {
                            "/bin/sh",
                            "-c"
                        },
                        Cmd = new List<string> { script },
                        AttachStdout = true,
                        AttachStderr = true,
                        Tty = false,
                        HostConfig = new HostConfig
                        {
                            Memory = _options.MemoryLimitBytes,
                            NanoCPUs = _options.NanoCpus,
                            PidsLimit = 256,
                            Privileged = false,
                            CapDrop = new List<string> { "ALL" },
                            SecurityOpt = new List<string>
                            {
                                "no-new-privileges:true"
                            }
                        }
                    },
                    timeoutCts.Token);

            containerId = response.ID;

            _logger.LogInformation(
                "Created Docker container {ContainerId}.",
                containerId);

            var started =
                await _dockerClient.Containers.StartContainerAsync(
                    containerId,
                    new ContainerStartParameters(),
                    timeoutCts.Token);

            if (!started)
            {
                throw new InvalidOperationException(
                    $"Failed to start container {containerId}.");
            }

            var result =
                await _dockerClient.Containers.WaitContainerAsync(
                    containerId,
                    timeoutCts.Token);

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
                    timeoutCts.Token);

            var (stdout, stderr) =
                await logStream.ReadOutputToEndAsync(timeoutCts.Token);

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                _logger.LogInformation(
                    "Container stdout:\n{Output}",
                    stdout);
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                _logger.LogError(
                    "Container stderr:\n{Output}",
                    stderr);
            }

            if (result.StatusCode != 0)
            {
                throw new InvalidOperationException(
                    $"Pipeline execution failed with exit code " +
                    $"{result.StatusCode}.");
            }

            _logger.LogInformation(
                "Container {ContainerId} completed successfully.",
                containerId);
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
            if (containerId is not null)
            {
                try
                {
                    await _dockerClient.Containers.RemoveContainerAsync(
                        containerId,
                        new ContainerRemoveParameters
                        {
                            Force = true,
                            RemoveVolumes = true
                        },
                        CancellationToken.None);

                    _logger.LogInformation(
                        "Removed Docker container {ContainerId}.",
                        containerId);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Failed to remove Docker container {ContainerId}.",
                        containerId);
                }
            }
        }
    }
}
