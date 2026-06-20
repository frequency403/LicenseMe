using System.Reactive;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using ReactiveUI;

namespace LicenseMe.Avalonia.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IConfigManager _configManager;
    private bool _cachingEnabled = true;
    private string? _defaultSpdxId;
    private int? _maxScanDepth;
    private string _newExcludedPath = string.Empty;
    private List<string> _excludedPaths = [];

    public bool CachingEnabled
    {
        get => _cachingEnabled;
        set => this.RaiseAndSetIfChanged(ref _cachingEnabled, value);
    }

    public string? DefaultSpdxId
    {
        get => _defaultSpdxId;
        set => this.RaiseAndSetIfChanged(ref _defaultSpdxId, value);
    }

    public int? MaxScanDepth
    {
        get => _maxScanDepth;
        set => this.RaiseAndSetIfChanged(ref _maxScanDepth, value);
    }

    public string NewExcludedPath
    {
        get => _newExcludedPath;
        set => this.RaiseAndSetIfChanged(ref _newExcludedPath, value);
    }

    public List<string> ExcludedPaths
    {
        get => _excludedPaths;
        set => this.RaiseAndSetIfChanged(ref _excludedPaths, value);
    }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> AddExcludedPathCommand { get; }
    public ReactiveCommand<string, Unit> RemoveExcludedPathCommand { get; }

    public SettingsViewModel(IConfigManager configManager)
    {
        this._configManager = configManager;

        SaveCommand = ReactiveCommand.CreateFromTask(ExecuteSaveAsync);
        AddExcludedPathCommand = ReactiveCommand.CreateFromTask(ExecuteAddExcludedAsync);
        RemoveExcludedPathCommand = ReactiveCommand.Create<string>(ExecuteRemoveExcluded);

        // Load on construction; errors surface via ThrownExceptions
        LoadCommand = ReactiveCommand.CreateFromTask(ExecuteLoadAsync);
        LoadCommand.Execute().Subscribe();
    }

    public ReactiveCommand<Unit, Unit> LoadCommand { get; }

    private async Task ExecuteLoadAsync(CancellationToken ct)
    {
        var config = await _configManager.LoadAsync(ct);
        CachingEnabled = config.CachingEnabled;
        DefaultSpdxId = config.DefaultLicenseSpdxId;
        MaxScanDepth = config.MaxScanDepth;
        ExcludedPaths = [.. config.ExcludedPaths];
    }

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

    private async Task ExecuteAddExcludedAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(NewExcludedPath)) return;
        ExcludedPaths = [.. ExcludedPaths, NewExcludedPath.Trim()];
        NewExcludedPath = string.Empty;
        await ExecuteSaveAsync(ct);
    }

    private void ExecuteRemoveExcluded(string path)
    {
        ExcludedPaths = ExcludedPaths.Where(p => p != path).ToList();
        ExecuteSaveAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
}
