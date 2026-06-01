using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>Unit tests for the low-level UE4 tagged-property reader primitives.</summary>
public class UePropertyReaderTests
{
    [Fact]
    public void ReadFString_RoundTripsAscii_AndAdvancesPosition()
    {
        var bytes = UeFixtureBuilder.FString("EnzymeGeyserRecorderComponent");
        var pos = 0;

        var s = UePropertyReader.ReadFString(bytes, ref pos);

        s.Should().Be("EnzymeGeyserRecorderComponent");
        pos.Should().Be(bytes.Length, "the cursor advances past the whole FString incl. its null terminator");
    }

    [Fact]
    public void ReadFString_EmptyString_IsZeroLength()
    {
        var pos = 0;
        UePropertyReader.ReadFString(UeFixtureBuilder.FString(string.Empty), ref pos).Should().BeEmpty();
    }

    [Fact]
    public void ReadStream_ParsesAFlatPropertyList()
    {
        var stream = UeFixtureBuilder.Concat(
            UeFixtureBuilder.IntProp("LevelIndex", -1),
            UeFixtureBuilder.NameProp("HordeDTKey", "Forest_Horde"),
            UeFixtureBuilder.FString("None"));

        var props = UePropertyReader.ReadStream(stream);

        props.Should().HaveCount(2);
        props[0].Name.Should().Be("LevelIndex");
        props[0].Type.Should().Be("IntProperty");
        props[1].Name.Should().Be("HordeDTKey");
        props[1].Type.Should().Be("NameProperty");
    }

    [Fact]
    public void ReadStream_StopsAtNoneTerminator()
    {
        var stream = UeFixtureBuilder.Concat(
            UeFixtureBuilder.IntProp("First", 1),
            UeFixtureBuilder.FString("None"),
            UeFixtureBuilder.IntProp("AfterNone", 2)); // must NOT be read

        UePropertyReader.ReadStream(stream).Should().ContainSingle().Which.Name.Should().Be("First");
    }

    [Fact]
    public void ReadStream_RecursesIntoStructChildren()
    {
        var stream = UeFixtureBuilder.Concat(
            UeFixtureBuilder.StructProp("Outer", "DemoStruct", UeFixtureBuilder.IntProp("Inner", 42)),
            UeFixtureBuilder.FString("None"));

        var outer = UePropertyReader.ReadStream(stream).Should().ContainSingle().Subject;
        outer.StructName.Should().Be("DemoStruct");
        outer.Children.Should().ContainSingle().Which.Name.Should().Be("Inner");
    }
}
