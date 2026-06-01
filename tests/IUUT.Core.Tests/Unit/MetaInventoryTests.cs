using FluentAssertions;
using IUUT.Core.Abstractions;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using IUUT.Core.Validation;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class MetaInventoryTests
{
    private const string Sample = """
        {
            "InventoryID": "MetaInventoryID_Main",
            "Items": [
                {
                    "ItemStaticData": { "RowName": "Envirosuit_Tier2", "DataTableName": "D_ItemsStatic" },
                    "ItemDynamicData": [
                        { "PropertyType": "ItemableStack", "Value": 1 },
                        { "PropertyType": "Durability", "Value": 5500 }
                    ],
                    "ItemCustomStats": [],
                    "CustomProperties": { "Stats": [], "Alterations": [] },
                    "DatabaseGUID": "F44CB30140004789820E20B75577DEA1",
                    "ItemOwnerLookupId": -1,
                    "RuntimeTags": { "GameplayTags": [] }
                }
            ]
        }
        """;

    [Fact]
    public void Parse_ReadsItem_AndPreservesUnknownBlocks()
    {
        var inventory = MetaInventoryParser.Parse(Sample);

        inventory.InventoryId.Should().Be("MetaInventoryID_Main");
        var item = inventory.Items.Should().ContainSingle().Subject;
        item.ItemStaticData.RowName.Should().Be("Envirosuit_Tier2");
        item.DatabaseGuid.Should().Be("F44CB30140004789820E20B75577DEA1");
        item.ItemOwnerLookupId.Should().Be(-1);
        item.ItemDynamicData.Should().HaveCount(2);
        item.ItemDynamicData.Should().Contain(p => p.PropertyType == "Durability");
        item.AdditionalData.Should().ContainKeys("ItemCustomStats", "CustomProperties", "RuntimeTags");
    }

    [Fact]
    public void RoundTrip_PreservesItemAndUnknownBlocks()
    {
        var reparsed = MetaInventoryParser.Parse(MetaInventorySerializer.Serialize(MetaInventoryParser.Parse(Sample)));

        reparsed.Items.Should().ContainSingle();
        reparsed.Items[0].AdditionalData.Should().ContainKey("RuntimeTags");
        reparsed.Items[0].ItemDynamicData.Should().HaveCount(2);
    }

    [Fact]
    public void StashEditService_AddItem_MintsUppercaseHexGuid_AndStashDefaults()
    {
        var service = new StashEditService(new SystemGuidProvider());
        var inventory = new MetaInventoryModel();

        var item = service.AddItem(inventory, "Envirosuit_Tier2");

        item.DatabaseGuid.Should().MatchRegex("^[0-9A-F]{32}$", "game GUIDs are 32 uppercase hex digits, no dashes");
        item.ItemStaticData.DataTableName.Should().Be("D_ItemsStatic");
        item.ItemOwnerLookupId.Should().Be(-1);
        inventory.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Fact]
    public void StashEditService_AddItem_ProducesUniqueGuids_ThatPassValidation()
    {
        var service = new StashEditService(new SystemGuidProvider());
        var inventory = new MetaInventoryModel();

        service.AddItem(inventory, "A");
        service.AddItem(inventory, "B");

        inventory.Items.Select(i => i.DatabaseGuid).Should().OnlyHaveUniqueItems();
        ValidationEngine.ValidateUniqueDatabaseGuids(inventory.Items.Select(i => i.DatabaseGuid)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void StashEditService_RemoveItem_ReportsPresence()
    {
        var service = new StashEditService(new SystemGuidProvider());
        var inventory = new MetaInventoryModel();
        var item = service.AddItem(inventory, "A");

        service.RemoveItem(inventory, item.DatabaseGuid).Should().BeTrue();
        service.RemoveItem(inventory, item.DatabaseGuid).Should().BeFalse();
        inventory.Items.Should().BeEmpty();
    }

    [Fact]
    public void StashEditService_SetStack_ClampsToHardMax_AndRoundTrips()
    {
        var service = new StashEditService(new SystemGuidProvider());
        var inventory = new MetaInventoryModel();
        var item = service.AddItem(inventory, "Wood");

        service.SetStack(item, 5000); // way over the 100 cap
        StashEditService.GetStack(item).Should().Be(StashEditService.MaxStack);

        service.SetStack(item, 0); // under the floor
        StashEditService.GetStack(item).Should().Be(1);

        service.SetStack(item, 42);
        StashEditService.GetStack(item).Should().Be(42);

        // Survives a serialize → parse round-trip.
        var reparsed = MetaInventoryParser.Parse(MetaInventorySerializer.Serialize(inventory));
        StashEditService.GetStack(reparsed.Items.Single()).Should().Be(42);
    }

    [Fact]
    public void StashEditService_SetStack_UpdatesExistingProperty_WithoutDuplicating()
    {
        var service = new StashEditService(new SystemGuidProvider());
        var item = MetaInventoryParser.Parse(Sample).Items.Single(); // already has an ItemableStack

        service.SetStack(item, 7);

        item.ItemDynamicData.Count(p => p.PropertyType == "ItemableStack").Should().Be(1);
        StashEditService.GetStack(item).Should().Be(7);
    }
}
