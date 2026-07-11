using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using LicenseMe.Avalonia.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using ReactiveUI.Avalonia;
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace LicenseMe.Avalonia.Tests;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace()
        .UseReactiveUI(configure =>
        {
            configure
                .WithPlatformServices()
                .WithAvalonia()
                .WithExceptionHandler(new ExceptionHandler(NullLogger<ExceptionHandler>.Instance));
        })
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}