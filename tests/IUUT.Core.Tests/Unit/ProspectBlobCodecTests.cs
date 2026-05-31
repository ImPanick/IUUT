using FluentAssertions;
using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class ProspectBlobCodecTests
{
    // Deterministic, non-trivially-compressible payload (mimics an FProperty stream).
    private static byte[] SamplePayload() =>
        Enumerable.Range(0, 50_000).Select(i => (byte)((i * 31 + 7) % 256)).ToArray();

    [Fact]
    public void CompressThenDecompress_RoundTripsExactly()
    {
        var data = SamplePayload();

        var roundTripped = ProspectBlobCodec.Decompress(ProspectBlobCodec.CompressToBase64(data));

        roundTripped.Should().Equal(data);
    }

    [Fact]
    public void Compress_ProducesAZlibStream_StartingWithCmfByte()
    {
        var compressed = Convert.FromBase64String(ProspectBlobCodec.CompressToBase64(SamplePayload()));

        compressed[0].Should().Be(0x78, "a 32K-window zlib stream always starts with the 0x78 CMF byte");
    }

    [Fact]
    public void ComputeHash_IsUppercaseHexSha1Length()
    {
        ProspectBlobCodec.ComputeHash(SamplePayload()).Should().MatchRegex("^[0-9A-F]{40}$");
    }

    [Fact]
    public void SetUncompressed_ReencodesAndRestamps_SoVerifyHashPasses()
    {
        var data = SamplePayload();
        var blob = new ProspectBlobModel { Key = "actors" };

        ProspectBlobCodec.SetUncompressed(blob, data);

        blob.UncompressedLength.Should().Be(data.Length);
        blob.TotalLength.Should().Be(blob.DataLength).And.BeGreaterThan(0);
        blob.Hash.Should().MatchRegex("^[0-9A-F]{40}$");
        ProspectBlobVerifier.VerifyHash(blob).Should().BeTrue("the re-stamped blob must pass the game's load-time hash check");
        ProspectBlobCodec.Decompress(blob.BinaryBlob).Should().Equal(data);
    }

    [Fact]
    public void SetUncompressed_IsRepeatable_ProducingTheSameHash()
    {
        var data = SamplePayload();
        var a = new ProspectBlobModel();
        var b = new ProspectBlobModel();

        ProspectBlobCodec.SetUncompressed(a, data);
        ProspectBlobCodec.SetUncompressed(b, data);

        b.Hash.Should().Be(a.Hash, "the hash is over the uncompressed bytes, so identical input → identical hash");
    }
}
