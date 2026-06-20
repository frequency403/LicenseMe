using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LicenseMe.Avalonia.ViewModels;
using LicenseMe.Avalonia.Views;
using Microsoft.Extensions.Logging;

namespace LicenseMe.Avalonia;

public sealed class App(ILogger<App> logger, MainWindowViewModel mainWindowViewModel) : Application
{

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        logger.LogInformation("Application started");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            logger.LogInformation("Creating main window");
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
            logger.LogInformation("Main window created");
        }
    }
}
