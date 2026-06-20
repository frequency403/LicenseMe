using System.ComponentModel;
using LicenseMe.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LicenseMe.Cli.Commands;

public sealed class ScanCommandSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    [Description("Root path to scan for git repositories.")]
    public required string Path { get; init; }

    [CommandOption("--max-depth <depth>")]
    [Description("Maximum directory depth. Overrides config value if specified.")]
    public int? MaxDepth { get; init; }
}

public sealed class ScanCommand(IRepositoryScanner scanner, IAnsiConsole ansiConsole) : AsyncCommand<ScanCommandSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ScanCommandSettings settings, CancellationToken cancellationToken)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("Repository").LeftAligned())
            .AddColumn(new TableColumn("License").Centered())
            .AddColumn(new TableColumn("README").Centered())
            .AddColumn(new TableColumn("Path").LeftAligned());

        var found = 0;
        await ansiConsole.Progress()
            .AutoClear(true)
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Scanning...[/]", autoStart: true);
                task.IsIndeterminate = true;

                await foreach (var repo in scanner.ScanAsync(settings.Path, cancellationToken))
                {
                    table.AddRow(
                        Markup.Escape(repo.Name),
                        repo.HasLicense ? "[green]✓[/]" : "[red]✗[/]",
                        repo.HasReadme  ? "[green]✓[/]" : "[red]✗[/]",
                        Markup.Escape(repo.FullPath));
                    found++;
                }

                task.StopTask();
            });

        ansiConsole.Write(table);
        ansiConsole.MarkupLine($"[grey]Found [bold]{found}[/] repositories.[/]");
        return 0;
    }
}
