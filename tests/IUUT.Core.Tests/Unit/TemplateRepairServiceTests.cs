using FluentAssertions;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Recovery;
using IUUT.Core.Serializers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class TemplateRepairServiceTests
{
    private const string SteamId = "76561198000000000";
    private readonly TemplateRepairService _service = new();

    [Fact]
    public void Repair_CorruptProfile_RebuildsMinimalSkeletonWithUserId()
    {
        var result = _service.Repair("Profile.json", "{ this is broken", SteamId);

        result.CanRepair.Should().BeTrue();
        result.IsPartial.Should().BeTrue();
        result.Salvaged.Should().BeFalse();

        var profile = ProfileParser.Parse(result.NewContent!);
        profile.UserId.Should().Be(SteamId);
        profile.MetaResources.Should().BeEmpty();
    }

    [Fact]
    public void Repair_CorruptCharacters_RebuildsEmptyRoster()
    {
        var result = _service.Repair("Characters.json", "not json at all", SteamId);

        result.CanRepair.Should().BeTrue();
        result.IsPartial.Should().BeTrue();
        CharactersParser.Parse(result.NewContent!).Should().BeEmpty();
    }

    [Fact]
    public void Repair_IntactProfile_IsSalvagedNotFlattened()
    {
        var original = ProfileSerializer.Serialize(new ProfileModel
        {
            UserId = SteamId,
            MetaResources = [new MetaResource { MetaRow = "Credits", Count = 42 }],
        });

        var result = _service.Repair("Profile.json", original, SteamId);

        result.Salvaged.Should().BeTrue();
        result.IsPartial.Should().BeFalse();
        ProfileParser.Parse(result.NewContent!).MetaResources.Should().ContainSingle(m => m.Count == 42);
    }

    [Fact]
    public void Repair_UnmodeledFile_IsRefused()
    {
        var result = _service.Repair("MetaInventory.json", "{ broken", SteamId);

        result.CanRepair.Should().BeFalse();
        result.NewContent.Should().BeNull();
        result.IsPartial.Should().BeFalse();
    }
}
