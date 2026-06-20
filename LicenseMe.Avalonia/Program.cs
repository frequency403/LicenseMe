using Avalonia;
using Avalonia.ReactiveUI;
using LicenseMe.Avalonia.ViewModels;
using LicenseMe.Core.Extensions;
using LicenseMe.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                svc.AddSingleton<MainWindowViewModel>();
                svc.AddTransient<ScanViewModel>();
                svc.AddTransient<LicensePickerViewModel>();
                svc.AddTransient<SettingsViewModel>();
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
