namespace IUUT.Core.Prospects.World;

/// <summary>The structural kind of a <see cref="UeNode"/> — how its value is framed.</summary>
public enum UeNodeKind
{
    /// <summary>A scalar/opaque value (Int, Name, Str, Bool, a UE native struct, …) — value is raw bytes.</summary>
    Leaf,

    /// <summary>A <c>StructProperty</c> whose value is a child property list (+ <c>None</c> terminator).</summary>
    Struct,

    /// <summary>An <c>ArrayProperty</c> of <c>StructProperty</c>: count + inner tag (preamble) then element lists.</summary>
    StructArray,

    /// <summary>An <c>ArrayProperty</c> of <c>ByteProperty</c> whose bytes are a nested property stream.</summary>
    ByteStream,

    /// <summary>A synthetic node for one element of a struct array (a property list, no tag/header of its own).</summary>
    StructElement,
}

/// <summary>
/// One node of a full-fidelity Unreal Engine tagged-property tree (the write-capable model behind the
/// prospect world editor). Unlike the read-only <see cref="UeProperty"/>, this captures every header
/// field and frames its value as <c>preamble (raw) + children + tail (raw)</c> so the tree round-trips
/// byte-for-byte: only the header is rebuilt from fields, everything else bottoms out at original bytes.
/// </summary>
/// <remarks>
/// Offsets are into the owning <see cref="UeBlob.Data"/> buffer. <see cref="ReplacementValue"/> holds an
/// edited <see cref="UeNodeKind.Leaf"/> value; <see cref="Dirty"/> marks the node (and is propagated to
/// ancestors) when an edit requires header re-emission.
/// </remarks>
public sealed class UeNode
{
    /// <summary>Property name (the tag's leading <c>FString</c>). Empty for <see cref="UeNodeKind.StructElement"/>.</summary>
    public string Name { get; set; } = "";

    /// <summary>UE property type, e.g. <c>StructProperty</c>. Empty for <see cref="UeNodeKind.StructElement"/>.</summary>
    public string Type { get; set; } = "";

    /// <summary>The tag's <c>ArrayIndex</c> field (almost always 0).</summary>
    public int ArrayIndex { get; set; }

    /// <summary>For a <c>StructProperty</c>, the struct name; else <c>null</c>.</summary>
    public string? StructName { get; set; }

    /// <summary>For a <c>StructProperty</c>, the 16-byte struct Guid; else <c>null</c>.</summary>
    public byte[]? StructGuid { get; set; }

    /// <summary>For an <c>ArrayProperty</c>, the element type (e.g. <c>StructProperty</c>/<c>ByteProperty</c>); else <c>null</c>.</summary>
    public string? InnerType { get; set; }

    /// <summary>For <c>ByteProperty</c>/<c>EnumProperty</c>, the enum name; else <c>null</c>.</summary>
    public string? EnumName { get; set; }

    /// <summary>For a <c>BoolProperty</c>, the in-tag boolean byte; else <c>null</c>.</summary>
    public byte? BoolValue { get; set; }

    /// <summary>For a <c>MapProperty</c>, the key type; else <c>null</c>.</summary>
    public string? MapKeyType { get; set; }

    /// <summary>For a <c>MapProperty</c>, the value type; else <c>null</c>.</summary>
    public string? MapValueType { get; set; }

    /// <summary>For a <c>SetProperty</c>, the element type; else <c>null</c>.</summary>
    public string? SetElementType { get; set; }

    /// <summary>The <c>HasPropertyGuid</c> byte (0 or 1).</summary>
    public byte HasGuid { get; set; }

    /// <summary>The optional 16-byte property Guid present when <see cref="HasGuid"/> is 1; else <c>null</c>.</summary>
    public byte[]? PropertyGuid { get; set; }

    /// <summary>The structural kind of this node.</summary>
    public UeNodeKind Kind { get; set; }

    /// <summary>Start offset of the whole tag (header+value) in <see cref="UeBlob.Data"/>.</summary>
    public int TagStart { get; set; }

    /// <summary>End offset (exclusive) of the whole tag in <see cref="UeBlob.Data"/>.</summary>
    public int TagEnd { get; set; }

    /// <summary>Start offset of this node's value in <see cref="UeBlob.Data"/>.</summary>
    public int ValueStart { get; set; }

    /// <summary>Offset where child content begins (after any count/inner-tag preamble).</summary>
    public int ContentStart { get; set; }

    /// <summary>Offset where child content ends (before the trailing <c>None</c>/padding tail).</summary>
    public int ContentEnd { get; set; }

    /// <summary>End offset (exclusive) of this node's value in <see cref="UeBlob.Data"/>.</summary>
    public int ValueEnd { get; set; }

    /// <summary>Parsed children (for containers); empty for <see cref="UeNodeKind.Leaf"/>.</summary>
    public List<UeNode> Children { get; } = new();

    /// <summary>An edited replacement for a <see cref="UeNodeKind.Leaf"/> value; <c>null</c> if unedited.</summary>
    public byte[]? ReplacementValue { get; set; }

    /// <summary>Whether this node needs header re-emission (set on edit and propagated to ancestors).</summary>
    public bool Dirty { get; set; }
}
