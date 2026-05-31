namespace IUUT.Core.Models;

/// <summary>
/// <c>flags_&lt;SteamID&gt;.dat</c> — binary engine unlock flags (master doc §8.11), a separate
/// namespace from <c>Profile.UnlockedFlags</c>. Decoded from / encoded to the 82-byte-ish binary
/// layout by <c>FlagsFileCodec</c>.
/// </summary>
public sealed class FlagsFileModel
{
    /// <summary>The 17-char SteamID64 the file belongs to (stored as a UE FString with a trailing NUL).</summary>
    public required string SteamId { get; set; }

    /// <summary>The engine unlock flag IDs (little-endian u32 each on disk).</summary>
    public List<uint> Flags { get; set; } = [];
}
