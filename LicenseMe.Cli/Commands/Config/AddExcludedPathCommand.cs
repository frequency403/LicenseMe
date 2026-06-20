using System.ComponentModel;
using LicenseMe.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LicenseMe.Cli.Commands.Config;

public sealed class AddExcludedPathSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    [Description("Absolute path to exclude from scanning.")]
    public required string Path { get; init; }
}

public sealed class AddExcludedPathCommand(IConfigManager configManager)
    : AsyncCommand<AddExcludedPathSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context, AddExcludedPathSettings settings, CancellationToken cancellationToken)
    {
        var config = await configManager.LoadAsync(cancellationToken);
        if (config.ExcludedPaths.Contains(settings.Path, StringComparer.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(settings.Path)} is already excluded.[/]");
            return 0;
        }

        var updated = new List<string>(config.ExcludedPaths) { settings.Path };
        await configManager.SaveAsync(config with { ExcludedPaths = updated }, cancellationToken);
        AnsiConsole.MarkupLine($"[green]Excluded:[/] {Markup.Escape(settings.Path)}");
        return 0;
    }
}
