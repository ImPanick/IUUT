using System.Text;

namespace IUUT.Core.Prospects.World;

/// <summary>
/// A read-only recursive-descent reader for Unreal Engine 4 tagged-property streams — the format of
/// the decompressed prospect world blob (<c>StateRecorderBlobs</c>; see docs/DATA-PROVENANCE.md).
/// Each property is an <c>FPropertyTag</c>: <c>FString Name, FString Type, int32 Size, int32 ArrayIndex,
/// [type metadata], byte HasGuid, [Guid], value[Size]</c>. The reader navigates into struct and
/// struct-array payloads (where the item/inventory data lives) and uses the tag's <c>Size</c> to skip
/// everything else, so it stays byte-synced even across UE native structs it doesn't interpret.
/// </summary>
/// <remarks>
/// This is a deliberately defensive <em>inspection</em> reader for the spike: it never throws on
/// malformed input (it bails a subtree and resyncs via <c>Size</c>), and it does not attempt to read
/// native (non-tagged) structs like <c>Transform</c>/<c>Vector</c> as properties.
/// </remarks>
public static class UePropertyReader
{
    private const int MaxDepth = 96;

    /// <summary>UE native (non-tagged) structs serialized as raw bytes — never recursed into.</summary>
    private static readonly HashSet<string> _nativeStructs = new(StringComparer.Ordinal)
    {
        "Guid", "Vector", "Vector2D", "Vector4", "Rotator", "Quat", "Transform", "Box", "Box2D",
        "IntPoint", "IntVector", "Color", "LinearColor", "DateTime", "Timespan", "Matrix", "Plane",
        "TwoVectors", "RandomStream", "FrameNumber", "SoftObjectPath", "PrimaryAssetType",
        "PrimaryAssetId", "FloatRange", "Int32Range", "FloatRangeBound", "Int32RangeBound",
    };

    /// <summary>Parses the top-level tagged-property stream of a decompressed prospect world blob.</summary>
    public static IReadOnlyList<UeProperty> ReadStream(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var pos = 0;
        return ReadPropertyList(data, ref pos, data.Length, 0);
    }

    /// <summary>
    /// Reads an <c>FString</c> at <paramref name="pos"/> (advancing it): <c>int32</c> length then the
    /// chars incl. null terminator — positive length is ASCII/Latin-1, negative is UTF-16LE. Returns
    /// <c>null</c> (without advancing past a sane point) when the length is implausible.
    /// </summary>
    public static string? ReadFString(byte[] d, ref int pos)
    {
        ArgumentNullException.ThrowIfNull(d);
        if (pos < 0 || pos + 4 > d.Length)
        {
            return null;
        }

        var len = BitConverter.ToInt32(d, pos);
        pos += 4;
        switch (len)
        {
            case 0:
                return string.Empty;
            case > 0 when len <= 0x10000 && pos + len <= d.Length:
                {
                    var s = Encoding.Latin1.GetString(d, pos, len - 1); // drop null terminator
                    pos += len;
                    return s;
                }
            case < 0 when len >= -0x10000 && pos + (-len * 2) <= d.Length:
                {
                    var units = -len;
                    var s = Encoding.Unicode.GetString(d, pos, (units - 1) * 2); // drop null terminator
                    pos += units * 2;
                    return s;
                }
            default:
                pos -= 4; // implausible — rewind so the caller can treat it as a boundary
                return null;
        }
    }

    private static List<UeProperty> ReadPropertyList(byte[] d, ref int pos, int end, int depth)
    {
        var result = new List<UeProperty>();
        if (depth > MaxDepth)
        {
            return result;
        }

        while (pos < end)
        {
            var tagStart = pos;
            var name = ReadFString(d, ref pos);
            if (string.IsNullOrEmpty(name) || string.Equals(name, "None", StringComparison.Ordinal))
            {
                break; // "None" (or an unreadable name) terminates a property list
            }

            var type = ReadFString(d, ref pos);
            if (string.IsNullOrEmpty(type) || !type.EndsWith("Property", StringComparison.Ordinal) || pos + 8 > end)
            {
                pos = tagStart; // not a real tag — let the parent resync via its Size
                break;
            }

            var size = BitConverter.ToInt32(d, pos);
            pos += 4;
            pos += 4; // ArrayIndex (unused)

            string? structName = null;
            string? innerType = null;
            switch (type)
            {
                case "StructProperty":
                    structName = ReadFString(d, ref pos);
                    pos += 16; // struct Guid
                    break;
                case "ArrayProperty":
                    innerType = ReadFString(d, ref pos);
                    break;
                case "ByteProperty":
                case "EnumProperty":
                    ReadFString(d, ref pos); // enum name
                    break;
                case "BoolProperty":
                    pos += 1; // bool value lives in the tag
                    break;
                case "MapProperty":
                    ReadFString(d, ref pos); // key type
                    ReadFString(d, ref pos); // value type
                    break;
                case "SetProperty":
                    ReadFString(d, ref pos); // element type
                    break;
            }

            pos += 1; // HasPropertyGuid byte
            // (we do not consume an optional property-level Guid; none observed in these blobs)

            if (size < 0 || pos + size > end)
            {
                pos = tagStart;
                break; // corrupt/over-long — resync at the parent
            }

            var valueOffset = pos;
            var valueEnd = pos + size;
            IReadOnlyList<UeProperty> children = Array.Empty<UeProperty>();

            if (string.Equals(type, "ArrayProperty", StringComparison.Ordinal) &&
                string.Equals(innerType, "StructProperty", StringComparison.Ordinal))
            {
                children = ReadStructArray(d, valueOffset, valueEnd, depth + 1);
            }
            else if (string.Equals(type, "ArrayProperty", StringComparison.Ordinal) &&
                     string.Equals(innerType, "ByteProperty", StringComparison.Ordinal))
            {
                children = ReadNestedByteStream(d, valueOffset, valueEnd, depth + 1);
            }
            else if (string.Equals(type, "StructProperty", StringComparison.Ordinal) &&
                     ShouldRecurseStruct(structName, d, valueOffset, valueEnd))
            {
                var inner = valueOffset;
                children = ReadPropertyList(d, ref inner, valueEnd, depth + 1);
            }

            result.Add(new UeProperty
            {
                Name = name,
                Type = type,
                StructName = structName,
                InnerType = innerType,
                ValueOffset = valueOffset,
                ValueSize = size,
                Children = children,
            });

            pos = valueEnd; // ALWAYS advance by the declared size — keeps us byte-synced
        }

        return result;
    }

    /// <summary>
    /// Reads a <c>StructProperty</c> array value: <c>int32 Count</c>, one inner element tag, then
    /// <c>Count</c> tagged-property structs (each terminated by <c>None</c>).
    /// </summary>
    private static List<UeProperty> ReadStructArray(byte[] d, int start, int end, int depth)
    {
        var result = new List<UeProperty>();
        if (depth > MaxDepth || start + 4 > end)
        {
            return result;
        }

        var pos = start;
        var count = BitConverter.ToInt32(d, pos);
        pos += 4;
        if (count < 0 || count > 1_000_000)
        {
            return result;
        }

        // Inner element FPropertyTag: Name, Type(=StructProperty), Size, ArrayIndex, StructName, Guid, HasGuid.
        var name = ReadFString(d, ref pos);
        var type = ReadFString(d, ref pos);
        if (string.IsNullOrEmpty(name) || !string.Equals(type, "StructProperty", StringComparison.Ordinal) || pos + 8 > end)
        {
            return result;
        }

        pos += 4; // total element-data size
        pos += 4; // ArrayIndex
        var elementStruct = ReadFString(d, ref pos); // element struct name (e.g. InventorySlotSaveData)
        pos += 16; // struct Guid
        pos += 1; // HasGuid

        for (var i = 0; i < count && pos < end; i++)
        {
            var elementStart = pos;
            var element = ReadPropertyList(d, ref pos, end, depth + 1);
            result.Add(new UeProperty
            {
                Name = name,
                Type = "StructProperty",
                StructName = elementStruct,
                ValueOffset = elementStart,
                ValueSize = pos - elementStart,
                Children = element,
            });
        }

        return result;
    }

    /// <summary>
    /// Recurses into a <c>TArray&lt;uint8&gt;</c> value (<c>ArrayProperty</c> of <c>ByteProperty</c>) whose
    /// raw bytes are themselves a serialized tagged-property stream — the Icarus recorder pattern, where
    /// each actor's <c>BinaryData</c> holds the component save-state (incl. <c>SavedInventories</c>).
    /// The value is <c>int32 Count</c> then <c>Count</c> raw bytes; recurses only if those bytes peek as
    /// a tagged-property stream (otherwise it's genuine binary and is left alone).
    /// </summary>
    private static IReadOnlyList<UeProperty> ReadNestedByteStream(byte[] d, int start, int end, int depth)
    {
        if (start + 4 > end)
        {
            return Array.Empty<UeProperty>();
        }

        var count = BitConverter.ToInt32(d, start);
        var innerStart = start + 4;
        var innerEnd = count >= 0 ? Math.Min(end, innerStart + count) : end;
        if (innerStart >= innerEnd || !LooksLikeTaggedProperty(d, innerStart, innerEnd))
        {
            return Array.Empty<UeProperty>();
        }

        var pos = innerStart;
        return ReadPropertyList(d, ref pos, innerEnd, depth);
    }

    /// <summary>
    /// Whether a struct payload is a tagged-property struct we can safely descend into. Known UE
    /// native structs are skipped; otherwise the bytes are peeked (see <see cref="LooksLikeTaggedProperty"/>).
    /// </summary>
    private static bool ShouldRecurseStruct(string? structName, byte[] d, int start, int end)
    {
        if (structName is not null && _nativeStructs.Contains(structName))
        {
            return false;
        }

        return LooksLikeTaggedProperty(d, start, end);
    }

    /// <summary>
    /// Peeks whether the bytes at <paramref name="start"/> begin a tagged-property stream: an empty
    /// struct (<c>None</c>), or a <c>Name</c> <c>FString</c> followed by a <c>*Property</c> type <c>FString</c>.
    /// </summary>
    private static bool LooksLikeTaggedProperty(byte[] d, int start, int end)
    {
        var pos = start;
        var first = ReadFString(d, ref pos);
        if (string.Equals(first, "None", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.IsNullOrEmpty(first) || pos >= end)
        {
            return false;
        }

        var second = ReadFString(d, ref pos);
        return !string.IsNullOrEmpty(second) && second.EndsWith("Property", StringComparison.Ordinal);
    }
}
