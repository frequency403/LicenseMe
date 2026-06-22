using System.Reactive.Linq;
using LicenseMe.Avalonia.ViewModels;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LicenseMe.Avalonia.Tests.ViewModels;

public sealed class ScanViewModelTests
{
    private readonly IRepositoryScanner _scannerMock = Substitute.For<IRepositoryScanner>();
    private readonly IProgressReporter<string> _progressReporterMock = Substitute.For<IProgressReporter<string>>();

    [Fact]
    public async Task ScanCommand_PopulatesRepositories()
    {
        var expected = new DiscoveredRepository(
            "/home/user/repo", "repo", false, false, null, null);

        _scannerMock.ScanAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(expected));

        var sut = new ScanViewModel(_scannerMock, _progressReporterMock)
        {
            ScanRoot = "/home/user"
        };

        await sut.ExecuteScanCommand.Execute();

        sut.Repositories.ShouldHaveSingleItem();
        sut.Repositories[0].ShouldBe(expected);
    }

    [Fact]
    public void ScanCommand_CannotExecute_WhenScanRootIsEmpty()
    {
        var sut = new ScanViewModel(_scannerMock, _progressReporterMock)
        {
            ScanRoot = string.Empty
        };

        var canExecute = false;
        sut.ExecuteScanCommand.CanExecute.Subscribe(v => canExecute = v);

        canExecute.ShouldBeFalse();
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
