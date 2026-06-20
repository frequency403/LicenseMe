using System.ComponentModel;
using LicenseMe.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LicenseMe.Cli.Commands.Config;

public sealed class SetMaxDepthSettings : CommandSettings
{
    [CommandArgument(0, "<depth>")]
    [Description("Maximum directory depth for scanning. Pass 0 to remove the limit.")]
    public required int Depth { get; init; }
}

public sealed class SetMaxDepthCommand(IConfigManager configManager)
    : AsyncCommand<SetMaxDepthSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context, SetMaxDepthSettings settings, CancellationToken cancellationToken)
    {
        var config = await configManager.LoadAsync(cancellationToken);
        var newDepth = settings.Depth <= 0 ? (int?)null : settings.Depth;
        await configManager.SaveAsync(config with { MaxScanDepth = newDepth }, cancellationToken);

        var msg = newDepth.HasValue
            ? $"[green]Max scan depth set to[/] [bold]{newDepth}[/]."
            : "[green]Scan depth limit removed.[/]";
        AnsiConsole.MarkupLine(msg);
        return 0;
    }
}
