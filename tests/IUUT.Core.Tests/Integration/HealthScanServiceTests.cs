using System.Text;
using FluentAssertions;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class HealthScanServiceTests : IDisposable
{
    private readonly TempDir _temp = new();
    private readonly HealthScanService _sut = new();

    private void Write(string relativePath, string content)
    {
        var full = Path.Combine(_temp.Path, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    private static FileHealthStatus StatusOf(HealthReport report, string fileName) =>
        report.Files.First(f => Path.GetFileName(f.RelativePath) == fileName).Status;

    [Fact]
    public void ScanProfile_ReportsPerFileStatus()
    {
        Write("Profile.json", "{}");
        Write("Characters.json", """{ "Characters.json": [] }""");
        Write("MetaInventory.json", "this is not json");
        Write(Path.Combine("Prospects", "Good.json"), ProspectBlobFactory.ProspectJson(new UTF8Encoding(false).GetBytes("good payload")));
        Write(Path.Combine("Prospects", "Bad.json"), ProspectBlobFactory.ProspectJson(new UTF8Encoding(false).GetBytes("payload"), hashOverride: "0000"));
        // Accolades.json / BestiaryData.json / Mounts.json / Loadouts.json intentionally absent.

        var report = _sut.ScanProfile(_temp.Path);

        StatusOf(report, "Profile.json").Should().Be(FileHealthStatus.Ok);
        StatusOf(report, "Characters.json").Should().Be(FileHealthStatus.Ok);
        StatusOf(report, "MetaInventory.json").Should().Be(FileHealthStatus.Unparseable);
        StatusOf(report, "Accolades.json").Should().Be(FileHealthStatus.Missing);
        StatusOf(report, "Good.json").Should().Be(FileHealthStatus.Ok);
        StatusOf(report, "Bad.json").Should().Be(FileHealthStatus.BlobHashMismatch);

        report.IsHealthy.Should().BeFalse();
        report.IssueCount.Should().Be(2, "the corrupt inventory and the mismatched prospect blob");
        report.Issues.Select(i => Path.GetFileName(i.RelativePath))
            .Should().BeEquivalentTo("MetaInventory.json", "Bad.json");
    }

    [Fact]
    public void ScanProfile_AllValid_IsHealthy()
    {
        Write("Profile.json", "{}");
        Write("Characters.json", """{ "Characters.json": [] }""");
        Write("Accolades.json", "{}");
        Write("BestiaryData.json", "{}");
        Write("MetaInventory.json", "{}");
        Write("Mounts.json", "{}");
        Write(Path.Combine("Loadout", "Loadouts.json"), "{}");

        var report = _sut.ScanProfile(_temp.Path);

        report.IsHealthy.Should().BeTrue();
        report.IssueCount.Should().Be(0);
        report.OkCount.Should().Be(7);
    }

    [Fact]
    public void ScanProfile_EmptyFolder_AllMissingButHealthy()
    {
        var report = _sut.ScanProfile(_temp.Path);

        report.Files.Should().OnlyContain(f => f.Status == FileHealthStatus.Missing);
        report.IsHealthy.Should().BeTrue("missing optional files are not corruption");
    }

    public void Dispose() => _temp.Dispose();
}
