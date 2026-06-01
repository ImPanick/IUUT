namespace IUUT.Core.Prospects.World;

/// <summary>
/// One node in a parsed Unreal Engine tagged-property tree (an <c>FPropertyTag</c>): the property's
/// name, its UE type (e.g. <c>StructProperty</c>, <c>ArrayProperty</c>, <c>NameProperty</c>), the
/// type-specific metadata (struct/inner type), and the byte range of its serialized value. Container
/// properties (struct / struct-array) carry their parsed <see cref="Children"/>.
/// </summary>
/// <remarks>
/// This is the read-only spike model for the prospect world blob (see docs/DATA-PROVENANCE.md
/// "Prospect world-save anatomy"). It is deliberately a navigation/inspection shape, not a full
/// round-trippable materialization — values are referenced by offset, not copied.
/// </remarks>
public sealed class UeProperty
{
    /// <summary>The property name (the leading <c>FString</c> of the tag), e.g. <c>ItemStaticData</c>.</summary>
    public required string Name { get; init; }

    /// <summary>The UE property type, e.g. <c>StructProperty</c>, <c>ArrayProperty</c>, <c>NameProperty</c>.</summary>
    public required string Type { get; init; }

    /// <summary>For a <c>StructProperty</c>, the concrete struct name (e.g. <c>InventorySlotSaveData</c>); else <c>null</c>.</summary>
    public string? StructName { get; init; }

    /// <summary>For an <c>ArrayProperty</c>, the element type (e.g. <c>StructProperty</c>); else <c>null</c>.</summary>
    public string? InnerType { get; init; }

    /// <summary>Byte offset (into the decompressed stream) where this property's value begins.</summary>
    public int ValueOffset { get; init; }

    /// <summary>Declared value size in bytes (the tag's <c>Size</c> field). Used to skip precisely.</summary>
    public int ValueSize { get; init; }

    /// <summary>Parsed child properties for navigable containers (struct / struct-array); empty otherwise.</summary>
    public IReadOnlyList<UeProperty> Children { get; init; } = Array.Empty<UeProperty>();
}
