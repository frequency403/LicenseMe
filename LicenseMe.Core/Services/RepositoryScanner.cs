using System.Runtime.CompilerServices;
using LibGit2Sharp;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LicenseMe.Core.Services;

public sealed class RepositoryScanner(
    ILicenseFileDetector licenseDetector,
    IReadmeFileDetector readmeDetector,
    IOptions<LicenseMeConfig> options,
    ILogger<RepositoryScanner> logger) : IRepositoryScanner
{
    public async IAsyncEnumerable<DiscoveredRepository> ScanAsync(
        string rootPath,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var config = options.Value;

        await foreach (var repoPath in EnumerateReposAsync(rootPath, 0, config.MaxScanDepth, ct))
        {
            if (IsExcluded(repoPath, config.ExcludedPaths))
                continue;

            licenseDetector.TryDetect(repoPath, out var licensePath);
            readmeDetector.TryDetect(repoPath, out var readmePath);

            yield return new DiscoveredRepository(
                fullPath: repoPath,
                name: Path.GetFileName(repoPath),
                hasLicense: licensePath is not null,
                hasReadme: readmePath is not null,
                detectedLicensePath: licensePath,
                detectedReadmePath: readmePath);
        }
    }

    private async IAsyncEnumerable<string> EnumerateReposAsync(
        string path,
        int depth,
        int? maxDepth,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (maxDepth.HasValue && depth > maxDepth.Value)
            yield break;

        ct.ThrowIfCancellationRequested();

        IEnumerable<string> subDirs;
        try
        {
            subDirs = Directory.EnumerateDirectories(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogDebug("Access denied scanning {Path}: {Message}", path, ex.Message);
            yield break;
        }
        catch (IOException ex)
        {
            logger.LogDebug("IO error scanning {Path}: {Message}", path, ex.Message);
            yield break;
        }

        foreach (var dir in subDirs)
        {
            ct.ThrowIfCancellationRequested();

            if (Repository.IsValid(dir))
            {
                // Do not recurse into a repo – submodules are separate concerns
                yield return dir;
                continue;
            }

            await foreach (var nested in EnumerateReposAsync(dir, depth + 1, maxDepth, ct))
                yield return nested;
        }
    }

    private static bool IsExcluded(string path, IEnumerable<string> excludedPaths)
        => excludedPaths.Any(excluded =>
            path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
}
