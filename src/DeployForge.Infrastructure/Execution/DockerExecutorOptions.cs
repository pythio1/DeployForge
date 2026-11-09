namespace DeployForge.Infrastructure.Execution;

public sealed class DockerExecutorOptions
{
    public string Image { get; set; } = "mcr.microsoft.com/dotnet/sdk:10.0";

    public long MemoryLimitBytes { get; set; } = 512 * 1024 * 1024;

    public long NanoCpus { get; set; } = 1_000_000_000;

    public int TimeoutSeconds { get; set; } = 300;
}
