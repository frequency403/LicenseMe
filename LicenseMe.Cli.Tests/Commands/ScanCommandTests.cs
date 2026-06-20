using LicenseMe.Cli.Commands;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using NSubstitute;
using Shouldly;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
using Xunit;

namespace LicenseMe.Cli.Tests.Commands;

public sealed class ScanCommandTests
{
    private readonly IRepositoryScanner _scannerMock = Substitute.For<IRepositoryScanner>();

    [Fact]
    public async Task ExecuteAsync_WithRepositories_ReturnsZero()
    {
        _scannerMock.ScanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(
                new DiscoveredRepository("/home/user/repo", "repo", true, true, null, null)));

        var fakeRegistrar = new FakeTypeRegistrar();
        var testConsole = new TestConsole();
        fakeRegistrar.RegisterInstance(typeof(ScanCommand), new ScanCommand(_scannerMock, testConsole));
        var app = new CommandAppTester(fakeRegistrar);
        app.Configure(cfg =>
        {
            cfg.AddCommand<ScanCommand>("scan");
        });
        app.SetDefaultCommand<ScanCommand>();
        
        var result = await app.RunAsync(["scan /home/user"], TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        testConsole.Output.ShouldContain("/home/user/repo");
    }

    private static async IAsyncEnumerable<DiscoveredRepository> ToAsyncEnumerable(
        params DiscoveredRepository[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
