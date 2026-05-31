using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class MountsTests
{
    private const string Sample = """
        {
            "SavedMounts": [
                {
                    "MountName": "Snowball",
                    "MountLevel": 12,
                    "MountType": "Arctic_Moa",
                    "MountIconName": "Mount_001",
                    "RecorderBlob": { "BinaryData": [1, 2, 3, 4] }
                }
            ]
        }
        """;

    private readonly MountEditService _service = new();

    [Fact]
    public void Parse_ReadsFields_AndPreservesRecorderBlob()
    {
        var mounts = MountsParser.Parse(Sample);

        var mount = mounts.SavedMounts.Should().ContainSingle().Subject;
        mount.MountName.Should().Be("Snowball");
        mount.MountLevel.Should().Be(12);
        mount.MountType.Should().Be("Arctic_Moa");
        mount.AdditionalData.Should().ContainKey("RecorderBlob");
    }

    [Fact]
    public void Edit_ThenRoundTrip_KeepsRecorderBlob()
    {
        var mounts = MountsParser.Parse(Sample);
        _service.SetName(mounts.SavedMounts[0], "Frostbite");
        _service.SetLevel(mounts.SavedMounts[0], 30);

        var reparsed = MountsParser.Parse(MountsSerializer.Serialize(mounts));

        reparsed.SavedMounts[0].MountName.Should().Be("Frostbite");
        reparsed.SavedMounts[0].MountLevel.Should().Be(30);
        reparsed.SavedMounts[0].AdditionalData.Should().ContainKey("RecorderBlob");
    }
}
