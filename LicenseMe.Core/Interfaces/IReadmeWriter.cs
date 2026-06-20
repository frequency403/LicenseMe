namespace LicenseMe.Core.Interfaces;

public interface IReadmeWriter
{
    Task WriteAsync(string repoPath, string repoName, CancellationToken ct = default);
}
