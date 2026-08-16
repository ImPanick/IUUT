namespace IUUT.Core.Prospects.World;

/// <summary>
/// Which of a character's inventories a record is — identified from the numeric <c>InventoryID</c>
/// the save stores, and verified against real saves by what each one actually contains.
/// </summary>
public enum CharacterInventoryKind
{
    /// <summary>The toolbelt / hotbar (id 2) — axe, pickaxe, bow, and the bare-fist entry.</summary>
    Hotbar,

    /// <summary>The main inventory grid (id 3) — the big bag the envirosuit expands.</summary>
    Bag,

    /// <summary>The consumable column (id 4) — oxygen, food, and water containers.</summary>
    Consumables,

    /// <summary>The equipment doll (id 5) — armour, envirosuit, cosmetics, and backpack.</summary>
    Equipment,

    /// <summary>The suit's module bay (id 11) — what the game labels AUXILIARY.</summary>
    Auxiliary,

    /// <summary>The light slot (id 12) — the lantern.</summary>
    Lantern,

    /// <summary>An inventory id this build does not recognise; shown, never hidden.</summary>
    Other,
}

/// <summary>One occupied slot: what is in it and where it sits in the grid.</summary>
public sealed record InventorySlotItem(int Location, string RowName, string ItemGuid)
{
    /// <summary>Whether this entry actually holds something.</summary>
    public bool HasItem => RowName.Length > 0 && !string.Equals(RowName, "None", StringComparison.Ordinal);
}

/// <summary>
/// One of a character's inventories, with its occupied slots addressed by grid position.
/// </summary>
public sealed record CharacterInventory(
    int InventoryId,
    CharacterInventoryKind Kind,
    IReadOnlyList<InventorySlotItem> Items)
{
    /// <summary>How many entries the save holds for this inventory.</summary>
    public int OccupiedCount => Items.Count(i => i.HasItem);

    /// <summary>
    /// The smallest grid this inventory can be drawn on without hiding anything. The save records
    /// only occupied slots, so this is a floor, not the true capacity — see
    /// <see cref="ProspectInventoryReader"/> for where real capacity comes from.
    /// </summary>
    public int MinimumCapacity => Items.Count == 0 ? 0 : Items.Max(i => i.Location) + 1;

    /// <summary>What the game calls this panel.</summary>
    public string Label => Kind switch
    {
        CharacterInventoryKind.Hotbar => "Toolbelt",
        CharacterInventoryKind.Bag => "Inventory",
        CharacterInventoryKind.Consumables => "Oxygen / Food / Water",
        CharacterInventoryKind.Equipment => "Character",
        CharacterInventoryKind.Auxiliary => "Auxiliary",
        CharacterInventoryKind.Lantern => "Light",
        _ => $"Inventory {InventoryId}",
    };

    /// <summary>
    /// The equipment doll's slot names, in the fixed order every real save uses. Null for any
    /// other inventory, or an index outside the doll.
    /// </summary>
    public string? SlotName(int location) =>
        Kind != CharacterInventoryKind.Equipment
            ? null
            : location switch
            {
                0 => "Head",
                1 => "Chest",
                2 => "Arms",
                3 => "Legs",
                4 => "Feet",
                5 => "Envirosuit",
                6 => "Skin",
                7 => "Cap",
                8 => "Backpack",
                _ => null,
            };
}

/// <summary>
/// READ-ONLY decode of a character's carried inventories, laid out the way the game shows them.
/// <para>
/// A character keeps six inventories under <c>SavedInventories</c>. Their ids are stable, and were
/// identified from real saves by their contents rather than assumed: 2 is the toolbelt (it holds
/// the axe, the bow, and the bare-fist entry), 3 the main grid, 4 the oxygen/food/water column,
/// 5 the equipment doll, 11 the suit's AUXILIARY module bay, and 12 the lantern.
/// </para>
/// <para>
/// SLOTS ARE STORED SPARSELY. Only occupied slots are written, each carrying its own
/// <c>Location</c> — a real character's consumable inventory holds exactly two entries, at
/// Location 0 and Location 2. So the save records what is carried and never how much fits, and
/// <see cref="CharacterInventory.MinimumCapacity"/> is a floor rather than the true size.
/// </para>
/// <para>
/// Real capacity is base + what your gear grants, and both live in the game's own data rather than
/// the save: <c>InventoryInfo.StartingSlots</c> gives the base (the backpack starts at 24), and an
/// equipped item grants more through its armour stats — <c>Envirosuit_Larkwell_Alpha</c> resolves to
/// <c>Undersuit_Larkwell_Alpha</c>, whose <c>ArmourStats</c> carry <c>BaseBackpackSlots_+ = 6</c> and
/// <c>BaseUpgradeSlots_+ = 4</c>, exactly the +6 inventory and +4 module slots the game displays.
/// Talents can add more still, which is why a UI should draw
/// <c>max(MinimumCapacity, base + granted)</c> — that way an unmodelled bonus can never hide an item.
/// </para>
/// </summary>
public sealed class ProspectInventoryReader
{
    private const string SavedInventories = "SavedInventories";

    /// <summary>Decodes every inventory belonging to <paramref name="character"/>.</summary>
    public IReadOnlyList<CharacterInventory> Read(byte[] decompressed, ProspectCharacter character)
    {
        ArgumentNullException.ThrowIfNull(decompressed);
        ArgumentNullException.ThrowIfNull(character);

        var inventories = new List<CharacterInventory>();
        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null || character.RecorderIndex < 0 || character.RecorderIndex >= recorders.Children.Count)
        {
            return inventories;
        }

        var actor = recorders.Children[character.RecorderIndex];
        var array = FindTyped(actor, SavedInventories, "ArrayProperty");
        if (array is null)
        {
            return inventories;
        }

        foreach (var entry in array.Children)
        {
            var id = ProspectCharacterReader.FindInt(entry, decompressed, "InventoryID") ?? -1;
            var slots = FindTyped(entry, "Slots", "ArrayProperty");
            var items = new List<InventorySlotItem>();

            if (slots is not null)
            {
                foreach (var slot in slots.Children)
                {
                    var row = ProspectCharacterReader.FindString(slot, decompressed, "ItemStaticData") ?? "";
                    items.Add(new InventorySlotItem(
                        ProspectCharacterReader.FindInt(slot, decompressed, "Location") ?? items.Count,
                        row,
                        ProspectCharacterReader.FindString(slot, decompressed, "ItemGuid") ?? ""));
                }
            }

            inventories.Add(new CharacterInventory(id, Classify(id), items));
        }

        return inventories;
    }

    /// <summary>Maps a stored inventory id to the panel the game draws it as.</summary>
    public static CharacterInventoryKind Classify(int inventoryId) => inventoryId switch
    {
        2 => CharacterInventoryKind.Hotbar,
        3 => CharacterInventoryKind.Bag,
        4 => CharacterInventoryKind.Consumables,
        5 => CharacterInventoryKind.Equipment,
        11 => CharacterInventoryKind.Auxiliary,
        12 => CharacterInventoryKind.Lantern,
        _ => CharacterInventoryKind.Other,
    };

    private static UeProperty? FindTyped(UeProperty node, string name, string type)
    {
        if (string.Equals(node.Name, name, StringComparison.Ordinal) &&
            string.Equals(node.Type, type, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            if (FindTyped(child, name, type) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
