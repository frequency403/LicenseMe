using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace LicenseMe.Avalonia.ViewModels;

public sealed partial class ScanViewModel : ViewModelBase
{
    public ReactiveProgressReporter<string>? ProgressReporter { get; }
    private readonly IRepositoryScanner _scanner;
    private CancellationTokenSource? _scanCts;

    [Reactive]
    private string _scanRoot = string.Empty;

    [Reactive]
    private bool _isScanning = false;

    [Reactive]
    private DiscoveredRepository? _selectedRepository = null;

    // [ReactiveCommand(CanExecute = ...)] only wires up the CanExecute observable for members whose
    // *declared* type is IObservable<bool> - checked by the generator via a type-name match, not via
    // interface implementation, so a plain bool property (like IsScanning, or what CanScan used to be as
    // an [ObservableAsProperty] bool) never qualifies. The generator doesn't report a diagnostic when the
    // match fails either, it just silently omits the CanExecute wiring - these two fields exist purely to
    // give it something of the right type to bind to.
    private readonly IObservable<bool> _canScan;
    private readonly IObservable<bool> _canCancelScan;

    public ObservableCollection<DiscoveredRepository> Repositories { get; } = [];

    public ScanViewModel(IRepositoryScanner scanner, [FromKeyedServices("RepositoryReporter")] IProgressReporter<string> progressReporter)
    {
        ProgressReporter = progressReporter as ReactiveProgressReporter<string>;
        _scanner = scanner;
        _canScan = this.WhenAnyValue(x => x.ScanRoot, x => x.IsScanning,
            (root, scanning) => !string.IsNullOrWhiteSpace(root) && !scanning);
        _canCancelScan = this.WhenAnyValue(x => x.IsScanning);
    }

    [ReactiveCommand(CanExecute = nameof(_canCancelScan))]
    private async Task CancelScanAsync()
    {
        if(_scanCts is not null)
            await _scanCts.CancelAsync();
    }

    [ReactiveCommand(CanExecute = nameof(_canScan))]
    private async Task ExecuteScanAsync(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _scanCts = cts;

        IsScanning = true;
        Repositories.Clear();
        try
        {
            await foreach (var repo in _scanner.ScanAsync(ScanRoot, cts.Token))
                Repositories.Add(repo);
        }
        finally
        {
            _scanCts = null;
            IsScanning = false;
        }
    }

    [ReactiveCommand]
    private async Task ExecuteBrowseAsync(CancellationToken ct)
    {
        // Folder picker is invoked from the View via interaction to keep the ViewModel testable.
        // The View subscribes to FolderPickerInteraction and sets ScanRoot.
        await FolderPickerInteraction.Handle(Unit.Default);
    }

    public Interaction<Unit, string?> FolderPickerInteraction { get; } = new();
}
