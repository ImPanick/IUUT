using FluentAssertions;
using IUUT.Core.Abstractions;
using IUUT.Core.Editing;
using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class CustomFileServiceTests : IDisposable
{
    private readonly TempDir _temp = new();
    private readonly CustomFileService _service = new(
        new SafeSaveWriter(new BackupManager(FixedClock.Default), new SystemGuidProvider()));

    [Fact]
    public async Task LoadMountsAsync_MissingFile_ReturnsNull()
    {
        (await _service.LoadMountsAsync(_temp.Path)).Should().BeNull();
    }

    [Fact]
    public async Task SaveMountsAsync_PersistsEdit_AndBacksUp()
    {
        var path = Path.Combine(_temp.Path, CustomFileService.MountsFile);
        File.WriteAllText(path, MountsSerializer.Serialize(new MountsModel
        {
            SavedMounts = { new Mount { MountName = "Old", MountLevel = 1, MountType = "Arctic_Moa" } },
        }));

        var mounts = await _service.LoadMountsAsync(_temp.Path);
        mounts!.SavedMounts.Single().MountName = "Bessie";
        mounts.SavedMounts.Single().MountLevel = 50;

        var result = await _service.SaveMountsAsync(_temp.Path, mounts);

        result.Ok.Should().BeTrue();
        var reloaded = MountsParser.Parse(File.ReadAllText(path)).SavedMounts.Single();
        reloaded.MountName.Should().Be("Bessie");
        reloaded.MountLevel.Should().Be(50);
        reloaded.MountType.Should().Be("Arctic_Moa", "the un-edited fields round-trip");
        Directory.GetFiles(_temp.Path, CustomFileService.MountsFile + BackupManager.BackupInfix + "*").Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoadStashAsync_RoundTripsItems()
    {
        var path = Path.Combine(_temp.Path, CustomFileService.StashFile);
        File.WriteAllText(path, MetaInventorySerializer.Serialize(new MetaInventoryModel
        {
            InventoryId = "MetaInventoryID_Main",
            Items = { new MetaItem { DatabaseGuid = "ABC123", ItemStaticData = new DataTableRef { RowName = "Item_Wood" } } },
        }));

        var stash = await _service.LoadStashAsync(_temp.Path);

        stash.Should().NotBeNull();
        stash!.Items.Should().ContainSingle(i => i.DatabaseGuid == "ABC123" && i.ItemStaticData.RowName == "Item_Wood");
    }

    [Fact]
    public async Task LoadLoadoutsAsync_FromSubfolder_RoundTrips()
    {
        var dir = Directory.CreateDirectory(Path.Combine(_temp.Path, "Loadout")).FullName;
        File.WriteAllText(
            Path.Combine(dir, "Loadouts.json"),
            LoadoutsSerializer.Serialize(new LoadoutsModel { Loadouts = { new LoadoutEntry { ChrSlot = 1, LoadoutGuid = "L1" } } }));

        var loadouts = await _service.LoadLoadoutsAsync(_temp.Path);

        loadouts.Should().NotBeNull();
        loadouts!.Loadouts.Should().ContainSingle(l => l.ChrSlot == 1 && l.LoadoutGuid == "L1");
    }

    public void Dispose() => _temp.Dispose();
}
