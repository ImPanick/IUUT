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
        new SafeSaveWriter(new BackupManager(FixedClock.Default), new SystemGuidProvider()),
        new BackupManager(FixedClock.Default));

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

    [Fact]
    public async Task SaveFlagsAsync_PersistsEdit_AndBacksUp()
    {
        const string steamId = "11111111111111111";
        var path = Path.Combine(_temp.Path, $"flags_{steamId}.dat");
        File.WriteAllBytes(path, FlagsFileCodec.Write(new FlagsFileModel { SteamId = steamId, Flags = { 1, 2, 3 } }));

        var flags = await _service.LoadFlagsAsync(_temp.Path);
        flags!.Flags.Add(99);

        var ok = await _service.SaveFlagsAsync(_temp.Path, flags);

        ok.Should().BeTrue();
        var reloaded = FlagsFileCodec.Read(File.ReadAllBytes(path));
        reloaded.Flags.Should().Contain(99).And.Contain(1);
        Directory.GetFiles(_temp.Path, $"flags_{steamId}.dat{BackupManager.BackupInfix}*").Should().NotBeEmpty();
        File.Exists(path + ".iuut-tmp").Should().BeFalse("the temp is cleaned up");
    }

    [Fact]
    public async Task LoadFlagsAsync_NoFlagsFile_ReturnsNull()
    {
        (await _service.LoadFlagsAsync(_temp.Path)).Should().BeNull();
    }

    [Fact]
    public async Task SaveAssociatedProspectsAsync_PersistsUnstick()
    {
        var file = Path.Combine(_temp.Path, "AssociatedProspects_Slot_1.json");
        File.WriteAllText(file, AssociatedProspectsSerializer.Serialize(new AssociatedProspectsModel
        {
            ContainerKey = "AssociatedProspects_Slot_1.json",
            Prospects = { new AssociatedProspect { ProspectId = "P1" }, new AssociatedProspect { ProspectId = "P2" } },
        }));

        _service.ResolveAssociatedProspectFiles(_temp.Path).Should().ContainSingle();
        var model = await _service.LoadAssociatedProspectsAsync(file);
        new ProspectEditService().Unstick(model!, "P1").Should().BeTrue();

        var result = await _service.SaveAssociatedProspectsAsync(file, model!);

        result.Ok.Should().BeTrue();
        AssociatedProspectsParser.Parse(File.ReadAllText(file)).Prospects.Should().ContainSingle(p => p.ProspectId == "P2");
    }

    [Fact]
    public async Task SaveJsonTextAsync_RejectsMalformed_AcceptsValid()
    {
        var file = Path.Combine(_temp.Path, "Profile.json");
        File.WriteAllText(file, "{\"a\":1}");

        (await _service.SaveJsonTextAsync(file, "{not json")).Ok.Should().BeFalse();
        File.ReadAllText(file).Should().Be("{\"a\":1}", "a rejected write leaves the original");

        (await _service.SaveJsonTextAsync(file, "{\"a\":2}")).Ok.Should().BeTrue();
        (await _service.ReadTextAsync(file)).Should().Be("{\"a\":2}");
    }

    [Fact]
    public void ListJsonFiles_IncludesTopLevelAndLoadoutSubfolder()
    {
        File.WriteAllText(Path.Combine(_temp.Path, "Profile.json"), "{}");
        var loadout = Directory.CreateDirectory(Path.Combine(_temp.Path, "Loadout")).FullName;
        File.WriteAllText(Path.Combine(loadout, "Loadouts.json"), "{}");

        var files = _service.ListJsonFiles(_temp.Path);

        files.Should().HaveCount(2);
        files.Should().Contain(f => f.EndsWith("Profile.json", StringComparison.Ordinal));
        files.Should().Contain(f => f.EndsWith("Loadouts.json", StringComparison.Ordinal));
    }

    public void Dispose() => _temp.Dispose();
}
