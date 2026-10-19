using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;
using IUUT.Core.Prospects.World;

namespace IUUT.Core.Editing;

/// <summary>
/// Moves items trapped inside a prospect world back to the orbital stash (the "return trapped items"
/// feature) — but ONLY items the orbital stash can hold.
/// <para>
/// The orbital stash holds workshop items (<c>Item.Meta</c>), and nothing else: across the live
/// <c>MetaInventory.json</c> and every backup of it, the game has never written a single non-Meta
/// row (<c>docs/DATA-PROVENANCE.md</c> records the same: the stash rejects raw resources and building
/// pieces). A prospect is the opposite — on real saves more than 99% of what is "trapped" is Wood,
/// Fiber, Charcoal and the like. This service previously moved all of it, pushing tens of thousands
/// of rows the game does not accept into the stash file and deleting them from the world. Now a
/// stash-ineligible item is never touched: it stays exactly where it is in the world, and the result
/// says so, so the user can reach it another way (grave recovery, character rescue).
/// </para>
/// <para>
/// Eligible items move ONE STASH ENTRY PER WORLD SLOT, carrying that slot's own stack and durability.
/// They are no longer coalesced into invented stacks: the game itself writes no stash entry with a
/// stack above 1 on any available save, and a stack of 56 arrows built from six slots is a shape the
/// game never produced. A worn tool comes back worn, not silently repaired.
/// </para>
/// </summary>
/// <remarks>
/// Model-level only (no file I/O), so it is unit-testable; both the prospect blob and the stash model
/// are mutated in place, and the prospect is re-encoded only when something actually moved.
/// </remarks>
public sealed class ProspectReturnService
{
    private readonly StashEditService _stash;
    private readonly Func<string, bool> _canGoToStash;

    /// <summary>
    /// Creates the service. <paramref name="canGoToStash"/> decides which item rows the orbital stash
    /// accepts — in the app, membership of the <c>Item.Meta</c> items catalog. It is required on
    /// purpose: there is no way to construct this service that skips the check.
    /// </summary>
    public ProspectReturnService(StashEditService stash, Func<string, bool> canGoToStash)
    {
        ArgumentNullException.ThrowIfNull(stash);
        ArgumentNullException.ThrowIfNull(canGoToStash);
        _stash = stash;
        _canGoToStash = canGoToStash;
    }

    /// <summary>
    /// One kind of trapped item: its RowName, how many slots hold it, the total quantity, and whether
    /// the orbital stash can take it.
    /// </summary>
    public sealed record TrappedItem(string RowName, int SlotCount, int TotalQuantity, bool CanGoToStash = true);

    /// <summary>The outcome of a return: what moved, and what had to stay in the world.</summary>
    public sealed record ProspectReturnResult(
        IReadOnlyList<TrappedItem> Items,
        int SlotsRemoved,
        int TotalQuantity,
        int StashStacksAdded,
        IReadOnlyList<TrappedItem>? LeftInWorld = null)
    {
        /// <summary>Items left in the world because the stash cannot hold them.</summary>
        public IReadOnlyList<TrappedItem> LeftBehind => LeftInWorld ?? [];
    }

    /// <summary>
    /// Lists the items trapped in a prospect (read-only), grouped by RowName with summed quantities,
    /// each flagged with whether it can go to the orbital stash.
    /// </summary>
    public IReadOnlyList<TrappedItem> Preview(ProspectFileModel prospect)
    {
        ArgumentNullException.ThrowIfNull(prospect);
        var editor = OpenEditor(prospect);
        return Summarize(editor.FindItemSlots());
    }

    /// <summary>
    /// Returns trapped, stash-eligible items to <paramref name="stash"/>. <paramref name="rowNames"/>
    /// null = every eligible item, else only matching RowNames; <paramref name="scope"/> further
    /// restricts to slots whose owning actor matches (e.g. <see cref="SlotOwner.IsPlayerOwned"/>).
    /// Ineligible items are never removed from the world.
    /// </summary>
    public ProspectReturnResult Return(
        ProspectFileModel prospect,
        MetaInventoryModel stash,
        IReadOnlySet<string>? rowNames = null,
        Func<ProspectWorldEditor.SlotRef, bool>? scope = null)
    {
        ArgumentNullException.ThrowIfNull(prospect);
        ArgumentNullException.ThrowIfNull(stash);

        var editor = OpenEditor(prospect);
        var inScope = editor.FindItemSlots()
            .Where(s => (rowNames is null || rowNames.Contains(s.RowName)) && (scope is null || scope(s)))
            .ToList();

        var moving = inScope.Where(s => _canGoToStash(s.RowName)).ToList();
        var staying = inScope.Where(s => !_canGoToStash(s.RowName)).ToList();

        var entriesAdded = 0;
        foreach (var slot in moving)
        {
            // One entry per world slot. Should a slot ever exceed the stash's stack ceiling, split it
            // rather than clamp — clamping would silently destroy the difference.
            var remaining = Math.Max(1, slot.Stack);
            while (remaining > 0)
            {
                var take = Math.Min(remaining, StashEditService.MaxStack);
                var added = _stash.AddItem(stash, slot.RowName);
                _stash.SetStack(added, take);
                if (slot.Durability is { } durability)
                {
                    _stash.SetDurability(added, durability);
                }

                remaining -= take;
                entriesAdded++;
            }
        }

        if (moving.Count > 0)
        {
            foreach (var slot in moving)
            {
                editor.RemoveSlot(slot);
            }

            ProspectBlobCodec.SetUncompressed(prospect.ProspectBlob, editor.Serialize());
        }

        var moved = Summarize(moving);
        return new ProspectReturnResult(moved, moving.Count, moved.Sum(i => i.TotalQuantity), entriesAdded, Summarize(staying));
    }

    /// <summary>
    /// Returns only the player's recoverable items — carried inventory, deployed containers, and mount
    /// bags (<see cref="SlotOwner.IsPlayerOwned"/>) — leaving world machines (drills, geysers) untouched.
    /// </summary>
    public ProspectReturnResult ReturnPlayerOwned(ProspectFileModel prospect, MetaInventoryModel stash) =>
        Return(prospect, stash, scope: s => SlotOwner.IsPlayerOwned(s.OwnerComponentClass));

    private static ProspectWorldEditor OpenEditor(ProspectFileModel prospect)
    {
        var decompressed = ProspectBlobCodec.Decompress(prospect.ProspectBlob.BinaryBlob);
        return new ProspectWorldEditor(UeBlob.Parse(decompressed));
    }

    private List<TrappedItem> Summarize(IReadOnlyList<ProspectWorldEditor.SlotRef> slots) =>
        slots
            .GroupBy(s => s.RowName, StringComparer.Ordinal)
            .Select(g => new TrappedItem(g.Key, g.Count(), g.Sum(s => Math.Max(1, s.Stack)), _canGoToStash(g.Key)))
            .OrderByDescending(i => i.CanGoToStash)
            .ThenByDescending(i => i.TotalQuantity)
            .ToList();
}
