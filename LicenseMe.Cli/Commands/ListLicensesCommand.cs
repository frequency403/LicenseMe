using OpenSourceInitiative.LicenseApi.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LicenseMe.Cli.Commands;

public sealed class ListLicensesCommand(IOsiClient osiClient, IAnsiConsole ansiConsole) : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("SPDX ID").LeftAligned())
            .AddColumn(new TableColumn("Name").LeftAligned())
            .AddColumn(new TableColumn("OSI Approved").Centered());

        await foreach (var license in osiClient.GetAllLicensesAsyncEnumerable(cancellationToken))
        {
            if (license is null) continue;
            table.AddRow(
                Markup.Escape(license.SpdxId ?? string.Empty),
                Markup.Escape(license.Name ?? string.Empty),
                license.Approved ? "[green]✓[/]" : "[grey]-[/]");
        }

        ansiConsole.Write(table);
        return 0;
    }
}
