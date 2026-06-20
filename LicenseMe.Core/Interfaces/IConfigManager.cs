using LicenseMe.Core.Domain.Models;

namespace LicenseMe.Core.Interfaces;

public interface IConfigManager
{
    Task<LicenseMeConfig> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(LicenseMeConfig config, CancellationToken ct = default);
}
