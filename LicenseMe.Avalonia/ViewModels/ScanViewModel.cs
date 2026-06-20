using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using ReactiveUI;

namespace LicenseMe.Avalonia.ViewModels;

public sealed class ScanViewModel : ViewModelBase
{
    private readonly IRepositoryScanner _scanner;

    public string ScanRoot
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public bool IsScanning
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public DiscoveredRepository? SelectedRepository
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ObservableCollection<DiscoveredRepository> Repositories { get; } = [];

    public ReactiveCommand<Unit, Unit> ScanCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseCommand { get; }

    public ScanViewModel(IRepositoryScanner scanner)
    {
        this._scanner = scanner;

        var canScan = this.WhenAnyValue(x => x.ScanRoot, x => x.IsScanning,
            (root, scanning) => !string.IsNullOrWhiteSpace(root) && !scanning);

        ScanCommand = ReactiveCommand.CreateFromTask(ExecuteScanAsync, canScan);
        BrowseCommand = ReactiveCommand.CreateFromTask(ExecuteBrowseAsync);
    }

    private async Task ExecuteScanAsync(CancellationToken ct)
    {
        IsScanning = true;
        Repositories.Clear();
        try
        {
            await foreach (var repo in _scanner.ScanAsync(ScanRoot, ct))
                Repositories.Add(repo);
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task ExecuteBrowseAsync(CancellationToken ct)
    {
        // Folder picker is invoked from the View via interaction to keep the ViewModel testable.
        // The View subscribes to FolderPickerInteraction and sets ScanRoot.
        await FolderPickerInteraction.Handle(Unit.Default);
    }

    public Interaction<Unit, string?> FolderPickerInteraction { get; } = new();
}
