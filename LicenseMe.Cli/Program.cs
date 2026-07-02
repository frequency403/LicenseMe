using LicenseMe.Cli.Commands;
using LicenseMe.Cli.Commands.Config;
using LicenseMe.Cli.Infrastructure;
using LicenseMe.Core.Extensions;
using LicenseMe.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile(ConfigManager.ConfigFileFullPath, optional: true, reloadOnChange: false);

builder.AddLicenseMeCore();

var registrar = new TypeRegistrar(builder.Services);
var app = new CommandApp(registrar);

app.Configure(cfg =>
{
    cfg.SetApplicationName("license-me");
    cfg.UseAssemblyInformationalVersion();

    cfg.AddCommand<ScanCommand>("scan")
       .WithDescription("Scan a directory tree for git repositories and report license/readme status.");

    cfg.AddCommand<FixCommand>("fix")
       .WithDescription("Interactively add missing LICENSE and README files to repositories.");

    cfg.AddCommand<ListLicensesCommand>("list-licenses")
       .WithDescription("List all OSI-approved licenses with their SPDX identifiers.");

    cfg.AddBranch("config", branch =>
    {
        branch.SetDescription("Manage LicenseMe configuration.");
        branch.AddCommand<SetDefaultLicenseCommand>("set-default-license");
        branch.AddCommand<SetMaxDepthCommand>("set-max-depth");
        branch.AddCommand<AddExcludedPathCommand>("add-excluded-path");
        branch.AddCommand<RemoveExcludedPathCommand>("remove-excluded-path");
        branch.AddCommand<ToggleCachingCommand>("toggle-caching");
    });
});

return await app.RunAsync(args);
