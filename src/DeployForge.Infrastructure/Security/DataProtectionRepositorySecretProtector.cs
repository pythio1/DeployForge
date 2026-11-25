using DeployForge.Application.RepositoryCredentials;
using Microsoft.AspNetCore.DataProtection;

namespace DeployForge.Infrastructure.Security;

public sealed class DataProtectionRepositorySecretProtector
    : IRepositorySecretProtector
{
    private const string Purpose =
        "DeployForge.RepositoryCredential.Secret.v1";

    private readonly IDataProtector _protector;

    public DataProtectionRepositorySecretProtector(
        IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return _protector.Protect(secret);
    }

    public string Unprotect(string protectedSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedSecret);
        return _protector.Unprotect(protectedSecret);
    }
}
