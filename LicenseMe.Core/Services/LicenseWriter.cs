using LicenseMe.Core.Interfaces;
using OpenSourceInitiative.LicenseApi.Interfaces;

namespace LicenseMe.Core.Services;

public sealed class LicenseWriter(IOsiClient osiClient) : ILicenseWriter
{
    public async Task WriteAsync(string repoPath, string spdxId, CancellationToken ct = default)
    {
        var results = await osiClient.GetBySpdxIdAsync(spdxId, ct);
        var license = results.FirstOrDefault(l => l is not null)
            ?? throw new InvalidOperationException(
                $"No OSI license found for SPDX ID '{spdxId}'.");

        var licenseText = license.LicenseText
            ?? throw new InvalidOperationException(
                $"License text unavailable for '{spdxId}'.");

        await File.WriteAllTextAsync(
            Path.Combine(repoPath, "LICENSE"),
            licenseText,
            ct);
    }
}
