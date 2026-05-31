using FluentAssertions;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class ProspectFileTests
{
    private const string Sample = """
        {
            "ProspectInfo": {
                "ProspectID": "Olympus_A",
                "ProspectDTKey": "Outpost006_Olympus",
                "Difficulty": "Medium"
            },
            "ProspectBlob": {
                "Key": "actors",
                "Hash": "ABC123",
                "TotalLength": 10,
                "DataLength": 10,
                "UncompressedLength": 42,
                "BinaryBlob": "eJzExampleBase64Blob"
            }
        }
        """;

    [Fact]
    public void Parse_ReadsHeaderIdentifiers_AndBlob()
    {
        var file = ProspectFileParser.Parse(Sample);

        file.ProspectInfo.ProspectId.Should().Be("Olympus_A");
        file.ProspectInfo.ProspectDtKey.Should().Be("Outpost006_Olympus");
        file.ProspectInfo.AdditionalData.Should().ContainKey("Difficulty");
        file.ProspectBlob.Key.Should().Be("actors");
        file.ProspectBlob.UncompressedLength.Should().Be(42);
        file.ProspectBlob.BinaryBlob.Should().Be("eJzExampleBase64Blob");
    }

    [Fact]
    public void RoundTrip_PreservesHeaderExtrasAndBlobVerbatim()
    {
        var reparsed = ProspectFileParser.Parse(
            ProspectFileSerializer.Serialize(ProspectFileParser.Parse(Sample)));

        reparsed.ProspectInfo.AdditionalData.Should().ContainKey("Difficulty");
        reparsed.ProspectBlob.BinaryBlob.Should().Be("eJzExampleBase64Blob", "the world blob is never touched by a header edit");
        reparsed.ProspectBlob.Hash.Should().Be("ABC123");
    }
}
