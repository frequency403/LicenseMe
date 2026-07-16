using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LicenseMe.Avalonia.ViewModels;
using LicenseMe.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LicenseMe.Avalonia;

public sealed class App(ILogger<App> logger, IServiceProvider serviceProvider) : Application
{
    public App() : this(NullLogger<App>.Instance, new ServiceCollection().BuildServiceProvider()) { }
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
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.ViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;
            logger.LogInformation("Main window created");
        }
        base.OnFrameworkInitializationCompleted();
    }
}
