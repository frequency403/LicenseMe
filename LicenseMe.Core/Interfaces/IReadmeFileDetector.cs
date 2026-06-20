namespace LicenseMe.Core.Interfaces;

public interface IReadmeFileDetector
{
    bool TryDetect(string repoPath, out string? filePath);
}
