using Avalonia.Controls;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace LicenseMe.Avalonia;

public sealed class ViewRegistration : ReactiveObject
{
    private readonly IServiceProvider _services;
    public bool IsShown { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    
    public bool IsDefault { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string? Description { get; }
    public Type ViewType { get; }
    public Type ViewModelType { get; }
    public ServiceLifetime Lifetime { get; }
    public MaterialIconKind IconKind { get; }

    // Every call respects the registered lifetime automatically.
    // Singleton → same instance, Transient → new instance, Scoped → scope-dependent.
    public Control Instance
    {
        get
        {
            var view = (Control)_services.GetRequiredService(ViewType);
            view.DataContext = _services.GetRequiredService(ViewModelType);
            return view;
        }
    }

    internal ViewRegistration(
        Type viewType,
        Type viewModelType,
        IServiceProvider services,
        ServiceLifetime lifetime,
        MaterialIconKind iconKind,
        string? displayName,
        string? description,
        bool isDefault)
    {
        ViewType = viewType;
        ViewModelType = viewModelType;
        Lifetime = lifetime;
        Name = viewType.Name;
        DisplayName = displayName ?? viewType.Name;
        Description = description;
        IconKind = iconKind;
        _services = services;
        IsDefault = isDefault;
    }
}