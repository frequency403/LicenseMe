using System.ComponentModel;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.Options;
using OpenSourceInitiative.LicenseApi.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace LicenseMe.Cli.Commands;

public sealed class FixCommandSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    [Description("Root path to scan for repositories that need fixing.")]
    public required string Path { get; init; }

    [CommandOption("--license-only")]
    [Description("Only add missing LICENSE files; skip README.")]
    public bool LicenseOnly { get; init; }

    [CommandOption("--readme-only")]
    [Description("Only add missing README files; skip LICENSE.")]
    public bool ReadmeOnly { get; init; }

    [CommandOption("--spdx-id <id>")]
    [Description("SPDX identifier for the license to apply. Skips the interactive picker.")]
    public string? SpdxId { get; init; }

    [CommandOption("-y|--yes")]
    [Description("Apply to all found repositories without interactive selection.")]
    public bool Yes { get; init; }
}

public sealed class FixCommand(
    IRepositoryScanner scanner,
    ILicenseWriter licenseWriter,
    IReadmeWriter readmeWriter,
    IOsiClient osiClient,
    IAnsiConsole ansiConsole,
    IOptions<LicenseMeConfig> options) : AsyncCommand<FixCommandSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, FixCommandSettings settings, CancellationToken cancellationToken)
    {
        var config = options.Value;
        var candidates = new List<DiscoveredRepository>();

        await ansiConsole.Status().StartAsync("Scanning…", async _ =>
        {
            await foreach (var repo in scanner.ScanAsync(settings.Path, cancellationToken))
            {
                var needsLicense = !settings.ReadmeOnly && !repo.HasLicense;
                var needsReadme  = !settings.LicenseOnly && !repo.HasReadme;
                if (needsLicense || needsReadme)
                    candidates.Add(repo);
            }
        });

        if (candidates.Count == 0)
        {
            ansiConsole.MarkupLine("[green]All repositories are already properly set up.[/]");
            return 0;
        }

        var selected = settings.Yes
            ? candidates
            : (List<DiscoveredRepository>)ansiConsole.Prompt(
                new MultiSelectionPrompt<DiscoveredRepository>()
                    .Title("Select repositories to fix [grey](space to toggle, enter to confirm)[/]:")
                    .UseConverter(r =>
                        $"{r.Name}  [grey]{(r.HasLicense ? "" : "no LICENSE")} {(r.HasReadme ? "" : "no README")}[/]")
                    .AddChoices(candidates));

        if (selected.Count == 0)
        {
            ansiConsole.MarkupLine("[yellow]No repositories selected. Aborted.[/]");
            return 0;
        }

        var spdxId = !settings.ReadmeOnly
            ? settings.SpdxId
              ?? config.DefaultLicenseSpdxId
              ?? await PromptForLicenseAsync()
            : null;

        await ansiConsole.Progress().StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Applying…", maxValue: selected.Count);
            foreach (var repo in selected)
            {
                if (!settings.ReadmeOnly && !repo.HasLicense && spdxId is not null)
                    await licenseWriter.WriteAsync(repo.FullPath, spdxId, cancellationToken);

                if (!settings.LicenseOnly && !repo.HasReadme)
                    await readmeWriter.WriteAsync(repo.FullPath, repo.Name, cancellationToken);

                task.Increment(1);
            }
        });

        ansiConsole.MarkupLine($"[green]Done.[/] Fixed [bold]{selected.Count}[/] repositories.");
        return 0;
    }

    private async Task<string> PromptForLicenseAsync()
    {
        var licenses = new List<OpenSourceInitiative.LicenseApi.Models.OsiLicense>();
        await ansiConsole.Status().StartAsync("Fetching license catalog…", async _ =>
        {
            await foreach (var l in osiClient.GetAllLicensesAsyncEnumerable())
                if (l is not null) licenses.Add(l);
        });

        return ansiConsole.Prompt(
            new SelectionPrompt<OpenSourceInitiative.LicenseApi.Models.OsiLicense>()
                .Title("Select a license:")
                .PageSize(20)
                .UseConverter(l => $"[bold]{l.SpdxId}[/]  {l.Name}")
                .AddChoices(licenses)).SpdxId!;
    }
}
