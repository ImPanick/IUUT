using FluentAssertions;
using IUUT.Core.Abstractions;
using IUUT.Core.Editing;
using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.ProspectBlob;
using IUUT.Core.Prospects.World;
using IUUT.Core.Serializers;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

/// <summary>
/// End-to-end (on-disk) tests for "return trapped items → orbital stash": a prospect file + a
/// MetaInventory.json in a temp save folder are loaded, items are moved, and both files are written
/// atomically with backups.
/// </summary>
public sealed class ProspectReturnFileServiceTests : IDisposable
{
    private readonly TempDir _temp = new();
    private readonly CustomFileService _files = new(
        new SafeSaveWriter(new BackupManager(FixedClock.Default), new SystemGuidProvider()),
        new BackupManager(FixedClock.Default));

    private ProspectReturnFileService NewService() =>
        new(_files, new ProspectReturnService(new StashEditService(new SystemGuidProvider())));

    private string WriteProspect(string name, params byte[][] slots)
    {
        var blob = new ProspectBlobModel { Key = "actors" };
        ProspectBlobCodec.SetUncompressed(blob, UeFixtureBuilder.WorldWithSlots(slots));
        var path = Path.Combine(_temp.Path, name);
        File.WriteAllText(path, ProspectFileSerializer.Serialize(new ProspectFileModel { ProspectBlob = blob }));
        return path;
    }

    private void WriteEmptyStash() =>
        File.WriteAllText(
            Path.Combine(_temp.Path, CustomFileService.StashFile),
            MetaInventorySerializer.Serialize(new MetaInventoryModel { InventoryId = "1" }));

    private static byte[] Slot(string row, int stack) =>
        UeFixtureBuilder.InventorySlot(row, (ProspectWorldEditor.StackIndex, stack));

    [Fact]
    public async Task ReturnAsync_MovesItemsToStash_RewritesProspect_AndBacksUpBoth()
    {
        WriteEmptyStash();
        var prospectPath = WriteProspect("Olympus.json", Slot("Wood", 100), Slot("Wood", 100), Slot("Stone", 20));

        var result = await NewService().ReturnAsync(prospectPath, _temp.Path);

        result.Ok.Should().BeTrue();
        result.Moved!.TotalQuantity.Should().Be(220);
        result.StashBackupPath.Should().NotBeNull();
        result.ProspectBackupPath.Should().NotBeNull();

        // Stash received the items (quantity conserved).
        var stash = MetaInventoryParser.Parse(File.ReadAllText(Path.Combine(_temp.Path, CustomFileService.StashFile)));
        stash.Items.Sum(StashEditService.GetStack).Should().Be(220);

        // Prospect was emptied and still passes the game's hash check.
        var prospect = ProspectFileParser.Parse(File.ReadAllText(prospectPath));
        ProspectBlobVerifier.VerifyHash(prospect.ProspectBlob).Should().BeTrue();
        new ProspectWorldEditor(UeBlob.Parse(ProspectBlobCodec.Decompress(prospect.ProspectBlob.BinaryBlob)))
            .FindItemSlots().Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewAsync_ListsTrappedItems_WithoutWriting()
    {
        var prospectPath = WriteProspect("Styx.json", Slot("Fiber", 50), Slot("Fiber", 50));
        var before = File.ReadAllText(prospectPath);

        var preview = await NewService().PreviewAsync(prospectPath);

        preview.Single(i => i.RowName == "Fiber").TotalQuantity.Should().Be(100);
        File.ReadAllText(prospectPath).Should().Be(before, "preview must not modify the file");
    }

    [Fact]
    public async Task ReturnAsync_MissingProspect_FailsGracefully()
    {
        var result = await NewService().ReturnAsync(Path.Combine(_temp.Path, "nope.json"), _temp.Path);

        result.Ok.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    public void Dispose() => _temp.Dispose();
}
