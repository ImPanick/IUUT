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

    /// <summary>The parent node (null for roots). Used to propagate <see cref="Dirty"/> to ancestors on edit.</summary>
    public UeNode? Parent { get; set; }

    /// <summary>An edited replacement for a <see cref="UeNodeKind.Leaf"/> value; <c>null</c> if unedited.</summary>
    public byte[]? ReplacementValue { get; set; }

    /// <summary>
    /// For a synthetic node not backed by the original buffer (e.g. a cloned slot inserted into an array),
    /// its fully-formed bytes. When set, the node is emitted verbatim from these bytes.
    /// </summary>
    public byte[]? RawBytes { get; set; }

    /// <summary>Whether this node needs header re-emission (set on edit and propagated to ancestors).</summary>
    public bool Dirty { get; set; }

    // --- StructArray inner-element tag fields (captured so the count/size preamble can be rebuilt on edit) ---

    /// <summary>For a <see cref="UeNodeKind.StructArray"/>, the inner element tag's name.</summary>
    public string? ArrayInnerName { get; set; }

    /// <summary>For a <see cref="UeNodeKind.StructArray"/>, the inner element struct name.</summary>
    public string? ArrayInnerStructName { get; set; }

    /// <summary>For a <see cref="UeNodeKind.StructArray"/>, the inner element tag's 16-byte struct Guid.</summary>
    public byte[]? ArrayInnerGuid { get; set; }

    /// <summary>For a <see cref="UeNodeKind.StructArray"/>, the inner element tag's <c>HasGuid</c> byte.</summary>
    public byte ArrayInnerHasGuid { get; set; }

    /// <summary>For a <see cref="UeNodeKind.StructArray"/>, the inner element tag's array index.</summary>
    public int ArrayInnerArrayIndex { get; set; }

    /// <summary>Marks this node and all its ancestors dirty so their headers/sizes are recomputed on serialize.</summary>
    public void MarkDirty()
    {
        for (var n = this; n is not null; n = n.Parent)
        {
            n.Dirty = true;
        }
    }

    /// <summary>
    /// Deep-copies this node and its descendants (sharing the owning blob's byte buffer via the same
    /// offsets, so unedited parts still reconstruct from the original bytes). Used to duplicate an
    /// inventory slot before patching it. <see cref="Parent"/> is left null for the clone root.
    /// </summary>
    public UeNode DeepClone()
    {
        var clone = new UeNode
        {
            Name = Name,
            Type = Type,
            ArrayIndex = ArrayIndex,
            StructName = StructName,
            StructGuid = StructGuid?.ToArray(),
            InnerType = InnerType,
            EnumName = EnumName,
            BoolValue = BoolValue,
            MapKeyType = MapKeyType,
            MapValueType = MapValueType,
            SetElementType = SetElementType,
            HasGuid = HasGuid,
            PropertyGuid = PropertyGuid?.ToArray(),
            Kind = Kind,
            TagStart = TagStart,
            TagEnd = TagEnd,
            ValueStart = ValueStart,
            ContentStart = ContentStart,
            ContentEnd = ContentEnd,
            ValueEnd = ValueEnd,
            ReplacementValue = ReplacementValue?.ToArray(),
            RawBytes = RawBytes?.ToArray(),
            Dirty = Dirty,
            ArrayInnerName = ArrayInnerName,
            ArrayInnerStructName = ArrayInnerStructName,
            ArrayInnerGuid = ArrayInnerGuid?.ToArray(),
            ArrayInnerHasGuid = ArrayInnerHasGuid,
            ArrayInnerArrayIndex = ArrayInnerArrayIndex,
        };

        foreach (var child in Children)
        {
            var childClone = child.DeepClone();
            childClone.Parent = clone;
            clone.Children.Add(childClone);
        }

        return clone;
    }
}
