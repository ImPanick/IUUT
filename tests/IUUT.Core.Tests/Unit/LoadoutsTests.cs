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
}
