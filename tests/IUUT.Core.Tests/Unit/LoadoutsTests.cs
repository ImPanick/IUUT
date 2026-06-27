using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class LoadoutsTests
{
    private const string SuitGuid = "AAAAAAAA000000000000000000000001";
    private const string MetaItemGuid = "BBBBBBBB000000000000000000000002";

    private const string Sample = """
        {
            "Loadouts": [
                {
                    "ChrSlot": 1,
                    "Guid": "LOADOUT00000000000000000000000001",
                    "EnviroSuit": { "DatabaseGUID": "AAAAAAAA000000000000000000000001" },
                    "MetaItems": [ { "DatabaseGUID": "BBBBBBBB000000000000000000000002" } ]
                }
            ]
        }
        """;

    private readonly LoadoutCrossReference _xref = new();

    [Fact]
    public void Parse_ReadsEntry_AndPreservesSubBlocks()
    {
        var loadouts = LoadoutsParser.Parse(Sample);

        var entry = loadouts.Loadouts.Should().ContainSingle().Subject;
        entry.ChrSlot.Should().Be(1);
        entry.LoadoutGuid.Should().Be("LOADOUT00000000000000000000000001");
        entry.AdditionalData.Should().ContainKeys("EnviroSuit", "MetaItems");
    }

    [Fact]
    public void RoundTrip_PreservesNestedItemGuids()
    {
        var reparsed = LoadoutsParser.Parse(LoadoutsSerializer.Serialize(LoadoutsParser.Parse(Sample)));

        _xref.ReferencedDatabaseGuids(reparsed).Should().BeEquivalentTo([SuitGuid, MetaItemGuid]);
    }

    [Fact]
    public void ReferencedDatabaseGuids_FindsNestedItemGuids()
    {
        var loadouts = LoadoutsParser.Parse(Sample);

        _xref.ReferencedDatabaseGuids(loadouts).Should().BeEquivalentTo([SuitGuid, MetaItemGuid]);
        _xref.IsReferenced(loadouts, SuitGuid).Should().BeTrue();
        _xref.IsReferenced(loadouts, "CCCCCCCC000000000000000000000003").Should().BeFalse();
    }

    [Fact]
    public void DanglingReferences_AreLoadoutGuidsMissingFromStash()
    {
        var loadouts = LoadoutsParser.Parse(Sample);
        var stash = new MetaInventoryModel { Items = [new MetaItem { DatabaseGuid = SuitGuid }] };

        _xref.DanglingReferences(loadouts, stash).Should().BeEquivalentTo([MetaItemGuid]);
    }

    // The real on-disk shape: each loadout names its prospect + carries items by ItemStaticData RowName.
    private const string RichSample = """
        {
            "Loadouts": [
                {
                    "ChrSlot": 3,
                    "Guid": "LOADOUT00000000000000000000000003",
                    "AssociatedProspect": { "ProspectID": "Olympus", "ProspectState": "Active" },
                    "EnviroSuit": { "ItemStaticData": { "RowName": "Envirosuit_Larkwell_Alpha" } },
                    "MetaItems": [
                        { "ItemStaticData": { "RowName": "Meta_Knife_Inaris_Delta" } },
                        { "ItemStaticData": { "RowName": "Meta_Knife_Inaris_Delta" } },
                        { "ItemStaticData": { "RowName": "Meta_Axe" } }
                    ],
                    "bInsured": false,
                    "bSettled": true
                },
                {
                    "ChrSlot": 1,
                    "Guid": "LOADOUT00000000000000000000000001",
                    "AssociatedProspect": { "ProspectID": "Styx", "ProspectState": "Active" },
                    "EnviroSuit": { "ItemStaticData": { "RowName": "None" } },
                    "MetaItems": []
                }
            ]
        }
        """;

    [Fact]
    public void Summarize_ResolvesProspect_EnviroSuit_AndGroupedItems()
    {
        var summaries = _xref.Summarize(LoadoutsParser.Parse(RichSample));

        var olympus = summaries.Should().ContainSingle(s => s.ProspectId == "Olympus").Subject;
        olympus.ChrSlot.Should().Be(3);
        olympus.ProspectState.Should().Be("Active");
        olympus.Settled.Should().BeTrue();
        olympus.Insured.Should().BeFalse();
        olympus.EnviroSuitRowName.Should().Be("Envirosuit_Larkwell_Alpha");
        olympus.Items.Should().BeEquivalentTo([
            new LoadoutItemRef("Meta_Axe", 1),
            new LoadoutItemRef("Meta_Knife_Inaris_Delta", 2),
        ], "duplicate meta items are grouped with a count");
    }

    [Fact]
    public void Summarize_EmptyEnviroSuitAndItems_AreNullAndEmpty()
    {
        var summaries = _xref.Summarize(LoadoutsParser.Parse(RichSample));

        var styx = summaries.Should().ContainSingle(s => s.ProspectId == "Styx").Subject;
        styx.EnviroSuitRowName.Should().BeNull("the \"None\" slot is not a real envirosuit");
        styx.Items.Should().BeEmpty();
    }
}
