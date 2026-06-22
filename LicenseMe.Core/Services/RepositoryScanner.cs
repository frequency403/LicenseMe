using System.Runtime.CompilerServices;
using System.Threading.Channels;
using LibGit2Sharp;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LicenseMe.Core.Services;

public sealed class RepositoryScanner(
    [FromKeyedServices("RepositoryReporter")] IProgressReporter<string> progressReporter,
    ILicenseFileDetector licenseDetector,
    IReadmeFileDetector readmeDetector,
    IOptions<LicenseMeConfig> options,
    ILogger<RepositoryScanner> logger) : IRepositoryScanner
{
    
    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        IgnoreInaccessible = true,
        ReturnSpecialDirectories = false,
        RecurseSubdirectories = false,
    };
    
    public async IAsyncEnumerable<DiscoveredRepository> ScanAsync(
        string rootPath,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var config = options.Value;

        await foreach (var repoPath in EnumerateReposAsync(rootPath, 0, config.MaxScanDepth, ct))
        {
            if (IsExcluded(repoPath))
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
        if (depth > maxDepth)
            yield break;

        ct.ThrowIfCancellationRequested();
        var channel = Channel.CreateUnbounded<string>();
        
        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(path, "*", EnumerationOptions))
                {
                    progressReporter.TryUpdateProgress(dir);
                    ct.ThrowIfCancellationRequested();

                    if (Repository.IsValid(dir))
                    {
                        logger.LogDebug("Found repository at {Path}", dir);
                        // Do not recurse into a repo – submodules are separate concerns
                        channel.Writer.TryWrite(dir);
                        continue;
                    }

                    await foreach (var nested in EnumerateReposAsync(dir, depth + 1, maxDepth, ct))
                        channel.Writer.TryWrite(nested);
                }
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, ct);
        
        await foreach(var dir in channel.Reader.ReadAllAsync(ct))
            yield return dir;
    }

    private bool IsExcluded(string path)
        => options.Value.ExcludedPaths.Any(excluded =>
            path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
}