using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Pins the blob hash as an INTEROP FORMAT, not just a value.
/// <para>
/// IUUT shipped this field uppercase while Icarus writes it lowercase. Nothing caught it: the
/// verifier compares case-insensitively, so every round-trip through IUUT looked perfect, and the
/// only observer that cared was the game — which responded to the mismatch by discarding the world
/// and regenerating it. To the player that is every base and every item gone.
/// </para>
/// <para>
/// The lesson these tests encode: when a field is handed back to someone else's parser, asserting
/// the VALUE is not enough. Assert the spelling.
/// </para>
/// </summary>
public class ProspectBlobHashFormatTests
{
    private static byte[] Payload => Encoding.UTF8.GetBytes("prospect world bytes");

    // The game's own spelling, computed independently of the code under test.
    private static string GameStyleHash(byte[] data)
    {
#pragma warning disable CA5350 // Interop with the game's hash format, not security.
        return Convert.ToHexString(SHA1.HashData(data)).ToLowerInvariant();
#pragma warning restore CA5350
    }

    [Fact]
    public void ComputeHash_IsLowercase_LikeEveryHashTheGameWrites()
    {
        var hash = ProspectBlobCodec.ComputeHash(Payload);

        hash.Should().Be(hash.ToLowerInvariant(), "Icarus writes this field lowercase on every prospect");
        hash.Should().NotBe(hash.ToUpperInvariant(), "an uppercase hash is what caused a world to be regenerated");
        hash.Should().HaveLength(40);
    }

    [Fact]
    public void ComputeHash_MatchesTheGamesSpelling_Exactly()
    {
        // Ordinal, not OrdinalIgnoreCase — case-insensitive comparison is precisely what hid the bug.
        ProspectBlobCodec.ComputeHash(Payload).Should().Be(GameStyleHash(Payload));
    }

    [Fact]
    public void SetUncompressed_StampsAHashTheGameWouldAccept()
    {
        var blob = new ProspectBlobModel();

        ProspectBlobCodec.SetUncompressed(blob, Payload);

        blob.Hash.Should().Be(GameStyleHash(Payload));
        blob.UncompressedLength.Should().Be(Payload.Length);
        blob.TotalLength.Should().Be(Convert.FromBase64String(blob.BinaryBlob).Length);
        blob.DataLength.Should().Be(blob.TotalLength);
    }

    [Fact]
    public void SetUncompressed_RoundTripsExactly()
    {
        var blob = new ProspectBlobModel();
        var payload = Enumerable.Range(0, 5000).Select(i => (byte)(i % 251)).ToArray();

        ProspectBlobCodec.SetUncompressed(blob, payload);

        ProspectBlobCodec.Decompress(blob.BinaryBlob).Should().Equal(payload);
    }

    [Fact]
    public void VerifyRoundTrip_RejectsABlobWhoseHashDoesNotMatchItsBytes()
    {
        var blob = new ProspectBlobModel();
        ProspectBlobCodec.SetUncompressed(blob, Payload);

        blob.Hash = GameStyleHash(Encoding.UTF8.GetBytes("different bytes"));

        var verify = () => ProspectBlobCodec.VerifyRoundTrip(blob, Payload);
        verify.Should().Throw<InvalidDataException>().WithMessage("*Nothing was written*");
    }

    [Fact]
    public void VerifyRoundTrip_RejectsTheUppercaseFormThatBrokeTheGame()
    {
        var blob = new ProspectBlobModel();
        ProspectBlobCodec.SetUncompressed(blob, Payload);

        blob.Hash = blob.Hash.ToUpperInvariant();

        var verify = () => ProspectBlobCodec.VerifyRoundTrip(blob, Payload);
        verify.Should().Throw<InvalidDataException>(
            "the game rejects a hash it did not spell that way, and rejection means a wiped world");
    }

    [Fact]
    public void VerifyRoundTrip_RejectsALengthFieldThatDisagreesWithTheBytes()
    {
        var blob = new ProspectBlobModel();
        ProspectBlobCodec.SetUncompressed(blob, Payload);

        blob.UncompressedLength = Payload.Length + 1;

        var verify = () => ProspectBlobCodec.VerifyRoundTrip(blob, Payload);
        verify.Should().Throw<InvalidDataException>();
    }
}
