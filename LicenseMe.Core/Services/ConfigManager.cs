using System.Text.Json;
using System.Text.Json.Serialization;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;

namespace LicenseMe.Core.Services;

public sealed class ConfigManager : IConfigManager
{
    public static string ConfigPath { get; } = BuildConfigPath();

    public async Task<LicenseMeConfig> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(ConfigPath))
            return LicenseMeConfig.Default;

        await using var stream = File.OpenRead(ConfigPath);
        return await JsonSerializer.DeserializeAsync(
                   stream, LicenseMeConfigJsonContext.Default.LicenseMeConfig, ct)
               ?? LicenseMeConfig.Default;
    }

    public async Task SaveAsync(LicenseMeConfig config, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        await using var stream = File.Open(ConfigPath, FileMode.Create);
        await JsonSerializer.SerializeAsync(
            stream, config, LicenseMeConfigJsonContext.Default.LicenseMeConfig, ct);
    }

    private static string BuildConfigPath()
    {
        var basePath = OperatingSystem.IsWindows()
            ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            : Path.Combine(
                Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                ?? Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile), ".config"));

        return Path.Combine(basePath, "licenseme", "config.json");
    }
}

[JsonSerializable(typeof(LicenseMeConfig))]
[JsonSerializable(typeof(List<string>))]
[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class LicenseMeConfigJsonContext : JsonSerializerContext;
