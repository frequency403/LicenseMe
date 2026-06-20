namespace LicenseMe.Core.Interfaces;

public interface ILicenseFileDetector
{
    bool TryDetect(string repoPath, out string? filePath);
}
