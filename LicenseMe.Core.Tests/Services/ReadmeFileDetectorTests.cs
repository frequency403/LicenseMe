using LicenseMe.Core.Services;
using Shouldly;
using Xunit;

namespace LicenseMe.Core.Tests.Services;

public sealed class ReadmeFileDetectorTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly ReadmeFileDetector _sut = new();

    public ReadmeFileDetectorTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Theory]
    [InlineData("README.md")]
    [InlineData("README")]
    [InlineData("README.txt")]
    [InlineData("README.rst")]
    public void TryDetect_KnownVariant_ReturnsTrue(string fileName)
    {
        File.WriteAllText(Path.Combine(_tempDir, fileName), "# Project");

        var result = _sut.TryDetect(_tempDir, out var path);

        result.ShouldBeTrue();
        path.ShouldNotBeNull();
    }

    [Fact]
    public void TryDetect_NoFilePresent_ReturnsFalse()
    {
        _sut.TryDetect(_tempDir, out var path).ShouldBeFalse();
        path.ShouldBeNull();
    }
}
