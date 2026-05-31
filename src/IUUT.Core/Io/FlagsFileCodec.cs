using System.Text;
using IUUT.Core.Models;

namespace IUUT.Core.Io;

/// <summary>
/// Reads/writes the <c>flags_&lt;SteamID&gt;.dat</c> binary layout (master doc §8.11):
/// <code>
/// u32  length prefix (UE FString length = chars + 1 for the trailing NUL)
/// ...  ASCII SteamID (length-1 chars)
/// u8   NUL terminator
/// u32  flag count N
/// u32[N] flag IDs (all little-endian)
/// </code>
/// A 17-char SteamID with 14 flags is the observed 82-byte file.
/// </summary>
public static class FlagsFileCodec
{
    /// <summary>Decodes the binary flags file into a <see cref="FlagsFileModel"/>.</summary>
    /// <exception cref="InvalidDataException">The bytes are too short or structurally invalid.</exception>
    public static FlagsFileModel Read(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            using var stream = new MemoryStream(data, writable: false);
            using var reader = new BinaryReader(stream, Encoding.ASCII);

            var lengthPrefix = reader.ReadUInt32(); // chars + 1 (trailing NUL)
            if (lengthPrefix < 1)
            {
                throw new InvalidDataException("flags file: length prefix must include the trailing NUL.");
            }

            var charCount = checked((int)lengthPrefix - 1);
            var steamId = Encoding.ASCII.GetString(reader.ReadBytes(charCount));
            _ = reader.ReadByte(); // NUL terminator

            var count = reader.ReadUInt32();
            var flags = new List<uint>(checked((int)count));
            for (var i = 0; i < count; i++)
            {
                flags.Add(reader.ReadUInt32());
            }

            return new FlagsFileModel { SteamId = steamId, Flags = flags };
        }
        catch (EndOfStreamException ex)
        {
            throw new InvalidDataException("flags file: unexpected end of data.", ex);
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException("flags file: declared length is out of range.", ex);
        }
    }

    /// <summary>Encodes a <see cref="FlagsFileModel"/> back to the binary layout.</summary>
    public static byte[] Write(FlagsFileModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrEmpty(model.SteamId);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII);

        var idBytes = Encoding.ASCII.GetBytes(model.SteamId);
        writer.Write((uint)(idBytes.Length + 1)); // FString length includes the trailing NUL
        writer.Write(idBytes);                     // raw bytes, NOT BinaryWriter's length-prefixed string
        writer.Write((byte)0);                     // NUL terminator
        writer.Write((uint)model.Flags.Count);
        foreach (var flag in model.Flags)
        {
            writer.Write(flag);
        }

        writer.Flush();
        return stream.ToArray();
    }
}
