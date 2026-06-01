using System.Text;

namespace IUUT.Core.Tests.TestDoubles;

/// <summary>
/// Builds synthetic Unreal Engine 4 tagged-property byte streams for the prospect world-reader tests.
/// This contains NO real save data — it hand-assembles the exact on-disk shapes the reader parses
/// (leaf properties, structs, struct arrays, and the recorder <c>BinaryData</c> nested byte stream),
/// so the parser is verified deterministically without shipping any player's prospect blob.
/// </summary>
internal static class UeFixtureBuilder
{
    /// <summary>Serializes a UE <c>FString</c>: int32 length (incl. null) then Latin-1 bytes + null terminator.</summary>
    public static byte[] FString(string s)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        WriteFString(w, s);
        w.Flush();
        return ms.ToArray();
    }

    /// <summary>Writes a UE <c>FString</c> to <paramref name="w"/>.</summary>
    public static void WriteFString(BinaryWriter w, string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            w.Write(0);
            return;
        }

        var bytes = Encoding.Latin1.GetBytes(s);
        w.Write(bytes.Length + 1);
        w.Write(bytes);
        w.Write((byte)0);
    }

    /// <summary>A <c>NameProperty</c> whose value is an <c>FName</c> string.</summary>
    public static byte[] NameProp(string name, string value) => Tag(name, "NameProperty", FString(value), meta: null);

    /// <summary>A <c>StrProperty</c> whose value is an <c>FString</c>.</summary>
    public static byte[] StrProp(string name, string value) => Tag(name, "StrProperty", FString(value), meta: null);

    /// <summary>An <c>IntProperty</c>.</summary>
    public static byte[] IntProp(string name, int value) => Tag(name, "IntProperty", BitConverter.GetBytes(value), meta: null);

    /// <summary>A <c>StructProperty</c> whose payload is the given child property tags + a <c>None</c> terminator.</summary>
    public static byte[] StructProp(string name, string structName, params byte[][] childProps)
    {
        var payload = Concat(Concat(childProps), FString("None"));
        var meta = Concat(FString(structName), new byte[16]); // struct name + struct Guid
        return Tag(name, "StructProperty", payload, meta);
    }

    /// <summary>
    /// An <c>ArrayProperty</c> of <c>StructProperty</c>: int32 count, one inner element tag, then each
    /// element's property list (each terminated by <c>None</c>). The element struct name is recorded.
    /// </summary>
    public static byte[] StructArrayProp(string name, string elementStruct, IReadOnlyList<byte[]> elements)
    {
        var elementsBytes = Concat(elements.Select(e => Concat(e, FString("None"))).ToArray());

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(elements.Count);
        WriteFString(w, name);            // inner tag name
        WriteFString(w, "StructProperty");
        w.Write(elementsBytes.Length);    // total element-data size
        w.Write(0);                       // arrayIndex
        WriteFString(w, elementStruct);   // element struct name
        w.Write(new byte[16]);            // struct Guid
        w.Write((byte)0);                 // HasGuid
        w.Write(elementsBytes);
        w.Flush();

        return Tag(name, "ArrayProperty", ms.ToArray(), FString("StructProperty"));
    }

    /// <summary>
    /// An <c>ArrayProperty</c> of <c>ByteProperty</c> (a <c>TArray&lt;uint8&gt;</c>) whose raw bytes are a
    /// nested tagged-property stream — the recorder <c>BinaryData</c> pattern. Value = int32 count + bytes.
    /// </summary>
    public static byte[] ByteStreamProp(string name, params byte[][] innerProps)
    {
        var inner = Concat(Concat(innerProps), FString("None"));

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(inner.Length); // byte count
        w.Write(inner);
        w.Flush();

        return Tag(name, "ArrayProperty", ms.ToArray(), FString("ByteProperty"));
    }

    /// <summary>Concatenates byte arrays.</summary>
    public static byte[] Concat(params byte[][] parts)
    {
        using var ms = new MemoryStream();
        foreach (var p in parts)
        {
            ms.Write(p, 0, p.Length);
        }

        return ms.ToArray();
    }

    private static byte[] Tag(string name, string type, byte[] value, byte[]? meta)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        WriteFString(w, name);
        WriteFString(w, type);
        w.Write(value.Length); // Size (int32)
        w.Write(0);            // ArrayIndex (int32)
        if (meta is not null)
        {
            w.Write(meta);
        }

        w.Write((byte)0); // HasPropertyGuid
        w.Write(value);
        w.Flush();
        return ms.ToArray();
    }
}
