using System.ComponentModel;
using LicenseMe.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LicenseMe.Cli.Commands.Config;

public sealed class RemoveExcludedPathSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    [Description("Absolute path to remove from the exclusion list.")]
    public required string Path { get; init; }
}

public sealed class RemoveExcludedPathCommand(IConfigManager configManager)
    : AsyncCommand<RemoveExcludedPathSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context, RemoveExcludedPathSettings settings, CancellationToken cancellationToken)
    {
        var config = await configManager.LoadAsync(cancellationToken);
        var updated = config.ExcludedPaths
            .Where(p => !p.Equals(settings.Path, StringComparison.OrdinalIgnoreCase))
            .ToList();

        await configManager.SaveAsync(config with { ExcludedPaths = updated }, cancellationToken);
        AnsiConsole.MarkupLine($"[green]Removed exclusion:[/] {Markup.Escape(settings.Path)}");
        return 0;
    }
}
