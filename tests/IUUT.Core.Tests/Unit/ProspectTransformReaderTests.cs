using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies the ActorTransform decode. Icarus writes Transform as TAGGED sub-properties even
/// though the generic reader treats Transform as an opaque native struct, so this decoder walks
/// the value span itself. Also pins the property the write phase depends on: the translation
/// vector is a fixed 12 bytes at a stable offset, making a rebase an in-place write.
/// </summary>
public class ProspectTransformReaderTests
{
    private static (byte[] Data, UeProperty Node) Transform(float x, float y, float z, float scale = 1f)
    {
        var data = UeFixtureBuilder.ActorTransform(x, y, z, scale);
        var node = UePropertyReader.ReadStream(data).Single(p => p.Name == "ActorTransform");
        return (data, node);
    }

    [Fact]
    public void Read_DecodesTranslationRotationAndScale()
    {
        var (data, node) = Transform(-180760.47f, 267420.91f, -17974.78f, scale: 2f);

        var t = ProspectTransformReader.Read(data, node);

        t.Should().NotBeNull();
        t!.X.Should().BeApproximately(-180760.47f, 0.01f);
        t.Y.Should().BeApproximately(267420.91f, 0.01f);
        t.Z.Should().BeApproximately(-17974.78f, 0.01f);
        t.RotationW.Should().Be(1f);
        t.ScaleX.Should().Be(2f);

        var (mx, my, _) = t.Metres;
        mx.Should().BeApproximately(-1807.60, 0.01, "UE units are centimetres");
        my.Should().BeApproximately(2674.21, 0.01);
    }

    [Fact]
    public void TranslationOffset_PointsAtTwelveWritableBytes()
    {
        var (data, node) = Transform(100f, 200f, 300f);

        var offset = ProspectTransformReader.TranslationOffset(data, node);

        offset.Should().NotBeNull("the write phase rebases position by overwriting these bytes in place");
        BitConverter.ToSingle(data, offset!.Value).Should().Be(100f);
        BitConverter.ToSingle(data, offset.Value + 4).Should().Be(200f);
        BitConverter.ToSingle(data, offset.Value + 8).Should().Be(300f);

        // Rebasing is size-preserving: overwrite in place, length never changes.
        var before = data.Length;
        BitConverter.GetBytes(555f).CopyTo(data, offset.Value);
        data.Length.Should().Be(before);
        ProspectTransformReader.Read(data, node)!.X.Should().Be(555f);
    }

    [Fact]
    public void DistanceTo_MeasuresPlanarMetres()
    {
        var (dataA, nodeA) = Transform(0f, 0f, 0f);
        var (dataB, nodeB) = Transform(30000f, 40000f, 99999f);

        var a = ProspectTransformReader.Read(dataA, nodeA)!;
        var b = ProspectTransformReader.Read(dataB, nodeB)!;

        a.DistanceTo(b).Should().BeApproximately(500, 0.01, "3-4-5 triangle in cm, height ignored");
    }

    [Fact]
    public void Read_NonTransformValue_ReturnsNullRatherThanGarbage()
    {
        var data = UeFixtureBuilder.StrProp("ActorTransform", "not-a-transform");
        var node = UePropertyReader.ReadStream(data).Single();

        ProspectTransformReader.Read(data, node).Should().BeNull();
    }
}
