using System.Text.Json;
using System.Text.Json.Serialization;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;

namespace LicenseMe.Core.Services;

public sealed class ConfigManager : IConfigManager
{
    private const string ConfigName = "LicenseMeConfig.json";
    private const string DatabaseName = "LicenseDatabase.db";
    private const string Licenseme = "LicenseMe";
    private const string LogsFolderName = "Logs";
    private const string LogFileExtension = ".log";
    public static string BasePath { get; } = BuildConfigPath();
    public static string LogPath { get; } = Path.Combine(BasePath, LogsFolderName);
    public static string CurrentApplicationLogName { get; } = Path.ChangeExtension(string.Join("_", AppDomain.CurrentDomain.FriendlyName.ToLower().Trim(' ') + LogsFolderName.ToLower(), "<date:yyyyMMdd>"), LogFileExtension);
    public static string DatabaseFileFullPath { get; } = Path.Combine(BasePath,  DatabaseName);
    public static string ConfigFileFullPath { get; } = Path.Combine(BasePath, ConfigName);

    public async Task<LicenseMeConfig> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(ConfigFileFullPath))
        {
            var defaultConfig = LicenseMeConfig.Default;
            await SaveAsync(defaultConfig, ct);
            return defaultConfig;
        }

        await using var stream = File.OpenRead(ConfigFileFullPath);
        return await JsonSerializer.DeserializeAsync(
                   stream, LicenseMeConfigJsonContext.Default.LicenseMeConfig, ct)
               ?? LicenseMeConfig.Default;
    }

    public async Task SaveAsync(LicenseMeConfig config, CancellationToken ct = default)
    {
        await using var stream = File.Open(ConfigFileFullPath, FileMode.OpenOrCreate);
        await JsonSerializer.SerializeAsync(
            stream, config, LicenseMeConfigJsonContext.Default.LicenseMeConfig, ct);
    }

    private static string BuildConfigPath(bool appSpecificFolder = false)
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create), Licenseme);
        if(appSpecificFolder)
            basePath = Path.Combine(basePath, AppDomain.CurrentDomain.FriendlyName.ToLower().Trim(' '));
        if(!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);
        return basePath;
    }
}

[JsonSerializable(typeof(LicenseMeConfig))]
[JsonSerializable(typeof(List<string>))]
[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class LicenseMeConfigJsonContext : JsonSerializerContext;
