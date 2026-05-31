using System.IO.Compression;
using System.Security.Cryptography;
using IUUT.Core.Models;

namespace IUUT.Core.ProspectBlob;

/// <summary>
/// The full prospect world-blob codec (field guide §8.1, master §8.9) — the write side that
/// <see cref="ProspectBlobVerifier"/> deferred (WP-28). Decompresses, recompresses, and re-stamps a
/// <see cref="ProspectBlobModel"/> after the uncompressed FProperty bytes are mutated.
/// </summary>
/// <remarks>
/// Recompression uses <see cref="ZLibStream"/>, which writes a complete zlib stream — the
/// <c>78 xx</c> header, raw deflate, and the big-endian Adler-32 trailer of the uncompressed bytes —
/// in one pass. This is the documented approach: hand-stitching <see cref="DeflateStream"/> + manual
/// header bytes drops the Adler-32 trailer and causes silent load-time rejection.
/// </remarks>
public static class ProspectBlobCodec
{
    /// <summary>Decodes a base64 zlib blob to its uncompressed bytes (delegates to the verifier).</summary>
    public static byte[] Decompress(string base64Blob) => ProspectBlobVerifier.Decompress(base64Blob);

    /// <summary>Compresses <paramref name="uncompressed"/> into a complete zlib stream and base64-encodes it.</summary>
    public static string CompressToBase64(byte[] uncompressed) => Convert.ToBase64String(Compress(uncompressed));

    /// <summary>The game's blob hash: uppercase-hex SHA-1 of the <em>uncompressed</em> bytes.</summary>
    public static string ComputeHash(byte[] uncompressed)
    {
        ArgumentNullException.ThrowIfNull(uncompressed);
        // SHA-1 is the game's blob-integrity format (field guide §8.1) — interop, NOT security.
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms — interop with the game's hash.
        return Convert.ToHexString(SHA1.HashData(uncompressed));
#pragma warning restore CA5350
    }

    /// <summary>
    /// Re-encodes <paramref name="blob"/> from new uncompressed bytes: recompresses, then updates
    /// <see cref="ProspectBlobModel.BinaryBlob"/>, <see cref="ProspectBlobModel.Hash"/>,
    /// <see cref="ProspectBlobModel.UncompressedLength"/>, and the compressed-length mirrors
    /// (<see cref="ProspectBlobModel.TotalLength"/> / <see cref="ProspectBlobModel.DataLength"/>) so
    /// the blob passes the game's load-time hash check.
    /// </summary>
    public static void SetUncompressed(ProspectBlobModel blob, byte[] uncompressed)
    {
        ArgumentNullException.ThrowIfNull(blob);
        ArgumentNullException.ThrowIfNull(uncompressed);

        var compressed = Compress(uncompressed);
        blob.BinaryBlob = Convert.ToBase64String(compressed);
        blob.Hash = ComputeHash(uncompressed);
        blob.UncompressedLength = uncompressed.Length;
        blob.TotalLength = compressed.Length;
        blob.DataLength = compressed.Length;
    }

    private static byte[] Compress(byte[] uncompressed)
    {
        ArgumentNullException.ThrowIfNull(uncompressed);

        using var output = new MemoryStream();
        // Dispose the ZLibStream (inner using) BEFORE reading the buffer so the Adler-32 trailer is flushed.
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(uncompressed, 0, uncompressed.Length);
        }

        return output.ToArray();
    }
}
