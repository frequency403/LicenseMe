namespace LicenseMe.Core.Domain.Models;

/// <summary>
/// Represents a git repository found during a scan, including the status of
/// its license and readme files. SpdxIds is modelled as a list to support
/// multi-license scenarios in future.
/// </summary>
public sealed record DiscoveredRepository(
    string FullPath,
    string Name,
    bool HasLicense,
    bool HasReadme,
    string? DetectedLicensePath,
    string? DetectedReadmePath,
    IReadOnlyList<string> SpdxIds)
{
    public DiscoveredRepository(
        string fullPath,
        string name,
        bool hasLicense,
        bool hasReadme,
        string? detectedLicensePath,
        string? detectedReadmePath)
        : this(fullPath, name, hasLicense, hasReadme,
               detectedLicensePath, detectedReadmePath, []) { }
}
