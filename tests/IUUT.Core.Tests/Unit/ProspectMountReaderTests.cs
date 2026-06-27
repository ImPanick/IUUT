using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies <see cref="ProspectMountReader"/> against synthetic UE tagged-property streams (no real
/// save data) — a mount is a <c>StateRecorderBlobs</c> actor whose recorder is the
/// <c>IcarusMountCharacterRecorderComponent</c>, carrying a <c>MountName</c> and a
/// <c>BP_Mount_&lt;X&gt;_C</c> class. Real-save behaviour was confirmed separately (Styx = 7 mounts).
/// </summary>
public class ProspectMountReaderTests
{
    private const string MountComponent = "/Script/Icarus.IcarusMountCharacterRecorderComponent";

    private static byte[] Mount(string actorClass, string mountName) =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", MountComponent),
            UeFixtureBuilder.StrProp("MountActorClass", actorClass),
            UeFixtureBuilder.ByteStreamProp("BinaryData", UeFixtureBuilder.StrProp("MountName", mountName)));

    private static byte[] NonMount() =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.DeployableRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp("BinaryData", UeFixtureBuilder.StrProp("Other", "x")));

    private static byte[] World(params byte[][] actors) =>
        UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob", actors);

    [Fact]
    public void Read_FindsMountsWithNameAndType_IgnoringNonMountActors()
    {
        var world = World(
            Mount("BP_Mount_Horse_C", "Bessie"),
            NonMount(),
            Mount("BP_Mount_Buffalo_C", "Tank"));

        var mounts = new ProspectMountReader().Read(world);

        mounts.Should().HaveCount(2, "the deployable recorder is not a mount");
        mounts.Should().ContainSingle(m => m.Name == "Bessie").Which.MountType.Should().Be("Horse");
        mounts.Should().ContainSingle(m => m.Name == "Tank").Which.MountType.Should().Be("Buffalo");
    }

    [Fact]
    public void Read_UnnamedMount_LabelsByType()
    {
        var mount = new ProspectMountReader().Read(World(Mount("BP_Mount_Moa_C", ""))).Should().ContainSingle().Subject;

        mount.Name.Should().BeEmpty();
        mount.MountType.Should().Be("Moa");
        mount.Label.Should().Be("Moa", "an unnamed mount falls back to its type");
    }

    [Fact]
    public void Read_NoMountsOrGarbage_ReturnsEmpty_AndDoesNotThrow()
    {
        new ProspectMountReader().Read(World(NonMount())).Should().BeEmpty();
        new ProspectMountReader()
            .Read(Enumerable.Range(0, 4000).Select(i => (byte)(i * 31 % 256)).ToArray())
            .Should().BeEmpty();
    }
}
