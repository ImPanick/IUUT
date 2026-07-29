using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies the Field Guide edits — tracked stats, task-list checklists, and fishing records —
/// including the thing that matters most: the surrounding Accolades data (and each entry's own
/// unknown members) must survive verbatim, because these blocks are edited in their preserved
/// JSON form rather than re-typed.
/// </summary>
public class FieldGuideEditServiceTests
{
    private const string AccoladesJson = """
        {
          "CompletedAccolades": [
            { "Accolade": { "RowName": "ForestScanComplete", "DataTableName": "D_Accolades" }, "Unknown": 42 }
          ],
          "PlayerTrackers": {
            "(RowName=\"DistanceTraveled\",DataTableName=\"D_PlayerTrackers\")": 622132,
            "(RowName=\"BerriesCollected\",DataTableName=\"D_PlayerTrackers\")": 3351
          },
          "PlayerTaskListTrackers": {
            "(RowName=\"VisitBiomesList\",DataTableName=\"D_PlayerTrackers\")": {
              "CompletedTasks": [ "Arctic", "Conifer" ],
              "UnknownField": "keep-me"
            }
          },
          "SomeFutureBlock": { "keep": true }
        }
        """;

    private static AccoladesModel Accolades() => AccoladesParser.Parse(AccoladesJson);

    [Fact]
    public void ListStats_And_ListTaskLists_DecodeTheStructStringKeys()
    {
        var service = new FieldGuideEditService();
        var accolades = Accolades();

        var stats = service.ListStats(accolades);
        stats.Should().HaveCount(2);
        stats.Single(s => s.RowName == "DistanceTraveled").Value.Should().Be(622132);

        var lists = service.ListTaskLists(accolades);
        lists.Should().ContainSingle().Which.CompletedTasks.Should().BeEquivalentTo(["Arctic", "Conifer"]);
    }

    [Fact]
    public void SetStat_UpdatesExisting_AddsMissing_AndPreservesEverythingElse()
    {
        var service = new FieldGuideEditService();
        var accolades = Accolades();

        service.SetStat(accolades, "DistanceTraveled", 999).Should().BeTrue();
        service.SetStat(accolades, "DistanceTraveled", 999).Should().BeFalse("setting the same value is a no-op");
        service.SetStat(accolades, "CreatureKills", 250).Should().BeTrue("a never-recorded stat is added");
        service.SetStat(accolades, "BerriesCollected", -5).Should().BeTrue();

        var reread = AccoladesParser.Parse(AccoladesSerializer.Serialize(accolades));
        var stats = service.ListStats(reread).ToDictionary(s => s.RowName, s => s.Value, StringComparer.Ordinal);
        stats["DistanceTraveled"].Should().Be(999);
        stats["CreatureKills"].Should().Be(250);
        stats["BerriesCollected"].Should().Be(0, "values are clamped to zero");

        // Everything untouched must survive the round-trip verbatim (CONSTITUTION VI).
        reread.CompletedAccolades.Should().ContainSingle()
            .Which.AdditionalData!.Should().ContainKey("Unknown");
        reread.AdditionalData!.Should().ContainKey("SomeFutureBlock");
        service.ListTaskLists(reread).Should().ContainSingle()
            .Which.CompletedTasks.Should().BeEquivalentTo(["Arctic", "Conifer"]);
    }

    [Fact]
    public void SetTaskCompleted_TicksAndUnticks_KeepingTheEntrysUnknownMembers()
    {
        var service = new FieldGuideEditService();
        var accolades = Accolades();

        service.SetTaskCompleted(accolades, "VisitBiomesList", "Riverlands", completed: true).Should().BeTrue();
        service.SetTaskCompleted(accolades, "VisitBiomesList", "Arctic", completed: true).Should().BeFalse("already done");
        service.SetTaskCompleted(accolades, "VisitBiomesList", "Conifer", completed: false).Should().BeTrue();

        var reread = AccoladesParser.Parse(AccoladesSerializer.Serialize(accolades));
        service.ListTaskLists(reread).Single().CompletedTasks
            .Should().BeEquivalentTo(["Arctic", "Riverlands"]);

        reread.AdditionalData!["PlayerTaskListTrackers"].GetRawText()
            .Should().Contain("keep-me", "the entry's unknown members ride along");
    }

    [Fact]
    public void SetFish_AddsAndUpdatesRecords()
    {
        var service = new FieldGuideEditService();
        var bestiary = new BestiaryModel();

        var added = service.SetFish(bestiary, "Fish_01", caught: 3, quality: 61, weight: 5915, length: 620);
        added.FishRow.RowName.Should().Be("Fish_01");
        added.FishRow.DataTableName.Should().Be("D_FishData");
        bestiary.FishTracking.Should().ContainSingle();

        service.SetFish(bestiary, "Fish_01", caught: 10, quality: 99, weight: 1, length: -4);
        bestiary.FishTracking.Should().ContainSingle("the same fish is updated, never duplicated");
        var entry = bestiary.FishTracking[0];
        entry.CaughtCount.Should().Be(10);
        entry.MaxQuality.Should().Be(99);
        entry.MaxLength.Should().Be(0, "values are clamped to zero");
    }
}
