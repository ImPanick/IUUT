using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// The compressed world-state blob inside a <c>Prospects\&lt;name&gt;.json</c> file
/// (field guide §8, master §8.9). <see cref="BinaryBlob"/> is base64(zlib(FProperty))
/// and <see cref="Hash"/> is the SHA-1 of the <em>uncompressed</em> bytes.
/// Named <c>...Model</c> to avoid clashing with the <c>IUUT.Core.ProspectBlob</c> namespace.
/// </summary>
public sealed class ProspectBlobModel
{
    /// <summary>Recorder key, e.g. <c>actors</c>.</summary>
    public string Key { get; set; } = "";

    /// <summary>SHA-1 (hex) of the uncompressed bytes; the game's tamper/partial-write check.</summary>
    public string Hash { get; set; } = "";

    /// <summary>Compressed length.</summary>
    public long TotalLength { get; set; }

    /// <summary>Compressed length (mirror of <see cref="TotalLength"/> in observed saves).</summary>
    public long DataLength { get; set; }

    /// <summary>Uncompressed length.</summary>
    public long UncompressedLength { get; set; }

    /// <summary>Base64 of the zlib-wrapped (header <c>78 9C</c> + deflate + Adler-32) FProperty stream.</summary>
    public string BinaryBlob { get; set; } = "";

    /// <summary>Unknown members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
