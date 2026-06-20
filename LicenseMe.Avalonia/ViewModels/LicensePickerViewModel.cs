using System.Collections.ObjectModel;
using System.Reactive;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;
using ReactiveUI;

namespace LicenseMe.Avalonia.ViewModels;

public sealed class LicensePickerViewModel : ViewModelBase
{
    private readonly IOsiClient _osiClient;
    private string _searchText = string.Empty;
    private OsiLicense? _selectedLicense;
    private bool _isLoading;

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public OsiLicense? SelectedLicense
    {
        get => _selectedLicense;
        set => this.RaiseAndSetIfChanged(ref _selectedLicense, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ObservableCollection<OsiLicense> Licenses { get; } = [];

    // Filtered view is computed in the View via CollectionViewSource or ReactiveList
    public ReactiveCommand<Unit, Unit> LoadCommand { get; }

    public LicensePickerViewModel(IOsiClient osiClient)
    {
        this._osiClient = osiClient;
        LoadCommand = ReactiveCommand.CreateFromTask(ExecuteLoadAsync);
    }

    private async Task ExecuteLoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        Licenses.Clear();
        try
        {
            await foreach (var license in _osiClient.GetAllLicensesAsyncEnumerable())
            {
                ct.ThrowIfCancellationRequested();
                if (license is not null) Licenses.Add(license);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
