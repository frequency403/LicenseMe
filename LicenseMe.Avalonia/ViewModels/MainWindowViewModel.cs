using System.Collections.ObjectModel;
using System.Reactive.Disposables.Fluent;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using LicenseMe.Cache.Context;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Timer = System.Timers.Timer;

namespace LicenseMe.Avalonia.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    [Reactive] private bool _isExpanded;
    [Reactive] private double _openPaneLength;
    [Reactive] private double _compactPaneLength;

    [ObservableAsProperty(ReadOnly = true)]
    private string _title = string.Empty;

    private const double IconWidth = 36;
    private const double ItemPadding = 32;
    private const double ColumnSpacing = 10;
    private const double FontSize = 14;

    private readonly ILogger<MainWindowViewModel> _logger;

    public ObservableCollection<ViewRegistration> Views { get; } = new();

    public ViewRegistration? CurrentPage
    {
        get;
        set
        {
            field?.IsShown = false;
            value?.IsShown = true;
            this.RaiseAndSetIfChanged(ref field, value);
        }
    }

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, IEnumerable<ViewRegistration> viewRegistrations)
    {
        _logger = logger;
        foreach (var viewRegistration in viewRegistrations)
        {
            logger.LogDebug("Adding view registration: {ViewName}", viewRegistration.Name);
            Views.Add(viewRegistration);
            if (viewRegistration.IsDefault)
                CurrentPage = viewRegistration;
        }

        CalculatePaneLengths();
        _titleHelper = this
            .WhenAnyValue(x => x.CurrentPage,
                (currentPage) => string.Join(" ", AppDomain.CurrentDomain.FriendlyName,
                    currentPage?.DisplayName ?? string.Empty)).ToProperty(this, vm => vm.Title);
    }

    private void CalculatePaneLengths()
    {
        if (Views.Count == 0)
        {
            CompactPaneLength = 56;
            OpenPaneLength = 220;
            return;
        }

        var typeface = new Typeface(FontFamily.Default);

        var maxDisplayNameWidth = Views.Max(v =>
            new TextLayout(v.DisplayName, typeface, FontSize, Brushes.Black, TextAlignment.Left)
                .Width);

        var maxDescriptionWidth = Views.Max(v =>
            new TextLayout(v.Description, typeface, FontSize, Brushes.Black, TextAlignment.Left)
                .Width);

        CompactPaneLength = Math.Max(IconWidth, maxDisplayNameWidth) + ItemPadding + 5;
        OpenPaneLength = CompactPaneLength + ColumnSpacing + maxDescriptionWidth + ItemPadding;
    }

    [ReactiveCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    [ReactiveCommand]
    private void SwitchView(ViewRegistration viewRegistration)
    {
        _logger.LogDebug("Switching to view: {ViewName}", viewRegistration.Name);
        CurrentPage = viewRegistration;
    }
}