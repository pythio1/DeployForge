using DeployForge.Application.Projects;
using DeployForge.Domain.Entities;

namespace DeployForge.Application.RepositoryCredentials;

public sealed class SetRepositoryCredential(
    IProjectRepository projectRepository,
    IRepositoryCredentialRepository credentialRepository,
    IRepositorySecretProtector secretProtector)
{
    public const string GitHubProvider = "github";

    public async Task<SetRepositoryCredentialResult> ExecuteAsync(
        Guid projectId,
        string provider,
        string secret,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(
                provider?.Trim(),
                GitHubProvider,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only the 'github' repository credential provider is supported.",
                nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException(
                "Repository credential secret is required.",
                nameof(secret));
        }

        var project = await projectRepository.GetByIdAsync(
            projectId,
            cancellationToken);

        if (project is null)
        {
            throw new KeyNotFoundException(
                $"Project with ID '{projectId}' was not found.");
        }

        var encryptedSecret = secretProtector.Protect(secret.Trim());
        var credential = await credentialRepository.GetByProjectIdAsync(
            projectId,
            cancellationToken);

        if (credential is null)
        {
            credential = new RepositoryCredential(
                projectId,
                GitHubProvider,
                encryptedSecret);

            await credentialRepository.AddAsync(
                credential,
                cancellationToken);

            return new SetRepositoryCredentialResult(credential, true);
        }

        credential.UpdateEncryptedSecret(encryptedSecret);
        await credentialRepository.UpdateAsync(
            credential,
            cancellationToken);

        return new SetRepositoryCredentialResult(credential, false);
    }
}

public sealed record SetRepositoryCredentialResult(
    RepositoryCredential Credential,
    bool Created);
