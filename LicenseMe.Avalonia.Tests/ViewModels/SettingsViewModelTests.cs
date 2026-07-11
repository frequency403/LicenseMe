using System.Reactive.Linq;
using Avalonia.Headless.XUnit;
using LicenseMe.Avalonia.ViewModels;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LicenseMe.Avalonia.Tests.ViewModels;

public sealed class SettingsViewModelTests
{
    private readonly IConfigManager _configMock = Substitute.For<IConfigManager>();

    [AvaloniaFact]
    public async Task OnLoad_PopulatesPropertiesFromConfig()
    {
        _configMock.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(new LicenseMeConfig
            {
                CachingEnabled = false,
                DefaultLicenseSpdxId = "Apache-2.0",
                MaxScanDepth = 5,
                ExcludedPaths = ["/tmp"]
            });

        var sut = new SettingsViewModel(_configMock);
        await sut.LoadCommand.Execute();

        sut.CachingEnabled.ShouldBeFalse();
        sut.DefaultSpdxId.ShouldBe("Apache-2.0");
        sut.MaxScanDepth.ShouldBe(5);
        sut.ExcludedPaths.ShouldContain("/tmp");
    }
}
