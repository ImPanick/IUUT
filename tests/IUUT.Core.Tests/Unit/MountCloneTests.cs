using System.Text.Json;
using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies mount-rescue slice 1 (roster restore/clone): the clone is a deep copy — the
/// authoritative RecorderBlob rides along byte-for-byte — with only the name changed.
/// </summary>
public class MountCloneTests
{
    [Fact]
    public void Clone_DeepCopiesTheMount_RenamesIt_AndAppendsToTheRoster()
    {
        var source = new Mount
        {
            MountName = "Original",
            MountLevel = 30,
            MountType = "Buffalo",
            MountIconName = "icon_buffalo",
            AdditionalData = new Dictionary<string, JsonElement>
            {
                ["RecorderBlob"] = JsonSerializer.SerializeToElement(new { BinaryData = "b64-opaque-bytes", Version = 3 }),
            },
        };
        var model = new MountsModel { SavedMounts = { source } };

        var clone = new MountEditService().Clone(model, source, "Revived");

        model.SavedMounts.Should().HaveCount(2).And.EndWith(clone);
        clone.Should().NotBeSameAs(source);
        clone.MountName.Should().Be("Revived");
        clone.MountLevel.Should().Be(30);
        clone.MountType.Should().Be("Buffalo");
        clone.AdditionalData!["RecorderBlob"].GetRawText()
            .Should().Be(source.AdditionalData!["RecorderBlob"].GetRawText(), "the blob must ride along verbatim");
        source.MountName.Should().Be("Original", "the source is never touched");
    }
}
