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
        new(_files, new ProspectReturnService(
            new StashEditService(new SystemGuidProvider()),
            row => row.StartsWith("Meta_", StringComparison.Ordinal)));

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
    public async Task ReturnAsync_MovesWorkshopItemsToStash_LeavesTheRest_AndBacksUpBoth()
    {
        WriteEmptyStash();
        var prospectPath = WriteProspect("Olympus.json", Slot("Meta_Arrow", 12), Slot("Meta_Bolt", 8), Slot("Wood", 100));

        var result = await NewService().ReturnAsync(prospectPath, _temp.Path);

        result.Ok.Should().BeTrue();
        result.Moved!.TotalQuantity.Should().Be(20);
        result.Moved.LeftBehind.Should().ContainSingle(i => i.RowName == "Wood");
        result.StashBackupPath.Should().NotBeNull();
        result.ProspectBackupPath.Should().NotBeNull();

        // Only workshop rows reached the stash; quantity conserved.
        var stash = MetaInventoryParser.Parse(File.ReadAllText(Path.Combine(_temp.Path, CustomFileService.StashFile)));
        stash.Items.Sum(StashEditService.GetStack).Should().Be(20);
        stash.Items.Should().OnlyContain(i => i.ItemStaticData!.RowName.StartsWith("Meta_", StringComparison.Ordinal));

        // The Wood is still in the world, and the world still passes the game's hash check.
        var prospect = ProspectFileParser.Parse(File.ReadAllText(prospectPath));
        ProspectBlobVerifier.VerifyHash(prospect.ProspectBlob).Should().BeTrue();
        new ProspectWorldEditor(UeBlob.Parse(ProspectBlobCodec.Decompress(prospect.ProspectBlob.BinaryBlob)))
            .FindItemSlots().Should().ContainSingle(s => s.RowName == "Wood");
    }

    [Fact]
    public async Task ReturnAsync_WithNothingStashable_WritesNeitherFile()
    {
        WriteEmptyStash();
        var stashPath = Path.Combine(_temp.Path, CustomFileService.StashFile);
        var prospectPath = WriteProspect("Raw.json", Slot("Wood", 100), Slot("Stone", 20));
        var prospectBefore = File.ReadAllText(prospectPath);
        var stashBefore = File.ReadAllText(stashPath);

        var result = await NewService().ReturnAsync(prospectPath, _temp.Path);

        result.Ok.Should().BeTrue();
        result.Moved!.SlotsRemoved.Should().Be(0);
        File.ReadAllText(prospectPath).Should().Be(prospectBefore, "nothing moved, so the world file is untouched");
        File.ReadAllText(stashPath).Should().Be(stashBefore);
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
