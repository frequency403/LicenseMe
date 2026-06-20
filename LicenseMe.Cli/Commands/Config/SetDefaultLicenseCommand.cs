using System.ComponentModel;
using LicenseMe.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LicenseMe.Cli.Commands.Config;

public sealed class SetDefaultLicenseSettings : CommandSettings
{
    [CommandArgument(0, "<spdx-id>")]
    [Description("SPDX identifier of the license to use as default (e.g. MIT, Apache-2.0).")]
    public required string SpdxId { get; init; }
}

public sealed class SetDefaultLicenseCommand(IConfigManager configManager)
    : AsyncCommand<SetDefaultLicenseSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context, SetDefaultLicenseSettings settings, CancellationToken cancellationToken)
    {
        var config = await configManager.LoadAsync(cancellationToken);
        await configManager.SaveAsync(config with { DefaultLicenseSpdxId = settings.SpdxId }, cancellationToken);
        AnsiConsole.MarkupLine($"[green]Default license set to[/] [bold]{settings.SpdxId}[/].");
        return 0;
    }
}
