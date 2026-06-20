using Avalonia.ReactiveUI;
using LicenseMe.Avalonia.ViewModels;

namespace LicenseMe.Avalonia.Views;

public sealed partial class SettingsView : ReactiveUserControl<SettingsViewModel>
{
    public SettingsView() => InitializeComponent();
}
