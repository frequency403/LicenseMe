using LicenseMe.Core.Interfaces;

namespace LicenseMe.Core.Services;

public sealed class LicenseFileDetector : ILicenseFileDetector
{
    private static readonly string[] Candidates =
    [
        "LICENSE", "LICENCE", "LICENSE.md", "LICENSE.txt",
        "LICENSE.rst", "LICENCE.md", "LICENCE.txt", "COPYING", "COPYING.md"
    ];

    public bool TryDetect(string repoPath, out string? filePath)
    {
        // Fast path: case-sensitive check for common names
        foreach (var candidate in Candidates)
        {
            var path = Path.Combine(repoPath, candidate);
            if (File.Exists(path))
            {
                filePath = path;
                return true;
            }
        }

        // Fallback: case-insensitive enumeration (required on Linux)
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
