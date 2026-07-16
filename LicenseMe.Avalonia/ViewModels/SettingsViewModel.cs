using System.Reactive;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace LicenseMe.Avalonia.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IConfigManager _configManager;

    [Reactive] private bool _cachingEnabled = true;
    [Reactive] private string? _defaultSpdxId;
    [Reactive] private int? _maxScanDepth;
    [Reactive] private string _newExcludedPath = string.Empty;
    [Reactive] private List<string> _excludedPaths = [];

    public SettingsViewModel(IConfigManager configManager)
    {
        this._configManager = configManager;
        // Load on construction; errors surface via ThrownExceptions
        ExecuteLoadCommand.Execute().Subscribe();
    }


    [ReactiveCommand]
    private async Task ExecuteLoadAsync(CancellationToken ct)
    {
        var config = await _configManager.LoadAsync(ct);
        CachingEnabled = config.CachingEnabled;
        DefaultSpdxId = config.DefaultLicenseSpdxId;
        MaxScanDepth = config.MaxScanDepth;
        ExcludedPaths = [.. config.ExcludedPaths];
    }

    [ReactiveCommand]
    private async Task ExecuteSaveAsync(CancellationToken ct)
    {
        var config = new LicenseMeConfig
        {
            CachingEnabled = CachingEnabled,
            DefaultLicenseSpdxId = DefaultSpdxId,
            MaxScanDepth = MaxScanDepth,
            ExcludedPaths = ExcludedPaths,
        };
        await _configManager.SaveAsync(config, ct);
    }

    [ReactiveCommand]
    private async Task ExecuteAddExcludedAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(NewExcludedPath)) return;
        ExcludedPaths = [.. ExcludedPaths, NewExcludedPath.Trim()];
        NewExcludedPath = string.Empty;
        await ExecuteSaveAsync(ct);
    }

    [ReactiveCommand]
    private void ExecuteRemoveExcluded(string path)
    {
        ExcludedPaths = ExcludedPaths.Where(p => p != path).ToList();
        ExecuteSaveAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}
