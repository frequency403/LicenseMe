using ReactiveUI;

namespace LicenseMe.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _currentPage;

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    public ScanViewModel ScanVm { get; }
    public SettingsViewModel SettingsVm { get; }

    public MainWindowViewModel(ScanViewModel scanVm, SettingsViewModel settingsVm)
    {
        ScanVm = scanVm;
        SettingsVm = settingsVm;
        _currentPage = scanVm;
    }
}
