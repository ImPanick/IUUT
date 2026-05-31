using System.Text;
using FluentAssertions;
using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class ProspectBlobVerifierTests
{
    private static readonly byte[] _payload = Encoding.UTF8.GetBytes("StateRecorderBlobs example payload — actors etc.");

    [Fact]
    public void Decompress_RoundTrips_payload()
    {
        var (base64, _) = ProspectBlobFactory.Build(_payload);

        ProspectBlobVerifier.Decompress(base64).Should().Equal(_payload);
    }

    [Fact]
    public void VerifyHash_ValidBlob_ReturnsTrue()
    {
        var (base64, hash) = ProspectBlobFactory.Build(_payload);
        var blob = new ProspectBlobModel { BinaryBlob = base64, Hash = hash, UncompressedLength = _payload.Length };

        ProspectBlobVerifier.VerifyHash(blob).Should().BeTrue();
    }

    [Fact]
    public void VerifyHash_LowercaseHash_StillMatches()
    {
        var (base64, hash) = ProspectBlobFactory.Build(_payload);
        var blob = new ProspectBlobModel { BinaryBlob = base64, Hash = hash.ToLowerInvariant() };

        ProspectBlobVerifier.VerifyHash(blob).Should().BeTrue("the game stores lowercase hex");
    }

    [Fact]
    public void VerifyHash_TamperedHash_ReturnsFalse()
    {
        var (base64, _) = ProspectBlobFactory.Build(_payload);
        var blob = new ProspectBlobModel { BinaryBlob = base64, Hash = "00000000000000000000000000000000DEADBEEF" };

        ProspectBlobVerifier.VerifyHash(blob).Should().BeFalse();
    }

    [Fact]
    public void VerifyHash_CorruptBase64_ReturnsFalseWithoutThrowing()
    {
        var blob = new ProspectBlobModel { BinaryBlob = "not valid base64 !!!", Hash = "abcd" };

        ProspectBlobVerifier.VerifyHash(blob).Should().BeFalse();
    }

    [Fact]
    public void VerifyHash_EmptyBlob_ReturnsFalse()
    {
        ProspectBlobVerifier.VerifyHash(new ProspectBlobModel()).Should().BeFalse();
    }
}
