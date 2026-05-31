using System.IO.Compression;
using System.Security.Cryptography;
using IUUT.Core.Models;

namespace IUUT.Core.ProspectBlob;

/// <summary>
/// Read-side of the prospect blob codec (field guide §8.1): decode the base64 zlib
/// stream and verify its SHA-1 against <see cref="ProspectBlobModel.Hash"/>. The
/// re-encode side (mutate → recompress → Adler-32 → re-hash) is WP-28.
/// </summary>
public static class ProspectBlobVerifier
{
    /// <summary>
    /// Decodes <paramref name="base64Blob"/> (base64 of a full zlib stream) to the
    /// uncompressed bytes. <see cref="ZLibStream"/> handles the <c>78 9C</c> header and
    /// validates the Adler-32 trailer, so a tampered stream throws <see cref="InvalidDataException"/>.
    /// </summary>
    public static byte[] Decompress(string base64Blob)
    {
        ArgumentException.ThrowIfNullOrEmpty(base64Blob);

        var compressed = Convert.FromBase64String(base64Blob);
        using var input = new MemoryStream(compressed);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// Verifies that the SHA-1 of the decompressed bytes equals <see cref="ProspectBlobModel.Hash"/>
    /// (case-insensitive hex). Returns <c>false</c> for empty input, bad base64, or a
    /// corrupt zlib stream — never throws.
    /// </summary>
    public static bool VerifyHash(ProspectBlobModel blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        if (string.IsNullOrEmpty(blob.BinaryBlob) || string.IsNullOrEmpty(blob.Hash))
        {
            return false;
        }

        try
        {
            var bytes = Decompress(blob.BinaryBlob);
            // SHA-1 is the game's blob-integrity hash format (Icarus-Analysis §8.1), an interop
            // requirement — NOT used for security. Hence the weak-crypto analyzer suppression.
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms — interop with the game's hash.
            var actual = Convert.ToHexString(SHA1.HashData(bytes));
#pragma warning restore CA5350
            return string.Equals(actual, blob.Hash, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false; // not valid base64
        }
        catch (InvalidDataException)
        {
            return false; // not a valid zlib stream / Adler-32 mismatch
        }
    }
}
