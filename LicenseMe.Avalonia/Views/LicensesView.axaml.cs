using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using LicenseMe.Avalonia.ViewModels;

namespace LicenseMe.Avalonia.Views;

public partial class LicensesView : ReactiveUserControl<LicensesViewModel>
{
    public LicensesView()
    {
        InitializeComponent();
    }
}