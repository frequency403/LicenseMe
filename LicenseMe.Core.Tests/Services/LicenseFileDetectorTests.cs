using LicenseMe.Core.Services;
using Shouldly;
using Xunit;

namespace LicenseMe.Core.Tests.Services;

public sealed class LicenseFileDetectorTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly LicenseFileDetector _sut = new();

    public LicenseFileDetectorTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Theory]
    [InlineData("LICENSE")]
    [InlineData("LICENSE.md")]
    [InlineData("LICENSE.txt")]
    [InlineData("LICENCE")]
    [InlineData("COPYING")]
    [InlineData("COPYING.md")]
    public void TryDetect_KnownVariant_ReturnsTrue(string fileName)
    {
        File.WriteAllText(Path.Combine(_tempDir, fileName), "MIT License");

        var result = _sut.TryDetect(_tempDir, out var path);

        result.ShouldBeTrue();
        path.ShouldNotBeNull();
        Path.GetFileName(path).ShouldBe(fileName);
    }

    [Fact]
    public void TryDetect_NoFilePresent_ReturnsFalse()
    {
        var result = _sut.TryDetect(_tempDir, out var path);

        result.ShouldBeFalse();
        path.ShouldBeNull();
    }

    [Fact]
    public void TryDetect_CaseInsensitiveMatch_ReturnsTrue()
    {
        File.WriteAllText(Path.Combine(_tempDir, "license.md"), "Apache License");

        var result = _sut.TryDetect(_tempDir, out var path);

        result.ShouldBeTrue();
        path.ShouldNotBeNull();
    }
}
