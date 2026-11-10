using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace DeployForge.Infrastructure.Security;

public static class DataProtectionServiceCollectionExtensions
{
    public static IServiceCollection AddDeployForgeDataProtection(
        this IServiceCollection services,
        string keysDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keysDirectory);

        if (!Path.IsPathFullyQualified(keysDirectory))
        {
            throw new ArgumentException(
                "Data Protection keys directory must be an absolute path.",
                nameof(keysDirectory));
        }

        var directory = new DirectoryInfo(keysDirectory);

        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException(
                $"Data Protection keys directory does not exist: {keysDirectory}");
        }

        services.AddDataProtection()
            .SetApplicationName("DeployForge")
            .PersistKeysToFileSystem(directory);

        return services;
    }
}
