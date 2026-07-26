using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies loadout recovery: the guided bInsured flip (idempotent, one boolean only) and
/// dangling-reference restore (exact GUID + RowName recreated in the stash).
/// </summary>
public class LoadoutRecoveryServiceTests
{
    private const string LoadoutsJson = """
        {
          "Loadouts": [
            {
              "ChrSlot": 0,
              "Guid": "LOADOUT-1",
              "bInsured": false,
              "bSettled": true,
              "EnviroSuit": { "ItemStaticData": { "RowName": "Meta_Suit", "DataTableName": "D_ItemsStatic" }, "DatabaseGUID": "AAAA1111" },
              "MetaItems": [
                { "ItemStaticData": { "RowName": "Meta_Sword", "DataTableName": "D_ItemsStatic" }, "DatabaseGUID": "BBBB2222" },
                { "ItemStaticData": { "RowName": "None" }, "DatabaseGUID": "" }
              ]
            },
            {
              "ChrSlot": 1,
              "Guid": "LOADOUT-2",
              "bInsured": true,
              "MetaItems": []
            }
          ]
        }
        """;

    private static LoadoutsModel Loadouts() => LoadoutsParser.Parse(LoadoutsJson);

    private static MetaInventoryModel StashWith(params string[] guids)
    {
        var stash = new MetaInventoryModel();
        foreach (var guid in guids)
        {
            stash.Items.Add(new MetaItem { DatabaseGuid = guid });
        }

        return stash;
    }

    [Fact]
    public void Preview_CountsUninsured_AndFindsDanglingWithRowNames()
    {
        var preview = new LoadoutRecoveryService().Preview(Loadouts(), StashWith("AAAA1111"));

        preview.UninsuredLoadouts.Should().Be(1, "loadout 2 is already insured");
        preview.Dangling.Should().ContainSingle("the suit exists in the stash; the empty GUID is not a reference");
        preview.Dangling[0].DatabaseGuid.Should().Be("BBBB2222");
        preview.Dangling[0].RowName.Should().Be("Meta_Sword", "the RowName comes from the same loadout block");
        preview.Restorable.Should().Be(1);
    }

    [Fact]
    public void InsureAll_FlipsOnlyUninsured_AndIsIdempotent()
    {
        var service = new LoadoutRecoveryService();
        var loadouts = Loadouts();

        service.InsureAll(loadouts).Should().Be(1);
        service.InsureAll(loadouts).Should().Be(0, "the flip is idempotent");
        service.Preview(loadouts, StashWith()).UninsuredLoadouts.Should().Be(0);
    }

    [Fact]
    public void RestoreDangling_RecreatesTheExactGuidAndRowName()
    {
        var service = new LoadoutRecoveryService();
        var loadouts = Loadouts();
        var stash = StashWith("AAAA1111");

        var added = service.RestoreDangling(loadouts, stash);

        added.Should().Be(1);
        var restored = stash.Items.Single(i => i.DatabaseGuid == "BBBB2222");
        restored.ItemStaticData.RowName.Should().Be("Meta_Sword");
        restored.ItemOwnerLookupId.Should().Be(-1, "stash items are unowned");
        service.Preview(loadouts, stash).Dangling.Should().BeEmpty("the loadout is whole again");
    }
}
