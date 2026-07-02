using Avalonia;
using Avalonia.Input.Platform;
using LicenseMe.Avalonia.Extensions;
using LicenseMe.Avalonia.ViewModels;
using LicenseMe.Avalonia.Views;
using LicenseMe.Core.Extensions;
using LicenseMe.Core.Interfaces;
using LicenseMe.Core.Services;
using Material.Icons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ReactiveUI.Avalonia;

namespace LicenseMe.Avalonia;

public class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        using var mainCancellationTokenSource = new CancellationTokenSource();
        var hostBuilder = Host.CreateApplicationBuilder(args);

        hostBuilder.Configuration.AddJsonFile(source =>
        {
            source.ReloadOnChange = true;
            source.Optional = true;
            source.Path = ConfigManager.ConfigFileFullPath;
        });
        
        hostBuilder.AddLicenseMeCore();
        hostBuilder.Services.AddSingleton<App>();
        hostBuilder.Services.AddSingleton<ExceptionHandler>();
        hostBuilder.Services.AddKeyedSingleton<IProgressReporter<string>, ReactiveProgressReporter<string>>("RepositoryReporter");
        hostBuilder.Services.AddKeyedSingleton<IProgressReporter<string>, ReactiveProgressReporter<string>>("LicenseReporter");
        hostBuilder.Services.AddSingleton<MainWindowViewModel>();

        hostBuilder.Services.AddSingleton<MainWindow>();
        hostBuilder.Services.AddTransient<IClipboard>(sp => sp.GetRequiredService<MainWindow>().Clipboard ?? throw new InvalidOperationException("MainWindow not found"));

        hostBuilder.Services.RegisterViewWithViewModel<ScanView, ScanViewModel>(displayName: "Scan",
            description: "Scan for licenses", iconKind: MaterialIconKind.Scan, isDefault: true);
        hostBuilder.Services.RegisterViewWithViewModel<SettingsView, SettingsViewModel>(displayName: "Settings",
            description: "Application settings", iconKind: MaterialIconKind.Settings);
        hostBuilder.Services.RegisterViewWithViewModel<LicensesView, LicensesViewModel>(displayName: "Licenses",
            description: "View licenses", iconKind: MaterialIconKind.License, lifetime: ServiceLifetime.Transient);
        
        hostBuilder.Logging.AddSimpleConsole(opt =>
        {
            opt.ColorBehavior = LoggerColorBehavior.Enabled;
            opt.IncludeScopes = true;
            opt.UseUtcTimestamp = false;
            opt.SingleLine = true;
            opt.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
        });
        
        using var host = hostBuilder.Build();
        var app = host.Services.GetRequiredService<App>();
        var exceptionHandler = host.Services.GetRequiredService<ExceptionHandler>();
        
        var appBuilder = AppBuilder.Configure(() => app)
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI(configure =>
            {
                configure
                    .WithPlatformServices()
                    .WithAvalonia()
                    .WithExceptionHandler(exceptionHandler);
            });
        
        await host.StartAsync(mainCancellationTokenSource.Token);
        appBuilder.StartWithClassicDesktopLifetime(args);
        await host.StopAsync(mainCancellationTokenSource.Token);
        
    }
}
