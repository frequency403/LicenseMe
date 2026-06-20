namespace LicenseMe.Core.Interfaces;

public interface ILicenseWriter
{
    /// <summary>Writes a LICENSE file to <paramref name="repoPath"/> using the given SPDX ID.</summary>
    Task WriteAsync(string repoPath, string spdxId, CancellationToken ct = default);
}
