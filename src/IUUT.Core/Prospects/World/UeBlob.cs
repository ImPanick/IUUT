using System.Text;

namespace IUUT.Core.Prospects.World;

/// <summary>
/// A full-fidelity, write-capable parse of a decompressed prospect world blob (UE4 tagged properties).
/// <see cref="Parse"/> builds a <see cref="UeNode"/> tree that <see cref="Serialize"/> can reconstruct
/// byte-for-byte; <c>Serialize(forceReconstruct: true)</c> rebuilds every header from parsed fields and
/// is the lossless round-trip gate that must equal the input before any edit is attempted. Edits mark
/// nodes <see cref="UeNode.Dirty"/>; only dirty nodes are re-emitted (headers + sizes recomputed),
/// clean nodes copy their original bytes.
/// </summary>
public sealed class UeBlob
{
    private const int MaxDepth = 128;

    private static readonly HashSet<string> NativeStructs = new(StringComparer.Ordinal)
    {
        "Guid", "Vector", "Vector2D", "Vector4", "Rotator", "Quat", "Transform", "Box", "Box2D",
        "IntPoint", "IntVector", "Color", "LinearColor", "DateTime", "Timespan", "Matrix", "Plane",
        "TwoVectors", "RandomStream", "FrameNumber", "SoftObjectPath", "PrimaryAssetType",
        "PrimaryAssetId", "FloatRange", "Int32Range", "FloatRangeBound", "Int32RangeBound",
    };

    private UeBlob(byte[] data, List<UeNode> roots, int rootsEnd)
    {
        Data = data;
        Roots = roots;
        RootsEnd = rootsEnd;
    }

    /// <summary>The original decompressed bytes (the reconstruction's source of truth for unchanged spans).</summary>
    public byte[] Data { get; }

    /// <summary>The top-level properties (the <c>StateRecorderBlobs</c> array and any siblings).</summary>
    public List<UeNode> Roots { get; }

    /// <summary>Offset after the last top-level property — everything past it is emitted verbatim as the document tail.</summary>
    public int RootsEnd { get; }

    /// <summary>Parses a decompressed prospect world blob into a write-capable tree.</summary>
    public static UeBlob Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var pos = 0;
        var roots = ParseList(data, ref pos, data.Length, 0);
        foreach (var root in roots)
        {
            SetParents(root);
        }

        return new UeBlob(data, roots, pos);
    }

    private static void SetParents(UeNode node)
    {
        foreach (var child in node.Children)
        {
            child.Parent = node;
            SetParents(child);
        }
    }

    /// <summary>
    /// Reconstructs the blob bytes. With <paramref name="forceReconstruct"/> every node's header is
    /// rebuilt from its parsed fields (the round-trip gate); otherwise only <see cref="UeNode.Dirty"/>
    /// nodes are rebuilt and clean nodes copy their original bytes.
    /// </summary>
    public byte[] Serialize(bool forceReconstruct = false)
    {
        using var ms = new MemoryStream(Data.Length);
        foreach (var root in Roots)
        {
            var bytes = EmitNode(root, forceReconstruct);
            ms.Write(bytes, 0, bytes.Length);
        }

        ms.Write(Data, RootsEnd, Data.Length - RootsEnd);
        return ms.ToArray();
    }

    private byte[] EmitNode(UeNode node, bool force)
    {
        if (node.RawBytes is not null)
        {
            return node.RawBytes; // synthetic node (e.g. a freshly built element): emit verbatim
        }

        if (node.Kind == UeNodeKind.StructElement)
        {
            return EmitValue(node, force); // synthetic framing: a property list, no header of its own
        }

        if (!node.Dirty && !force)
        {
            return Slice(node.TagStart, node.TagEnd);
        }

        var value = EmitValue(node, force);
        var header = EmitHeader(node, value.Length);

        var result = new byte[header.Length + value.Length];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(value, 0, result, header.Length, value.Length);
        return result;
    }

    private byte[] EmitValue(UeNode node, bool force)
    {
        if (node.Kind == UeNodeKind.Leaf)
        {
            return node.ReplacementValue ?? Slice(node.ValueStart, node.ValueEnd);
        }

        using var body = new MemoryStream();
        foreach (var child in node.Children)
        {
            var bytes = EmitNode(child, force);
            body.Write(bytes, 0, bytes.Length);
        }

        var bodyBytes = body.ToArray();
        var tail = Slice(node.ContentEnd, node.ValueEnd); // None terminator / padding

        using var ms = new MemoryStream();
        switch (node.Kind)
        {
            case UeNodeKind.StructArray:
                // value = count + inner tag (preamble) + elements + tail. Rebuild the preamble when dirty
                // so the element count and inner-tag total-size track edits.
                var preamble = node.Dirty
                    ? BuildArrayPreamble(node, bodyBytes.Length)
                    : Slice(node.ValueStart, node.ContentStart);
                ms.Write(preamble, 0, preamble.Length);
                ms.Write(bodyBytes, 0, bodyBytes.Length);
                ms.Write(tail, 0, tail.Length);
                break;

            case UeNodeKind.ByteStream:
                // value = int32(byteCount) + content; content = elements + tail (the nested None).
                var contentLength = bodyBytes.Length + tail.Length;
                if (node.Dirty)
                {
                    WriteInt32(ms, contentLength);
                }
                else
                {
                    ms.Write(Data, node.ValueStart, node.ContentStart - node.ValueStart);
                }

                ms.Write(bodyBytes, 0, bodyBytes.Length);
                ms.Write(tail, 0, tail.Length);
                break;

            default: // Struct / StructElement — preamble is empty
                ms.Write(Data, node.ValueStart, node.ContentStart - node.ValueStart);
                ms.Write(bodyBytes, 0, bodyBytes.Length);
                ms.Write(tail, 0, tail.Length);
                break;
        }

        return ms.ToArray();
    }

    private static byte[] BuildArrayPreamble(UeNode node, int totalElementSize)
    {
        using var ms = new MemoryStream();
        WriteInt32(ms, node.Children.Count);
        WriteFString(ms, node.ArrayInnerName ?? node.Name);
        WriteFString(ms, "StructProperty");
        WriteInt32(ms, totalElementSize);
        WriteInt32(ms, node.ArrayInnerArrayIndex);
        WriteFString(ms, node.ArrayInnerStructName ?? "");
        ms.Write(node.ArrayInnerGuid ?? new byte[16], 0, 16);
        ms.WriteByte(node.ArrayInnerHasGuid);
        return ms.ToArray();
    }

    private static byte[] EmitHeader(UeNode node, int valueLength)
    {
        using var ms = new MemoryStream();
        WriteFString(ms, node.Name);
        WriteFString(ms, node.Type);
        WriteInt32(ms, valueLength);
        WriteInt32(ms, node.ArrayIndex);

        switch (node.Type)
        {
            case "StructProperty":
                WriteFString(ms, node.StructName ?? "");
                ms.Write(node.StructGuid ?? new byte[16], 0, 16);
                break;
            case "ArrayProperty":
                WriteFString(ms, node.InnerType ?? "");
                break;
            case "ByteProperty":
            case "EnumProperty":
                WriteFString(ms, node.EnumName ?? "");
                break;
            case "BoolProperty":
                ms.WriteByte(node.BoolValue ?? 0);
                break;
            case "MapProperty":
                WriteFString(ms, node.MapKeyType ?? "");
                WriteFString(ms, node.MapValueType ?? "");
                break;
            case "SetProperty":
                WriteFString(ms, node.SetElementType ?? "");
                break;
        }

        ms.WriteByte(node.HasGuid);
        if (node.HasGuid == 1 && node.PropertyGuid is not null)
        {
            ms.Write(node.PropertyGuid, 0, 16);
        }

        return ms.ToArray();
    }

    private byte[] Slice(int start, int end)
    {
        var len = end - start;
        var buf = new byte[len];
        Buffer.BlockCopy(Data, start, buf, 0, len);
        return buf;
    }

    private static List<UeNode> ParseList(byte[] d, ref int pos, int end, int depth)
    {
        var nodes = new List<UeNode>();
        if (depth > MaxDepth)
        {
            return nodes;
        }

        while (pos < end)
        {
            var tagStart = pos;
            var name = UePropertyReader.ReadFString(d, ref pos);
            if (string.IsNullOrEmpty(name) || string.Equals(name, "None", StringComparison.Ordinal))
            {
                pos = tagStart; // leave "None"/garbage for the parent's tail
                break;
            }

            var type = UePropertyReader.ReadFString(d, ref pos);
            if (string.IsNullOrEmpty(type) || !type.EndsWith("Property", StringComparison.Ordinal) || pos + 8 > end)
            {
                pos = tagStart;
                break;
            }

            var size = BitConverter.ToInt32(d, pos);
            pos += 4;
            var arrayIndex = BitConverter.ToInt32(d, pos);
            pos += 4;

            var node = new UeNode { Name = name, Type = type, ArrayIndex = arrayIndex, TagStart = tagStart };

            switch (type)
            {
                case "StructProperty":
                    node.StructName = UePropertyReader.ReadFString(d, ref pos);
                    node.StructGuid = ReadBytes(d, ref pos, 16);
                    break;
                case "ArrayProperty":
                    node.InnerType = UePropertyReader.ReadFString(d, ref pos);
                    break;
                case "ByteProperty":
                case "EnumProperty":
                    node.EnumName = UePropertyReader.ReadFString(d, ref pos);
                    break;
                case "BoolProperty":
                    node.BoolValue = pos < end ? d[pos] : (byte)0;
                    pos += 1;
                    break;
                case "MapProperty":
                    node.MapKeyType = UePropertyReader.ReadFString(d, ref pos);
                    node.MapValueType = UePropertyReader.ReadFString(d, ref pos);
                    break;
                case "SetProperty":
                    node.SetElementType = UePropertyReader.ReadFString(d, ref pos);
                    break;
            }

            node.HasGuid = pos < end ? d[pos] : (byte)0;
            pos += 1;
            if (node.HasGuid == 1)
            {
                node.PropertyGuid = ReadBytes(d, ref pos, 16);
            }

            var valueStart = pos;
            if (size < 0 || valueStart + size > end)
            {
                pos = tagStart;
                break;
            }

            var valueEnd = valueStart + size;
            node.ValueStart = valueStart;
            node.ValueEnd = valueEnd;
            Classify(d, node, depth);
            node.TagEnd = valueEnd;
            pos = valueEnd;
            nodes.Add(node);
        }

        return nodes;
    }

    private static void Classify(byte[] d, UeNode node, int depth)
    {
        // Default: opaque leaf — value emitted as raw bytes (preamble empty, tail = whole value).
        node.Kind = UeNodeKind.Leaf;
        node.ContentStart = node.ValueStart;
        node.ContentEnd = node.ValueStart;

        if (string.Equals(node.Type, "StructProperty", StringComparison.Ordinal))
        {
            if ((node.StructName is not null && NativeStructs.Contains(node.StructName)) ||
                !LooksLikeTaggedProperty(d, node.ValueStart, node.ValueEnd))
            {
                return; // native/opaque struct
            }

            node.Kind = UeNodeKind.Struct;
            node.ContentStart = node.ValueStart;
            var cp = node.ValueStart;
            node.Children.AddRange(ParseList(d, ref cp, node.ValueEnd, depth + 1));
            node.ContentEnd = cp;
            return;
        }

        if (string.Equals(node.Type, "ArrayProperty", StringComparison.Ordinal) &&
            string.Equals(node.InnerType, "StructProperty", StringComparison.Ordinal))
        {
            ClassifyStructArray(d, node, depth);
            return;
        }

        if (string.Equals(node.Type, "ArrayProperty", StringComparison.Ordinal) &&
            string.Equals(node.InnerType, "ByteProperty", StringComparison.Ordinal))
        {
            ClassifyByteStream(d, node, depth);
        }
    }

    private static void ClassifyStructArray(byte[] d, UeNode node, int depth)
    {
        var cp = node.ValueStart;
        if (cp + 4 > node.ValueEnd)
        {
            return; // stays a leaf
        }

        var count = BitConverter.ToInt32(d, cp);
        cp += 4;
        // Inner element tag: Name, Type(StructProperty), Size, ArrayIndex, StructName, Guid(16), HasGuid.
        var innerName = UePropertyReader.ReadFString(d, ref cp);
        var innerType = UePropertyReader.ReadFString(d, ref cp);
        if (count < 0 || count > 2_000_000 || !string.Equals(innerType, "StructProperty", StringComparison.Ordinal) || cp + 8 > node.ValueEnd)
        {
            return; // not a struct array we can frame — leave as leaf
        }

        cp += 4; // total element-data size (rebuilt from element bytes on edit)
        var innerArrayIndex = BitConverter.ToInt32(d, cp);
        cp += 4;
        var innerStructName = UePropertyReader.ReadFString(d, ref cp);
        var innerGuid = ReadBytes(d, ref cp, 16);
        var innerHasGuid = cp < node.ValueEnd ? d[cp] : (byte)0;
        cp += 1;

        node.Kind = UeNodeKind.StructArray;
        node.ArrayInnerName = innerName;
        node.ArrayInnerStructName = innerStructName;
        node.ArrayInnerGuid = innerGuid;
        node.ArrayInnerHasGuid = innerHasGuid;
        node.ArrayInnerArrayIndex = innerArrayIndex;
        node.ContentStart = cp;
        for (var i = 0; i < count && cp < node.ValueEnd; i++)
        {
            var elemStart = cp;
            var props = ParseList(d, ref cp, node.ValueEnd, depth + 1);
            ConsumeNone(d, ref cp); // each struct element ends with a None terminator
            node.Children.Add(new UeNode
            {
                Kind = UeNodeKind.StructElement,
                ValueStart = elemStart,
                ContentStart = elemStart,
                ContentEnd = cp - NoneLength(d, cp),
                ValueEnd = cp,
                TagStart = elemStart,
                TagEnd = cp,
            }.WithChildren(props));
        }

        node.ContentEnd = cp;
    }

    private static void ClassifyByteStream(byte[] d, UeNode node, int depth)
    {
        var cp = node.ValueStart;
        if (cp + 4 > node.ValueEnd)
        {
            return;
        }

        var byteCount = BitConverter.ToInt32(d, cp);
        cp += 4;
        var innerStart = cp;
        var innerEnd = byteCount >= 0 ? Math.Min(node.ValueEnd, innerStart + byteCount) : node.ValueEnd;
        if (innerStart >= innerEnd || !LooksLikeTaggedProperty(d, innerStart, innerEnd))
        {
            return; // genuine binary — leave as leaf
        }

        node.Kind = UeNodeKind.ByteStream;
        node.ContentStart = innerStart;
        var icp = innerStart;
        node.Children.AddRange(ParseList(d, ref icp, innerEnd, depth + 1));
        node.ContentEnd = icp;
    }

    private static void ConsumeNone(byte[] d, ref int pos)
    {
        var save = pos;
        var s = UePropertyReader.ReadFString(d, ref pos);
        if (!string.Equals(s, "None", StringComparison.Ordinal))
        {
            pos = save; // not a None here; leave position (its bytes fall into the element tail)
        }
    }

    private static int NoneLength(byte[] d, int afterPos)
    {
        // Length of the "None" FString immediately preceding afterPos (used to split element content/tail).
        // "None" is 5 chars incl. null => int32(5) + 5 bytes = 9.
        var probe = afterPos - 9;
        if (probe >= 0)
        {
            var p = probe;
            if (string.Equals(UePropertyReader.ReadFString(d, ref p), "None", StringComparison.Ordinal) && p == afterPos)
            {
                return 9;
            }
        }

        return 0;
    }

    private static bool LooksLikeTaggedProperty(byte[] d, int start, int end)
    {
        var pos = start;
        var first = UePropertyReader.ReadFString(d, ref pos);
        if (string.Equals(first, "None", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.IsNullOrEmpty(first) || pos >= end)
        {
            return false;
        }

        var second = UePropertyReader.ReadFString(d, ref pos);
        return !string.IsNullOrEmpty(second) && second.EndsWith("Property", StringComparison.Ordinal);
    }

    private static byte[] ReadBytes(byte[] d, ref int pos, int count)
    {
        var buf = new byte[count];
        if (pos + count <= d.Length)
        {
            Buffer.BlockCopy(d, pos, buf, 0, count);
        }

        pos += count;
        return buf;
    }

    private static void WriteFString(MemoryStream ms, string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            WriteInt32(ms, 0);
            return;
        }

        var bytes = Encoding.Latin1.GetBytes(s);
        WriteInt32(ms, bytes.Length + 1);
        ms.Write(bytes, 0, bytes.Length);
        ms.WriteByte(0);
    }

    private static void WriteInt32(MemoryStream ms, int value)
    {
        Span<byte> b = stackalloc byte[4];
        BitConverter.TryWriteBytes(b, value);
        ms.Write(b);
    }
}

/// <summary>Small fluent helper to attach parsed children to a synthetic element node.</summary>
internal static class UeNodeExtensions
{
    public static UeNode WithChildren(this UeNode node, IEnumerable<UeNode> children)
    {
        node.Children.AddRange(children);
        return node;
    }
}
