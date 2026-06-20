using LicenseMe.Core.Interfaces;

namespace LicenseMe.Core.Services;

public sealed class ReadmeFileDetector : IReadmeFileDetector
{
    private static readonly string[] Candidates =
    [
        "README.md", "README", "README.txt", "README.rst",
        "README.adoc", "readme.md"
    ];

    public bool TryDetect(string repoPath, out string? filePath)
    {
        foreach (var candidate in Candidates)
        {
            var path = Path.Combine(repoPath, candidate);
            if (File.Exists(path))
            {
                filePath = path;
                return true;
            }
        }

        try
        {
            var match = Directory
                .EnumerateFiles(repoPath, "*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => Candidates.Contains(
                    Path.GetFileName(f), StringComparer.OrdinalIgnoreCase));

            filePath = match;
            return match is not null;
        }
        catch
        {
            filePath = null;
            return false;
        }
    }
}
