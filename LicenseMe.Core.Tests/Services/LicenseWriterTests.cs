using LicenseMe.Core.Services;
using NSubstitute;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;
using Shouldly;
using Xunit;

namespace LicenseMe.Core.Tests.Services;

public sealed class LicenseWriterTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly IOsiClient _osiClientMock = Substitute.For<IOsiClient>();
    private readonly LicenseWriter _sut;

    public LicenseWriterTests()
    {
        Directory.CreateDirectory(_tempDir);
        _sut = new LicenseWriter(_osiClientMock);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task WriteAsync_ValidSpdxId_CreatesLicenseFile()
    {
        var license = new OsiLicense { SpdxId = "MIT" };
        _osiClientMock.GetBySpdxIdAsync("MIT")
            .Returns(Task.FromResult<IEnumerable<OsiLicense?>>([license]));

        await _sut.WriteAsync(_tempDir, "MIT");

        var licensePath = Path.Combine(_tempDir, "LICENSE");
        File.Exists(licensePath).ShouldBeTrue();
    }

    [Fact]
    public async Task WriteAsync_UnknownSpdxId_ThrowsInvalidOperationException()
    {
        _osiClientMock.GetBySpdxIdAsync(Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<OsiLicense?>>([null]));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.WriteAsync(_tempDir, "UNKNOWN-1.0"));
    }
}
