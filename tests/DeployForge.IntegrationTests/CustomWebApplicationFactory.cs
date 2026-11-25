using DeployForge.Infrastructure.Persistence;
using DeployForge.Application.PipelineRuns;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DeployForge.IntegrationTests;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private static readonly SemaphoreSlim MigrationLock = new(1, 1);
    private static readonly string DataProtectionKeysPath = Path.Combine(
        Path.GetTempPath(),
        $"deployforge-integration-keys-{Environment.ProcessId}");

    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=deployforge_test;" +
        "Username=deployforge;Password=dev_password";

    public CustomWebApplicationFactory()
    {
        Directory.CreateDirectory(DataProtectionKeysPath);
        Environment.SetEnvironmentVariable(
            "DEPLOYFORGE_DATAPROTECTION_KEYS_PATH",
            DataProtectionKeysPath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        TestConnectionString,
                    ["DEPLOYFORGE_DATAPROTECTION_KEYS_PATH"] =
                        DataProtectionKeysPath
                });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IPipelineRunQueue>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(TestConnectionString));
            services.AddSingleton<IPipelineRunQueue, TestPipelineRunQueue>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();

        MigrationLock.Wait();

        try
        {
            using var scope = host.Services.CreateScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            dbContext.Database.Migrate();
        }
        finally
        {
            MigrationLock.Release();
        }

        host.Start();

        return host;
    }

    private sealed class TestPipelineRunQueue : IPipelineRunQueue
    {
        public Task PublishAsync(
            PipelineRunRequested message,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
