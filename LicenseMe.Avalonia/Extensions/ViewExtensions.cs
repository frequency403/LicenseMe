using Avalonia.Controls;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace LicenseMe.Avalonia.Extensions;

public static class ViewExtensions
{
    private sealed class DefaultViewMarker;

    public static IServiceCollection RegisterViewWithViewModel<TView, TViewModel>(
        this IServiceCollection services,
        string? displayName = null,
        string? description = null,
        bool isDefault = false,
        MaterialIconKind iconKind = MaterialIconKind.QuestionMarkCircleOutline,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TView : Control
        where TViewModel : class
    {
        if (isDefault && services.Any(d => d.ServiceType == typeof(DefaultViewMarker)))
            throw new InvalidOperationException("Only one view can be marked as default.");

        if (isDefault)
            services.AddSingleton<DefaultViewMarker>();

        services.Add(new ServiceDescriptor(typeof(TView), typeof(TView), lifetime));
        services.Add(new ServiceDescriptor(typeof(TViewModel), typeof(TViewModel), lifetime));

        services.AddSingleton<ViewRegistration>(sp => new ViewRegistration(
            viewType: typeof(TView),
            viewModelType: typeof(TViewModel),
            services: sp,
            lifetime: lifetime,
            iconKind: iconKind,
            displayName: displayName,
            description: description,
            isDefault: isDefault));

        return services;
    }

    public static ViewRegistration ResolveView<TView>(this IServiceProvider sp)
        where TView : Control
        => sp.ResolveView(typeof(TView).Name);

    public static ViewRegistration ResolveView(this IServiceProvider sp, string viewName)
        => sp.GetServices<ViewRegistration>()
               .FirstOrDefault(r => r.Name == viewName)
           ?? throw new KeyNotFoundException($"No view registered under '{viewName}'.");
}