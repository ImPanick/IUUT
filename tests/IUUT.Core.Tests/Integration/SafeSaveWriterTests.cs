using System.Text.Json;
using FluentAssertions;
using IUUT.Core.Abstractions;
using IUUT.Core.Io;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class SafeSaveWriterTests : IDisposable
{
    private readonly TempDir _temp = new();
    private readonly SafeSaveWriter _sut;

    public SafeSaveWriterTests()
    {
        var backups = new BackupManager(FixedClock.Default);
        _sut = new SafeSaveWriter(backups, new SystemGuidProvider());
    }

    private static void AlwaysValid(string content) { _ = content; }

    private static void RequireValidJson(string content) => _ = JsonDocument.Parse(content);

    private int TempArtifactCount() =>
        Directory.GetFiles(_temp.Path, "*.iuut-tmp-*").Length;

    [Fact]
    public async Task WriteAsync_OnSuccess_WritesNewContentAndCreatesBackup()
    {
        var file = _temp.File("Profile.json");
        await File.WriteAllTextAsync(file, "{\"old\":true}");

        var result = await _sut.WriteAsync(file, "{\"new\":true}", RequireValidJson);

        result.Ok.Should().BeTrue();
        (await File.ReadAllTextAsync(file)).Should().Be("{\"new\":true}");
        result.BackupPath.Should().NotBeNull();
        File.Exists(result.BackupPath!).Should().BeTrue();
        (await File.ReadAllTextAsync(result.BackupPath!)).Should().Be("{\"old\":true}");
    }

    [Fact]
    public async Task WriteAsync_WritesUtf8WithoutBom()
    {
        var file = _temp.File("Profile.json");

        await _sut.WriteAsync(file, "{\"x\":1}", AlwaysValid);

        var bytes = await File.ReadAllBytesAsync(file);
        bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF },
            "save files must be UTF-8 WITHOUT a BOM (Icarus-Analysis §10)");
    }

    [Fact]
    public async Task WriteAsync_OnValidationFailure_LeavesOriginalIntact()
    {
        var file = _temp.File("Profile.json");
        await File.WriteAllTextAsync(file, "{\"good\":true}");

        var result = await _sut.WriteAsync(file, "this is not json", RequireValidJson);

        result.Ok.Should().BeFalse();
        result.Error.Should().BeAssignableTo<JsonException>();
        (await File.ReadAllTextAsync(file)).Should().Be("{\"good\":true}",
            "the live file is validated as a temp copy and only renamed in on success, so a "
            + "failed write never replaces the original (CONSTITUTION III; CODE_STYLE §10)");
    }

    [Fact]
    public async Task WriteAsync_NewFile_OnValidationFailure_DoesNotCreateFile()
    {
        var file = _temp.File("BrandNew.json");

        var result = await _sut.WriteAsync(file, "not json", RequireValidJson);

        result.Ok.Should().BeFalse();
        File.Exists(file).Should().BeFalse("a new file that fails validation must not be created");
    }

    [Fact]
    public async Task WriteAsync_OnSuccess_LeavesNoTempArtifact()
    {
        var file = _temp.File("Profile.json");

        await _sut.WriteAsync(file, "{\"x\":1}", RequireValidJson);

        TempArtifactCount().Should().Be(0, "the temp file is renamed onto the target on success");
    }

    [Fact]
    public async Task WriteAsync_OnFailure_LeavesNoTempArtifact()
    {
        var file = _temp.File("Profile.json");
        await File.WriteAllTextAsync(file, "{\"good\":true}");

        await _sut.WriteAsync(file, "not json", RequireValidJson);

        TempArtifactCount().Should().Be(0, "the temp file is deleted when a write fails");
    }

    public void Dispose() => _temp.Dispose();
}
