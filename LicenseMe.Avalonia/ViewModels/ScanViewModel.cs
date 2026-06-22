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
    
    [ObservableAsProperty(ReadOnly = true, InitialValue = "false")]
    private bool _canScan = false;
    
    public ObservableCollection<DiscoveredRepository> Repositories { get; } = [];

    public ScanViewModel(IRepositoryScanner scanner, [FromKeyedServices("RepositoryReporter")] IProgressReporter<string> progressReporter)
    {
        ProgressReporter = progressReporter as ReactiveProgressReporter<string>;
        _scanner = scanner;
        _canScanHelper = this.WhenAnyValue(x => x.ScanRoot, x => x.IsScanning,
            (root, scanning) => !string.IsNullOrWhiteSpace(root) && !scanning).ToProperty(this, vm => vm.CanScan);
    }

    [ReactiveCommand(CanExecute = nameof(IsScanning))]
    private async Task CancelScanAsync()
    {
        if(_scanCts is not null)
            await _scanCts.CancelAsync();
    }
    
    [ReactiveCommand(CanExecute = nameof(CanScan))]
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
