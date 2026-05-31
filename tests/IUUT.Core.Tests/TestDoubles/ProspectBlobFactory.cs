using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace IUUT.Core.Tests.TestDoubles;

/// <summary>
/// Builds valid synthetic prospect blobs (zlib-wrapped, base64) and prospect JSON for
/// codec/health tests — no real save data. Mirrors the game's encoding so
/// <c>ProspectBlobVerifier</c> can be exercised end to end.
/// </summary>
internal static class ProspectBlobFactory
{
    /// <summary>Returns (base64 zlib stream, uppercase-hex SHA-1 of the uncompressed payload).</summary>
    public static (string Base64, string Hash) Build(byte[] payload)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(payload, 0, payload.Length);
        }

        var base64 = Convert.ToBase64String(output.ToArray());

        // SHA-1 only to reproduce the game's blob-integrity hash format (interop, not security).
#pragma warning disable CA5350
        var hash = Convert.ToHexString(SHA1.HashData(payload));
#pragma warning restore CA5350

        return (base64, hash);
    }

    /// <summary>Builds a full prospect-file JSON with a (optionally hash-overridden) blob.</summary>
    public static string ProspectJson(byte[] payload, string? hashOverride = null)
    {
        var (base64, hash) = Build(payload);
        var doc = new
        {
            ProspectInfo = new { ProspectID = "Example", ProspectState = "Active" },
            ProspectBlob = new
            {
                Key = "actors",
                Hash = hashOverride ?? hash,
                TotalLength = 0,
                DataLength = 0,
                UncompressedLength = payload.Length,
                BinaryBlob = base64,
            },
        };

        return JsonSerializer.Serialize(doc);
    }
}
