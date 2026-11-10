namespace DeployForge.Domain.Entities;

public sealed class Project
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string RepositoryUrl { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    // EF Core использует этот конструктор при чтении из БД.
    private Project() { }
    public RepositoryCredential? RepositoryCredential { get; private set; }
    public void SetRepositoryCredential(RepositoryCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        if (credential.ProjectId != Id)
            throw new InvalidOperationException(
                "Repository credential belongs to another project.");

        RepositoryCredential = credential;
    }

    public Project(string name, string repositoryUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Project name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(repositoryUrl))
            throw new ArgumentException(
                "Repository URL is required.", nameof(repositoryUrl));


        Id = Guid.NewGuid();
        Name = name.Trim();
        RepositoryUrl = repositoryUrl.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }
    private readonly List<Pipeline> _pipelines = [];
    public IReadOnlyCollection<Pipeline> Pipelines => _pipelines.AsReadOnly();
}
