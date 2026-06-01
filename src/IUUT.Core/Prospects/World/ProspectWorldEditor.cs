using System.Text;

namespace IUUT.Core.Prospects.World;

/// <summary>
/// Mutating editor for a parsed prospect world blob (<see cref="UeBlob"/>): remove, duplicate, and
/// retype inventory items. Edits mark nodes dirty so <see cref="UeBlob.Serialize(bool)"/> recomputes every
/// affected length prefix (slot-array count, inner-tag size, the recorder <c>BinaryData</c> byte count,
/// and all ancestor struct/array sizes) while untouched bytes are copied verbatim. The result is fed to
/// <c>ProspectBlobCodec.SetUncompressed</c> to recompress and re-stamp the hash.
/// </summary>
/// <remarks>
/// Item identity = the <c>ItemStaticData</c> NameProperty (a <c>D_ItemsStatic</c> RowName). "Duplicate"
/// deep-clones a real slot (so every required field is present) and optionally patches the RowName;
/// "remove" drops a slot element. This is the in-prospect / return-to-stash machinery.
/// </remarks>
public sealed class ProspectWorldEditor
{
    private const string SlotStruct = "InventorySlotSaveData";
    private const string ItemStaticDataProperty = "ItemStaticData";
    private const string DynamicDataProperty = "DynamicData";

    /// <summary>The <c>DynamicData</c> Index for an item's stack count (verified against real saves).</summary>
    public const int StackIndex = 7;

    /// <summary>The <c>DynamicData</c> Index for an item's durability (verified against real saves).</summary>
    public const int DurabilityIndex = 9;

    private const string ComponentClassProperty = "ComponentClassName";

    /// <summary>
    /// A reference to one inventory slot: its containing array, the element node, the item RowName,
    /// stack/durability, and the recorder component class of the owning actor (e.g.
    /// <c>/Script/Icarus.PlayerInventoryRecorderComponent</c>) — used to scope "return to stash" to the
    /// player's items rather than every world container.
    /// </summary>
    public sealed record SlotRef(UeNode Array, UeNode Element, string RowName, int Stack, int? Durability, string? OwnerComponentClass);

    private readonly UeBlob _blob;

    /// <summary>Wraps a parsed world blob for editing.</summary>
    public ProspectWorldEditor(UeBlob blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        _blob = blob;
    }

    /// <summary>The underlying blob (call <see cref="UeBlob.Serialize(bool)"/> to get the edited bytes).</summary>
    public UeBlob Blob => _blob;

    /// <summary>Every filled inventory slot in the world, in document order.</summary>
    public IReadOnlyList<SlotRef> FindItemSlots()
    {
        var slots = new List<SlotRef>();
        foreach (var root in _blob.Roots)
        {
            Collect(root, slots);
        }

        return slots;
    }

    /// <summary>Removes a slot from its inventory (e.g. to free a trapped item before returning it to the stash).</summary>
    public void RemoveSlot(SlotRef slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (slot.Array.Children.Remove(slot.Element))
        {
            slot.Array.MarkDirty();
        }
    }

    /// <summary>
    /// Duplicates a slot (deep-cloning a real one so all fields are valid), optionally changing the item
    /// to <paramref name="newItemRowName"/>. Returns the new slot's reference.
    /// </summary>
    public SlotRef DuplicateSlot(SlotRef slot, string? newItemRowName = null)
    {
        ArgumentNullException.ThrowIfNull(slot);
        var clone = slot.Element.DeepClone();
        clone.Parent = slot.Array;

        var rowName = slot.RowName;
        if (!string.IsNullOrEmpty(newItemRowName))
        {
            SetItemRowName(clone, newItemRowName);
            rowName = newItemRowName;
        }

        slot.Array.Children.Add(clone);
        slot.Array.MarkDirty();
        return new SlotRef(slot.Array, clone, rowName, slot.Stack, slot.Durability, slot.OwnerComponentClass);
    }

    /// <summary>Changes the item in a slot to <paramref name="newItemRowName"/> (a <c>D_ItemsStatic</c> RowName).</summary>
    public void RetypeSlot(SlotRef slot, string newItemRowName)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentException.ThrowIfNullOrEmpty(newItemRowName);
        SetItemRowName(slot.Element, newItemRowName);
    }

    /// <summary>Sets a slot's stack count (the <c>DynamicData</c> Index-7 value). Returns false if the slot has no stack entry.</summary>
    public bool SetStack(SlotRef slot, int stack)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return SetDynamicValue(slot.Element, StackIndex, stack);
    }

    /// <summary>Sets a slot's durability (the <c>DynamicData</c> Index-9 value). Returns false if the slot has no durability entry.</summary>
    public bool SetDurability(SlotRef slot, int durability)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return SetDynamicValue(slot.Element, DurabilityIndex, durability);
    }

    /// <summary>Serializes the edited world to decompressed bytes (feed to <c>ProspectBlobCodec.SetUncompressed</c>).</summary>
    public byte[] Serialize() => _blob.Serialize(false);

    private void Collect(UeNode node, List<SlotRef> slots)
    {
        if (node.Kind == UeNodeKind.StructArray &&
            string.Equals(node.ArrayInnerStructName, SlotStruct, StringComparison.Ordinal))
        {
            foreach (var element in node.Children)
            {
                var rowName = ReadItemRowName(element);
                if (rowName is not null)
                {
                    var stack = ReadDynamicValue(element, StackIndex) ?? 1;
                    var durability = ReadDynamicValue(element, DurabilityIndex);
                    slots.Add(new SlotRef(node, element, rowName, stack, durability, FindOwnerComponentClass(element)));
                }
            }
        }

        foreach (var child in node.Children)
        {
            Collect(child, slots);
        }
    }

    private static UeNode? FindItemStaticData(UeNode element) =>
        element.Children.FirstOrDefault(c =>
            string.Equals(c.Name, ItemStaticDataProperty, StringComparison.Ordinal) &&
            string.Equals(c.Type, "NameProperty", StringComparison.Ordinal));

    /// <summary>
    /// Walks up from a slot to the owning actor record and returns its <c>ComponentClassName</c>
    /// (the recorder component, which identifies whether the inventory is the player's, a deployable
    /// container's, a creature's, etc.). Null if not found.
    /// </summary>
    private string? FindOwnerComponentClass(UeNode slotElement)
    {
        for (var n = slotElement.Parent; n is not null; n = n.Parent)
        {
            var cc = n.Children.FirstOrDefault(c =>
                string.Equals(c.Name, ComponentClassProperty, StringComparison.Ordinal) &&
                string.Equals(c.Type, "StrProperty", StringComparison.Ordinal));
            if (cc is not null)
            {
                var pos = cc.ValueStart;
                return UePropertyReader.ReadFString(_blob.Data, ref pos);
            }
        }

        return null;
    }

    private string? ReadItemRowName(UeNode element)
    {
        var leaf = FindItemStaticData(element);
        if (leaf is null)
        {
            return null;
        }

        if (leaf.ReplacementValue is not null)
        {
            var rp = 0;
            return UePropertyReader.ReadFString(leaf.ReplacementValue, ref rp);
        }

        var pos = leaf.ValueStart;
        return UePropertyReader.ReadFString(_blob.Data, ref pos);
    }

    private static void SetItemRowName(UeNode element, string rowName)
    {
        var leaf = FindItemStaticData(element)
            ?? throw new InvalidOperationException("Slot has no ItemStaticData property to retype.");
        leaf.ReplacementValue = EncodeFString(rowName);
        leaf.MarkDirty();
    }

    /// <summary>
    /// Reads a slot's <c>DynamicData</c> value for the given enum <paramref name="index"/> (7 = stack,
    /// 9 = durability): finds the <c>InventorySlotDynamicData</c> element whose <c>Index</c> equals it and
    /// returns its <c>Value</c>. Returns <c>null</c> when the slot has no such entry.
    /// </summary>
    private int? ReadDynamicValue(UeNode element, int index)
    {
        var valueLeaf = FindDynamicValueLeaf(element, index);
        return valueLeaf is null ? null : ReadInt(valueLeaf);
    }

    private bool SetDynamicValue(UeNode element, int index, int value)
    {
        var valueLeaf = FindDynamicValueLeaf(element, index);
        if (valueLeaf is null)
        {
            return false;
        }

        valueLeaf.ReplacementValue = BitConverter.GetBytes(value); // same-width int32, in place
        valueLeaf.MarkDirty();
        return true;
    }

    private UeNode? FindDynamicValueLeaf(UeNode element, int index)
    {
        var dynamicData = element.Children.FirstOrDefault(c =>
            string.Equals(c.Name, DynamicDataProperty, StringComparison.Ordinal) &&
            c.Kind == UeNodeKind.StructArray);
        if (dynamicData is null)
        {
            return null;
        }

        foreach (var entry in dynamicData.Children)
        {
            var indexLeaf = entry.Children.FirstOrDefault(c => string.Equals(c.Name, "Index", StringComparison.Ordinal));
            var valueLeaf = entry.Children.FirstOrDefault(c => string.Equals(c.Name, "Value", StringComparison.Ordinal));
            if (indexLeaf is not null && valueLeaf is not null && ReadInt(indexLeaf) == index)
            {
                return valueLeaf;
            }
        }

        return null;
    }

    private int ReadInt(UeNode leaf) =>
        leaf.ReplacementValue is not null
            ? BitConverter.ToInt32(leaf.ReplacementValue, 0)
            : BitConverter.ToInt32(_blob.Data, leaf.ValueStart);

    private static byte[] EncodeFString(string s)
    {
        using var ms = new MemoryStream();
        if (string.IsNullOrEmpty(s))
        {
            ms.Write(BitConverter.GetBytes(0));
            return ms.ToArray();
        }

        var bytes = Encoding.Latin1.GetBytes(s);
        ms.Write(BitConverter.GetBytes(bytes.Length + 1));
        ms.Write(bytes, 0, bytes.Length);
        ms.WriteByte(0);
        return ms.ToArray();
    }
}
