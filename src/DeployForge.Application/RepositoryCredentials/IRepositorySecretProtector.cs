namespace DeployForge.Application.RepositoryCredentials;

public interface IRepositorySecretProtector
{
    string Protect(string secret);

    string Unprotect(string protectedSecret);
}
