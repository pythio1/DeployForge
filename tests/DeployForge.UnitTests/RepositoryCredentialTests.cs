using DeployForge.Domain.Entities;

namespace DeployForge.UnitTests;

public class RepositoryCredentialTests
{
    [Fact]
    public void Constructor_ShouldNormalizeProvider()
    {
        var credential = new RepositoryCredential(
            Guid.NewGuid(),
            " GitHub ",
            "encrypted-value");

        Assert.Equal("github", credential.Provider);
        Assert.Equal("encrypted-value", credential.EncryptedSecret);
        Assert.NotEqual(Guid.Empty, credential.Id);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenProjectIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new RepositoryCredential(
                Guid.Empty,
                "github",
                "encrypted-value"));
    }

    [Fact]
    public void UpdateEncryptedSecret_ShouldReplaceSecret()
    {
        var credential = new RepositoryCredential(
            Guid.NewGuid(),
            "github",
            "old-value");

        credential.UpdateEncryptedSecret("new-value");

        Assert.Equal("new-value", credential.EncryptedSecret);
    }

    [Fact]
    public void UpdateEncryptedSecret_ShouldThrow_WhenSecretIsEmpty()
    {
        var credential = new RepositoryCredential(
            Guid.NewGuid(),
            "github",
            "old-value");

        Assert.Throws<ArgumentException>(() =>
            credential.UpdateEncryptedSecret(" "));
    }
}
