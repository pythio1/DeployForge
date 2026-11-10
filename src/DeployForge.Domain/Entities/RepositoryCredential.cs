namespace DeployForge.Domain.Entities;

public sealed class RepositoryCredential
{
    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string EncryptedSecret { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    private RepositoryCredential() { }

    public RepositoryCredential(
        Guid projectId,
        string provider,
        string encryptedSecret)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException(
                "Project ID is required.",
                nameof(projectId));

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException(
                "Provider is required.",
                nameof(provider));

        if (string.IsNullOrWhiteSpace(encryptedSecret))
            throw new ArgumentException(
                "Encrypted secret is required.",
                nameof(encryptedSecret));

        Id = Guid.NewGuid();
        ProjectId = projectId;
        Provider = provider.Trim();
        EncryptedSecret = encryptedSecret;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateEncryptedSecret(string encryptedSecret)
    {
        if (string.IsNullOrWhiteSpace(encryptedSecret))
            throw new ArgumentException(
                "Encrypted secret is required.",
                nameof(encryptedSecret));

        EncryptedSecret = encryptedSecret;
    }
}
