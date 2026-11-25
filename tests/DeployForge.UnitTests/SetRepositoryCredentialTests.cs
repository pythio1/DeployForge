using DeployForge.Application.Projects;
using DeployForge.Application.RepositoryCredentials;
using DeployForge.Domain.Entities;

namespace DeployForge.UnitTests;

public class SetRepositoryCredentialTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldProtectAndCreateCredential()
    {
        var project = new Project(
            "Private project",
            "https://github.com/example/private-repository");
        var projectRepository = new FakeProjectRepository(project);
        var credentialRepository = new FakeCredentialRepository();
        var protector = new FakeSecretProtector();
        var command = new SetRepositoryCredential(
            projectRepository,
            credentialRepository,
            protector);

        var result = await command.ExecuteAsync(
            project.Id,
            "GitHub",
            "  github-token  ");

        Assert.True(result.Created);
        Assert.Equal("github", result.Credential.Provider);
        Assert.Equal(
            "protected:github-token",
            result.Credential.EncryptedSecret);
        Assert.Equal("github-token", protector.LastProtectedValue);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateExistingCredential()
    {
        var project = new Project(
            "Private project",
            "https://github.com/example/private-repository");
        var existing = new RepositoryCredential(
            project.Id,
            "github",
            "protected:old-token");
        var credentialRepository = new FakeCredentialRepository(existing);
        var command = new SetRepositoryCredential(
            new FakeProjectRepository(project),
            credentialRepository,
            new FakeSecretProtector());

        var result = await command.ExecuteAsync(
            project.Id,
            "github",
            "new-token");

        Assert.False(result.Created);
        Assert.Equal(existing.Id, result.Credential.Id);
        Assert.Equal(
            "protected:new-token",
            result.Credential.EncryptedSecret);
        Assert.Equal(1, credentialRepository.UpdateCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenProjectDoesNotExist()
    {
        var protector = new FakeSecretProtector();
        var command = new SetRepositoryCredential(
            new FakeProjectRepository(),
            new FakeCredentialRepository(),
            protector);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            command.ExecuteAsync(
                Guid.NewGuid(),
                "github",
                "token"));

        Assert.Null(protector.LastProtectedValue);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectUnsupportedProvider()
    {
        var command = new SetRepositoryCredential(
            new FakeProjectRepository(),
            new FakeCredentialRepository(),
            new FakeSecretProtector());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            command.ExecuteAsync(
                Guid.NewGuid(),
                "gitlab",
                "token"));
    }

    private sealed class FakeProjectRepository(params Project[] projects)
        : IProjectRepository
    {
        private readonly List<Project> _projects = [.. projects];

        public Task AddAsync(
            Project project,
            CancellationToken cancellationToken = default)
        {
            _projects.Add(project);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Project>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Project>>(_projects);
        }

        public Task<Project?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _projects.FirstOrDefault(project => project.Id == id));
        }
    }

    private sealed class FakeCredentialRepository(
        params RepositoryCredential[] credentials)
        : IRepositoryCredentialRepository
    {
        private readonly List<RepositoryCredential> _credentials =
            [.. credentials];

        public int UpdateCount { get; private set; }

        public Task<RepositoryCredential?> GetByProjectIdAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _credentials.FirstOrDefault(
                    credential => credential.ProjectId == projectId));
        }

        public Task AddAsync(
            RepositoryCredential credential,
            CancellationToken cancellationToken = default)
        {
            _credentials.Add(credential);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            RepositoryCredential credential,
            CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteByProjectIdAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            var removed = _credentials.RemoveAll(
                credential => credential.ProjectId == projectId);
            return Task.FromResult(removed > 0);
        }
    }

    private sealed class FakeSecretProtector : IRepositorySecretProtector
    {
        public string? LastProtectedValue { get; private set; }

        public string Protect(string secret)
        {
            LastProtectedValue = secret;
            return $"protected:{secret}";
        }

        public string Unprotect(string protectedSecret)
        {
            return protectedSecret["protected:".Length..];
        }
    }
}
