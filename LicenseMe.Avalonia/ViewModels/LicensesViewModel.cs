using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Avalonia.Input.Platform;
using Avalonia.ReactiveUI;
using LicenseMe.Cache.Context;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace LicenseMe.Avalonia.ViewModels;

public partial class LicensesViewModel : ViewModelBase
{
    [Reactive] private OsiLicense _selectedLicense;
    [Reactive] private string? _searchText;
    [Reactive] private OsiLicenseKeyword? _selectedKeyword;
    [ObservableAsProperty(ReadOnly = true)] private IEnumerable<OsiLicense> _filteredLicenses = [];
    
    private readonly ILogger<LicensesViewModel> _logger;
    private readonly IClipboard _clipboard;

    public LicensesViewModel(ILogger<LicensesViewModel> logger, 
        LicenseDbContext dbContext,
        IClipboard clipboard,
        [FromKeyedServices("LicenseReporter")] 
        IProgressReporter<string> progressReporter)
    {
        _logger = logger;
        ProgressReporter = progressReporter;
        _clipboard = clipboard;
        
        var keywordChanged = KeywordFilters
            .Select(f => f.WhenAnyValue(x => x.IsSelected))
            .Merge()
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default);

        _filteredLicensesHelper = this.WhenAnyValue(x => x.SearchText)
            .CombineLatest(dbContext.WhenAnyValue(x => x.Licenses),
                keywordChanged,
                (search, licenses, _) => ApplyFilter(search, KeywordFilters, licenses))
            .Throttle(TimeSpan.FromMilliseconds(150))
            .ObserveOn(AvaloniaScheduler.Instance)
            .ToProperty(this, x => x.FilteredLicenses).DisposeWith(Disposables);

    }

    public IProgressReporter<string> ProgressReporter { get; }

    public IReadOnlyList<KeywordFilter> KeywordFilters { get; } =
        Enum.GetValues<OsiLicenseKeyword>()
            .Select(k => new KeywordFilter(k))
            .ToList();
    
    
    [ReactiveCommand]
    private async Task CopyLicenseToClipboardAsync(string licenseText)
    {
        try
        {
            await _clipboard.SetTextAsync(licenseText);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to copy license to clipboard");
        }
    }
    
    private static IEnumerable<OsiLicense> ApplyFilter(
        string? search, IReadOnlyList<KeywordFilter> filters, IEnumerable<OsiLicense>? all)
    {
        var result = all ?? [];

        if (!string.IsNullOrWhiteSpace(search))
            result = result.Where(l =>
                (l.SpdxId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (l.Id?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

        var active = filters.Where(f => f.IsSelected).Select(f => f.Keyword).ToHashSet();
        if (active.Count > 0)
            result = result.Where(l => l.Keywords.Any(active.Contains));

        return result;
    }
}

public partial class KeywordFilter(OsiLicenseKeyword keyword) : ReactiveObject
{
    public OsiLicenseKeyword Keyword { get; } = keyword;

    // PascalCase -> spaced display name
    public string DisplayName { get; } =
        Regex.Replace(keyword.ToString(), "(?<=[a-z])(?=[A-Z])", " ");

    [Reactive] private bool _isSelected;
}