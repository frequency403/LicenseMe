using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Avalonia.Input.Platform;
using Avalonia.ReactiveUI;
using LicenseMe.Cache.Context;
using LicenseMe.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
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
        IDbContextFactory<LicenseDbContext> dbContextFactory,
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
            .CombineLatest(keywordChanged, (search, _) => search)
            .Throttle(TimeSpan.FromMilliseconds(150))
            .Select(search => Observable.FromAsync(ct => QueryLicensesAsync(dbContextFactory, search, KeywordFilters, ct)))
            .Switch()
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
    
    private async Task<IEnumerable<OsiLicense>> QueryLicensesAsync(
        IDbContextFactory<LicenseDbContext> dbContextFactory,
        string? search,
        IReadOnlyList<KeywordFilter> filters,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            IQueryable<OsiLicense> query = dbContext.Licenses;
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l =>
                    EF.Functions.Like(l.SpdxId, $"%{search}%") ||
                    EF.Functions.Like(l.Id, $"%{search}%"));

            var licenses = await query.ToListAsync(cancellationToken);

            var active = filters.Where(f => f.IsSelected).Select(f => f.Keyword).ToHashSet();
            return active.Count == 0
                ? licenses
                : licenses.Where(l => l.Keywords.Any(active.Contains)).ToList();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning(e, "Failed to query licenses for search {Search}", search);
            return [];
        }
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