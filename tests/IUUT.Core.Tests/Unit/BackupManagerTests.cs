using FluentAssertions;
using IUUT.Core.Io;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public sealed class BackupManagerTests : IDisposable
{
    private readonly TempDir _temp = new();

    [Fact]
    public void CreateBackup_UsesIuutBackupTimestampNaming()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 5, 12, 12, 44, 0, TimeSpan.Zero));
        var sut = new BackupManager(clock);
        var file = _temp.File("Profile.json");
        File.WriteAllText(file, "{}");

        var backup = sut.CreateBackup(file);

        Path.GetFileName(backup).Should().Be("Profile.json.iuut-backup-20260512-124400");
        File.Exists(backup).Should().BeTrue();
        File.ReadAllText(backup).Should().Be("{}");
        File.Exists(file).Should().BeTrue("the original must be left in place");
    }

    [Fact]
    public void CreateBackup_CalledTwiceSameSecond_DisambiguatesWithSuffix()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 5, 12, 12, 44, 0, TimeSpan.Zero));
        var sut = new BackupManager(clock);
        var file = _temp.File("Profile.json");
        File.WriteAllText(file, "{}");

        var first = sut.CreateBackup(file);
        var second = sut.CreateBackup(file);

        Path.GetFileName(first).Should().Be("Profile.json.iuut-backup-20260512-124400");
        Path.GetFileName(second).Should().Be("Profile.json.iuut-backup-20260512-124400-2");
        first.Should().NotBe(second);
    }

    [Fact]
    public void CreateBackup_MissingFile_Throws()
    {
        var sut = new BackupManager(FixedClock.Default);

        var act = () => sut.CreateBackup(_temp.File("does-not-exist.json"));

        act.Should().Throw<FileNotFoundException>();
    }

    public void Dispose() => _temp.Dispose();
}
