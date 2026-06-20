using LicenseMe.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LicenseMe.Cli.Commands.Config;

public sealed class ToggleCachingCommand(IConfigManager configManager) : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var config = await configManager.LoadAsync(cancellationToken);
        var next = !config.CachingEnabled;
        await configManager.SaveAsync(config with { CachingEnabled = next }, cancellationToken);
        AnsiConsole.MarkupLine(next
            ? "[green]License caching enabled.[/]"
            : "[yellow]License caching disabled.[/]");
        return 0;
    }
}
