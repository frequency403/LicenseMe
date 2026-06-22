using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using LicenseMe.Avalonia.ViewModels;

namespace LicenseMe.Avalonia.Behaviors;

public sealed class PaneLengthMeasureBehavior : Behavior<ListBox>
{
    private bool _measured;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject!.LayoutUpdated += OnLayoutUpdated;
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.LayoutUpdated -= OnLayoutUpdated;
        base.OnDetaching();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_measured) return;
        if (AssociatedObject?.DataContext is not MainWindowViewModel vm) return;

        var containers = AssociatedObject.GetRealizedContainers().ToList();
        if (containers.Count == 0 || containers.Count < vm.Views.Count) return;

        var maxCompactWidth = 0.0;
        var maxDescriptionWidth = 0.0;
        var columnSpacing = 0.0;

        foreach (var container in containers)
        {
            var grid = container.GetVisualDescendants()
                .OfType<Grid>()
                .FirstOrDefault();

            if (grid is null) continue;

            columnSpacing = grid.ColumnSpacing;

            var col0Width = grid.ColumnDefinitions[0].ActualWidth;
            var col1Width = grid.ColumnDefinitions[1].ActualWidth;

            // Layout noch nicht stabil — abbrechen ohne zu schreiben
            if (col0Width == 0 && col1Width == 0) return;

            var itemPadding = container is ListBoxItem lbi
                ? lbi.Padding.Left + lbi.Padding.Right
                : 0.0;

            maxCompactWidth = Math.Max(
                maxCompactWidth,
                col0Width + columnSpacing + col1Width + itemPadding
            );

            var descriptionBlock = grid.GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(tb => Grid.GetColumn(tb) == 2);

            if (descriptionBlock is null) continue;

            var descWidth = descriptionBlock.IsVisible
                ? grid.ColumnDefinitions[2].ActualWidth
                : MeasureText(
                    descriptionBlock.Text,
                    descriptionBlock.FontSize,
                    descriptionBlock.FontFamily
                );

            maxDescriptionWidth = Math.Max(maxDescriptionWidth, descWidth);
        }

        if (maxCompactWidth == 0) return;

        vm.CompactPaneLength = maxCompactWidth;
        vm.OpenPaneLength = maxCompactWidth + columnSpacing + maxDescriptionWidth;
        _measured = true;
    }

    private static double MeasureText(string? text, double fontSize, FontFamily fontFamily)
    {
        if (string.IsNullOrEmpty(text)) return 0.0;

        return new TextLayout(
            text,
            new Typeface(fontFamily),
            fontSize,
            Brushes.Transparent,
            TextAlignment.Left
        ).Width;
    }
}