using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LicenseMe.Avalonia.ViewModels;
using LicenseMe.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LicenseMe.Avalonia;

public sealed class App(ILogger<App> logger, IServiceProvider serviceProvider) : Application
{

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        #if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        logger.LogInformation("Application started");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            logger.LogInformation("Creating main window");
            desktop.MainWindow = new MainWindow
            {
                ViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
            logger.LogInformation("Main window created");
        }
    }
}
