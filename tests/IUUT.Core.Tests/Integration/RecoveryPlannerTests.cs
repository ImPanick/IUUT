using FluentAssertions;
using IUUT.Core.Models;
using IUUT.Core.Recovery;
using IUUT.Core.Serializers;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class RecoveryPlannerTests : IDisposable
{
    private const string SteamId = "11111111111111111";

    private readonly TempDir _temp = new();
    private readonly RecoveryPlanner _planner =
        new(new HealthScanService(), new BackupChainWalker(), new TemplateRepairService());

    private string _dir = "";

    private string Dir => _dir.Length > 0
        ? _dir
        : _dir = Directory.CreateDirectory(Path.Combine(_temp.Path, SteamId)).FullName;

    private void Write(string name, string content) =>
        File.WriteAllText(Path.Combine(Dir, name), content);

    private static RecoveryFileAction Action(RecoveryPlan plan, string name) =>
        plan.Actions.Single(a => a.RelativePath == name);

    [Fact]
    public void Plan_RestoresFromCleanBackup_TemplatesModeled_RefusesUnmodeled_AndFlagsPartial()
    {
        // Profile: corrupt + a clean game backup → restore.
        Write("Profile.json", "{ broken");
        Write("Profile.json.backup", ProfileSerializer.Serialize(new ProfileModel { UserId = SteamId }));
        // Characters: corrupt, no backup → template (modeled).
        Write("Characters.json", "not json");
        // MetaInventory: corrupt, no backup → unrecoverable (unmodeled).
        Write("MetaInventory.json", "{ broken");
        // Accolades: clean → already OK.
        Write("Accolades.json", AccoladesSerializer.Serialize(new AccoladesModel()));

        var plan = _planner.Plan(Dir);

        Action(plan, "Profile.json").Outcome.Should().Be(RecoveryOutcome.RestoreFromGameBackup);
        Action(plan, "Profile.json").SourceBackupPath.Should().EndWith("Profile.json.backup");

        var characters = Action(plan, "Characters.json");
        characters.Outcome.Should().Be(RecoveryOutcome.TemplateRepair);
        characters.RepairedContent.Should().NotBeNullOrEmpty();

        Action(plan, "MetaInventory.json").Outcome.Should().Be(RecoveryOutcome.Unrecoverable);
        Action(plan, "Accolades.json").Outcome.Should().Be(RecoveryOutcome.AlreadyOk);

        plan.PartialRecovery.Should().BeTrue("a template repair and an unrecoverable file are present");
        plan.HasWork.Should().BeTrue();
    }

    [Fact]
    public void Plan_OrdersActionsByRestoreOrder()
    {
        Write("Profile.json", ProfileSerializer.Serialize(new ProfileModel { UserId = SteamId }));
        Write("Characters.json", CharactersSerializer.Serialize(new List<CharacterModel>()));
        Write("Accolades.json", AccoladesSerializer.Serialize(new AccoladesModel()));

        var plan = _planner.Plan(Dir);

        var order = plan.Actions.Select(a => a.RelativePath).ToList();
        order.IndexOf("Profile.json").Should().BeLessThan(order.IndexOf("Characters.json"));
        order.IndexOf("Characters.json").Should().BeLessThan(order.IndexOf("Accolades.json"));
    }

    [Fact]
    public void Plan_AllClean_HasNoWork_AndIsNotPartial()
    {
        Write("Profile.json", ProfileSerializer.Serialize(new ProfileModel { UserId = SteamId }));
        Write("Characters.json", CharactersSerializer.Serialize(new List<CharacterModel>()));

        var plan = _planner.Plan(Dir);

        plan.HasWork.Should().BeFalse();
        plan.PartialRecovery.Should().BeFalse();
        plan.Actions.Should().OnlyContain(a => a.Outcome == RecoveryOutcome.AlreadyOk);
    }

    public void Dispose() => _temp.Dispose();
}
