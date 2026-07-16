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
                interaction.SetOutput(string.Empty);
                var topLevel = TopLevel.GetTopLevel(this)!;
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions { Title = "Select scan root", AllowMultiple = false });
                if(folders.Count == 0)
                    return;
                if (folders[0].Path.LocalPath is not { } picked) 
                    return;
                
                ViewModel.ScanRoot = picked;
                interaction.SetOutput(picked);
            });
        });
    }
}
