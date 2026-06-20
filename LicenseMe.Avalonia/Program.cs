using Avalonia;
using Avalonia.ReactiveUI;
using LicenseMe.Avalonia;

return AppBuilder
    .Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .UseReactiveUI()
    .StartWithClassicDesktopLifetime(args);
