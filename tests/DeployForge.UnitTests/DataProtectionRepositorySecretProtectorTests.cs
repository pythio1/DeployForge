using DeployForge.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;

namespace DeployForge.UnitTests;

public class DataProtectionRepositorySecretProtectorTests
{
    [Fact]
    public void ProtectAndUnprotect_ShouldRoundTripAcrossProviders()
    {
        var keysDirectory = Directory.CreateTempSubdirectory(
            "deployforge-data-protection-");

        try
        {
            var firstProvider = DataProtectionProvider.Create(
                keysDirectory,
                configuration =>
                    configuration.SetApplicationName("DeployForge"));
            var secondProvider = DataProtectionProvider.Create(
                keysDirectory,
                configuration =>
                    configuration.SetApplicationName("DeployForge"));
            var firstProtector =
                new DataProtectionRepositorySecretProtector(firstProvider);
            var secondProtector =
                new DataProtectionRepositorySecretProtector(secondProvider);

            const string secret = "github-token";
            var protectedSecret = firstProtector.Protect(secret);

            Assert.NotEqual(secret, protectedSecret);
            Assert.DoesNotContain(secret, protectedSecret);
            Assert.Equal(secret, secondProtector.Unprotect(protectedSecret));
        }
        finally
        {
            keysDirectory.Delete(recursive: true);
        }
    }
}
