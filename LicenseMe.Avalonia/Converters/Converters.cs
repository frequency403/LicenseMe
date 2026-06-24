using Avalonia.Data.Converters;

namespace LicenseMe.Avalonia.Converters;

public static class Converters
{
    public static FuncValueConverter<string, int> StringToIntConverter { get; } =
        new(s => string.IsNullOrEmpty(s) ? 2 : 1);
    
    public static FuncValueConverter<string, int> StringToColumnConverter { get; } =
        new(s => string.IsNullOrEmpty(s) ? 0 : 1);
    
    public static FuncMultiValueConverter<string?, string> StringCoalescer { get; } =
        new((values) =>
        {
            foreach (var value in values)
            {
                if(!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return string.Empty;
        });
}