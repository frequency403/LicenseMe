using System.Text.Json;
using System.Text.Json.Serialization;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;

namespace LicenseMe.Core.Services;

public sealed class ConfigManager : IConfigManager
{
    public static string ConfigBasePath { get; } = BuildConfigPath();
    public static string ConfigPath { get; } = Path.Combine(ConfigBasePath, "config.json");

    public async Task<LicenseMeConfig> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(ConfigPath))
        {
            var defaultConfig = LicenseMeConfig.Default;
            await SaveAsync(defaultConfig, ct);
            return defaultConfig;
        }

        await using var stream = File.OpenRead(ConfigPath);
        return await JsonSerializer.DeserializeAsync(
                   stream, LicenseMeConfigJsonContext.Default.LicenseMeConfig, ct)
               ?? LicenseMeConfig.Default;
    }

    public async Task SaveAsync(LicenseMeConfig config, CancellationToken ct = default)
    {
        await using var stream = File.Open(ConfigPath, FileMode.OpenOrCreate);
        await JsonSerializer.SerializeAsync(
            stream, config, LicenseMeConfigJsonContext.Default.LicenseMeConfig, ct);
    }

    private static string BuildConfigPath()
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create), AppDomain.CurrentDomain.FriendlyName.ToLower().Trim(' '));
        if(!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);
        return basePath;
    }
}

[JsonSerializable(typeof(LicenseMeConfig))]
[JsonSerializable(typeof(List<string>))]
[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class LicenseMeConfigJsonContext : JsonSerializerContext;
