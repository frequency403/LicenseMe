using Avalonia;
using Avalonia.ReactiveUI;
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

namespace LicenseMe.Avalonia;

public class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        using var mainCancellationTokenSource = new CancellationTokenSource();
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
                cfg.AddJsonFile(ConfigManager.ConfigPath, optional: true))
            .ConfigureServices((ctx, svc) =>
            {
                svc.AddLicenseMeCore(ctx.Configuration);
                svc.AddSingleton<App>();
                svc.AddKeyedSingleton<IProgressReporter<string>, ReactiveProgressReporter<string>>("RepositoryReporter");
                svc.AddKeyedSingleton<IProgressReporter<string>, ReactiveProgressReporter<string>>("LicenseReporter");
                svc.AddSingleton<MainWindowViewModel>();

                svc.RegisterViewWithViewModel<ScanView, ScanViewModel>(displayName: "Scan",
                    description: "Scan for licenses", iconKind: MaterialIconKind.Scan, isDefault: true, lifetime: ServiceLifetime.Singleton);
                svc.RegisterViewWithViewModel<SettingsView, SettingsViewModel>(displayName: "Settings",
                    description: "Application settings", iconKind: MaterialIconKind.Settings);
                svc.RegisterViewWithViewModel<LicensesView, LicensesViewModel>(displayName: "Licenses",
                    description: "View licenses", iconKind: MaterialIconKind.License, lifetime: ServiceLifetime.Singleton);
            })
            .ConfigureLogging((_, builder) =>
            {
                builder.AddSimpleConsole(opt =>
                {
                    opt.ColorBehavior = LoggerColorBehavior.Enabled;
                    opt.IncludeScopes = true;
                    opt.UseUtcTimestamp = false;
                    opt.SingleLine = true;
                    opt.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                });
            })
            .Build();

        var appBuilder = AppBuilder.Configure(() => host.Services.GetRequiredService<App>())
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
        
        await host.StartAsync(mainCancellationTokenSource.Token);
        appBuilder.StartWithClassicDesktopLifetime(args);
        await host.StopAsync(mainCancellationTokenSource.Token);
        
    }
}
