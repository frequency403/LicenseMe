using LicenseMe.Cli.Commands;
using NSubstitute;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;
using Shouldly;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;
using Xunit;

namespace LicenseMe.Cli.Tests.Commands;

public sealed class ListLicensesCommandTests
{
    private readonly IOsiClient _osiMock = Substitute.For<IOsiClient>();

    [Fact]
    public async Task ExecuteAsync_ReturnsZero()
    {
        _osiMock.GetAllLicensesAsyncEnumerable(TestContext.Current.CancellationToken).Returns(ToAsyncEnumerable(
                new OsiLicense { SpdxId = "MIT", Name = "MIT License", Approved = true }));

        var fakeRegistrar = new FakeTypeRegistrar();
        var testConsole = new TestConsole();
        
        fakeRegistrar.RegisterInstance(typeof(ListLicensesCommand), new ListLicensesCommand(_osiMock, testConsole));
        var commandTesterApp = new CommandAppTester(fakeRegistrar);
        commandTesterApp.SetDefaultCommand<ListLicensesCommand>();

        var result = await commandTesterApp.RunAsync(cancellationToken: TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
    }

    private static async IAsyncEnumerable<OsiLicense?> ToAsyncEnumerable(
        params OsiLicense[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
