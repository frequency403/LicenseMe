using System.Reactive;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using LicenseMe.Avalonia.ViewModels;
using ReactiveUI;

namespace LicenseMe.Avalonia.Views;

public sealed partial class ScanView : ReactiveUserControl<ScanViewModel>
{
    public ScanView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel!.FolderPickerInteraction.RegisterHandler(async interaction =>
            {
                var topLevel = TopLevel.GetTopLevel(this)!;
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions { Title = "Select scan root", AllowMultiple = false });

                var picked = folders.FirstOrDefault()?.Path.LocalPath;
                if (picked is not null) ViewModel.ScanRoot = picked;

                interaction.SetOutput(picked);
            });
        });
    }
}
