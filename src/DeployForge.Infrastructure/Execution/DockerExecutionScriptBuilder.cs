using DeployForge.Domain.Entities;

namespace DeployForge.Infrastructure.Execution;

internal static class DockerExecutionScriptBuilder
{
    public static string BuildCloneScript(
        string repositoryUrl,
        bool hasSecret)
    {
        var quotedRepositoryUrl = Quote(repositoryUrl);
        var authentication = hasSecret
            ? "cat > /tmp/deployforge-askpass <<'EOF'\n" +
              "#!/bin/sh\n" +
              "case \"$1\" in\n" +
              "  *Username*) printf '%s\\n' 'x-access-token' ;;\n" +
              "  *Password*) printf '%s\\n' \"$DEPLOYFORGE_REPOSITORY_SECRET\" ;;\n" +
              "esac\n" +
              "EOF\n" +
              "chmod 700 /tmp/deployforge-askpass\n" +
              "export GIT_ASKPASS=/tmp/deployforge-askpass\n"
            : string.Empty;

        return "set -eu\n" +
               "command -v git >/dev/null 2>&1 || " +
               "{ echo 'Git is not installed in the executor image.' >&2; exit 127; }\n" +
               authentication +
               $"git clone --depth 1 -- {quotedRepositoryUrl} /workspace\n" +
               "rm -f /tmp/deployforge-askpass\n";
    }

    public static string BuildExecutionScript(
        IReadOnlyList<PipelineStep> steps)
    {
        return "set -eu\n" +
               "cd /workspace\n" +
               string.Join(
                   "\n",
                   steps.OrderBy(step => step.Order)
                       .Select(step => step.Command));
    }

    private static string Quote(string value)
    {
        return "'" + value.Replace("'", "'\"'\"'") + "'";
    }
}
