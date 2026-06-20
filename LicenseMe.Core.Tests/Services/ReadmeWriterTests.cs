using LicenseMe.Core.Services;
using Shouldly;
using Xunit;

namespace LicenseMe.Core.Tests.Services;

public sealed class ReadmeWriterTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly ReadmeWriter _sut = new();

    public ReadmeWriterTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task WriteAsync_CreatesReadmeWithRepoName()
    {
        await _sut.WriteAsync(_tempDir, "my-project");

        var readmePath = Path.Combine(_tempDir, "README.md");
        File.Exists(readmePath).ShouldBeTrue();
        var content = await File.ReadAllTextAsync(readmePath);
        content.ShouldContain("# my-project");
        content.ShouldContain("[LICENSE](LICENSE)");
    }
}
