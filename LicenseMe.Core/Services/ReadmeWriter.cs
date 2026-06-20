using LicenseMe.Core.Interfaces;

namespace LicenseMe.Core.Services;

public sealed class ReadmeWriter : IReadmeWriter
{
    private const string Template =
        """
        # {0}

        > TODO: Add a short project description.

        ## License

        See [LICENSE](LICENSE).
        """;

    public async Task WriteAsync(string repoPath, string repoName, CancellationToken ct = default)
        => await File.WriteAllTextAsync(
            Path.Combine(repoPath, "README.md"),
            string.Format(Template, repoName),
            ct);
}
