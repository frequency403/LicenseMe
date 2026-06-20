using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LicenseMe.Avalonia.ViewModels;
using LicenseMe.Avalonia.Views;
using LicenseMe.Core.Extensions;
using LicenseMe.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LicenseMe.Avalonia;

public sealed partial class App : Application
{
    private IHost? _host;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
                cfg.AddJsonFile(ConfigManager.ConfigPath, optional: true))
            .ConfigureServices((ctx, svc) =>
            {
                svc.AddLicenseMeCore(ctx.Configuration);
                svc.AddSingleton<MainWindowViewModel>();
                svc.AddTransient<ScanViewModel>();
                svc.AddTransient<LicensePickerViewModel>();
                svc.AddTransient<SettingsViewModel>();
            })
            .Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _host.Services.GetRequiredService<MainWindowViewModel>()
            };

            desktop.ShutdownRequested += (_, _) =>
                _host.StopAsync().GetAwaiter().GetResult();
        }

        _host.Start();
        base.OnFrameworkInitializationCompleted();
    }
}
